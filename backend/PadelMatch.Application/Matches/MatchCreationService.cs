using PadelMatch.Application.Clubs;
using PadelMatch.Application.Players;
using PadelMatch.Domain.Clubs;
using PadelMatch.Domain.Matches;
// MatchType also exists in System.IO (implicit usings); alias resolves the ambiguity.
using MatchType = PadelMatch.Domain.Matches.MatchType;

namespace PadelMatch.Application.Matches;

public sealed class MatchCreationService(
    IMatchRepository matchRepository, IMatchSeatRepository seatRepository, IClubRepository clubRepository,
    IPlayerRepository playerRepository, TimeProvider clock)
    : IMatchCreationService
{
    public async Task<Match> CreateMatchAsync(
        Guid organizerId,
        Guid courtSlotId,
        MatchType type,
        decimal? minLevel,
        decimal? maxLevel,
        int? minMatchesRequired,
        string? note,
        CancellationToken cancellationToken)
    {
        // Domain validation (including the organizer's score) happens before touching the CourtSlot,
        // so a request that was always going to fail never blocks the slot (design.md, "API y contratos").
        var match = type == MatchType.Competitive
            ? Match.CreateCompetitive(
                courtSlotId,
                organizerId,
                await RequireOrganizerLevelAsync(organizerId, cancellationToken),
                RequireLevel(minLevel, nameof(minLevel)),
                RequireLevel(maxLevel, nameof(maxLevel)),
                minMatchesRequired,
                note,
                clock.GetUtcNow())
            : Match.CreateFriendly(courtSlotId, organizerId, note, clock.GetUtcNow());

        var slot = await clubRepository.FindSlotByIdAsync(courtSlotId, cancellationToken)
            ?? throw new CourtSlotNotFoundException(courtSlotId);

        if (slot.Status != SlotStatus.Available)
        {
            throw new CourtSlotUnavailableException(courtSlotId);
        }

        slot.Book();

        await matchRepository.AddMatchAsync(match, cancellationToken);
        // 4 seats, all Available, created with the Match itself (design.md, "MatchCreationService crea las 4
        // plazas"): a match always has exactly 4 seats from the moment it exists, none created later.
        foreach (var _ in Enumerable.Range(0, 4))
        {
            await seatRepository.AddSeatAsync(MatchSeat.CreateAvailable(match.Id), cancellationToken);
        }
        await matchRepository.SaveChangesAsync(cancellationToken);

        return match;
    }

    private async Task<decimal> RequireOrganizerLevelAsync(Guid organizerId, CancellationToken cancellationToken)
    {
        var organizer = await playerRepository.FindByIdAsync(organizerId, cancellationToken);
        return organizer?.Level
            ?? throw new ArgumentException("A score is required to create a competitive match.", nameof(organizerId));
    }

    private static decimal RequireLevel(decimal? level, string paramName) =>
        level ?? throw new ArgumentException("MinLevel and MaxLevel are required for a competitive match.", paramName);
}
