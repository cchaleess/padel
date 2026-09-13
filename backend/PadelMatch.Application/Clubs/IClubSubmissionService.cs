using PadelMatch.Domain.Clubs;

namespace PadelMatch.Application.Clubs;

public interface IClubSubmissionService
{
    /// <exception cref="ArgumentException">Name or address is missing.</exception>
    Task<Club> SubmitClubAsync(
        Guid submittedByPlayerId, string name, string address, string? cityOrZone, CancellationToken cancellationToken);
}
