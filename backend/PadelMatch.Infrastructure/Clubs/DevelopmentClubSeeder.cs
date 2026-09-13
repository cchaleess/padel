using Microsoft.EntityFrameworkCore;
using PadelMatch.Domain.Clubs;
using PadelMatch.Infrastructure.Persistence;

namespace PadelMatch.Infrastructure.Clubs;

/// <summary>Development-only fixture data for the M2 read-only catalog. Deliberately not an EF migration
/// (`HasData`): that would pin fixed keys forever, and this fixture is expected to keep changing while
/// M2/M3 are built. Idempotent (skips if any club already exists) and never runs unless the caller checks
/// both `IsDevelopment()` and `Development:SeedClubs` — see Program.cs and design.md.</summary>
public static class DevelopmentClubSeeder
{
    private static readonly int[] SlotDaysAhead = [1, 2, 3, 4, 5];
    private static readonly int[] SlotHoursUtc = [9, 17, 20];
    private static readonly SlotDuration[] Durations =
        [SlotDuration.SixtyMinutes, SlotDuration.NinetyMinutes, SlotDuration.OneTwentyMinutes];

    public static async Task SeedIfEmptyAsync(PadelMatchDbContext dbContext, TimeProvider clock, CancellationToken cancellationToken)
    {
        if (await dbContext.Clubs.AnyAsync(cancellationToken))
        {
            return;
        }

        var now = clock.GetUtcNow();

        var madrid = Club.Create("Padel Club Chamartín", "Calle Mauricio Legendre 4, Madrid", "Madrid", 40.4636, -3.6883, now);
        var barcelona = Club.Create("Can Melis Pàdel", "Carrer de la Selva 22, Barcelona", "Barcelona", 41.4036, 2.1744, now);
        await dbContext.Clubs.AddRangeAsync([madrid, barcelona], cancellationToken);

        Court[] madridCourts =
        [
            Court.Create(madrid.Id, "Pista 1"),
            Court.Create(madrid.Id, "Pista 2")
        ];
        Court[] barcelonaCourts =
        [
            Court.Create(barcelona.Id, "Pista Central"),
            Court.Create(barcelona.Id, "Pista 2"),
            Court.Create(barcelona.Id, "Pista 3")
        ];
        await dbContext.Courts.AddRangeAsync([..madridCourts, ..barcelonaCourts], cancellationToken);

        var midnightUtc = new DateTimeOffset(now.UtcDateTime.Date, TimeSpan.Zero);
        var slots = new List<CourtSlot>();
        foreach (var court in madridCourts.Concat(barcelonaCourts))
        {
            foreach (var day in SlotDaysAhead)
            {
                foreach (var hour in SlotHoursUtc)
                {
                    var startsAt = midnightUtc.AddDays(day).AddHours(hour);
                    var duration = Durations[(day + hour) % Durations.Length];
                    slots.Add(CourtSlot.Create(court.Id, startsAt, duration));
                }
            }
        }
        await dbContext.CourtSlots.AddRangeAsync(slots, cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
