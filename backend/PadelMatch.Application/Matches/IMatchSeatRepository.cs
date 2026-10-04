using PadelMatch.Domain.Matches;

namespace PadelMatch.Application.Matches;

public interface IMatchSeatRepository
{
    Task AddSeatAsync(MatchSeat seat, CancellationToken cancellationToken);

    /// <summary>Fast-path check (design.md): does the player already have an active (Held and not expired, or
    /// Confirmed) seat in this match?</summary>
    Task<bool> HasActiveSeatAsync(Guid matchId, Guid playerId, DateTimeOffset now, CancellationToken cancellationToken);

    /// <summary>Atomically claims one Available-or-expired-Held seat for the player — the one at
    /// <paramref name="position"/> if given (the player picked a pair), any otherwise. Returns false if no
    /// candidate could be claimed (taken by someone else and not expired, or lost the race on it).</summary>
    Task<bool> TryHoldAsync(
        Guid matchId, Guid playerId, int? position, DateTimeOffset heldUntilUtc, DateTimeOffset now, CancellationToken cancellationToken);

    /// <summary>Atomically confirms the player's own Held-and-not-expired seat. Returns false if they have no
    /// such seat (never held one, it wasn't theirs, or it expired).</summary>
    Task<bool> TryConfirmAsync(Guid matchId, Guid playerId, DateTimeOffset now, CancellationToken cancellationToken);

    /// <summary>Atomically releases the player's own Held seat back to Available. Returns false if they have no
    /// Held seat in this match.</summary>
    Task<bool> TryReleaseAsync(Guid matchId, Guid playerId, CancellationToken cancellationToken);

    Task<int> CountConfirmedAsync(Guid matchId, CancellationToken cancellationToken);

    /// <summary>The player's active seat in this match (same "active" rule as <see cref="HasActiveSeatAsync"/>),
    /// or null — an expired Held counts as no seat, matching what Confirm would do with it.</summary>
    Task<PlayerSeat?> FindActiveSeatAsync(Guid matchId, Guid playerId, DateTimeOffset now, CancellationToken cancellationToken);

    /// <summary>Who holds the match's Confirmed seats, with their current level (plan §15, "jugadores confirmados"
    /// and "nivel individual"). Held seats are left out: they aren't shown to other players (plan §11).</summary>
    Task<IReadOnlyList<ConfirmedPlayer>> GetConfirmedPlayersAsync(Guid matchId, CancellationToken cancellationToken);
}

public sealed record PlayerSeat(int Position, SeatStatus Status, DateTimeOffset? HeldUntilUtc);

public sealed record ConfirmedPlayer(int Position, Guid PlayerId, string DisplayName, decimal? Level);
