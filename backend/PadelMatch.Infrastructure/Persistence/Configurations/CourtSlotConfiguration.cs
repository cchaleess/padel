using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PadelMatch.Domain.Clubs;

namespace PadelMatch.Infrastructure.Persistence.Configurations;

internal sealed class CourtSlotConfiguration : IEntityTypeConfiguration<CourtSlot>
{
    public void Configure(EntityTypeBuilder<CourtSlot> builder)
    {
        builder.ToTable("CourtSlots");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Duration).HasConversion<string>().HasMaxLength(20);
        builder.Property(s => s.Status).HasConversion<string>().HasMaxLength(20);

        builder.HasOne<Court>().WithMany().HasForeignKey(s => s.CourtId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(s => new { s.CourtId, s.StartsAt });
    }
}
