namespace PadelMatch.Application.Players;

public sealed class InvalidExternalIdentityException(string message, Exception? innerException = null)
    : Exception(message, innerException);
