using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using PadelMatch.Api.Auth;
using PadelMatch.Api.Matches;
using PadelMatch.Api.Players;
using PadelMatch.Api.Tests.TestSupport;
using PadelMatch.Domain.Clubs;
using PadelMatch.Domain.Matches;
using PadelMatch.Domain.Players;
using PadelMatch.Infrastructure.Persistence;
// MatchType also exists in System.IO (implicit usings); alias resolves the ambiguity.
using MatchType = PadelMatch.Domain.Matches.MatchType;

namespace PadelMatch.Api.Tests;

/// <summary>Leaving a seat (m7-leave-match): only before the start and while the match isn't full (4/4 paid);
/// the seat goes back to the feed and the organizer role passes on. Matches start in 30 minutes, so tests can move
/// the clock past the start without session tokens expiring.</summary>
public sealed class MatchLeaveTests : PlayerApiTestBase
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private static readonly LevelSurveyRequest InRange =
        new(YearsPlayingPadel.ThreeToFive, WeeklyFrequency.OnceAWeek, SelfPerceivedLevel.Intermediate);

    private static readonly LevelSurveyRequest Beginner =
        new(YearsPlayingPadel.LessThanOne, WeeklyFrequency.Rarely, SelfPerceivedLevel.Beginner);

    private TestPlayer Organizer { get; set; } = null!;

    [Fact]
    public async Task LeavingAnOpenMatchFreesTheSeatAndPutsItBackInTheFeed()
    {
        var match = await CreateMatchAsync(MatchType.Friendly);
        using var player = await JoinAsync(match.Id, survey: null);
        using var viewer = await NewPlayerAsync(survey: null);

        var response = await LeaveAsync(player, match.Id);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var detail = await GetDetailAsync(player, match.Id);
        Assert.Null(detail.MySeat);
        Assert.Equal(1, detail.ConfirmedSeats);
        Assert.Contains((await GetFeedAsync(viewer)).ForYou, m => m.Id == match.Id);
        // The freed seat is anyone's again.
        Assert.Equal(HttpStatusCode.OK, (await viewer.Client.PostAsync($"/api/matches/{match.Id}/hold", null)).StatusCode);
    }

    [Fact]
    public async Task AClosedMatchCannotBeLeftFromTheApp()
    {
        var match = await CreateMatchAsync(MatchType.Friendly);
        var others = new List<TestPlayer>();
        for (var i = 0; i < 3; i++)
        {
            others.Add(await JoinAsync(match.Id, survey: null));
        }

        var response = await LeaveAsync(others[0], match.Id);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        Assert.Equal("El partido está completo: ya no se puede abandonar.", problem.GetProperty("title").GetString());
        Assert.Equal(MatchStatus.Full, (await GetDetailAsync(others[0], match.Id)).Status);
        others.ForEach(p => p.Dispose());
    }

    [Fact]
    public async Task LeavingNeedsAConfirmedSeatAndAMatchNotYetStarted()
    {
        var match = await CreateMatchAsync(MatchType.Friendly);
        using var outsider = await NewPlayerAsync(survey: null);
        Assert.Equal(HttpStatusCode.Conflict, (await LeaveAsync(outsider, match.Id)).StatusCode);

        Factory.Clock.Advance(TimeSpan.FromMinutes(31));

        Assert.Equal(HttpStatusCode.Conflict, (await LeaveAsync(Organizer, match.Id)).StatusCode);
    }

    [Fact]
    public async Task WhenTheOrganizerLeavesTheRolePassesToTheLongestConfirmed()
    {
        var match = await CreateMatchAsync(MatchType.Friendly);
        using var earlier = await JoinAsync(match.Id, survey: null);
        Factory.Clock.Advance(TimeSpan.FromMinutes(1));
        using var later = await JoinAsync(match.Id, survey: null);

        Assert.Equal(HttpStatusCode.OK, (await LeaveAsync(Organizer, match.Id)).StatusCode);

        Assert.Equal(earlier.Id, (await GetDetailAsync(later, match.Id)).OrganizerId);
    }

    [Fact]
    public async Task APendingRequestIsApprovedWhenTheOnlyMissingVoterLeaves()
    {
        var match = await CreateMatchAsync(MatchType.Competitive);
        using var second = await JoinAsync(match.Id, InRange);
        using var requester = await NewPlayerAsync(Beginner);
        await requester.Client.PostAsync($"/api/matches/{match.Id}/exception-requests", null);
        await Organizer.Client.PostAsync($"/api/matches/{match.Id}/exception-requests/{requester.Id}/approve", null);

        await LeaveAsync(second, match.Id);

        Assert.Equal(AccessRequestStatus.Approved, (await GetDetailAsync(requester, match.Id)).MyAccess.RequestStatus);
    }

    // ---- helpers ----

    private async Task<MatchDetailResponse> CreateMatchAsync(MatchType type)
    {
        Organizer ??= await NewPlayerAsync(InRange, "Organizador");
        var slot = await SeedSlotAsync();
        var request = type == MatchType.Friendly
            ? new CreateMatchRequest(slot.Id, type, null, null, null, null)
            : new CreateMatchRequest(slot.Id, type, 2.5m, 3.5m, null, null);
        var response = await Organizer.Client.PostAsJsonAsync("/api/matches", request, JsonOptions);
        response.EnsureSuccessStatusCode();
        var match = (await response.Content.ReadFromJsonAsync<MatchDetailResponse>(JsonOptions))!;
        (await Organizer.Client.PostAsync($"/api/matches/{match.Id}/confirm", null)).EnsureSuccessStatusCode();
        return match;
    }

    private async Task<TestPlayer> JoinAsync(Guid matchId, LevelSurveyRequest? survey)
    {
        var player = await NewPlayerAsync(survey);
        (await player.Client.PostAsync($"/api/matches/{matchId}/hold", null)).EnsureSuccessStatusCode();
        (await player.Client.PostAsync($"/api/matches/{matchId}/confirm", null)).EnsureSuccessStatusCode();
        return player;
    }

    private async Task<TestPlayer> NewPlayerAsync(LevelSurveyRequest? survey, string displayName = "Jugador")
    {
        var client = Factory.CreateClient();
        var token = FakeExternalIdentityVerifier.CreateToken(
            AuthProvider.Google, $"sub-{Guid.NewGuid()}", $"{Guid.NewGuid()}@example.com", displayName);
        var auth = await (await client.PostAsJsonAsync("/api/auth/google", new GoogleAuthRequest(token), JsonOptions))
            .Content.ReadFromJsonAsync<AuthResponse>(JsonOptions);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.SessionToken);

        if (survey is not null)
        {
            await client.PutAsJsonAsync("/api/players/me", new UpdateProfileRequest(null, new DateOnly(1995, 3, 20), null), JsonOptions);
            (await client.PostAsJsonAsync("/api/players/me/level-survey", survey, JsonOptions)).EnsureSuccessStatusCode();
        }

        return new TestPlayer(auth.Player.Id, client);
    }

    private static Task<HttpResponseMessage> LeaveAsync(TestPlayer player, Guid matchId) =>
        player.Client.PostAsync($"/api/matches/{matchId}/leave", null);

    private static async Task<MatchDetailResponse> GetDetailAsync(TestPlayer player, Guid matchId) =>
        (await player.Client.GetFromJsonAsync<MatchDetailResponse>($"/api/matches/{matchId}", JsonOptions))!;

    private static async Task<MatchFeedResponse> GetFeedAsync(TestPlayer player) =>
        (await player.Client.GetFromJsonAsync<MatchFeedResponse>("/api/matches/feed", JsonOptions))!;

    private async Task<CourtSlot> SeedSlotAsync()
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PadelMatchDbContext>();
        var club = Club.Create("Club de pruebas", "Calle 1", null, null, null, Factory.Clock.GetUtcNow());
        var court = Court.Create(club.Id, "Pista 1");
        var startsAt = Factory.Clock.GetUtcNow().AddMinutes(30);
        var slot = CourtSlot.Create(court.Id, new DateTimeOffset(
            startsAt.Year, startsAt.Month, startsAt.Day, startsAt.Hour, startsAt.Minute, startsAt.Second, startsAt.Offset),
            SlotDuration.SixtyMinutes);
        dbContext.Clubs.Add(club);
        dbContext.Courts.Add(court);
        dbContext.CourtSlots.Add(slot);
        await dbContext.SaveChangesAsync();
        return slot;
    }

    private sealed record TestPlayer(Guid Id, HttpClient Client) : IDisposable
    {
        public void Dispose() => Client.Dispose();
    }
}
