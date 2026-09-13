using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using PadelMatch.Application.Clubs;

namespace PadelMatch.Api.Clubs;

public static class ClubEndpoints
{
    public static void MapClubEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/clubs").RequireAuthorization();

        group.MapGet("/nearby", async (
                double? lat, double? lng, string? cityOrZone,
                ClaimsPrincipal user, IClubDiscoveryService discovery, CancellationToken cancellationToken) =>
            {
                var clubs = await discovery.GetNearbyClubsAsync(GetPlayerId(user), lat, lng, cityOrZone, cancellationToken);
                return TypedResults.Ok(clubs.Select(ClubSummaryResponse.From).ToList());
            })
            .WithName("GetNearbyClubs")
            .WithSummary("Lists clubs ordered by proximity, falling back to city/zone or name when there's no location signal.");

        group.MapGet("/search", async (
                string q, IClubDiscoveryService discovery, CancellationToken cancellationToken) =>
            {
                var clubs = await discovery.SearchClubsAsync(q, cancellationToken);
                return TypedResults.Ok(clubs.Select(ClubSummaryResponse.From).ToList());
            })
            .WithName("SearchClubs")
            .WithSummary("Searches clubs by (partial) name, official and user-submitted alike.");

        group.MapGet("/{id:guid}", async Task<Results<Ok<ClubDetailResponse>, ProblemHttpResult>> (
                Guid id, IClubDiscoveryService discovery, CancellationToken cancellationToken) =>
            {
                try
                {
                    var details = await discovery.GetClubDetailsAsync(id, cancellationToken);
                    return TypedResults.Ok(ClubDetailResponse.From(details));
                }
                catch (ClubNotFoundException ex)
                {
                    return TypedResults.Problem(statusCode: StatusCodes.Status404NotFound, title: ex.Message);
                }
            })
            .WithName("GetClubDetails")
            .WithSummary("Returns a club's detail, including its courts.")
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/{id:guid}/slots", async Task<Results<Ok<List<CourtSlotResponse>>, ProblemHttpResult>> (
                Guid id, Guid? courtId, DateTimeOffset? from, DateTimeOffset? to,
                IClubDiscoveryService discovery, CancellationToken cancellationToken) =>
            {
                try
                {
                    var slots = await discovery.GetCourtSlotsAsync(id, courtId, from, to, cancellationToken);
                    return TypedResults.Ok(slots.Select(CourtSlotResponse.From).ToList());
                }
                catch (ClubNotFoundException ex)
                {
                    return TypedResults.Problem(statusCode: StatusCodes.Status404NotFound, title: ex.Message);
                }
            })
            .WithName("GetClubCourtSlots")
            .WithSummary("Lists available court slots for a club, optionally filtered by court and date range.")
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("", async Task<Results<Created<ClubDetailResponse>, ProblemHttpResult>> (
                SubmitClubRequest request, ClaimsPrincipal user, IClubSubmissionService submission, CancellationToken cancellationToken) =>
            {
                try
                {
                    var club = await submission.SubmitClubAsync(
                        GetPlayerId(user), request.Name, request.Address, request.CityOrZone, cancellationToken);
                    var response = ClubDetailResponse.From(new ClubDetails(club, []));
                    return TypedResults.Created($"/api/clubs/{club.Id}", response);
                }
                catch (ArgumentException ex)
                {
                    return TypedResults.Problem(statusCode: StatusCodes.Status400BadRequest, title: ex.Message);
                }
            })
            .WithName("SubmitClub")
            .WithSummary("Lets an authenticated player contribute a new, unverified club.")
            .ProducesProblem(StatusCodes.Status400BadRequest);
    }

    private static Guid GetPlayerId(ClaimsPrincipal user) =>
        Guid.Parse(user.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? throw new InvalidOperationException("The session token has no 'sub' claim."));
}
