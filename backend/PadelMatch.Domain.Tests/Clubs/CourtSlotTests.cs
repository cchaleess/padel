using PadelMatch.Domain.Clubs;

namespace PadelMatch.Domain.Tests.Clubs;

public class CourtSlotTests
{
    [Theory]
    [InlineData(SlotDuration.SixtyMinutes)]
    [InlineData(SlotDuration.NinetyMinutes)]
    [InlineData(SlotDuration.OneTwentyMinutes)]
    public void CreateComputesEndsAtFromDuration(SlotDuration duration)
    {
        var startsAt = new DateTimeOffset(2026, 9, 20, 10, 0, 0, TimeSpan.Zero);

        var slot = CourtSlot.Create(Guid.NewGuid(), startsAt, duration);

        Assert.Equal(startsAt.AddMinutes((int)duration), slot.EndsAt);
        Assert.Equal(SlotStatus.Available, slot.Status);
    }

    [Fact]
    public void CreateThrowsWhenDurationIsOutsideTheClosedList()
    {
        var invalidDuration = (SlotDuration)45;

        Assert.Throws<ArgumentException>(() =>
            CourtSlot.Create(Guid.NewGuid(), DateTimeOffset.UtcNow, invalidDuration));
    }
}
