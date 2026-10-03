using PadelMatch.Domain.Matches;
// MatchType also exists in System.IO (implicit usings); alias resolves the ambiguity.
using MatchType = PadelMatch.Domain.Matches.MatchType;

namespace PadelMatch.Domain.Tests.Matches;

public class MatchCompatibilityTests
{
    private static readonly DateTimeOffset NowUtc = new(2026, 9, 27, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void FriendlyIsAlwaysCompatible()
    {
        var match = Match.CreateFriendly(Guid.NewGuid(), Guid.NewGuid(), note: null, NowUtc);

        Assert.True(MatchCompatibility.IsCompatibleWithLevel(match, playerLevel: null));
        Assert.True(MatchCompatibility.IsCompatibleWithLevel(match, playerLevel: 1.0m));
    }

    [Fact]
    public void CompetitiveIsCompatibleWhenLevelFallsWithinRange()
    {
        var match = Match.CreateCompetitive(
            Guid.NewGuid(), Guid.NewGuid(), organizerLevelAtCreation: 3.5m,
            minLevel: 3.0m, maxLevel: 4.0m, minMatchesRequired: null, note: null, NowUtc);

        Assert.True(MatchCompatibility.IsCompatibleWithLevel(match, playerLevel: 3.0m));
        Assert.True(MatchCompatibility.IsCompatibleWithLevel(match, playerLevel: 4.0m));
    }

    [Fact]
    public void CompetitiveIsNotCompatibleWhenLevelFallsOutsideRange()
    {
        var match = Match.CreateCompetitive(
            Guid.NewGuid(), Guid.NewGuid(), organizerLevelAtCreation: 3.5m,
            minLevel: 3.0m, maxLevel: 4.0m, minMatchesRequired: null, note: null, NowUtc);

        Assert.False(MatchCompatibility.IsCompatibleWithLevel(match, playerLevel: 2.9m));
        Assert.False(MatchCompatibility.IsCompatibleWithLevel(match, playerLevel: 4.1m));
    }

    [Fact]
    public void CompetitiveIsNotCompatibleWithoutPlayerLevel()
    {
        var match = Match.CreateCompetitive(
            Guid.NewGuid(), Guid.NewGuid(), organizerLevelAtCreation: 3.5m,
            minLevel: 3.0m, maxLevel: 4.0m, minMatchesRequired: null, note: null, NowUtc);

        Assert.False(MatchCompatibility.IsCompatibleWithLevel(match, playerLevel: null));
    }
}
