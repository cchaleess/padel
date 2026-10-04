namespace PadelMatch.Application.Matches;

/// <summary>The match is closed (all four seats paid): its seats can no longer be left from the app. What happens
/// then (cancellation policies) isn't decided yet (m7-leave-match).</summary>
public sealed class MatchClosedException : Exception
{
    public MatchClosedException() : base("El partido está completo: ya no se puede abandonar.")
    {
    }
}
