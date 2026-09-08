using PadelMatch.Domain.Players;

namespace PadelMatch.Application.Players;

public interface IPlayerRepository
{
    Task<Player?> FindByExternalIdentityAsync(AuthProvider provider, string providerSubjectId, CancellationToken cancellationToken);
    Task<Player?> FindByIdAsync(Guid id, CancellationToken cancellationToken);
    Task AddAsync(Player player, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
