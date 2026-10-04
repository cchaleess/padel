namespace PadelMatch.Application.Matches;

/// <summary>One request per player and match; a rejection is final (m6-quality-rules proposal).</summary>
public sealed class AccessAlreadyRequestedException : Exception
{
    public AccessAlreadyRequestedException() : base("Ya has solicitado acceso a este partido.")
    {
    }
}
