using PadelMatch.Domain.Players;

namespace PadelMatch.Api.Players;

public sealed record PlayerProfileResponse(
    Guid Id,
    string DisplayName,
    string? CityOrZone,
    DateOnly? DateOfBirth,
    string? PhotoUrl,
    decimal? Level,
    LevelConfidence LevelConfidence,
    int MatchesPlayed)
{
    public static PlayerProfileResponse From(Player player) => new(
        player.Id,
        player.DisplayName,
        player.CityOrZone,
        player.DateOfBirth,
        player.PhotoUrl,
        player.Level,
        player.LevelConfidence,
        player.MatchesPlayed);
}

public sealed record UpdateProfileRequest(string? CityOrZone, DateOnly? DateOfBirth, string? PhotoUrl);

public sealed record LevelSurveyRequest(
    YearsPlayingPadel YearsPlaying, WeeklyFrequency WeeklyFrequency, SelfPerceivedLevel SelfPerceivedLevel);
