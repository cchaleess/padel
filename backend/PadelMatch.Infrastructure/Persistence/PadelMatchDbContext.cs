using Microsoft.EntityFrameworkCore;
using PadelMatch.Domain.Clubs;
using PadelMatch.Domain.Players;

namespace PadelMatch.Infrastructure.Persistence;

public sealed class PadelMatchDbContext(DbContextOptions<PadelMatchDbContext> options)
    : DbContext(options)
{
    public DbSet<Player> Players => Set<Player>();
    public DbSet<PlayerExternalIdentity> PlayerExternalIdentities => Set<PlayerExternalIdentity>();
    public DbSet<Club> Clubs => Set<Club>();
    public DbSet<Court> Courts => Set<Court>();
    public DbSet<CourtSlot> CourtSlots => Set<CourtSlot>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PadelMatchDbContext).Assembly);
    }
}
