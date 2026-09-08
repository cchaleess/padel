using Microsoft.EntityFrameworkCore;
using PadelMatch.Domain.Players;

namespace PadelMatch.Infrastructure.Persistence;

public sealed class PadelMatchDbContext(DbContextOptions<PadelMatchDbContext> options)
    : DbContext(options)
{
    public DbSet<Player> Players => Set<Player>();
    public DbSet<PlayerExternalIdentity> PlayerExternalIdentities => Set<PlayerExternalIdentity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PadelMatchDbContext).Assembly);
    }
}
