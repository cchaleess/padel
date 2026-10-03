using PadelMatch.Domain.Matches;
// MatchType also exists in System.IO (implicit usings); alias resolves the ambiguity.
using MatchType = PadelMatch.Domain.Matches.MatchType;

namespace PadelMatch.Domain.Tests.Matches;

public class MatchTests
{
    private static readonly DateTimeOffset NowUtc = new(2026, 9, 27, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void CreateFriendlyDoesNotRequireLevelOrRange()
    {
        var match = Match.CreateFriendly(Guid.NewGuid(), Guid.NewGuid(), "Partido tranquilo", NowUtc);

        Assert.Equal(MatchType.Friendly, match.Type);
        Assert.Equal(MatchStatus.Open, match.Status);
        Assert.Null(match.OrganizerLevelAtCreation);
        Assert.Null(match.MinLevel);
        Assert.Null(match.MaxLevel);
        Assert.Null(match.MinMatchesRequired);
    }

    [Fact]
    public void CreateCompetitiveSnapshotsOrganizerLevelAndRange()
    {
        var match = Match.CreateCompetitive(
            Guid.NewGuid(), Guid.NewGuid(), organizerLevelAtCreation: 3.5m,
            minLevel: 3.0m, maxLevel: 4.0m, minMatchesRequired: 5, note: null, NowUtc);

        Assert.Equal(MatchType.Competitive, match.Type);
        Assert.Equal(MatchStatus.Open, match.Status);
        Assert.Equal(3.5m, match.OrganizerLevelAtCreation);
        Assert.Equal(3.0m, match.MinLevel);
        Assert.Equal(4.0m, match.MaxLevel);
        Assert.Equal(5, match.MinMatchesRequired);
    }

    [Fact]
    public void CreateCompetitiveRejectsRangeWithMinAboveMax()
    {
        Assert.Throws<ArgumentException>(() => Match.CreateCompetitive(
            Guid.NewGuid(), Guid.NewGuid(), organizerLevelAtCreation: 3.5m,
            minLevel: 4.0m, maxLevel: 3.0m, minMatchesRequired: null, note: null, NowUtc));
    }

    [Fact]
    public void CreateCompetitiveRejectsOrganizerLevelOutsideRange()
    {
        Assert.Throws<ArgumentException>(() => Match.CreateCompetitive(
            Guid.NewGuid(), Guid.NewGuid(), organizerLevelAtCreation: 5.0m,
            minLevel: 3.0m, maxLevel: 4.0m, minMatchesRequired: null, note: null, NowUtc));
    }

    [Fact]
    public void CreateCompetitiveRejectsNegativeMinMatchesRequired()
    {
        Assert.Throws<ArgumentException>(() => Match.CreateCompetitive(
            Guid.NewGuid(), Guid.NewGuid(), organizerLevelAtCreation: 3.5m,
            minLevel: 3.0m, maxLevel: 4.0m, minMatchesRequired: -1, note: null, NowUtc));
    }

    [Fact]
    public void CreateRejectsNoteLongerThan500Characters()
    {
        var tooLong = new string('x', 501);

        Assert.Throws<ArgumentException>(() => Match.CreateFriendly(Guid.NewGuid(), Guid.NewGuid(), tooLong, NowUtc));
        Assert.Throws<ArgumentException>(() => Match.CreateCompetitive(
            Guid.NewGuid(), Guid.NewGuid(), organizerLevelAtCreation: 3.5m,
            minLevel: 3.0m, maxLevel: 4.0m, minMatchesRequired: null, note: tooLong, NowUtc));
    }
}
