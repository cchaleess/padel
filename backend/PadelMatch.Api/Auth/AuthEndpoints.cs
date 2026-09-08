using Microsoft.AspNetCore.Http.HttpResults;
using PadelMatch.Api.Players;
using PadelMatch.Application.Players;
using PadelMatch.Domain.Players;

namespace PadelMatch.Api.Auth;

public sealed record GoogleAuthRequest(string IdToken);

public sealed record AppleAuthRequest(string IdToken, string? DisplayName);

public sealed record AuthResponse(string SessionToken, PlayerProfileResponse Player);

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        app.MapPost("/api/auth/google", async Task<Results<Ok<AuthResponse>, ProblemHttpResult>> (
                GoogleAuthRequest request, IPlayerAuthenticator authenticator, CancellationToken cancellationToken) =>
                await AuthenticateAsync(authenticator, AuthProvider.Google, request.IdToken, displayNameOverride: null, cancellationToken))
            .WithName("AuthenticateWithGoogle")
            .WithSummary("Authenticates a player with a Google ID token, registering them on first sign-in.")
            .ProducesProblem(StatusCodes.Status400BadRequest);

        app.MapPost("/api/auth/apple", async Task<Results<Ok<AuthResponse>, ProblemHttpResult>> (
                AppleAuthRequest request, IPlayerAuthenticator authenticator, CancellationToken cancellationToken) =>
                await AuthenticateAsync(authenticator, AuthProvider.Apple, request.IdToken, request.DisplayName, cancellationToken))
            .WithName("AuthenticateWithApple")
            .WithSummary("Authenticates a player with an Apple ID token, registering them on first sign-in.")
            .ProducesProblem(StatusCodes.Status400BadRequest);
    }

    private static async Task<Results<Ok<AuthResponse>, ProblemHttpResult>> AuthenticateAsync(
        IPlayerAuthenticator authenticator, AuthProvider provider, string idToken, string? displayNameOverride,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await authenticator.AuthenticateAsync(provider, idToken, displayNameOverride, cancellationToken);
            return TypedResults.Ok(new AuthResponse(result.SessionToken, PlayerProfileResponse.From(result.Player)));
        }
        catch (InvalidExternalIdentityException)
        {
            return TypedResults.Problem(statusCode: StatusCodes.Status400BadRequest, title: "Invalid identity provider token");
        }
    }
}
