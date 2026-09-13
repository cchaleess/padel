using PadelMatch.Domain.Clubs;

namespace PadelMatch.Application.Clubs;

public sealed class ClubSubmissionService(IClubRepository repository, TimeProvider clock) : IClubSubmissionService
{
    public async Task<Club> SubmitClubAsync(
        Guid submittedByPlayerId, string name, string address, string? cityOrZone, CancellationToken cancellationToken)
    {
        var club = Club.SubmitByPlayer(name, address, cityOrZone, submittedByPlayerId, clock.GetUtcNow());
        await repository.AddClubAsync(club, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return club;
    }
}
