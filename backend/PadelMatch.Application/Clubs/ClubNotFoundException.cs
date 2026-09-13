namespace PadelMatch.Application.Clubs;

public sealed class ClubNotFoundException(Guid clubId) : Exception($"Club '{clubId}' was not found.");
