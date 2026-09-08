using PadelMatch.Domain.Players;

namespace PadelMatch.Application.Players;

public interface IPlayerProfileService
{
    /// <exception cref="PlayerNotFoundException" />
    Task<Player> GetProfileAsync(Guid playerId, CancellationToken cancellationToken);

    /// <exception cref="PlayerNotFoundException" />
    Task<Player> UpdateProfileAsync(
        Guid playerId, string? cityOrZone, DateOnly? dateOfBirth, string? photoUrl, CancellationToken cancellationToken);

    /// <exception cref="PlayerNotFoundException" />
    /// <exception cref="PlayerProfileIncompleteException">DateOfBirth is not set yet.</exception>
    Task<Player> CompleteLevelSurveyAsync(Guid playerId, LevelSurveyAnswers answers, CancellationToken cancellationToken);
}
