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
    public async Task CreatingAMatchCreatesFourAvailableSeats()
    {
        var match = await CreateFriendlyMatchAsync();

        await using var scope = Factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PadelMatchDbContext>();
        var seats = await dbContext.MatchSeats.Where(s => s.MatchId == match.Id).ToListAsync();

        Assert.Equal(4, seats.Count);
        Assert.All(seats, s => Assert.Equal(SeatStatus.Available, s.Status));
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
        for (var i = 0; i < 4; i++)
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

        // Fill the other 3 seats so the only remaining candidate is the one about to expire.
        for (var i = 0; i < 3; i++)
        {
            using var filler = Factory.CreateClient();
            await AuthenticateAsync(filler);
            await filler.PostAsync($"/api/matches/{match.Id}/hold", null);
        }

        using var firstHolder = Factory.CreateClient();
        await AuthenticateAsync(firstHolder);
        var firstHold = await firstHolder.PostAsync($"/api/matches/{match.Id}/hold", null);
        Assert.Equal(HttpStatusCode.OK, firstHold.StatusCode);

        Factory.Clock.Advance(TimeSpan.FromMinutes(5) + TimeSpan.FromSeconds(1));

        await AuthenticateAsync(Client);
        var secondHold = await Client.PostAsync($"/api/matches/{match.Id}/hold", null);

        Assert.Equal(HttpStatusCode.OK, secondHold.StatusCode);
    }

    [Fact]
    public async Task ConfirmingTheFourthSeatMarksTheMatchFull()
    {
        var match = await CreateFriendlyMatchAsync();

        for (var i = 0; i < 4; i++)
        {
            using var player = Factory.CreateClient();
            await AuthenticateAsync(player);
            await player.PostAsync($"/api/matches/{match.Id}/hold", null);
            await player.PostAsync($"/api/matches/{match.Id}/confirm", null);
        }

        var detail = await Client.GetFromJsonAsync<MatchDetailResponse>($"/api/matches/{match.Id}", JsonOptions);
        Assert.NotNull(detail);
        Assert.Equal(MatchStatus.Full, detail.Status);
    }

    [Fact]
    public async Task AFullMatchDoesNotAppearInTheFeed()
    {
        var match = await CreateFriendlyMatchAsync();
        for (var i = 0; i < 4; i++)
        {
            using var player = Factory.CreateClient();
            await AuthenticateAsync(player);
            await player.PostAsync($"/api/matches/{match.Id}/hold", null);
            await player.PostAsync($"/api/matches/{match.Id}/confirm", null);
        }

        await AuthenticateAsync(Client);
        var feed = await Client.GetFromJsonAsync<MatchFeedResponse>("/api/matches/feed", JsonOptions);

        Assert.NotNull(feed);
        Assert.DoesNotContain(feed.ForYou, m => m.Id == match.Id);
        Assert.DoesNotContain(feed.OutOfRange, m => m.Id == match.Id);
    }

    [Fact]
    public async Task ConcurrentHoldsOnTheLastAvailableSeatOnlyAllowOneToSucceed()
    {
        var match = await CreateFriendlyMatchAsync();
        for (var i = 0; i < 3; i++)
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

    private async Task<MatchDetailResponse> CreateFriendlyMatchAsync()
    {
        var slot = await SeedSlotAsync();
        await AuthenticateAsync(Client);
        var response = await Client.PostAsJsonAsync(
            "/api/matches", new CreateMatchRequest(slot.Id, MatchType.Friendly, null, null, null, null), JsonOptions);
        response.EnsureSuccessStatusCode();
        var match = await response.Content.ReadFromJsonAsync<MatchDetailResponse>(JsonOptions);
        Assert.NotNull(match);
        return match;
    }

    private static async Task AuthenticateAsync(HttpClient client)
    {
        var token = FakeExternalIdentityVerifier.CreateToken(
            AuthProvider.Google, $"sub-{Guid.NewGuid()}", $"{Guid.NewGuid()}@example.com", "Jugador");
        var response = await client.PostAsJsonAsync("/api/auth/google", new GoogleAuthRequest(token), JsonOptions);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<AuthResponse>(JsonOptions);
        Assert.NotNull(result);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", result.SessionToken);
    }

    private async Task<CourtSlot> SeedSlotAsync()
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PadelMatchDbContext>();
        var club = Club.Create("Club de pruebas", "Calle 1", null, null, null, Factory.Clock.GetUtcNow());
        var court = Court.Create(club.Id, "Pista 1");
        var startsAt = Round(Factory.Clock.GetUtcNow().AddDays(1));
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
