using Microsoft.AspNetCore.Http.HttpResults;
using PadelMatch.Api.Auth;
using PadelMatch.Api.Players;
using PadelMatch.Application.Players;
using PadelMatch.Domain.Players;

namespace PadelMatch.Api.Dev;

public sealed record DevSessionRequest(string Name);

/// <summary>Development-only tools to act as other players (specs/dev-player-simulation). Program.cs maps these
/// only when the environment is Development, so the routes don't exist anywhere else.</summary>
public static class DevEndpoints
{
    public static void MapDevEndpoints(this WebApplication app)
    {
        app.MapPost("/api/dev/session", async Task<Results<Ok<AuthResponse>, ProblemHttpResult>> (
                DevSessionRequest request, IPlayerRepository players, IPlayerSessionTokenIssuer tokenIssuer,
                TimeProvider clock, CancellationToken cancellationToken) =>
            {
                var name = request.Name?.Trim();
                if (string.IsNullOrEmpty(name))
                {
                    return TypedResults.Problem(statusCode: StatusCodes.Status400BadRequest, title: "A name is required.");
                }

                // Registered as Google with a "dev:" subject instead of a new AuthProvider: Google subjects are
                // numeric, so this can never collide with a real account, and the domain stays untouched.
                var slug = name.ToLowerInvariant().Replace(' ', '-');
                var subject = $"dev:{slug}";
                var player = await players.FindByExternalIdentityAsync(AuthProvider.Google, subject, cancellationToken);
                if (player is null)
                {
                    player = Player.Register(name, $"{slug}@dev.padelmatch.local", AuthProvider.Google, subject, clock.GetUtcNow());
                    await players.AddAsync(player, cancellationToken);
                }

                if (player.Level is null)
                {
                    // Same path as a real player's survey (Player.CompleteLevelSurvey), so the level is a real
                    // estimate; answers derive from the name, so each fictional player keeps a stable, varied level.
                    player.UpdateProfile(player.CityOrZone, player.DateOfBirth ?? new DateOnly(1990, 1, 1), player.PhotoUrl);
                    player.CompleteLevelSurvey(SurveyFor(slug));
                }

                await players.SaveChangesAsync(cancellationToken);

                return TypedResults.Ok(new AuthResponse(tokenIssuer.Issue(player), PlayerProfileResponse.From(player)));
            })
            .WithName("CreateDevSession")
            .WithSummary("Development only: signs in as a fictional player, created on first use and reused by name.")
            .ProducesProblem(StatusCodes.Status400BadRequest);
    }

    private static LevelSurveyAnswers SurveyFor(string slug)
    {
        // Deterministic across runs (string.GetHashCode is randomized per process).
        var hash = slug.Aggregate(0, (sum, c) => sum * 31 + c) & int.MaxValue;
        return new LevelSurveyAnswers(
            (YearsPlayingPadel)(1 + hash % 4),
            (WeeklyFrequency)(1 + hash / 4 % 4),
            (SelfPerceivedLevel)(1 + hash / 16 % 4));
    }
}
