namespace PadelMatch.Application.Matches;

public sealed class CourtSlotUnavailableException : Exception
{
    /// <summary>Fast-path: the slot was already not <c>Available</c> when checked.</summary>
    public CourtSlotUnavailableException(Guid courtSlotId) : base($"CourtSlot '{courtSlotId}' is not available.")
    {
    }

    /// <summary>Concurrency backstop: a concurrent request won the race on the unique CourtSlotId index
    /// (design.md, "Exclusividad del CourtSlot bajo concurrencia"). The losing request has no slot state to report.</summary>
    public CourtSlotUnavailableException() : base("CourtSlot is not available.")
    {
    }
}
