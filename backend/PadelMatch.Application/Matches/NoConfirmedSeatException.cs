namespace PadelMatch.Application.Matches;

/// <summary>Leaving needs a Confirmed seat of one's own.</summary>
public sealed class NoConfirmedSeatException : Exception
{
    public NoConfirmedSeatException() : base("No tienes una plaza confirmada en este partido.")
    {
    }
}
