namespace PadelMatch.Api.Tests.TestSupport;

/// <summary>Minimal controllable clock for tests that need to assert on time-based expiry (M5's 5-minute
/// Held window) without a real wait. Starts at the real current time so unrelated date comparisons (e.g.
/// CourtSlot.StartsAt seeded relative to "now") still make sense.</summary>
public sealed class MutableTimeProvider : TimeProvider
{
    private DateTimeOffset utcNow = DateTimeOffset.UtcNow;

    public override DateTimeOffset GetUtcNow() => utcNow;

    public void Advance(TimeSpan by) => utcNow += by;
}
