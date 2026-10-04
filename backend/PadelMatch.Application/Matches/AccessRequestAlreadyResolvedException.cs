namespace PadelMatch.Application.Matches;

/// <summary>Votes only apply to a Pending request.</summary>
public sealed class AccessRequestAlreadyResolvedException : Exception
{
    public AccessRequestAlreadyResolvedException() : base("Esta solicitud ya está resuelta.")
    {
    }
}
