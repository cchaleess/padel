using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PadelMatch.Domain.Clubs;
using PadelMatch.Domain.Matches;
using PadelMatch.Domain.Players;

namespace PadelMatch.Infrastructure.Persistence.Configurations;

internal sealed class MatchConfiguration : IEntityTypeConfiguration<Match>
{
    public void Configure(EntityTypeBuilder<Match> builder)
    {
        builder.ToTable("Matches");
        builder.HasKey(m => m.Id);

        builder.Property(m => m.Type).HasConversion<string>().HasMaxLength(20);
        builder.Property(m => m.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(m => m.OrganizerLevelAtCreation).HasColumnType("numeric(3,1)");
        builder.Property(m => m.MinLevel).HasColumnType("numeric(3,1)");
        builder.Property(m => m.MaxLevel).HasColumnType("numeric(3,1)");
        builder.Property(m => m.Note).HasMaxLength(500);
        builder.Property(m => m.CreatedAtUtc);

        builder.HasOne<CourtSlot>().WithMany().HasForeignKey(m => m.CourtSlotId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Player>().WithMany().HasForeignKey(m => m.OrganizerId).OnDelete(DeleteBehavior.Restrict);

        // Backstop of the CourtSlot exclusivity invariant under concurrency (design.md).
        builder.HasIndex(m => m.CourtSlotId).IsUnique();
    }
}
