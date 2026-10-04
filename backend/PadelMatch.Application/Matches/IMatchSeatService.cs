namespace PadelMatch.Application.Matches;

public interface IMatchSeatService
{
    /// <exception cref="PlayerAlreadyHasSeatException"/>
    /// <exception cref="SeatUnavailableException"/>
    /// <exception cref="AccessRequiresApprovalException">Outside a competitive match's criteria without an approved
    /// exception request.</exception>
    /// <returns>When the hold expires.</returns>
    Task<DateTimeOffset> HoldSeatAsync(Guid matchId, Guid playerId, int? position, CancellationToken cancellationToken);

    /// <exception cref="SeatNotHeldException"/>
    Task ConfirmSeatAsync(Guid matchId, Guid playerId, CancellationToken cancellationToken);

    /// <summary>Gives up a Confirmed seat, before the match starts and while it isn't closed (plan §23,
    /// m7-leave-match). The seat goes back to the feed. If the organizer leaves, the role passes to the
    /// longest-confirmed player (§18).</summary>
    /// <exception cref="MatchNotFoundException"/>
    /// <exception cref="MatchAlreadyStartedException"/>
    /// <exception cref="MatchClosedException">All four seats are paid.</exception>
    /// <exception cref="NoConfirmedSeatException"/>
    Task LeaveSeatAsync(Guid matchId, Guid playerId, CancellationToken cancellationToken);

    /// <exception cref="SeatNotHeldException"/>
    Task ReleaseSeatAsync(Guid matchId, Guid playerId, CancellationToken cancellationToken);
}
