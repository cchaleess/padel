using Microsoft.EntityFrameworkCore;
using PadelMatch.Application.Players;
using PadelMatch.Domain.Players;

namespace PadelMatch.Infrastructure.Persistence;

internal sealed class PlayerRepository(PadelMatchDbContext dbContext) : IPlayerRepository
{
    public Task<Player?> FindByExternalIdentityAsync(AuthProvider provider, string providerSubjectId, CancellationToken cancellationToken) =>
        dbContext.Players
            .FirstOrDefaultAsync(
                p => p.ExternalIdentities.Any(i => i.Provider == provider && i.ProviderSubjectId == providerSubjectId),
                cancellationToken);

    public Task<Player?> FindByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Players.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task AddAsync(Player player, CancellationToken cancellationToken) =>
        await dbContext.Players.AddAsync(player, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
