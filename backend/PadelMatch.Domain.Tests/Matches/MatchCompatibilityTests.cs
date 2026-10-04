using PadelMatch.Domain.Matches;
// MatchType also exists in System.IO (implicit usings); alias resolves the ambiguity.
using MatchType = PadelMatch.Domain.Matches.MatchType;

namespace PadelMatch.Domain.Tests.Matches;

public class MatchCompatibilityTests
{
    private static readonly DateTimeOffset NowUtc = new(2026, 9, 27, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void FriendlyHasNoCriteria()
    {
        var match = Match.CreateFriendly(Guid.NewGuid(), Guid.NewGuid(), note: null, NowUtc);

        Assert.Empty(MatchCompatibility.GetShortfalls(match, playerLevel: null, matchesPlayed: 0));
        Assert.True(MatchCompatibility.CanJoinDirectly(match, playerLevel: 1.0m, matchesPlayed: 0));
    }

    [Theory]
    [InlineData(3.0)]
    [InlineData(4.0)]
    public void CompetitiveAcceptsLevelsWithinTheRangeInclusive(double level)
    {
        var match = Competitive(minMatchesRequired: null);

        Assert.True(MatchCompatibility.CanJoinDirectly(match, (decimal)level, matchesPlayed: 0));
    }

    [Theory]
    [InlineData(2.9, AccessShortfall.LevelBelowRange)]
    [InlineData(4.1, AccessShortfall.LevelAboveRange)]
    public void CompetitiveReportsWhichSideOfTheRangeTheLevelFalls(double level, AccessShortfall expected)
    {
        var match = Competitive(minMatchesRequired: null);

        Assert.Equal([expected], MatchCompatibility.GetShortfalls(match, (decimal)level, matchesPlayed: 0));
    }

    [Fact]
    public void CompetitiveRequiresALevel()
    {
        var match = Competitive(minMatchesRequired: null);

        Assert.Equal([AccessShortfall.NoLevel], MatchCompatibility.GetShortfalls(match, playerLevel: null, matchesPlayed: 0));
    }

    [Fact]
    public void CompetitiveRequiresTheMinimumMatchesPlayed()
    {
        var match = Competitive(minMatchesRequired: 3);

        Assert.Equal([AccessShortfall.NotEnoughMatches], MatchCompatibility.GetShortfalls(match, 3.5m, matchesPlayed: 2));
        Assert.True(MatchCompatibility.CanJoinDirectly(match, 3.5m, matchesPlayed: 3));
    }

    [Fact]
    public void AllUnmetCriteriaAreReported()
    {
        var match = Competitive(minMatchesRequired: 3);

        Assert.Equal(
            [AccessShortfall.NoLevel, AccessShortfall.NotEnoughMatches],
            MatchCompatibility.GetShortfalls(match, playerLevel: null, matchesPlayed: 0));
    }

    private static Match Competitive(int? minMatchesRequired) => Match.CreateCompetitive(
        Guid.NewGuid(), Guid.NewGuid(), organizerLevelAtCreation: 3.5m,
        minLevel: 3.0m, maxLevel: 4.0m, minMatchesRequired, note: null, NowUtc);
}
