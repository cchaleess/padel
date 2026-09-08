namespace PadelMatch.Domain.Players;

public static class InitialLevelEstimator
{
    private const decimal MinLevel = 1.0m;
    private const decimal MaxLevel = 5.0m;
    private const int MinScore = 3;
    private const int MaxScore = 12;

    public static decimal Estimate(LevelSurveyAnswers answers)
    {
        var score = (int)answers.YearsPlaying + (int)answers.WeeklyFrequency + (int)answers.SelfPerceivedLevel;
        var normalized = MinLevel + (score - MinScore) * (MaxLevel - MinLevel) / (MaxScore - MinScore);
        return decimal.Round(normalized, 1, MidpointRounding.AwayFromZero);
    }
}
