using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PadelMatch.Domain.Players;

namespace PadelMatch.Infrastructure.Persistence.Configurations;

internal sealed class PlayerConfiguration : IEntityTypeConfiguration<Player>
{
    public void Configure(EntityTypeBuilder<Player> builder)
    {
        builder.ToTable("Players");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.DisplayName).IsRequired().HasMaxLength(200);
        builder.Property(p => p.Email).HasMaxLength(320);
        builder.Property(p => p.CityOrZone).HasMaxLength(200);
        builder.Property(p => p.PhotoUrl).HasMaxLength(2048);
        builder.Property(p => p.DateOfBirth).HasColumnType("date");
        builder.Property(p => p.Level).HasColumnType("numeric(3,1)");
        builder.Property(p => p.LevelConfidence).HasConversion<string>().HasMaxLength(20);
        builder.Property(p => p.CreatedAtUtc);

        builder.HasMany(p => p.ExternalIdentities)
            .WithOne()
            .HasForeignKey(i => i.PlayerId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(p => p.ExternalIdentities).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
