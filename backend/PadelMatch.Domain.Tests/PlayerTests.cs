using PadelMatch.Domain.Players;

namespace PadelMatch.Domain.Tests;

public class PlayerTests
{
    private static Player RegisterPlayer() =>
        Player.Register("Ana", "ana@example.com", AuthProvider.Google, "subject-1", DateTimeOffset.UtcNow);

    [Fact]
    public void CompleteLevelSurveyThrowsWhenDateOfBirthIsMissing()
    {
        var player = RegisterPlayer();
        var answers = new LevelSurveyAnswers(YearsPlayingPadel.LessThanOne, WeeklyFrequency.Rarely, SelfPerceivedLevel.Beginner);

        Assert.Throws<PlayerProfileIncompleteException>(() => player.CompleteLevelSurvey(answers));
    }

    [Fact]
    public void CompleteLevelSurveySucceedsOnceDateOfBirthIsSet()
    {
        var player = RegisterPlayer();
        player.UpdateProfile(cityOrZone: null, dateOfBirth: new DateOnly(1990, 1, 1), photoUrl: null);
        var answers = new LevelSurveyAnswers(YearsPlayingPadel.LessThanOne, WeeklyFrequency.Rarely, SelfPerceivedLevel.Beginner);

        player.CompleteLevelSurvey(answers);

        Assert.Equal(1.0m, player.Level);
        Assert.Equal(LevelConfidence.Low, player.LevelConfidence);
    }
}
