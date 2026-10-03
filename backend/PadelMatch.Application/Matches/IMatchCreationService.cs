using PadelMatch.Domain.Matches;
// MatchType also exists in System.IO (implicit usings); alias resolves the ambiguity.
using MatchType = PadelMatch.Domain.Matches.MatchType;

namespace PadelMatch.Application.Matches;

public interface IMatchCreationService
{
    /// <exception cref="CourtSlotNotFoundException">The CourtSlot doesn't exist.</exception>
    /// <exception cref="CourtSlotUnavailableException">The CourtSlot is not Available, including under concurrency.</exception>
    /// <exception cref="ArgumentException">Domain validation failed: missing organizer level or range for a
    /// Competitive match, invalid range, negative MinMatchesRequired, or a note over 500 characters.</exception>
    Task<Match> CreateMatchAsync(
        Guid organizerId,
        Guid courtSlotId,
        MatchType type,
        decimal? minLevel,
        decimal? maxLevel,
        int? minMatchesRequired,
        string? note,
        CancellationToken cancellationToken);
}
