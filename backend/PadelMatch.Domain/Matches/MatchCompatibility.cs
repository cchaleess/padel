namespace PadelMatch.Domain.Matches;

/// <summary>Whether a player's level makes a Match a real candidate to join. Used by the M4 discovery feed to
/// decide "for you" vs "out of range", and meant to be reused by the M5 join flow for the same check.</summary>
public static class MatchCompatibility
{
    public static bool IsCompatibleWithLevel(Match match, decimal? playerLevel) =>
        match.Type == MatchType.Friendly
        || (playerLevel is { } level && match.MinLevel <= level && level <= match.MaxLevel);
}
