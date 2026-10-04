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
                CreateMatchRequest request, ClaimsPrincipal user, IMatchCreationService creation,
                IMatchRepository matches, IMatchSeatRepository seats, IMatchAccessService access, TimeProvider clock,
                CancellationToken cancellationToken) =>
            {
                try
                {
                    var playerId = GetPlayerId(user);
                    var match = await creation.CreateMatchAsync(
                        playerId, request.CourtSlotId, request.Type,
                        request.MinLevel, request.MaxLevel, request.MinMatchesRequired, request.Note, cancellationToken);
                    var details = await matches.FindDetailsByIdAsync(match.Id, cancellationToken)
                        ?? throw new MatchNotFoundException(match.Id);
                    // Always null right after creating (creating isn't joining), resolved anyway so the response has
                    // a single shape (m5-mobile-confirmation design.md).
                    var mySeat = await seats.FindActiveSeatAsync(match.Id, playerId, clock.GetUtcNow(), cancellationToken);
                    var confirmedPlayers = await seats.GetConfirmedPlayersAsync(match.Id, cancellationToken);
                    var accessView = await access.GetAccessViewAsync(details.Match, playerId, cancellationToken);
                    return TypedResults.Created(
                        $"/api/matches/{match.Id}", MatchDetailResponse.From(details, mySeat, confirmedPlayers, accessView));
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
            .WithSummary("The player's feed: their own upcoming matches (confirmed = full, pending confirmation = still open), then joinable matches grouped into for-you (compatible) and out-of-range.");

        group.MapGet("/{id:guid}", async Task<Results<Ok<MatchDetailResponse>, ProblemHttpResult>> (
                Guid id, ClaimsPrincipal user, IMatchRepository matches, IMatchSeatRepository seats,
                IMatchAccessService access, TimeProvider clock, CancellationToken cancellationToken) =>
            {
                try
                {
                    var details = await matches.FindDetailsByIdAsync(id, cancellationToken)
                        ?? throw new MatchNotFoundException(id);
                    var playerId = GetPlayerId(user);
                    var mySeat = await seats.FindActiveSeatAsync(id, playerId, clock.GetUtcNow(), cancellationToken);
                    var confirmedPlayers = await seats.GetConfirmedPlayersAsync(id, cancellationToken);
                    var accessView = await access.GetAccessViewAsync(details.Match, playerId, cancellationToken);
                    return TypedResults.Ok(MatchDetailResponse.From(details, mySeat, confirmedPlayers, accessView));
                }
                catch (MatchNotFoundException ex)
                {
                    return TypedResults.Problem(statusCode: StatusCodes.Status404NotFound, title: ex.Message);
                }
            })
            .WithName("GetMatchDetails")
            .WithSummary("Returns a match's detail: club, court, schedule, type, level range/minimum/note, confirmed seats and players, and the caller's own seat.")
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/{id:guid}/hold", async Task<Results<Ok<SeatHoldResponse>, ProblemHttpResult>> (
                Guid id, HoldSeatRequest? request, ClaimsPrincipal user, IMatchSeatService seats,
                CancellationToken cancellationToken) =>
            {
                try
                {
                    var heldUntilUtc = await seats.HoldSeatAsync(id, GetPlayerId(user), request?.Position, cancellationToken);
                    return TypedResults.Ok(new SeatHoldResponse(heldUntilUtc));
                }
                catch (ArgumentOutOfRangeException ex)
                {
                    return TypedResults.Problem(statusCode: StatusCodes.Status400BadRequest, title: ex.Message);
                }
                catch (AccessRequiresApprovalException ex)
                {
                    // The shortfalls tell the app why, so it can offer to request access (m6-quality-rules).
                    return TypedResults.Problem(
                        statusCode: StatusCodes.Status403Forbidden, title: ex.Message,
                        extensions: new Dictionary<string, object?> { ["shortfalls"] = ex.Shortfalls.Select(s => s.ToString()).ToList() });
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
            .WithSummary("Claims one of the match's 4 seats for the authenticated player — the given position (0–1 pair A, 2–3 pair B) or any free one; held for 5 minutes.")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
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
        group.MapPost("/{id:guid}/exception-requests", async Task<Results<Created, ProblemHttpResult>> (
                Guid id, AccessRequestBody? body, ClaimsPrincipal user, IMatchAccessService access,
                CancellationToken cancellationToken) =>
            {
                var playerId = GetPlayerId(user);
                try
                {
                    await access.RequestAccessAsync(id, playerId, body?.Position, cancellationToken);
                    return TypedResults.Created($"/api/matches/{id}/exception-requests/{playerId}");
                }
                catch (MatchNotFoundException ex)
                {
                    return TypedResults.Problem(statusCode: StatusCodes.Status404NotFound, title: ex.Message);
                }
                catch (ArgumentOutOfRangeException ex)
                {
                    return TypedResults.Problem(statusCode: StatusCodes.Status400BadRequest, title: ex.Message);
                }
                catch (Exception ex) when (ex is AccessRequestsClosedException or PlayerAlreadyHasSeatException
                                               or AccessNotNeededException or AccessAlreadyRequestedException)
                {
                    return TypedResults.Problem(statusCode: StatusCodes.Status409Conflict, title: ex.Message);
                }
            })
            .WithName("RequestExceptionalJoin")
            .WithSummary("Asks the match's confirmed players to let in a player who doesn't meet its quality criteria, optionally from a given seat (0–3).")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        MapVote(group, "approve", approve: true);
        MapVote(group, "reject", approve: false);
    }

    /// <summary>Plan §37's route, keyed by the requester: one request per player and match.</summary>
    private static void MapVote(RouteGroupBuilder group, string action, bool approve) =>
        group.MapPost($"/{{id:guid}}/exception-requests/{{playerId:guid}}/{action}", async Task<Results<Ok<AccessVoteResponse>, ProblemHttpResult>> (
                Guid id, Guid playerId, ClaimsPrincipal user, IMatchAccessService access, CancellationToken cancellationToken) =>
            {
                try
                {
                    var status = await access.VoteAsync(id, playerId, GetPlayerId(user), approve, cancellationToken);
                    return TypedResults.Ok(new AccessVoteResponse(status));
                }
                catch (AccessRequestNotFoundException ex)
                {
                    return TypedResults.Problem(statusCode: StatusCodes.Status404NotFound, title: ex.Message);
                }
                catch (NotAConfirmedPlayerException ex)
                {
                    return TypedResults.Problem(statusCode: StatusCodes.Status403Forbidden, title: ex.Message);
                }
                catch (Exception ex) when (ex is VoteAlreadyCastException or AccessRequestAlreadyResolvedException)
                {
                    return TypedResults.Problem(statusCode: StatusCodes.Status409Conflict, title: ex.Message);
                }
            })
            .WithName(approve ? "ApproveExceptionalJoin" : "RejectExceptionalJoin")
            .WithSummary(approve
                ? "A confirmed player approves an access request; it's approved once every confirmed player has."
                : "A confirmed player rejects an access request; a single rejection rejects it.")
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

    public static void MapActivityEndpoints(this WebApplication app) =>
        app.MapGet("/api/activity", async (ClaimsPrincipal user, IMatchAccessService access, CancellationToken cancellationToken) =>
                TypedResults.Ok(ActivityResponse.From(await access.GetActivityAsync(GetPlayerId(user), cancellationToken))))
            .RequireAuthorization()
            .WithName("GetActivity")
            .WithSummary("The player's activity: access requests waiting for their vote, and their own requests.");

    private static Guid GetPlayerId(ClaimsPrincipal user) =>
        Guid.Parse(user.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? throw new InvalidOperationException("The session token has no 'sub' claim."));
}
