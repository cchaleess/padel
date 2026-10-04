namespace PadelMatch.Domain.Matches;

/// <summary>Why a player can't join a competitive match directly (plan §16). An empty list means they can.</summary>
public enum AccessShortfall
{
    NoLevel,
    LevelBelowRange,
    LevelAboveRange,
    NotEnoughMatches
}

/// <summary>Whether a player meets a Match's quality criteria (m6-quality-rules): level within the range and
/// enough matches played. Used by the feed ("for you" vs "out of range") and by the join flow, which demands an
/// approved exception request when this isn't met.</summary>
public static class MatchCompatibility
{
    /// <summary>Every unmet criterion, not just the first, so the player can be told all of them. Always empty for
    /// friendly matches.</summary>
    public static IReadOnlyList<AccessShortfall> GetShortfalls(Match match, decimal? playerLevel, int matchesPlayed)
    {
        if (match.Type == MatchType.Friendly)
        {
            return [];
        }

        var shortfalls = new List<AccessShortfall>();
        if (playerLevel is not { } level)
        {
            shortfalls.Add(AccessShortfall.NoLevel);
        }
        else if (level < match.MinLevel)
        {
            shortfalls.Add(AccessShortfall.LevelBelowRange);
        }
        else if (level > match.MaxLevel)
        {
            shortfalls.Add(AccessShortfall.LevelAboveRange);
        }

        if (matchesPlayed < (match.MinMatchesRequired ?? 0))
        {
            shortfalls.Add(AccessShortfall.NotEnoughMatches);
        }

        return shortfalls;
    }

    public static bool CanJoinDirectly(Match match, decimal? playerLevel, int matchesPlayed) =>
        GetShortfalls(match, playerLevel, matchesPlayed).Count == 0;
}
