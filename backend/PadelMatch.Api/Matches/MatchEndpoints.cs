using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using PadelMatch.Application.Matches;

namespace PadelMatch.Api.Matches;

public static class MatchEndpoints
{
    public static void MapMatchEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/matches").RequireAuthorization();

        group.MapPost("", async Task<Results<Created<MatchDetailResponse>, ProblemHttpResult>> (
                CreateMatchRequest request, ClaimsPrincipal user,
                IMatchCreationService creation, IMatchRepository matches, CancellationToken cancellationToken) =>
            {
                try
                {
                    var match = await creation.CreateMatchAsync(
                        GetPlayerId(user), request.CourtSlotId, request.Type,
                        request.MinLevel, request.MaxLevel, request.MinMatchesRequired, request.Note, cancellationToken);
                    var details = await matches.FindDetailsByIdAsync(match.Id, cancellationToken)
                        ?? throw new MatchNotFoundException(match.Id);
                    return TypedResults.Created($"/api/matches/{match.Id}", MatchDetailResponse.From(details));
                }
                catch (CourtSlotNotFoundException ex)
                {
                    return TypedResults.Problem(statusCode: StatusCodes.Status404NotFound, title: ex.Message);
                }
                catch (CourtSlotUnavailableException ex)
                {
                    return TypedResults.Problem(statusCode: StatusCodes.Status409Conflict, title: ex.Message);
                }
                catch (ArgumentException ex)
                {
                    return TypedResults.Problem(statusCode: StatusCodes.Status400BadRequest, title: ex.Message);
                }
            })
            .WithName("CreateMatch")
            .WithSummary("Creates a match from an available CourtSlot, already Open; the organizer is the authenticated player.")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapGet("/feed", async (
                double? lat, double? lng, string? cityOrZone,
                ClaimsPrincipal user, IMatchFeedService feedService, CancellationToken cancellationToken) =>
            {
                var feed = await feedService.GetFeedAsync(GetPlayerId(user), lat, lng, cityOrZone, cancellationToken);
                return TypedResults.Ok(MatchFeedResponse.From(feed));
            })
            .WithName("GetMatchFeed")
            .WithSummary("Lists open, upcoming matches near the player, grouped into for-you (compatible) and out-of-range.");

        group.MapGet("/{id:guid}", async Task<Results<Ok<MatchDetailResponse>, ProblemHttpResult>> (
                Guid id, IMatchRepository matches, CancellationToken cancellationToken) =>
            {
                try
                {
                    var details = await matches.FindDetailsByIdAsync(id, cancellationToken)
                        ?? throw new MatchNotFoundException(id);
                    return TypedResults.Ok(MatchDetailResponse.From(details));
                }
                catch (MatchNotFoundException ex)
                {
                    return TypedResults.Problem(statusCode: StatusCodes.Status404NotFound, title: ex.Message);
                }
            })
            .WithName("GetMatchDetails")
            .WithSummary("Returns a match's detail: club, court, schedule, type, and level range/minimum/note when applicable.")
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/{id:guid}/hold", async Task<Results<Ok<SeatHoldResponse>, ProblemHttpResult>> (
                Guid id, ClaimsPrincipal user, IMatchSeatService seats, CancellationToken cancellationToken) =>
            {
                try
                {
                    var heldUntilUtc = await seats.HoldSeatAsync(id, GetPlayerId(user), cancellationToken);
                    return TypedResults.Ok(new SeatHoldResponse(heldUntilUtc));
                }
                catch (PlayerAlreadyHasSeatException ex)
                {
                    return TypedResults.Problem(statusCode: StatusCodes.Status409Conflict, title: ex.Message);
                }
                catch (SeatUnavailableException ex)
                {
                    return TypedResults.Problem(statusCode: StatusCodes.Status409Conflict, title: ex.Message);
                }
            })
            .WithName("HoldMatchSeat")
            .WithSummary("Claims one of the match's 4 seats for the authenticated player; held for 5 minutes.")
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/{id:guid}/confirm", async Task<Results<Ok, ProblemHttpResult>> (
                Guid id, ClaimsPrincipal user, IMatchSeatService seats, CancellationToken cancellationToken) =>
            {
                try
                {
                    await seats.ConfirmSeatAsync(id, GetPlayerId(user), cancellationToken);
                    return TypedResults.Ok();
                }
                catch (SeatNotHeldException ex)
                {
                    return TypedResults.Problem(statusCode: StatusCodes.Status409Conflict, title: ex.Message);
                }
            })
            .WithName("ConfirmMatchSeat")
            .WithSummary("Confirms the authenticated player's held seat (simulated payment).")
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/{id:guid}/release", async Task<Results<Ok, ProblemHttpResult>> (
                Guid id, ClaimsPrincipal user, IMatchSeatService seats, CancellationToken cancellationToken) =>
            {
                try
                {
                    await seats.ReleaseSeatAsync(id, GetPlayerId(user), cancellationToken);
                    return TypedResults.Ok();
                }
                catch (SeatNotHeldException ex)
                {
                    return TypedResults.Problem(statusCode: StatusCodes.Status409Conflict, title: ex.Message);
                }
            })
            .WithName("ReleaseMatchSeat")
            .WithSummary("Releases the authenticated player's held seat back to Available before it expires.")
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static Guid GetPlayerId(ClaimsPrincipal user) =>
        Guid.Parse(user.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? throw new InvalidOperationException("The session token has no 'sub' claim."));
}
