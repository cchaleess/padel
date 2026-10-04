namespace PadelMatch.Application.Matches;

/// <summary>Leaving a seat is only possible before the match starts (plan §23).</summary>
public sealed class MatchAlreadyStartedException : Exception
{
    public MatchAlreadyStartedException() : base("El partido ya ha empezado.")
    {
    }
}
