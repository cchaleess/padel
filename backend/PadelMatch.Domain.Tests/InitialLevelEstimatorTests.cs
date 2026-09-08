using PadelMatch.Domain.Players;

namespace PadelMatch.Domain.Tests;

public class InitialLevelEstimatorTests
{
    [Fact]
    public void LowestAnswersProduceMinimumLevel()
    {
        var answers = new LevelSurveyAnswers(YearsPlayingPadel.LessThanOne, WeeklyFrequency.Rarely, SelfPerceivedLevel.Beginner);
        Assert.Equal(1.0m, InitialLevelEstimator.Estimate(answers));
    }

    [Fact]
    public void HighestAnswersProduceMaximumLevel()
    {
        var answers = new LevelSurveyAnswers(YearsPlayingPadel.MoreThanFive, WeeklyFrequency.FourOrMoreTimesAWeek, SelfPerceivedLevel.Competitive);
        Assert.Equal(5.0m, InitialLevelEstimator.Estimate(answers));
    }

    [Theory]
    [InlineData(YearsPlayingPadel.LessThanOne, WeeklyFrequency.Rarely, SelfPerceivedLevel.Beginner, 1.0)]
    [InlineData(YearsPlayingPadel.OneToTwo, WeeklyFrequency.Rarely, SelfPerceivedLevel.Beginner, 1.4)]
    [InlineData(YearsPlayingPadel.ThreeToFive, WeeklyFrequency.OnceAWeek, SelfPerceivedLevel.Intermediate, 2.8)]
    [InlineData(YearsPlayingPadel.ThreeToFive, WeeklyFrequency.TwoOrThreeTimesAWeek, SelfPerceivedLevel.Advanced, 3.7)]
    [InlineData(YearsPlayingPadel.MoreThanFive, WeeklyFrequency.TwoOrThreeTimesAWeek, SelfPerceivedLevel.Advanced, 4.1)]
    [InlineData(YearsPlayingPadel.MoreThanFive, WeeklyFrequency.FourOrMoreTimesAWeek, SelfPerceivedLevel.Competitive, 5.0)]
    public void KnownCombinationsProduceExpectedLevel(
        YearsPlayingPadel years, WeeklyFrequency frequency, SelfPerceivedLevel selfPerceived, decimal expected)
    {
        var answers = new LevelSurveyAnswers(years, frequency, selfPerceived);
        Assert.Equal(expected, InitialLevelEstimator.Estimate(answers));
    }

    [Fact]
    public void ResultIsAlwaysWithinRangeAndOnATenthStep()
    {
        foreach (var years in Enum.GetValues<YearsPlayingPadel>())
        {
            foreach (var frequency in Enum.GetValues<WeeklyFrequency>())
            {
                foreach (var selfPerceived in Enum.GetValues<SelfPerceivedLevel>())
                {
                    var level = InitialLevelEstimator.Estimate(new LevelSurveyAnswers(years, frequency, selfPerceived));
                    Assert.InRange(level, 1.0m, 5.0m);
                    Assert.Equal(0m, (level * 10m) % 1m);
                }
            }
        }
    }
}
