using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PadelMatch.Api.Auth;
using PadelMatch.Api.Matches;
using PadelMatch.Api.Players;
using PadelMatch.Api.Tests.TestSupport;
using PadelMatch.Domain.Clubs;
using PadelMatch.Domain.Players;
using PadelMatch.Infrastructure.Persistence;
// MatchType also exists in System.IO (implicit usings); alias resolves the ambiguity.
using MatchType = PadelMatch.Domain.Matches.MatchType;

namespace PadelMatch.Api.Tests;

public sealed class MatchTests : PlayerApiTestBase
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    [Fact]
    public async Task CreatingAFriendlyMatchDoesNotRequireOrganizerLevel()
    {
        var slot = await SeedAvailableSlotAsync();
        await AuthenticateAsync();

        var response = await Client.PostAsJsonAsync(
            "/api/matches", new CreateMatchRequest(slot.Id, MatchType.Friendly, null, null, null, "Partido relajado"), JsonOptions);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var match = await response.Content.ReadFromJsonAsync<MatchDetailResponse>(JsonOptions);
        Assert.NotNull(match);
        Assert.Equal(MatchType.Friendly, match.Type);
        Assert.Null(match.OrganizerLevelAtCreation);
        Assert.Null(match.MinLevel);
        Assert.Null(match.MinMatchesRequired);
    }

    [Fact]
    public async Task CreatingACompetitiveMatchWithoutOrganizerLevelFails()
    {
        var slot = await SeedAvailableSlotAsync();
        await AuthenticateAsync();

        var response = await Client.PostAsJsonAsync(
            "/api/matches", new CreateMatchRequest(slot.Id, MatchType.Competitive, 2.0m, 4.0m, null, null), JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreatingACompetitiveMatchSnapshotsTheOrganizerLevel()
    {
        var slot = await SeedAvailableSlotAsync();
        await AuthenticateAsync();
        var organizerLevel = await SetOrganizerLevelAsync();

        var response = await Client.PostAsJsonAsync(
            "/api/matches", new CreateMatchRequest(slot.Id, MatchType.Competitive, 2.5m, 3.5m, 3, "Buscamos nivel"), JsonOptions);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var match = await response.Content.ReadFromJsonAsync<MatchDetailResponse>(JsonOptions);
        Assert.NotNull(match);
        Assert.Equal(organizerLevel, match.OrganizerLevelAtCreation);
        Assert.Equal(2.5m, match.MinLevel);
        Assert.Equal(3.5m, match.MaxLevel);
        Assert.Equal(3, match.MinMatchesRequired);
    }

    [Fact]
    public async Task CreatingACompetitiveMatchRejectsAnInvalidRange()
    {
        var slot = await SeedAvailableSlotAsync();
        await AuthenticateAsync();
        await SetOrganizerLevelAsync();

        var response = await Client.PostAsJsonAsync(
            "/api/matches", new CreateMatchRequest(slot.Id, MatchType.Competitive, 3.5m, 2.5m, null, null), JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreatingAMatchBooksTheCourtSlotAndRejectsASecondAttempt()
    {
        var slot = await SeedAvailableSlotAsync();
        await AuthenticateAsync();

        var first = await Client.PostAsJsonAsync(
            "/api/matches", new CreateMatchRequest(slot.Id, MatchType.Friendly, null, null, null, null), JsonOptions);
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        var second = await Client.PostAsJsonAsync(
            "/api/matches", new CreateMatchRequest(slot.Id, MatchType.Friendly, null, null, null, null), JsonOptions);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);

        await using var scope = Factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PadelMatchDbContext>();
        var persistedSlot = await dbContext.CourtSlots.FirstAsync(s => s.Id == slot.Id);
        Assert.Equal(SlotStatus.Booked, persistedSlot.Status);
    }

    [Fact]
    public async Task ConcurrentCreationOnTheSameCourtSlotOnlyAllowsOneToSucceed()
    {
        var slot = await SeedAvailableSlotAsync();
        await AuthenticateAsync();

        var responses = await Task.WhenAll(Enumerable.Range(0, 5).Select(_ =>
            Client.PostAsJsonAsync(
                "/api/matches", new CreateMatchRequest(slot.Id, MatchType.Friendly, null, null, null, null), JsonOptions)));

        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.Created);
        Assert.Equal(4, responses.Count(r => r.StatusCode == HttpStatusCode.Conflict));
    }

    [Fact]
    public async Task GetMatchDetailsReturnsClubCourtAndScheduleAndFailsForAnUnknownMatch()
    {
        var slot = await SeedAvailableSlotAsync();
        await AuthenticateAsync();
        var created = await Client.PostAsJsonAsync(
            "/api/matches", new CreateMatchRequest(slot.Id, MatchType.Friendly, null, null, null, "Nota"), JsonOptions);
        var match = await created.Content.ReadFromJsonAsync<MatchDetailResponse>(JsonOptions);
        Assert.NotNull(match);

        var detail = await Client.GetFromJsonAsync<MatchDetailResponse>($"/api/matches/{match.Id}", JsonOptions);
        Assert.NotNull(detail);
        Assert.Equal(slot.CourtId, detail.CourtId);
        Assert.Equal(slot.StartsAt, detail.StartsAt);
        Assert.Equal(slot.EndsAt, detail.EndsAt);
        Assert.Equal("Nota", detail.Note);

        var notFound = await Client.GetAsync($"/api/matches/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, notFound.StatusCode);
    }

    [Fact]
    public async Task MatchEndpointsRequireAuthentication()
    {
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await Client.PostAsJsonAsync(
                "/api/matches", new CreateMatchRequest(Guid.NewGuid(), MatchType.Friendly, null, null, null, null), JsonOptions)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Client.GetAsync($"/api/matches/{Guid.NewGuid()}")).StatusCode);
    }

    private async Task AuthenticateAsync()
    {
        var token = FakeExternalIdentityVerifier.CreateToken(
            AuthProvider.Google, $"sub-{Guid.NewGuid()}", $"{Guid.NewGuid()}@example.com", "Jugador");
        var response = await Client.PostAsJsonAsync("/api/auth/google", new GoogleAuthRequest(token), JsonOptions);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<AuthResponse>(JsonOptions);
        Assert.NotNull(result);
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", result.SessionToken);
    }

    private async Task<decimal> SetOrganizerLevelAsync()
    {
        await Client.PutAsJsonAsync("/api/players/me", new UpdateProfileRequest(null, new DateOnly(1995, 3, 20), null), JsonOptions);
        var survey = new LevelSurveyRequest(YearsPlayingPadel.ThreeToFive, WeeklyFrequency.OnceAWeek, SelfPerceivedLevel.Intermediate);
        var response = await Client.PostAsJsonAsync("/api/players/me/level-survey", survey, JsonOptions);
        var profile = await response.Content.ReadFromJsonAsync<PlayerProfileResponse>(JsonOptions);
        Assert.NotNull(profile?.Level);
        return profile.Level!.Value;
    }

    private async Task<CourtSlot> SeedAvailableSlotAsync()
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PadelMatchDbContext>();
        var club = Club.Create("Club de pruebas", "Calle 1", "Madrid", null, null, DateTimeOffset.UtcNow);
        var court = Court.Create(club.Id, "Pista 1");
        // Rounded to the second: PostgreSQL's timestamptz only keeps microsecond precision, and comparing the
        // round-tripped value against the in-memory tick-precision DateTimeOffset would otherwise fail spuriously.
        var startsAt = new DateTimeOffset(DateTime.UtcNow.AddDays(1).Date.AddHours(10), TimeSpan.Zero);
        var slot = CourtSlot.Create(court.Id, startsAt, SlotDuration.SixtyMinutes);
        dbContext.Clubs.Add(club);
        dbContext.Courts.Add(court);
        dbContext.CourtSlots.Add(slot);
        await dbContext.SaveChangesAsync();
        return slot;
    }
}
