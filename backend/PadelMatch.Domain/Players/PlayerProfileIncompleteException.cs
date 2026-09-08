namespace PadelMatch.Domain.Players;

public sealed class PlayerProfileIncompleteException(string message) : Exception(message);
