using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PadelMatch.Domain.Players;

namespace PadelMatch.Infrastructure.Persistence.Configurations;

internal sealed class PlayerExternalIdentityConfiguration : IEntityTypeConfiguration<PlayerExternalIdentity>
{
    public void Configure(EntityTypeBuilder<PlayerExternalIdentity> builder)
    {
        builder.ToTable("PlayerExternalIdentities");
        builder.HasKey(i => i.Id);

        builder.Property(i => i.Provider).HasConversion<string>().HasMaxLength(20);
        builder.Property(i => i.ProviderSubjectId).IsRequired().HasMaxLength(500);
        builder.Property(i => i.LinkedAtUtc);

        builder.HasIndex(i => new { i.Provider, i.ProviderSubjectId }).IsUnique();
    }
}
