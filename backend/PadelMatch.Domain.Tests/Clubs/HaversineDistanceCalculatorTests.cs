using PadelMatch.Domain.Clubs;

namespace PadelMatch.Domain.Tests.Clubs;

public class HaversineDistanceCalculatorTests
{
    [Fact]
    public void DistanceKmIsZeroForTheSamePoint()
    {
        var distance = HaversineDistanceCalculator.DistanceKm(40.4168, -3.7038, 40.4168, -3.7038);

        Assert.Equal(0, distance, precision: 6);
    }

    [Fact]
    public void DistanceKmMatchesKnownMadridToBarcelonaDistance()
    {
        // Madrid (40.4168, -3.7038) to Barcelona (41.3851, 2.1734): ~504 km great-circle distance.
        var distance = HaversineDistanceCalculator.DistanceKm(40.4168, -3.7038, 41.3851, 2.1734);

        Assert.InRange(distance, 495, 515);
    }
}
