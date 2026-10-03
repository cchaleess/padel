using PadelMatch.Domain.Matches;

namespace PadelMatch.Application.Matches;

public interface IMatchSeatRepository
{
    Task AddSeatAsync(MatchSeat seat, CancellationToken cancellationToken);

    /// <summary>Fast-path check (design.md): does the player already have an active (Held and not expired, or
    /// Confirmed) seat in this match?</summary>
    Task<bool> HasActiveSeatAsync(Guid matchId, Guid playerId, DateTimeOffset now, CancellationToken cancellationToken);

    /// <summary>Atomically claims one Available-or-expired-Held seat for the player. Returns false if no
    /// candidate could be claimed (all held by others and not expired, or lost the race on the last one).</summary>
    Task<bool> TryHoldAsync(Guid matchId, Guid playerId, DateTimeOffset heldUntilUtc, DateTimeOffset now, CancellationToken cancellationToken);

    /// <summary>Atomically confirms the player's own Held-and-not-expired seat. Returns false if they have no
    /// such seat (never held one, it wasn't theirs, or it expired).</summary>
    Task<bool> TryConfirmAsync(Guid matchId, Guid playerId, DateTimeOffset now, CancellationToken cancellationToken);

    /// <summary>Atomically releases the player's own Held seat back to Available. Returns false if they have no
    /// Held seat in this match.</summary>
    Task<bool> TryReleaseAsync(Guid matchId, Guid playerId, CancellationToken cancellationToken);

    Task<int> CountConfirmedAsync(Guid matchId, CancellationToken cancellationToken);
}
