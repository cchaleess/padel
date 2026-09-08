using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using PadelMatch.Application.Players;
using PadelMatch.Domain.Players;

namespace PadelMatch.Api.Players;

public static class PlayerEndpoints
{
    public static void MapPlayerEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/players/me").RequireAuthorization();

        group.MapGet("", async (ClaimsPrincipal user, IPlayerProfileService profiles, CancellationToken cancellationToken) =>
            {
                var profile = await profiles.GetProfileAsync(GetPlayerId(user), cancellationToken);
                return TypedResults.Ok(PlayerProfileResponse.From(profile));
            })
            .WithName("GetOwnPlayerProfile")
            .WithSummary("Returns the authenticated player's own profile.");

        group.MapPut("", async (
                UpdateProfileRequest request, ClaimsPrincipal user, IPlayerProfileService profiles, CancellationToken cancellationToken) =>
            {
                var profile = await profiles.UpdateProfileAsync(
                    GetPlayerId(user), request.CityOrZone, request.DateOfBirth, request.PhotoUrl, cancellationToken);
                return TypedResults.Ok(PlayerProfileResponse.From(profile));
            })
            .WithName("UpdateOwnPlayerProfile")
            .WithSummary("Updates the authenticated player's editable profile fields.");

        group.MapPost("/level-survey", async Task<Results<Ok<PlayerProfileResponse>, ProblemHttpResult>> (
                LevelSurveyRequest request, ClaimsPrincipal user, IPlayerProfileService profiles, CancellationToken cancellationToken) =>
            {
                var answers = new LevelSurveyAnswers(request.YearsPlaying, request.WeeklyFrequency, request.SelfPerceivedLevel);
                try
                {
                    var profile = await profiles.CompleteLevelSurveyAsync(GetPlayerId(user), answers, cancellationToken);
                    return TypedResults.Ok(PlayerProfileResponse.From(profile));
                }
                catch (PlayerProfileIncompleteException ex)
                {
                    return TypedResults.Problem(statusCode: StatusCodes.Status400BadRequest, title: ex.Message);
                }
            })
            .WithName("CompleteOwnLevelSurvey")
            .WithSummary("Completes the initial level survey for the authenticated player.")
            .ProducesProblem(StatusCodes.Status400BadRequest);
    }

    private static Guid GetPlayerId(ClaimsPrincipal user) =>
        Guid.Parse(user.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? throw new InvalidOperationException("The session token has no 'sub' claim."));
}
