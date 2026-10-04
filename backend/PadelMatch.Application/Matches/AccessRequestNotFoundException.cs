namespace PadelMatch.Application.Matches;

/// <summary>No access request for that player in this match.</summary>
public sealed class AccessRequestNotFoundException : Exception
{
    public AccessRequestNotFoundException() : base("No existe esa solicitud de acceso.")
    {
    }
}
