using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PadelMatch.Domain.Matches;
using PadelMatch.Domain.Players;

namespace PadelMatch.Infrastructure.Persistence.Configurations;

internal sealed class MatchAccessRequestConfiguration : IEntityTypeConfiguration<MatchAccessRequest>
{
    public void Configure(EntityTypeBuilder<MatchAccessRequest> builder)
    {
        builder.ToTable("MatchAccessRequests");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Status).HasConversion<string>().HasMaxLength(20);

        builder.HasOne<Match>().WithMany().HasForeignKey(r => r.MatchId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Player>().WithMany().HasForeignKey(r => r.PlayerId).OnDelete(DeleteBehavior.Restrict);

        // One request per player and match: a rejection is final (m6-quality-rules proposal), and this is the
        // backstop when two requests from the same player race past the "already requested" check.
        builder.HasIndex(r => new { r.MatchId, r.PlayerId }).IsUnique();
    }
}

internal sealed class MatchAccessVoteConfiguration : IEntityTypeConfiguration<MatchAccessVote>
{
    public void Configure(EntityTypeBuilder<MatchAccessVote> builder)
    {
        builder.ToTable("MatchAccessVotes");
        builder.HasKey(v => v.Id);

        builder.HasOne<MatchAccessRequest>().WithMany().HasForeignKey(v => v.RequestId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Player>().WithMany().HasForeignKey(v => v.VoterId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(v => new { v.RequestId, v.VoterId }).IsUnique();
    }
}
