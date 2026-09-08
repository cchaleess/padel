using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using PadelMatch.Api.Auth;
using PadelMatch.Api.Players;
using PadelMatch.Api.Tests.TestSupport;
using PadelMatch.Domain.Players;

namespace PadelMatch.Api.Tests;

public sealed class PlayerTests : PlayerApiTestBase
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    [Fact]
    public async Task FirstGoogleLoginCreatesPlayerAndSecondLoginRecognizesTheSamePlayer()
    {
        var token = FakeExternalIdentityVerifier.CreateToken(AuthProvider.Google, "google-subject-1", "ana@example.com", "Ana");

        var first = await AuthenticateAsync("/api/auth/google", new GoogleAuthRequest(token));
        Assert.Equal("Ana", first.Player.DisplayName);
        Assert.Null(first.Player.Level);
        Assert.Equal(LevelConfidence.None, first.Player.LevelConfidence);
        Assert.Equal(0, first.Player.MatchesPlayed);

        var second = await AuthenticateAsync("/api/auth/google", new GoogleAuthRequest(token));
        Assert.Equal(first.Player.Id, second.Player.Id);
    }

    [Fact]
    public async Task AppleLoginWithoutDisplayNameDoesNotFail()
    {
        var token = FakeExternalIdentityVerifier.CreateToken(AuthProvider.Apple, "apple-subject-1", "sinnombre@example.com");
        var result = await AuthenticateAsync("/api/auth/apple", new AppleAuthRequest(token, DisplayName: null));
        Assert.NotEqual(Guid.Empty, result.Player.Id);
    }

    [Fact]
    public async Task AppleLoginPersistsClientSuppliedDisplayNameOnlyOnFirstSignIn()
    {
        var token = FakeExternalIdentityVerifier.CreateToken(AuthProvider.Apple, "apple-subject-2", "carlos@example.com");

        var first = await AuthenticateAsync("/api/auth/apple", new AppleAuthRequest(token, "Carlos Apple"));
        Assert.Equal("Carlos Apple", first.Player.DisplayName);

        var second = await AuthenticateAsync("/api/auth/apple", new AppleAuthRequest(token, DisplayName: null));
        Assert.Equal(first.Player.Id, second.Player.Id);
        Assert.Equal("Carlos Apple", second.Player.DisplayName);
    }

    [Fact]
    public async Task SameEmailAcrossGoogleAndAppleCreatesTwoDistinctPlayers()
    {
        const string sharedEmail = "shared@example.com";
        var googleToken = FakeExternalIdentityVerifier.CreateToken(AuthProvider.Google, "sub-g", sharedEmail, "Nombre G");
        var appleToken = FakeExternalIdentityVerifier.CreateToken(AuthProvider.Apple, "sub-a", sharedEmail);

        var googlePlayer = await AuthenticateAsync("/api/auth/google", new GoogleAuthRequest(googleToken));
        var applePlayer = await AuthenticateAsync("/api/auth/apple", new AppleAuthRequest(appleToken, "Nombre A"));

        Assert.NotEqual(googlePlayer.Player.Id, applePlayer.Player.Id);
    }

    [Fact]
    public async Task ProfileEndpointsRequireAuthentication()
    {
        using var unauthenticated = await Client.GetAsync("/api/players/me");
        Assert.Equal(HttpStatusCode.Unauthorized, unauthenticated.StatusCode);
        Assert.Equal("application/problem+json", unauthenticated.Content.Headers.ContentType?.MediaType);

        Assert.Equal(HttpStatusCode.Unauthorized,
            (await Client.PutAsJsonAsync("/api/players/me", new UpdateProfileRequest(null, null, null), JsonOptions)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await Client.PostAsJsonAsync(
                "/api/players/me/level-survey",
                new LevelSurveyRequest(YearsPlayingPadel.LessThanOne, WeeklyFrequency.Rarely, SelfPerceivedLevel.Beginner),
                JsonOptions)).StatusCode);
    }

    [Fact]
    public async Task AuthenticatedPlayerCanReadAndUpdateOwnProfile()
    {
        var token = FakeExternalIdentityVerifier.CreateToken(AuthProvider.Google, "google-subject-3", "perfil@example.com", "Jugadora");
        var authenticated = await AuthenticateAsync("/api/auth/google", new GoogleAuthRequest(token));
        UseSessionToken(authenticated.SessionToken);

        var initial = await (await Client.GetAsync("/api/players/me")).Content.ReadFromJsonAsync<PlayerProfileResponse>(JsonOptions);
        Assert.NotNull(initial);
        Assert.Null(initial.CityOrZone);
        Assert.Null(initial.Level);
        Assert.Equal(LevelConfidence.None, initial.LevelConfidence);

        var update = new UpdateProfileRequest("Madrid", new DateOnly(1990, 5, 1), "https://example.com/photo.png");
        var updated = await (await Client.PutAsJsonAsync("/api/players/me", update, JsonOptions))
            .Content.ReadFromJsonAsync<PlayerProfileResponse>(JsonOptions);
        Assert.NotNull(updated);
        Assert.Equal("Madrid", updated.CityOrZone);
        Assert.Equal(new DateOnly(1990, 5, 1), updated.DateOfBirth);
        Assert.Equal("https://example.com/photo.png", updated.PhotoUrl);

        var reread = await (await Client.GetAsync("/api/players/me")).Content.ReadFromJsonAsync<PlayerProfileResponse>(JsonOptions);
        Assert.NotNull(reread);
        Assert.Equal("Madrid", reread.CityOrZone);
    }

    [Fact]
    public async Task LevelSurveyIsRejectedWithoutDateOfBirth()
    {
        var token = FakeExternalIdentityVerifier.CreateToken(AuthProvider.Google, "google-subject-4", "sinfecha@example.com", "SinFecha");
        var authenticated = await AuthenticateAsync("/api/auth/google", new GoogleAuthRequest(token));
        UseSessionToken(authenticated.SessionToken);

        var survey = new LevelSurveyRequest(YearsPlayingPadel.MoreThanFive, WeeklyFrequency.FourOrMoreTimesAWeek, SelfPerceivedLevel.Competitive);
        using var response = await Client.PostAsJsonAsync("/api/players/me/level-survey", survey, JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task LevelSurveyComputesExpectedLevelWithLowConfidenceOnceDateOfBirthIsSet()
    {
        var token = FakeExternalIdentityVerifier.CreateToken(AuthProvider.Google, "google-subject-5", "nivel@example.com", "Nivel");
        var authenticated = await AuthenticateAsync("/api/auth/google", new GoogleAuthRequest(token));
        UseSessionToken(authenticated.SessionToken);

        await Client.PutAsJsonAsync("/api/players/me", new UpdateProfileRequest(null, new DateOnly(1995, 3, 20), null), JsonOptions);

        var survey = new LevelSurveyRequest(YearsPlayingPadel.MoreThanFive, WeeklyFrequency.FourOrMoreTimesAWeek, SelfPerceivedLevel.Competitive);
        var response = await Client.PostAsJsonAsync("/api/players/me/level-survey", survey, JsonOptions);
        var profile = await response.Content.ReadFromJsonAsync<PlayerProfileResponse>(JsonOptions);

        Assert.NotNull(profile);
        Assert.Equal(5.0m, profile.Level);
        Assert.Equal(LevelConfidence.Low, profile.LevelConfidence);
    }

    private async Task<AuthResponse> AuthenticateAsync<TRequest>(string url, TRequest request)
    {
        var response = await Client.PostAsJsonAsync(url, request, JsonOptions);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<AuthResponse>(JsonOptions);
        Assert.NotNull(result);
        return result;
    }

    private void UseSessionToken(string sessionToken) =>
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", sessionToken);
}
