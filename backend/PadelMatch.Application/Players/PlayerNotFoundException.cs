namespace PadelMatch.Application.Players;

public sealed class PlayerNotFoundException(Guid playerId) : Exception($"Player '{playerId}' was not found.");
