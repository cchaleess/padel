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

public sealed class MatchFeedTests : PlayerApiTestBase
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private static readonly LevelSurveyRequest SurveyForRange2_5To3_5 =
        new(YearsPlayingPadel.ThreeToFive, WeeklyFrequency.OnceAWeek, SelfPerceivedLevel.Intermediate);

    [Fact]
    public async Task FeedIsEmptyWhenThereAreNoMatches()
    {
        await AuthenticateAsync();

        var feed = await Client.GetFromJsonAsync<MatchFeedResponse>("/api/matches/feed", JsonOptions);

        Assert.NotNull(feed);
        Assert.Empty(feed.ForYou);
        Assert.Empty(feed.OutOfRange);
    }

    [Fact]
    public async Task FriendlyMatchAppearsInForYou()
    {
        var slot = await SeedSlotAsync();
        await AuthenticateAsync();
        var created = await CreateMatchAsync(slot.Id, MatchType.Friendly, null, null);

        await AuthenticateAsync();
        var feed = await Client.GetFromJsonAsync<MatchFeedResponse>("/api/matches/feed", JsonOptions);

        Assert.NotNull(feed);
        Assert.Contains(feed.ForYou, m => m.Id == created.Id);
        Assert.DoesNotContain(feed.OutOfRange, m => m.Id == created.Id);
    }

    [Fact]
    public async Task CompetitiveMatchCompatibleWithViewerLevelAppearsInForYou()
    {
        var slot = await SeedSlotAsync();
        await AuthenticateAsync();
        await SetLevelAsync();
        var created = await CreateMatchAsync(slot.Id, MatchType.Competitive, 2.5m, 3.5m);

        await AuthenticateAsync();
        await SetLevelAsync();
        var feed = await Client.GetFromJsonAsync<MatchFeedResponse>("/api/matches/feed", JsonOptions);

        Assert.NotNull(feed);
        Assert.Contains(feed.ForYou, m => m.Id == created.Id);
        Assert.DoesNotContain(feed.OutOfRange, m => m.Id == created.Id);
    }

    [Fact]
    public async Task CompetitiveMatchIsOutOfRangeForAViewerWithoutLevel()
    {
        var slot = await SeedSlotAsync();
        await AuthenticateAsync();
        await SetLevelAsync();
        var created = await CreateMatchAsync(slot.Id, MatchType.Competitive, 2.5m, 3.5m);

        await AuthenticateAsync();
        var feed = await Client.GetFromJsonAsync<MatchFeedResponse>("/api/matches/feed", JsonOptions);

        Assert.NotNull(feed);
        Assert.Contains(feed.OutOfRange, m => m.Id == created.Id);
        Assert.DoesNotContain(feed.ForYou, m => m.Id == created.Id);
    }

    [Fact]
    public async Task ExpiredMatchDoesNotAppearInEitherGroup()
    {
        var pastSlot = await SeedSlotAsync(DateTimeOffset.UtcNow.AddDays(-1));
        await AuthenticateAsync();
        var created = await CreateMatchAsync(pastSlot.Id, MatchType.Friendly, null, null);

        await AuthenticateAsync();
        var feed = await Client.GetFromJsonAsync<MatchFeedResponse>("/api/matches/feed", JsonOptions);

        Assert.NotNull(feed);
        Assert.DoesNotContain(feed.ForYou, m => m.Id == created.Id);
        Assert.DoesNotContain(feed.OutOfRange, m => m.Id == created.Id);
    }

    [Fact]
    public async Task FeedOrdersByProximityWhenCoordinatesAreProvided()
    {
        var nearSlot = await SeedSlotAsync(club: await SeedClubAsync(latitude: 40.4168, longitude: -3.7038));
        var farSlot = await SeedSlotAsync(club: await SeedClubAsync(latitude: 41.3874, longitude: 2.1686));

        await AuthenticateAsync();
        var far = await CreateMatchAsync(farSlot.Id, MatchType.Friendly, null, null);
        var near = await CreateMatchAsync(nearSlot.Id, MatchType.Friendly, null, null);

        await AuthenticateAsync();
        var feed = await Client.GetFromJsonAsync<MatchFeedResponse>(
            "/api/matches/feed?lat=40.4168&lng=-3.7038", JsonOptions);

        Assert.NotNull(feed);
        var ids = feed.ForYou.Select(m => m.Id).ToList();
        Assert.True(ids.IndexOf(near.Id) < ids.IndexOf(far.Id));
    }

    [Fact]
    public async Task FeedOrdersByCityOrZoneWhenNoCoordinatesAreProvided()
    {
        var madridSlot = await SeedSlotAsync(club: await SeedClubAsync(cityOrZone: "Madrid"));
        var barcelonaSlot = await SeedSlotAsync(club: await SeedClubAsync(cityOrZone: "Barcelona"));

        await AuthenticateAsync();
        var barcelona = await CreateMatchAsync(barcelonaSlot.Id, MatchType.Friendly, null, null);
        var madrid = await CreateMatchAsync(madridSlot.Id, MatchType.Friendly, null, null);

        await AuthenticateAsync();
        var feed = await Client.GetFromJsonAsync<MatchFeedResponse>(
            "/api/matches/feed?cityOrZone=Madrid", JsonOptions);

        Assert.NotNull(feed);
        var ids = feed.ForYou.Select(m => m.Id).ToList();
        Assert.True(ids.IndexOf(madrid.Id) < ids.IndexOf(barcelona.Id));
    }

    [Fact]
    public async Task FeedRequiresAuthentication()
    {
        var response = await Client.GetAsync("/api/matches/feed");
        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
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

    private async Task SetLevelAsync()
    {
        await Client.PutAsJsonAsync("/api/players/me", new UpdateProfileRequest(null, new DateOnly(1995, 3, 20), null), JsonOptions);
        var response = await Client.PostAsJsonAsync("/api/players/me/level-survey", SurveyForRange2_5To3_5, JsonOptions);
        var profile = await response.Content.ReadFromJsonAsync<PlayerProfileResponse>(JsonOptions);
        Assert.NotNull(profile?.Level);
    }

    private async Task<MatchDetailResponse> CreateMatchAsync(Guid courtSlotId, MatchType type, decimal? minLevel, decimal? maxLevel)
    {
        var response = await Client.PostAsJsonAsync(
            "/api/matches", new CreateMatchRequest(courtSlotId, type, minLevel, maxLevel, null, null), JsonOptions);
        response.EnsureSuccessStatusCode();
        var match = await response.Content.ReadFromJsonAsync<MatchDetailResponse>(JsonOptions);
        Assert.NotNull(match);
        // The feed only lists matches with a Confirmed seat (m5-mobile-confirmation): the organizer pays theirs.
        (await Client.PostAsync($"/api/matches/{match.Id}/confirm", null)).EnsureSuccessStatusCode();
        return match;
    }

    private async Task<Club> SeedClubAsync(double? latitude = null, double? longitude = null, string? cityOrZone = null)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PadelMatchDbContext>();
        var club = Club.Create("Club de pruebas", "Calle 1", cityOrZone, latitude, longitude, DateTimeOffset.UtcNow);
        dbContext.Clubs.Add(club);
        await dbContext.SaveChangesAsync();
        return club;
    }

    private async Task<CourtSlot> SeedSlotAsync(DateTimeOffset? startsAt = null, Club? club = null)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PadelMatchDbContext>();
        var effectiveClub = club ?? Club.Create("Club de pruebas", "Calle 1", null, null, null, DateTimeOffset.UtcNow);
        if (club is null)
        {
            dbContext.Clubs.Add(effectiveClub);
        }
        var court = Court.Create(effectiveClub.Id, "Pista 1");
        // Rounded to the second: PostgreSQL's timestamptz only keeps microsecond precision, and comparing the
        // round-tripped value against the in-memory tick-precision DateTimeOffset would otherwise fail spuriously.
        var effectiveStartsAt = Round(startsAt ?? DateTimeOffset.UtcNow.AddDays(1));
        var slot = CourtSlot.Create(court.Id, effectiveStartsAt, SlotDuration.SixtyMinutes);
        dbContext.Courts.Add(court);
        dbContext.CourtSlots.Add(slot);
        await dbContext.SaveChangesAsync();
        return slot;
    }

    private static DateTimeOffset Round(DateTimeOffset value) =>
        new(value.Year, value.Month, value.Day, value.Hour, value.Minute, value.Second, value.Offset);
}
