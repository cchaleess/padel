namespace PadelMatch.Application.Matches;

/// <summary>A voter can repeat their vote (idempotent) but not change it.</summary>
public sealed class VoteAlreadyCastException : Exception
{
    public VoteAlreadyCastException() : base("Ya has votado esta solicitud.")
    {
    }
}
