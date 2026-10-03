namespace PadelMatch.Application.Matches;

public sealed class CourtSlotNotFoundException(Guid courtSlotId) : Exception($"CourtSlot '{courtSlotId}' was not found.");
