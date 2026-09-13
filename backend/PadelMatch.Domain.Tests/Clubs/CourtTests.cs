using PadelMatch.Domain.Clubs;

namespace PadelMatch.Domain.Tests.Clubs;

public class CourtTests
{
    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void CreateThrowsWhenNameIsMissing(string name)
    {
        Assert.Throws<ArgumentException>(() => Court.Create(Guid.NewGuid(), name));
    }

    [Fact]
    public void CreateAssignsTheGivenClubId()
    {
        var clubId = Guid.NewGuid();

        var court = Court.Create(clubId, "Pista 1");

        Assert.Equal(clubId, court.ClubId);
        Assert.Equal("Pista 1", court.Name);
    }
}
