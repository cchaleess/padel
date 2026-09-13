using PadelMatch.Domain.Clubs;

namespace PadelMatch.Domain.Tests.Clubs;

public class ClubTests
{
    [Theory]
    [InlineData("", "Calle Falsa 123")]
    [InlineData(" ", "Calle Falsa 123")]
    [InlineData("Club Norte", "")]
    [InlineData("Club Norte", " ")]
    public void CreateThrowsWhenNameOrAddressIsMissing(string name, string address)
    {
        Assert.Throws<ArgumentException>(() => Club.Create(name, address, null, null, null, DateTimeOffset.UtcNow));
    }

    [Fact]
    public void CreateBuildsAnOfficialClubWithoutSubmitter()
    {
        var club = Club.Create("Club Norte", "Calle Falsa 123", "Madrid", 40.4, -3.7, DateTimeOffset.UtcNow);

        Assert.Equal(ClubStatus.Official, club.Status);
        Assert.Null(club.SubmittedByPlayerId);
        Assert.Equal(40.4, club.Latitude);
        Assert.Equal(-3.7, club.Longitude);
    }

    [Theory]
    [InlineData("", "Calle Falsa 123")]
    [InlineData("Club Norte", "")]
    public void SubmitByPlayerThrowsWhenNameOrAddressIsMissing(string name, string address)
    {
        Assert.Throws<ArgumentException>(() =>
            Club.SubmitByPlayer(name, address, null, Guid.NewGuid(), DateTimeOffset.UtcNow));
    }

    [Fact]
    public void SubmitByPlayerBuildsAnUnverifiedClubWithoutCoordinates()
    {
        var submittedBy = Guid.NewGuid();
        var club = Club.SubmitByPlayer("Club Aportado", "Calle Falsa 123", "Sevilla", submittedBy, DateTimeOffset.UtcNow);

        Assert.Equal(ClubStatus.UserSubmitted, club.Status);
        Assert.Equal(submittedBy, club.SubmittedByPlayerId);
        Assert.Null(club.Latitude);
        Assert.Null(club.Longitude);
    }
}
