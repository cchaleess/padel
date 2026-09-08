using PadelMatch.Domain.Players;

namespace PadelMatch.Application.Players;

public sealed class PlayerProfileService(IPlayerRepository repository) : IPlayerProfileService
{
    public async Task<Player> GetProfileAsync(Guid playerId, CancellationToken cancellationToken) =>
        await repository.FindByIdAsync(playerId, cancellationToken) ?? throw new PlayerNotFoundException(playerId);

    public async Task<Player> UpdateProfileAsync(
        Guid playerId, string? cityOrZone, DateOnly? dateOfBirth, string? photoUrl, CancellationToken cancellationToken)
    {
        var player = await repository.FindByIdAsync(playerId, cancellationToken) ?? throw new PlayerNotFoundException(playerId);
        player.UpdateProfile(cityOrZone, dateOfBirth, photoUrl);
        await repository.SaveChangesAsync(cancellationToken);
        return player;
    }

    public async Task<Player> CompleteLevelSurveyAsync(Guid playerId, LevelSurveyAnswers answers, CancellationToken cancellationToken)
    {
        var player = await repository.FindByIdAsync(playerId, cancellationToken) ?? throw new PlayerNotFoundException(playerId);
        player.CompleteLevelSurvey(answers);
        await repository.SaveChangesAsync(cancellationToken);
        return player;
    }
}
