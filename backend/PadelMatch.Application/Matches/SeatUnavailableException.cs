namespace PadelMatch.Application.Matches;

/// <summary>No candidate seat could be claimed: either every seat was Held/Confirmed by someone else, or a
/// concurrent request won the race on the same seat (design.md, "Backstop de concurrencia").</summary>
public sealed class SeatUnavailableException : Exception
{
    public SeatUnavailableException() : base("Esta plaza no está disponible temporalmente.")
    {
    }
}
