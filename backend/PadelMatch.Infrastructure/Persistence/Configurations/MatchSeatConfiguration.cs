using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PadelMatch.Domain.Matches;
using PadelMatch.Domain.Players;

namespace PadelMatch.Infrastructure.Persistence.Configurations;

internal sealed class MatchSeatConfiguration : IEntityTypeConfiguration<MatchSeat>
{
    public void Configure(EntityTypeBuilder<MatchSeat> builder)
    {
        builder.ToTable("MatchSeats");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Status).HasConversion<string>().HasMaxLength(20);

        builder.HasOne<Match>().WithMany().HasForeignKey(s => s.MatchId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Player>().WithMany().HasForeignKey(s => s.HolderId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(s => s.MatchId);

        // Each of the 4 positions (pair A: 0–1, pair B: 2–3) exists exactly once per match.
        builder.HasIndex(s => new { s.MatchId, s.Position }).IsUnique();

        // Backstop against a player ending up with two active seats in the same match under concurrency
        // (design.md, "Backstop de concurrencia: índice único parcial").
        builder.HasIndex(s => new { s.MatchId, s.HolderId })
            .IsUnique()
            .HasFilter("\"HolderId\" IS NOT NULL");
    }
}
