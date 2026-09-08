namespace PadelMatch.Domain.Players;

public enum YearsPlayingPadel
{
    LessThanOne = 1,
    OneToTwo = 2,
    ThreeToFive = 3,
    MoreThanFive = 4
}

public enum WeeklyFrequency
{
    Rarely = 1,
    OnceAWeek = 2,
    TwoOrThreeTimesAWeek = 3,
    FourOrMoreTimesAWeek = 4
}

public enum SelfPerceivedLevel
{
    Beginner = 1,
    Intermediate = 2,
    Advanced = 3,
    Competitive = 4
}

public sealed record LevelSurveyAnswers(
    YearsPlayingPadel YearsPlaying,
    WeeklyFrequency WeeklyFrequency,
    SelfPerceivedLevel SelfPerceivedLevel);
