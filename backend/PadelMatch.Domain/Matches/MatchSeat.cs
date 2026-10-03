namespace PadelMatch.Domain.Matches;

/// <summary>One of a Match's 4 seats (design.md, "Por qué MatchSeat, no plaza ni MatchSlot" — distinct from
/// CourtSlot, the court's time slot). Deliberately has no Hold/Confirm/Release mutators: those are atomic,
/// conditional claims implemented as ExecuteUpdateAsync in MatchSeatRepository, not load-then-save transitions
/// (design.md, "Las tres operaciones").</summary>
public sealed class MatchSeat
{
    public Guid Id { get; private set; }
    public Guid MatchId { get; private set; }
    public SeatStatus Status { get; private set; }
    public Guid? HolderId { get; private set; }
    public DateTimeOffset? HeldUntilUtc { get; private set; }

    private MatchSeat()
    {
    }

    public static MatchSeat CreateAvailable(Guid matchId) => new()
    {
        Id = Guid.NewGuid(),
        MatchId = matchId,
        Status = SeatStatus.Available,
        HolderId = null,
        HeldUntilUtc = null
    };
}
