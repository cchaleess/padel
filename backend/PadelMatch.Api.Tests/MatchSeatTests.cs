using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PadelMatch.Api.Auth;
using PadelMatch.Api.Matches;
using PadelMatch.Api.Tests.TestSupport;
using PadelMatch.Domain.Clubs;
using PadelMatch.Domain.Matches;
using PadelMatch.Domain.Players;
using PadelMatch.Infrastructure.Persistence;
// MatchType also exists in System.IO (implicit usings); alias resolves the ambiguity.
using MatchType = PadelMatch.Domain.Matches.MatchType;

namespace PadelMatch.Api.Tests;

public sealed class MatchSeatTests : PlayerApiTestBase
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    [Fact]
    public async Task CreatingAMatchHoldsTheOrganizersSeatAndLeavesThreeAvailable()
    {
        var match = await CreateFriendlyMatchAsync();

        await using var scope = Factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PadelMatchDbContext>();
        var seats = await dbContext.MatchSeats.Where(s => s.MatchId == match.Id).ToListAsync();

        Assert.Equal(4, seats.Count);
        var held = Assert.Single(seats, s => s.Status == SeatStatus.Held);
        Assert.Equal(match.OrganizerId, held.HolderId);
        Assert.Equal(0, held.Position);
        Assert.Equal([0, 1, 2, 3], seats.Select(s => s.Position).Order());
        Assert.Equal(3, seats.Count(s => s.Status == SeatStatus.Available));
    }

    [Fact]
    public async Task HoldingAnAvailableSeatMarksItHeldForFiveMinutes()
    {
        var match = await CreateFriendlyMatchAsync();
        await AuthenticateAsync(Client);
        var now = Factory.Clock.GetUtcNow();

        var response = await Client.PostAsync($"/api/matches/{match.Id}/hold", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var hold = await response.Content.ReadFromJsonAsync<SeatHoldResponse>(JsonOptions);
        Assert.NotNull(hold);
        Assert.Equal(now.AddMinutes(5), hold.HeldUntilUtc);
    }

    [Fact]
    public async Task ConfirmingAHeldSeatMarksItConfirmed()
    {
        var match = await CreateFriendlyMatchAsync();
        await AuthenticateAsync(Client);
        await Client.PostAsync($"/api/matches/{match.Id}/hold", null);

        var response = await Client.PostAsync($"/api/matches/{match.Id}/confirm", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ReleasingAHeldSeatMakesItAvailableAgain()
    {
        var match = await CreateFriendlyMatchAsync();
        await AuthenticateAsync(Client);
        await Client.PostAsync($"/api/matches/{match.Id}/hold", null);

        var release = await Client.PostAsync($"/api/matches/{match.Id}/release", null);
        Assert.Equal(HttpStatusCode.OK, release.StatusCode);

        // Released, so a second hold by the same player must succeed again (it would conflict if still held).
        var secondHold = await Client.PostAsync($"/api/matches/{match.Id}/hold", null);
        Assert.Equal(HttpStatusCode.OK, secondHold.StatusCode);
    }

    [Fact]
    public async Task HoldingASecondSeatInTheSameMatchFails()
    {
        var match = await CreateFriendlyMatchAsync();
        await AuthenticateAsync(Client);
        await Client.PostAsync($"/api/matches/{match.Id}/hold", null);

        var second = await Client.PostAsync($"/api/matches/{match.Id}/hold", null);

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task ConfirmingOrReleasingWithoutAHeldSeatFails()
    {
        var match = await CreateFriendlyMatchAsync();
        await AuthenticateAsync(Client);

        Assert.Equal(HttpStatusCode.Conflict, (await Client.PostAsync($"/api/matches/{match.Id}/confirm", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await Client.PostAsync($"/api/matches/{match.Id}/release", null)).StatusCode);
    }

    [Fact]
    public async Task HoldingWhenAllFourSeatsAreTakenFails()
    {
        var match = await CreateFriendlyMatchAsync();
        // The organizer already holds one seat; 3 more fill the match.
        for (var i = 0; i < 3; i++)
        {
            using var filler = Factory.CreateClient();
            await AuthenticateAsync(filler);
            var hold = await filler.PostAsync($"/api/matches/{match.Id}/hold", null);
            Assert.Equal(HttpStatusCode.OK, hold.StatusCode);
        }

        await AuthenticateAsync(Client);
        var response = await Client.PostAsync($"/api/matches/{match.Id}/hold", null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task AnExpiredHeldSeatCanBeClaimedByAnotherPlayer()
    {
        var match = await CreateFriendlyMatchAsync();

        // The organizer holds one seat; 2 fillers leave one for firstHolder. All four holds then expire together,
        // so the claim below exercises "an expired Held seat is claimable", whichever of them it picks.
        for (var i = 0; i < 2; i++)
        {
            using var filler = Factory.CreateClient();
            await AuthenticateAsync(filler);
            await filler.PostAsync($"/api/matches/{match.Id}/hold", null);
        }

        using var firstHolder = Factory.CreateClient();
        await AuthenticateAsync(firstHolder);
        var firstHold = await firstHolder.PostAsync($"/api/matches/{match.Id}/hold", null);
        Assert.Equal(HttpStatusCode.OK, firstHold.StatusCode);

        // Authenticate before advancing the clock: a token issued on the advanced clock carries an nbf over 5
        // minutes ahead of real time, which JWT validation (real clock, 5 min skew) can reject as not yet valid.
        await AuthenticateAsync(Client);
        Factory.Clock.Advance(TimeSpan.FromMinutes(5) + TimeSpan.FromSeconds(1));

        var secondHold = await Client.PostAsync($"/api/matches/{match.Id}/hold", null);

        Assert.Equal(HttpStatusCode.OK, secondHold.StatusCode);
    }

    [Fact]
    public async Task ConfirmingTheFourthSeatMarksTheMatchFull()
    {
        var match = await CreateFriendlyMatchAsync();

        await FillMatchAsync(match.Id);

        var detail = await Client.GetFromJsonAsync<MatchDetailResponse>($"/api/matches/{match.Id}", JsonOptions);
        Assert.NotNull(detail);
        Assert.Equal(MatchStatus.Full, detail.Status);
    }

    [Fact]
    public async Task AFullMatchDoesNotAppearInTheFeed()
    {
        var match = await CreateFriendlyMatchAsync();
        await FillMatchAsync(match.Id);

        using var viewer = Factory.CreateClient();
        await AuthenticateAsync(viewer);
        var feed = await viewer.GetFromJsonAsync<MatchFeedResponse>("/api/matches/feed", JsonOptions);

        Assert.NotNull(feed);
        Assert.DoesNotContain(feed.ForYou, m => m.Id == match.Id);
        Assert.DoesNotContain(feed.OutOfRange, m => m.Id == match.Id);
    }

    [Fact]
    public async Task ConcurrentHoldsOnTheLastAvailableSeatOnlyAllowOneToSucceed()
    {
        var match = await CreateFriendlyMatchAsync();
        // The organizer holds one seat; 2 fillers leave exactly one Available.
        for (var i = 0; i < 2; i++)
        {
            using var filler = Factory.CreateClient();
            await AuthenticateAsync(filler);
            await filler.PostAsync($"/api/matches/{match.Id}/hold", null);
        }

        var racers = new List<HttpClient>();
        for (var i = 0; i < 3; i++)
        {
            var racer = Factory.CreateClient();
            await AuthenticateAsync(racer);
            racers.Add(racer);
        }

        try
        {
            var responses = await Task.WhenAll(racers.Select(r => r.PostAsync($"/api/matches/{match.Id}/hold", null)));

            Assert.Single(responses, r => r.StatusCode == HttpStatusCode.OK);
            Assert.Equal(2, responses.Count(r => r.StatusCode == HttpStatusCode.Conflict));
        }
        finally
        {
            foreach (var racer in racers)
            {
                racer.Dispose();
            }
        }
    }

    [Fact]
    public async Task SeatEndpointsRequireAuthentication()
    {
        var match = await CreateFriendlyMatchAsync();
        // CreateFriendlyMatchAsync authenticates Client as the organizer; these calls must go out unauthenticated.
        using var anonymous = Factory.CreateClient();

        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.PostAsync($"/api/matches/{match.Id}/hold", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.PostAsync($"/api/matches/{match.Id}/confirm", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.PostAsync($"/api/matches/{match.Id}/release", null)).StatusCode);
    }

    [Fact]
    public async Task ANewMatchHoldsTheOrganizersSeatButConfirmsNone()
    {
        var now = Factory.Clock.GetUtcNow();
        var match = await CreateFriendlyMatchAsync();

        // Creating starts the organizer's join (Held, 5 minutes) but only paying confirms it.
        Assert.Equal(0, match.ConfirmedSeats);
        Assert.NotNull(match.MySeat);
        Assert.Equal(SeatStatus.Held, match.MySeat.Status);
        Assert.NotNull(match.MySeat.HeldUntilUtc);
        Assert.Equal(now.AddMinutes(5), match.MySeat.HeldUntilUtc.Value, TimeSpan.FromMilliseconds(1));

        using var other = Factory.CreateClient();
        await AuthenticateAsync(other);
        var otherView = await other.GetFromJsonAsync<MatchDetailResponse>($"/api/matches/{match.Id}", JsonOptions);
        Assert.NotNull(otherView);
        Assert.Equal(0, otherView.ConfirmedSeats);
        Assert.Null(otherView.MySeat);
    }

    [Fact]
    public async Task AHeldSeatShowsOnlyToItsHolder()
    {
        var match = await CreateFriendlyMatchAsync();
        using var holder = Factory.CreateClient();
        await AuthenticateAsync(holder);
        var hold = await (await holder.PostAsync($"/api/matches/{match.Id}/hold", null))
            .Content.ReadFromJsonAsync<SeatHoldResponse>(JsonOptions);
        Assert.NotNull(hold);

        var holderView = await holder.GetFromJsonAsync<MatchDetailResponse>($"/api/matches/{match.Id}", JsonOptions);
        Assert.NotNull(holderView);
        Assert.NotNull(holderView.MySeat);
        Assert.Equal(SeatStatus.Held, holderView.MySeat.Status);
        // PostgreSQL stores microseconds, .NET ticks are 100ns: the persisted value can lose the last digit.
        Assert.NotNull(holderView.MySeat.HeldUntilUtc);
        Assert.Equal(hold.HeldUntilUtc, holderView.MySeat.HeldUntilUtc.Value, TimeSpan.FromMilliseconds(1));
        Assert.Equal(0, holderView.ConfirmedSeats);

        // Held isn't shown to other players (plan §11): neither as their seat nor in the count.
        using var other = Factory.CreateClient();
        await AuthenticateAsync(other);
        var otherView = await other.GetFromJsonAsync<MatchDetailResponse>($"/api/matches/{match.Id}", JsonOptions);
        Assert.NotNull(otherView);
        Assert.Null(otherView.MySeat);
        Assert.Equal(0, otherView.ConfirmedSeats);
    }

    [Fact]
    public async Task AConfirmedSeatCountsAndShowsAsTheCallersSeat()
    {
        var match = await CreateFriendlyMatchAsync();
        await Client.PostAsync($"/api/matches/{match.Id}/confirm", null);

        var detail = await Client.GetFromJsonAsync<MatchDetailResponse>($"/api/matches/{match.Id}", JsonOptions);

        Assert.NotNull(detail);
        Assert.Equal(1, detail.ConfirmedSeats);
        Assert.NotNull(detail.MySeat);
        Assert.Equal(SeatStatus.Confirmed, detail.MySeat.Status);
    }

    [Fact]
    public async Task AnExpiredHeldSeatIsNoLongerTheCallersSeat()
    {
        // The organizer's seat is Held from creation; letting it lapse is the case under test.
        var match = await CreateFriendlyMatchAsync();

        Factory.Clock.Advance(TimeSpan.FromMinutes(5) + TimeSpan.FromSeconds(1));

        var detail = await Client.GetFromJsonAsync<MatchDetailResponse>($"/api/matches/{match.Id}", JsonOptions);
        Assert.NotNull(detail);
        Assert.Null(detail.MySeat);
    }

    [Fact]
    public async Task AMatchStaysOutOfTheFeedUntilItHasAConfirmedSeat()
    {
        var match = await CreateFriendlyMatchAsync();
        using var viewer = Factory.CreateClient();
        await AuthenticateAsync(viewer);

        var before = await viewer.GetFromJsonAsync<MatchFeedResponse>("/api/matches/feed", JsonOptions);
        Assert.NotNull(before);
        Assert.DoesNotContain(before.ForYou.Concat(before.OutOfRange), m => m.Id == match.Id);

        await Client.PostAsync($"/api/matches/{match.Id}/confirm", null);

        var after = await viewer.GetFromJsonAsync<MatchFeedResponse>("/api/matches/feed", JsonOptions);
        Assert.NotNull(after);
        Assert.Contains(after.ForYou.Concat(after.OutOfRange), m => m.Id == match.Id);
    }

    [Fact]
    public async Task TheFeedReportsConfirmedSeats()
    {
        var match = await CreateFriendlyMatchAsync();
        await Client.PostAsync($"/api/matches/{match.Id}/confirm", null);
        using var viewer = Factory.CreateClient();
        await AuthenticateAsync(viewer);

        var feed = await viewer.GetFromJsonAsync<MatchFeedResponse>("/api/matches/feed", JsonOptions);

        Assert.NotNull(feed);
        var item = Assert.Single(feed.ForYou.Concat(feed.OutOfRange), m => m.Id == match.Id);
        Assert.Equal(1, item.ConfirmedSeats);
    }

    [Fact]
    public async Task TheFeedHidesMatchesTheViewerOrganizedOrHoldsASeatIn()
    {
        var match = await CreateFriendlyMatchAsync();
        await Client.PostAsync($"/api/matches/{match.Id}/confirm", null);
        using var joiner = Factory.CreateClient();
        await AuthenticateAsync(joiner);
        await joiner.PostAsync($"/api/matches/{match.Id}/hold", null);

        // The organizer (confirmed) and a player with a live Held seat: neither can join it again.
        foreach (var client in new[] { Client, joiner })
        {
            var feed = await client.GetFromJsonAsync<MatchFeedResponse>("/api/matches/feed", JsonOptions);
            Assert.NotNull(feed);
            Assert.DoesNotContain(feed.ForYou.Concat(feed.OutOfRange), m => m.Id == match.Id);
        }

        // Once joiner's hold lapses (without ever paying), the match is joinable for them again.
        Factory.Clock.Advance(TimeSpan.FromMinutes(5) + TimeSpan.FromSeconds(1));
        var afterExpiry = await joiner.GetFromJsonAsync<MatchFeedResponse>("/api/matches/feed", JsonOptions);
        Assert.NotNull(afterExpiry);
        Assert.Contains(afterExpiry.ForYou.Concat(afterExpiry.OutOfRange), m => m.Id == match.Id);
    }

    [Fact]
    public async Task TheFeedListsOnlyMyMatchesWithAConfirmedSeatAsPending()
    {
        var confirmed = await CreateFriendlyMatchAsync();
        await Client.PostAsync($"/api/matches/{confirmed.Id}/confirm", null);
        var organizer = Client.DefaultRequestHeaders.Authorization;

        // A second match by the same organizer, left Held (never paid): not one of "my" matches yet.
        var slot = await SeedSlotAsync();
        Client.DefaultRequestHeaders.Authorization = organizer;
        var held = await (await Client.PostAsJsonAsync(
                "/api/matches", new CreateMatchRequest(slot.Id, MatchType.Friendly, null, null, null, null), JsonOptions))
            .Content.ReadFromJsonAsync<MatchDetailResponse>(JsonOptions);
        Assert.NotNull(held);

        var feed = await Client.GetFromJsonAsync<MatchFeedResponse>("/api/matches/feed", JsonOptions);

        Assert.NotNull(feed);
        var item = Assert.Single(feed.PendingConfirmation);
        Assert.Equal(confirmed.Id, item.Id);
        Assert.Equal(1, item.ConfirmedSeats);
        Assert.Empty(feed.Confirmed);
    }

    [Fact]
    public async Task TheDetailListsConfirmedPlayersOrganizerFirstButNotHeldOnes()
    {
        var match = await CreateFriendlyMatchAsync();
        await Client.PostAsync($"/api/matches/{match.Id}/confirm", null);

        using var joiner = Factory.CreateClient();
        var joinerId = await AuthenticateAsync(joiner, "Zoe");
        await joiner.PostAsync($"/api/matches/{match.Id}/hold", null);

        // Held: not shown to anyone yet (plan §11).
        var whileHeld = await joiner.GetFromJsonAsync<MatchDetailResponse>($"/api/matches/{match.Id}", JsonOptions);
        Assert.NotNull(whileHeld);
        var organizerOnly = Assert.Single(whileHeld.ConfirmedPlayers);
        Assert.Equal(match.OrganizerId, organizerOnly.PlayerId);

        await joiner.PostAsync($"/api/matches/{match.Id}/confirm", null);

        var detail = await joiner.GetFromJsonAsync<MatchDetailResponse>($"/api/matches/{match.Id}", JsonOptions);
        Assert.NotNull(detail);
        Assert.Equal([match.OrganizerId, joinerId], detail.ConfirmedPlayers.Select(p => p.PlayerId));
        Assert.Equal("Zoe", detail.ConfirmedPlayers[1].DisplayName);
    }

    [Fact]
    public async Task HoldingAChosenPositionClaimsThatSeat()
    {
        var match = await CreateFriendlyMatchAsync();
        using var joiner = Factory.CreateClient();
        await AuthenticateAsync(joiner);

        // Pair B, first seat: playing against the organizer rather than with them.
        var hold = await joiner.PostAsJsonAsync($"/api/matches/{match.Id}/hold", new HoldSeatRequest(2), JsonOptions);
        Assert.Equal(HttpStatusCode.OK, hold.StatusCode);
        await joiner.PostAsync($"/api/matches/{match.Id}/confirm", null);

        var detail = await joiner.GetFromJsonAsync<MatchDetailResponse>($"/api/matches/{match.Id}", JsonOptions);
        Assert.NotNull(detail);
        Assert.NotNull(detail.MySeat);
        Assert.Equal(2, detail.MySeat.Position);
        Assert.Equal(2, Assert.Single(detail.ConfirmedPlayers).Position);
    }

    [Fact]
    public async Task HoldingATakenPositionFailsEvenIfOthersAreFree()
    {
        var match = await CreateFriendlyMatchAsync();
        using var joiner = Factory.CreateClient();
        await AuthenticateAsync(joiner);

        // Position 0 is the organizer's (Held from creation); 1–3 are free, but the player asked for 0.
        var hold = await joiner.PostAsJsonAsync($"/api/matches/{match.Id}/hold", new HoldSeatRequest(0), JsonOptions);

        Assert.Equal(HttpStatusCode.Conflict, hold.StatusCode);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(4)]
    public async Task HoldingAnInvalidPositionIsABadRequest(int position)
    {
        var match = await CreateFriendlyMatchAsync();
        using var joiner = Factory.CreateClient();
        await AuthenticateAsync(joiner);

        var hold = await joiner.PostAsJsonAsync($"/api/matches/{match.Id}/hold", new HoldSeatRequest(position), JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, hold.StatusCode);
    }

    [Fact]
    public async Task TheFeedShowsTheParticipantsOwnMatchesAsPendingThenConfirmed()
    {
        var match = await CreateFriendlyMatchAsync();
        await Client.PostAsync($"/api/matches/{match.Id}/confirm", null);

        var whileOpen = await Client.GetFromJsonAsync<MatchFeedResponse>("/api/matches/feed", JsonOptions);
        Assert.NotNull(whileOpen);
        Assert.Contains(whileOpen.PendingConfirmation, m => m.Id == match.Id);
        Assert.DoesNotContain(whileOpen.Confirmed, m => m.Id == match.Id);
        Assert.DoesNotContain(whileOpen.ForYou.Concat(whileOpen.OutOfRange), m => m.Id == match.Id);

        // The organizer already confirmed above, so only the other three join (FillMatchAsync would confirm again).
        for (var i = 0; i < 3; i++)
        {
            using var player = Factory.CreateClient();
            await AuthenticateAsync(player);
            await player.PostAsync($"/api/matches/{match.Id}/hold", null);
            await player.PostAsync($"/api/matches/{match.Id}/confirm", null);
        }

        var whenFull = await Client.GetFromJsonAsync<MatchFeedResponse>("/api/matches/feed", JsonOptions);
        Assert.NotNull(whenFull);
        var item = Assert.Single(whenFull.Confirmed, m => m.Id == match.Id);
        Assert.Equal(4, item.ConfirmedSeats);
        Assert.DoesNotContain(whenFull.PendingConfirmation, m => m.Id == match.Id);
    }

    [Fact]
    public async Task APlayerCanHoldSeatsInOverlappingMatches()
    {
        // Overlapping matches are the player's call (user decision, m5-mobile-confirmation proposal): paying for a
        // seat they can't attend is the natural brake, as in Playtomic.
        var first = await CreateFriendlyMatchAsync();
        var overlapping = await CreateFriendlyMatchAsync(TimeSpan.FromMinutes(30));

        using var player = Factory.CreateClient();
        await AuthenticateAsync(player);
        await player.PostAsync($"/api/matches/{first.Id}/hold", null);
        await player.PostAsync($"/api/matches/{first.Id}/confirm", null);

        Assert.Equal(HttpStatusCode.OK, (await player.PostAsync($"/api/matches/{overlapping.Id}/hold", null)).StatusCode);
    }

    /// <summary>Confirms the organizer's (Client's) held seat, then 3 more players hold and confirm the rest.</summary>
    private async Task FillMatchAsync(Guid matchId)
    {
        Assert.Equal(HttpStatusCode.OK, (await Client.PostAsync($"/api/matches/{matchId}/confirm", null)).StatusCode);
        for (var i = 0; i < 3; i++)
        {
            using var player = Factory.CreateClient();
            await AuthenticateAsync(player);
            Assert.Equal(HttpStatusCode.OK, (await player.PostAsync($"/api/matches/{matchId}/hold", null)).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await player.PostAsync($"/api/matches/{matchId}/confirm", null)).StatusCode);
        }
    }

    private async Task<MatchDetailResponse> CreateFriendlyMatchAsync(TimeSpan? offsetFromTomorrow = null)
    {
        var slot = await SeedSlotAsync(offsetFromTomorrow);
        await AuthenticateAsync(Client);
        var response = await Client.PostAsJsonAsync(
            "/api/matches", new CreateMatchRequest(slot.Id, MatchType.Friendly, null, null, null, null), JsonOptions);
        response.EnsureSuccessStatusCode();
        var match = await response.Content.ReadFromJsonAsync<MatchDetailResponse>(JsonOptions);
        Assert.NotNull(match);
        return match;
    }

    private static async Task<Guid> AuthenticateAsync(HttpClient client, string displayName = "Jugador")
    {
        var token = FakeExternalIdentityVerifier.CreateToken(
            AuthProvider.Google, $"sub-{Guid.NewGuid()}", $"{Guid.NewGuid()}@example.com", displayName);
        var response = await client.PostAsJsonAsync("/api/auth/google", new GoogleAuthRequest(token), JsonOptions);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<AuthResponse>(JsonOptions);
        Assert.NotNull(result);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", result.SessionToken);
        return result.Player.Id;
    }

    private async Task<CourtSlot> SeedSlotAsync(TimeSpan? offsetFromTomorrow = null)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PadelMatchDbContext>();
        var club = Club.Create("Club de pruebas", "Calle 1", null, null, null, Factory.Clock.GetUtcNow());
        var court = Court.Create(club.Id, "Pista 1");
        var startsAt = Round(Factory.Clock.GetUtcNow().AddDays(1) + (offsetFromTomorrow ?? TimeSpan.Zero));
        var slot = CourtSlot.Create(court.Id, startsAt, SlotDuration.SixtyMinutes);
        dbContext.Clubs.Add(club);
        dbContext.Courts.Add(court);
        dbContext.CourtSlots.Add(slot);
        await dbContext.SaveChangesAsync();
        return slot;
    }

    private static DateTimeOffset Round(DateTimeOffset value) =>
        new(value.Year, value.Month, value.Day, value.Hour, value.Minute, value.Second, value.Offset);
}
