namespace PadelMatch.Application.Matches;

/// <summary>The player already meets the match's criteria, so there is nothing to request.</summary>
public sealed class AccessNotNeededException : Exception
{
    public AccessNotNeededException() : base("Puedes unirte directamente, no necesitas solicitud.")
    {
    }
}
