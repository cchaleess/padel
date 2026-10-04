namespace PadelMatch.Application.Matches;

/// <summary>The match is full or has already started.</summary>
public sealed class AccessRequestsClosedException : Exception
{
    public AccessRequestsClosedException() : base("Este partido ya no admite solicitudes.")
    {
    }
}
