namespace PadelMatch.Application.Matches;

/// <summary>Only players with a Confirmed seat in the match vote on its access requests (plan §16).</summary>
public sealed class NotAConfirmedPlayerException : Exception
{
    public NotAConfirmedPlayerException() : base("Solo los jugadores confirmados pueden votar.")
    {
    }
}
