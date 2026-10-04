namespace PadelMatch.Domain.Matches;

/// <summary>One of a Match's 4 seats (design.md, "Por qué MatchSeat, no plaza ni MatchSlot" — distinct from
/// CourtSlot, the court's time slot). Deliberately has no Hold/Confirm/Release mutators: those are atomic,
/// conditional claims implemented as ExecuteUpdateAsync in MatchSeatRepository, not load-then-save transitions
/// (design.md, "Las tres operaciones").</summary>
public sealed class MatchSeat
{
    /// <summary>How long a Held seat stays reserved for its holder (plan §11).</summary>
    public static readonly TimeSpan HoldDuration = TimeSpan.FromMinutes(5);

    public const int SeatsPerMatch = 4;

    public Guid Id { get; private set; }
    public Guid MatchId { get; private set; }

    /// <summary>0–3. Padel is 2 vs 2: positions 0–1 are pair A, 2–3 pair B (m5-mobile-confirmation). The
    /// organizer's seat is position 0.</summary>
    public int Position { get; private set; }
    public SeatStatus Status { get; private set; }
    public Guid? HolderId { get; private set; }
    public DateTimeOffset? HeldUntilUtc { get; private set; }

    private MatchSeat()
    {
    }

    public static MatchSeat CreateAvailable(Guid matchId, int position) => new()
    {
        Id = Guid.NewGuid(),
        MatchId = matchId,
        Position = RequireValidPosition(position),
        Status = SeatStatus.Available,
        HolderId = null,
        HeldUntilUtc = null
    };

    /// <summary>The organizer's seat, created Held together with the Match (m5-mobile-confirmation design.md):
    /// creating starts the organizer's join, but only paying confirms it.</summary>
    public static MatchSeat CreateHeldBy(Guid matchId, int position, Guid holderId, DateTimeOffset now) => new()
    {
        Id = Guid.NewGuid(),
        MatchId = matchId,
        Position = RequireValidPosition(position),
        Status = SeatStatus.Held,
        HolderId = holderId,
        HeldUntilUtc = now + HoldDuration
    };

    public static bool IsValidPosition(int position) => position is >= 0 and < SeatsPerMatch;

    private static int RequireValidPosition(int position) =>
        IsValidPosition(position)
            ? position
            : throw new ArgumentOutOfRangeException(nameof(position), position, $"A seat position is 0–{SeatsPerMatch - 1}.");
}
