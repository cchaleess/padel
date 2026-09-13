using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PadelMatch.Domain.Clubs;

namespace PadelMatch.Infrastructure.Persistence.Configurations;

internal sealed class CourtConfiguration : IEntityTypeConfiguration<Court>
{
    public void Configure(EntityTypeBuilder<Court> builder)
    {
        builder.ToTable("Courts");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name).IsRequired().HasMaxLength(100);

        builder.HasOne<Club>().WithMany().HasForeignKey(c => c.ClubId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(c => c.ClubId);
    }
}
