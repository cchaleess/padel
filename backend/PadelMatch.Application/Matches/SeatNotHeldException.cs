namespace PadelMatch.Application.Matches;

/// <summary>The caller has no seat Held in this match, or it already expired/isn't theirs — thrown when
/// TryConfirmAsync/TryReleaseAsync affect 0 rows (design.md).</summary>
public sealed class SeatNotHeldException : Exception
{
    public SeatNotHeldException() : base("No tienes ninguna plaza retenida en este partido.")
    {
    }
}
