namespace PadelMatch.Domain.Clubs;

public sealed class CourtSlot
{
    public Guid Id { get; private set; }
    public Guid CourtId { get; private set; }
    public DateTimeOffset StartsAt { get; private set; }
    public SlotDuration Duration { get; private set; }
    public DateTimeOffset EndsAt { get; private set; }
    public SlotStatus Status { get; private set; }

    private CourtSlot()
    {
    }

    /// <summary>The only construction point, for both the development seed and any future creation flow (M3+),
    /// so the closed list of durations (plan §9) is always enforced.</summary>
    public static CourtSlot Create(Guid courtId, DateTimeOffset startsAt, SlotDuration duration)
    {
        if (!Enum.IsDefined(duration))
        {
            throw new ArgumentException("Duration must be one of the allowed slot durations.", nameof(duration));
        }

        return new CourtSlot
        {
            Id = Guid.NewGuid(),
            CourtId = courtId,
            StartsAt = startsAt,
            Duration = duration,
            EndsAt = startsAt.AddMinutes((int)duration),
            Status = SlotStatus.Available
        };
    }

    /// <summary>Marks the slot as exclusively taken by a match (M3). The fast-path guard against a non-available
    /// slot; the real invariant under concurrency is the unique index on Matches.CourtSlotId.</summary>
    public void Book()
    {
        if (Status != SlotStatus.Available)
        {
            throw new InvalidOperationException("Only an available slot can be booked.");
        }

        Status = SlotStatus.Booked;
    }
}
