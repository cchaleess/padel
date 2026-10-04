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

/// <summary>Quality rules (m6-quality-rules): direct access, exception requests and unanimous approval. Levels
/// come from the real survey: <see cref="InRange"/> estimates 2.8, <see cref="Beginner"/> 1.0; matches here are
/// 2.5–3.5.</summary>
public sealed class MatchAccessTests : PlayerApiTestBase
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private static readonly LevelSurveyRequest InRange =
        new(YearsPlayingPadel.ThreeToFive, WeeklyFrequency.OnceAWeek, SelfPerceivedLevel.Intermediate);

    private static readonly LevelSurveyRequest Beginner =
        new(YearsPlayingPadel.LessThanOne, WeeklyFrequency.Rarely, SelfPerceivedLevel.Beginner);

    [Fact]
    public async Task AFriendlyMatchHasNoCriteria()
    {
        var match = await CreateMatchAsync(MatchType.Friendly);
        using var player = await NewPlayerAsync(survey: null);

        Assert.Equal(HttpStatusCode.OK, (await player.Client.PostAsync($"/api/matches/{match.Id}/hold", null)).StatusCode);
    }

    [Fact]
    public async Task APlayerWithinTheCriteriaJoinsDirectly()
    {
        var match = await CreateMatchAsync(MatchType.Competitive);
        using var player = await NewPlayerAsync(InRange);

        var detail = await GetDetailAsync(player, match.Id);
        Assert.True(detail.MyAccess.CanJoinDirectly);
        Assert.Equal(HttpStatusCode.OK, (await player.Client.PostAsync($"/api/matches/{match.Id}/hold", null)).StatusCode);
    }

    [Theory]
    [InlineData("beginner", 0, "LevelBelowRange")]
    [InlineData("none", 0, "NoLevel")]
    [InlineData("inRange", 1, "NotEnoughMatches")]
    public async Task APlayerOutsideTheCriteriaCannotHoldAndIsToldWhy(string level, int minMatches, string shortfall)
    {
        var match = await CreateMatchAsync(MatchType.Competitive, minMatches);
        using var player = await NewPlayerAsync(level switch { "beginner" => Beginner, "inRange" => InRange, _ => null });

        var hold = await player.Client.PostAsync($"/api/matches/{match.Id}/hold", null);

        Assert.Equal(HttpStatusCode.Forbidden, hold.StatusCode);
        var problem = await hold.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        Assert.Contains(shortfall, problem.GetProperty("shortfalls").EnumerateArray().Select(s => s.GetString()));
        var detail = await GetDetailAsync(player, match.Id);
        Assert.False(detail.MyAccess.CanJoinDirectly);
        Assert.Contains(Enum.Parse<AccessShortfall>(shortfall), detail.MyAccess.Shortfalls);
    }

    [Fact]
    public async Task APlayerCanRequestAccessOnlyOnce()
    {
        var match = await CreateMatchAsync(MatchType.Competitive);
        using var player = await NewPlayerAsync(Beginner);

        Assert.Equal(HttpStatusCode.Created, (await RequestAsync(player, match.Id)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await RequestAsync(player, match.Id)).StatusCode);
        Assert.Equal(AccessRequestStatus.Pending, (await GetDetailAsync(player, match.Id)).MyAccess.RequestStatus);
    }

    [Fact]
    public async Task APlayerWhoCanJoinDirectlyCannotRequest()
    {
        var match = await CreateMatchAsync(MatchType.Competitive);
        using var player = await NewPlayerAsync(InRange);

        Assert.Equal(HttpStatusCode.Conflict, (await RequestAsync(player, match.Id)).StatusCode);
    }

    [Fact]
    public async Task OnlyConfirmedPlayersVote()
    {
        var match = await CreateMatchAsync(MatchType.Competitive);
        using var requester = await NewPlayerAsync(Beginner);
        using var outsider = await NewPlayerAsync(InRange);
        await RequestAsync(requester, match.Id);

        Assert.Equal(HttpStatusCode.Forbidden, (await VoteAsync(requester, match.Id, requester.Id, approve: true)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await VoteAsync(outsider, match.Id, requester.Id, approve: true)).StatusCode);
    }

    [Fact]
    public async Task ASingleRejectionRejectsAndKeepsThePlayerOut()
    {
        var match = await CreateMatchAsync(MatchType.Competitive);
        using var second = await JoinAsync(match.Id, InRange);
        using var requester = await NewPlayerAsync(Beginner);
        await RequestAsync(requester, match.Id);

        await VoteAsync(Organizer, match.Id, requester.Id, approve: true);
        var reject = await VoteAsync(second, match.Id, requester.Id, approve: false);

        Assert.Equal(AccessRequestStatus.Rejected, (await reject.Content.ReadFromJsonAsync<AccessVoteResponse>(JsonOptions))!.Status);
        Assert.Equal(HttpStatusCode.Forbidden, (await requester.Client.PostAsync($"/api/matches/{match.Id}/hold", null)).StatusCode);
        // Final: no second request, no more votes.
        Assert.Equal(HttpStatusCode.Conflict, (await RequestAsync(requester, match.Id)).StatusCode);
    }

    [Fact]
    public async Task UnanimousApprovalLetsThePlayerHoldAndPay()
    {
        var match = await CreateMatchAsync(MatchType.Competitive);
        using var second = await JoinAsync(match.Id, InRange);
        using var requester = await NewPlayerAsync(Beginner);
        await RequestAsync(requester, match.Id);

        var first = await VoteAsync(Organizer, match.Id, requester.Id, approve: true);
        Assert.Equal(AccessRequestStatus.Pending, (await first.Content.ReadFromJsonAsync<AccessVoteResponse>(JsonOptions))!.Status);
        // Repeating a vote is a no-op; changing it isn't allowed.
        Assert.Equal(HttpStatusCode.OK, (await VoteAsync(Organizer, match.Id, requester.Id, approve: true)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await VoteAsync(Organizer, match.Id, requester.Id, approve: false)).StatusCode);

        var last = await VoteAsync(second, match.Id, requester.Id, approve: true);
        Assert.Equal(AccessRequestStatus.Approved, (await last.Content.ReadFromJsonAsync<AccessVoteResponse>(JsonOptions))!.Status);

        Assert.Equal(HttpStatusCode.OK, (await requester.Client.PostAsync($"/api/matches/{match.Id}/hold", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await requester.Client.PostAsync($"/api/matches/{match.Id}/confirm", null)).StatusCode);
    }

    [Fact]
    public async Task APlayerWhoConfirmsWhileARequestIsPendingMustAlsoVote()
    {
        var match = await CreateMatchAsync(MatchType.Competitive);
        using var requester = await NewPlayerAsync(Beginner);
        await RequestAsync(requester, match.Id);
        using var latecomer = await JoinAsync(match.Id, InRange);

        // Only the organizer was confirmed when it was requested; now there are two voters.
        var organizerVote = await VoteAsync(Organizer, match.Id, requester.Id, approve: true);
        Assert.Equal(AccessRequestStatus.Pending, (await organizerVote.Content.ReadFromJsonAsync<AccessVoteResponse>(JsonOptions))!.Status);

        var latecomerVote = await VoteAsync(latecomer, match.Id, requester.Id, approve: true);
        Assert.Equal(AccessRequestStatus.Approved, (await latecomerVote.Content.ReadFromJsonAsync<AccessVoteResponse>(JsonOptions))!.Status);
    }

    [Fact]
    public async Task TheLastTwoApprovalsArrivingTogetherApproveTheRequest()
    {
        var match = await CreateMatchAsync(MatchType.Competitive);
        using var second = await JoinAsync(match.Id, InRange);
        using var requester = await NewPlayerAsync(Beginner);
        await RequestAsync(requester, match.Id);

        var votes = await Task.WhenAll(
            VoteAsync(Organizer, match.Id, requester.Id, approve: true),
            VoteAsync(second, match.Id, requester.Id, approve: true));

        Assert.All(votes, v => Assert.Equal(HttpStatusCode.OK, v.StatusCode));
        var statuses = await Task.WhenAll(votes.Select(v => v.Content.ReadFromJsonAsync<AccessVoteResponse>(JsonOptions)));
        // The row lock serializes them: the first sees one approval (Pending), the second sees both (Approved).
        Assert.Contains(statuses, s => s!.Status == AccessRequestStatus.Approved);
        Assert.Equal(AccessRequestStatus.Approved, (await GetDetailAsync(requester, match.Id)).MyAccess.RequestStatus);
    }

    [Fact]
    public async Task PendingRequestsAndActivityAreShownToConfirmedPlayersOnly()
    {
        var match = await CreateMatchAsync(MatchType.Competitive);
        using var requester = await NewPlayerAsync(Beginner, "Rita");
        using var outsider = await NewPlayerAsync(InRange);
        await RequestAsync(requester, match.Id);

        var organizerView = await GetDetailAsync(Organizer, match.Id);
        var pending = Assert.Single(organizerView.PendingRequests);
        Assert.Equal("Rita", pending.Requester.DisplayName);
        Assert.Contains(AccessShortfall.LevelBelowRange, pending.Shortfalls);
        Assert.Equal(0, pending.Approvals);
        Assert.Equal(1, pending.VotersNeeded);
        Assert.Null(pending.MyVote);
        Assert.Empty((await GetDetailAsync(outsider, match.Id)).PendingRequests);

        var toVote = Assert.Single((await GetActivityAsync(Organizer)).ToVote);
        Assert.Equal(match.Id, toVote.Match.MatchId);
        Assert.Equal(requester.Id, toVote.Requester.PlayerId);
        var mine = Assert.Single((await GetActivityAsync(requester)).MyRequests);
        Assert.Equal(AccessRequestStatus.Pending, mine.Status);

        // Once voted, it leaves the voter's to-do list.
        await VoteAsync(Organizer, match.Id, requester.Id, approve: true);
        Assert.Empty((await GetActivityAsync(Organizer)).ToVote);
        Assert.Equal(AccessRequestStatus.Approved, Assert.Single((await GetActivityAsync(requester)).MyRequests).Status);
    }

    [Fact]
    public async Task TheFeedPutsInForYouOnlyWhatThePlayerCanJoinDirectly()
    {
        var inRange = await CreateMatchAsync(MatchType.Competitive);
        var needsMatches = await CreateMatchAsync(MatchType.Competitive, minMatchesRequired: 1, slotOffset: TimeSpan.FromHours(2));
        using var player = await NewPlayerAsync(InRange);

        var feed = await GetFeedAsync(player);
        Assert.Contains(feed.ForYou, m => m.Id == inRange.Id);
        Assert.Contains(feed.OutOfRange, m => m.Id == needsMatches.Id);

        // An approved request makes the match joinable, so it moves to "for you".
        await RequestAsync(player, needsMatches.Id);
        await VoteAsync(Organizer, needsMatches.Id, player.Id, approve: true);
        Assert.Contains((await GetFeedAsync(player)).ForYou, m => m.Id == needsMatches.Id);
    }

    // ---- helpers ----

    private TestPlayer Organizer { get; set; } = null!;

    /// <summary>A match 2.5–3.5 created and paid by <see cref="Organizer"/> (in range itself), so it has one
    /// confirmed player: the organizer.</summary>
    private async Task<MatchDetailResponse> CreateMatchAsync(
        MatchType type, int? minMatchesRequired = null, TimeSpan? slotOffset = null)
    {
        Organizer ??= await NewPlayerAsync(InRange, "Organizador");
        var slot = await SeedSlotAsync(slotOffset ?? TimeSpan.Zero);
        var request = type == MatchType.Friendly
            ? new CreateMatchRequest(slot.Id, type, null, null, null, null)
            : new CreateMatchRequest(slot.Id, type, 2.5m, 3.5m, minMatchesRequired, null);
        var response = await Organizer.Client.PostAsJsonAsync("/api/matches", request, JsonOptions);
        response.EnsureSuccessStatusCode();
        var match = (await response.Content.ReadFromJsonAsync<MatchDetailResponse>(JsonOptions))!;
        (await Organizer.Client.PostAsync($"/api/matches/{match.Id}/confirm", null)).EnsureSuccessStatusCode();
        return match;
    }

    private async Task<TestPlayer> JoinAsync(Guid matchId, LevelSurveyRequest survey)
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

    private static Task<HttpResponseMessage> RequestAsync(TestPlayer player, Guid matchId) =>
        player.Client.PostAsync($"/api/matches/{matchId}/exception-requests", null);

    private static Task<HttpResponseMessage> VoteAsync(TestPlayer voter, Guid matchId, Guid requesterId, bool approve) =>
        voter.Client.PostAsync($"/api/matches/{matchId}/exception-requests/{requesterId}/{(approve ? "approve" : "reject")}", null);

    private static async Task<MatchDetailResponse> GetDetailAsync(TestPlayer player, Guid matchId) =>
        (await player.Client.GetFromJsonAsync<MatchDetailResponse>($"/api/matches/{matchId}", JsonOptions))!;

    private static async Task<ActivityResponse> GetActivityAsync(TestPlayer player) =>
        (await player.Client.GetFromJsonAsync<ActivityResponse>("/api/activity", JsonOptions))!;

    private static async Task<MatchFeedResponse> GetFeedAsync(TestPlayer player) =>
        (await player.Client.GetFromJsonAsync<MatchFeedResponse>("/api/matches/feed", JsonOptions))!;

    private async Task<CourtSlot> SeedSlotAsync(TimeSpan offset)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PadelMatchDbContext>();
        var club = Club.Create("Club de pruebas", "Calle 1", null, null, null, Factory.Clock.GetUtcNow());
        var court = Court.Create(club.Id, "Pista 1");
        var startsAt = Factory.Clock.GetUtcNow().AddDays(1) + offset;
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
