namespace PadelMatch.Application.Matches;

public interface IMatchSeatService
{
    /// <exception cref="PlayerAlreadyHasSeatException"/>
    /// <exception cref="SeatUnavailableException"/>
    /// <returns>When the hold expires.</returns>
    Task<DateTimeOffset> HoldSeatAsync(Guid matchId, Guid playerId, int? position, CancellationToken cancellationToken);

    /// <exception cref="SeatNotHeldException"/>
    Task ConfirmSeatAsync(Guid matchId, Guid playerId, CancellationToken cancellationToken);

    /// <exception cref="SeatNotHeldException"/>
    Task ReleaseSeatAsync(Guid matchId, Guid playerId, CancellationToken cancellationToken);
}
