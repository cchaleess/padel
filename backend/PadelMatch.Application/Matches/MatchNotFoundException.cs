namespace PadelMatch.Application.Matches;

public sealed class MatchNotFoundException(Guid matchId) : Exception($"Match '{matchId}' was not found.");
