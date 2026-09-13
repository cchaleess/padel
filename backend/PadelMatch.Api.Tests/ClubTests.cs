using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PadelMatch.Api.Auth;
using PadelMatch.Api.Clubs;
using PadelMatch.Api.Players;
using PadelMatch.Api.Tests.TestSupport;
using PadelMatch.Domain.Clubs;
using PadelMatch.Domain.Players;
using PadelMatch.Infrastructure.Persistence;

namespace PadelMatch.Api.Tests;

public sealed class ClubTests : PlayerApiTestBase
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    [Fact]
    public async Task NearbyClubsOrdersByDistanceWhenCoordinatesAreGiven()
    {
        await SeedClubAsync("Club Madrid", "Madrid", 40.4168, -3.7038);
        await SeedClubAsync("Club Barcelona", "Barcelona", 41.3851, 2.1734);
        var valencia = await SeedClubAsync("Club Valencia", "Valencia", 39.4699, -0.3763);
        await AuthenticateDefaultPlayerAsync();

        // Closer to Valencia than to Madrid or Barcelona.
        var response = await Client.GetFromJsonAsync<List<ClubSummaryResponse>>(
            "/api/clubs/nearby?lat=39.5&lng=-0.4", JsonOptions);

        Assert.NotNull(response);
        Assert.Equal(valencia.Id, response[0].Id);
        Assert.True(response[0].DistanceKm < response[1].DistanceKm);
        Assert.True(response[1].DistanceKm < response[2].DistanceKm);
    }

    [Fact]
    public async Task NearbyClubsFallsBackToPlayerCityOrZoneWithoutCoordinates()
    {
        var madrid = await SeedClubAsync("Club Madrid", "Madrid", latitude: null, longitude: null);
        var barcelona = await SeedClubAsync("Club Barcelona", "Barcelona", latitude: null, longitude: null);
        await AuthenticateDefaultPlayerAsync();
        await Client.PutAsJsonAsync("/api/players/me", new UpdateProfileRequest("Madrid", null, null), JsonOptions);

        var response = await Client.GetFromJsonAsync<List<ClubSummaryResponse>>("/api/clubs/nearby", JsonOptions);

        Assert.NotNull(response);
        Assert.Equal(madrid.Id, response[0].Id);
        Assert.Equal(barcelona.Id, response[1].Id);
        Assert.Null(response[0].DistanceKm);
    }

    [Fact]
    public async Task NearbyClubsOrdersByNameWhenThereIsNoLocationSignal()
    {
        var zebra = await SeedClubAsync("Zebra Padel", "Sevilla", latitude: null, longitude: null);
        var alfa = await SeedClubAsync("Alfa Padel", "Bilbao", latitude: null, longitude: null);
        await AuthenticateDefaultPlayerAsync();

        var response = await Client.GetFromJsonAsync<List<ClubSummaryResponse>>("/api/clubs/nearby", JsonOptions);

        Assert.NotNull(response);
        Assert.Equal(alfa.Id, response[0].Id);
        Assert.Equal(zebra.Id, response[1].Id);
    }

    [Fact]
    public async Task SearchFindsOfficialAndUserSubmittedClubsByPartialName()
    {
        await SeedClubAsync("Padel Norte", "Madrid", latitude: null, longitude: null);
        await AuthenticateDefaultPlayerAsync();
        await Client.PostAsJsonAsync("/api/clubs", new SubmitClubRequest("Padel Sur Aportado", "Calle 1", "Madrid"), JsonOptions);

        var response = await Client.GetFromJsonAsync<List<ClubSummaryResponse>>("/api/clubs/search?q=Padel", JsonOptions);

        Assert.NotNull(response);
        Assert.Equal(2, response.Count);
        Assert.Contains(response, c => c.Status == ClubStatus.Official);
        Assert.Contains(response, c => c.Status == ClubStatus.UserSubmitted);
    }

    [Fact]
    public async Task SubmitClubCreatesUnverifiedClubAndRejectsMissingFields()
    {
        await AuthenticateDefaultPlayerAsync();

        var created = await Client.PostAsJsonAsync("/api/clubs", new SubmitClubRequest("Mi Club", "Calle 1", "Madrid"), JsonOptions);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var club = await created.Content.ReadFromJsonAsync<ClubDetailResponse>(JsonOptions);
        Assert.NotNull(club);
        Assert.Equal(ClubStatus.UserSubmitted, club.Status);

        var invalid = await Client.PostAsJsonAsync("/api/clubs", new SubmitClubRequest("", "Calle 1", null), JsonOptions);
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
    }

    [Fact]
    public async Task ClubDetailsReturns404ForAnUnknownClub()
    {
        await AuthenticateDefaultPlayerAsync();

        var response = await Client.GetAsync($"/api/clubs/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CourtSlotsRespectsRangeCourtFilterAndAvailabilityStatus()
    {
        var club = await SeedClubAsync("Club Con Pistas", "Madrid", latitude: null, longitude: null);
        var (courtA, courtB) = await SeedCourtsAsync(club.Id);
        var now = DateTimeOffset.UtcNow;
        await SeedSlotAsync(courtA.Id, now.AddDays(1), SlotDuration.NinetyMinutes);
        var outOfRange = await SeedSlotAsync(courtA.Id, now.AddDays(30), SlotDuration.NinetyMinutes);
        var otherCourt = await SeedSlotAsync(courtB.Id, now.AddDays(1), SlotDuration.SixtyMinutes);
        await AuthenticateDefaultPlayerAsync();

        var defaultWindow = await Client.GetFromJsonAsync<List<CourtSlotResponse>>($"/api/clubs/{club.Id}/slots", JsonOptions);
        Assert.NotNull(defaultWindow);
        Assert.DoesNotContain(defaultWindow, s => s.Id == outOfRange.Id);
        Assert.Equal(2, defaultWindow.Count);

        var filteredByCourt = await Client.GetFromJsonAsync<List<CourtSlotResponse>>(
            $"/api/clubs/{club.Id}/slots?courtId={courtA.Id}", JsonOptions);
        Assert.NotNull(filteredByCourt);
        Assert.DoesNotContain(filteredByCourt, s => s.Id == otherCourt.Id);
    }

    [Fact]
    public async Task ClubEndpointsRequireAuthentication()
    {
        Assert.Equal(HttpStatusCode.Unauthorized, (await Client.GetAsync("/api/clubs/nearby")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Client.GetAsync("/api/clubs/search?q=a")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Client.GetAsync($"/api/clubs/{Guid.NewGuid()}")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await Client.PostAsJsonAsync("/api/clubs", new SubmitClubRequest("X", "Y", null), JsonOptions)).StatusCode);
    }

    private async Task<string> AuthenticateDefaultPlayerAsync()
    {
        var token = FakeExternalIdentityVerifier.CreateToken(AuthProvider.Google, $"sub-{Guid.NewGuid()}", "jugador@example.com", "Jugador");
        var response = await Client.PostAsJsonAsync("/api/auth/google", new GoogleAuthRequest(token), JsonOptions);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<AuthResponse>(JsonOptions);
        Assert.NotNull(result);
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", result.SessionToken);
        return result.SessionToken;
    }

    private async Task<Club> SeedClubAsync(string name, string cityOrZone, double? latitude, double? longitude)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PadelMatchDbContext>();
        var club = Club.Create(name, $"Dirección de {name}", cityOrZone, latitude, longitude, DateTimeOffset.UtcNow);
        dbContext.Clubs.Add(club);
        await dbContext.SaveChangesAsync();
        return club;
    }

    private async Task<(Court CourtA, Court CourtB)> SeedCourtsAsync(Guid clubId)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PadelMatchDbContext>();
        var courtA = Court.Create(clubId, "Pista A");
        var courtB = Court.Create(clubId, "Pista B");
        dbContext.Courts.AddRange(courtA, courtB);
        await dbContext.SaveChangesAsync();
        return (courtA, courtB);
    }

    private async Task<CourtSlot> SeedSlotAsync(Guid courtId, DateTimeOffset startsAt, SlotDuration duration)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PadelMatchDbContext>();
        var slot = CourtSlot.Create(courtId, startsAt, duration);
        dbContext.CourtSlots.Add(slot);
        await dbContext.SaveChangesAsync();
        return slot;
    }
}
