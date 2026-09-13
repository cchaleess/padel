namespace PadelMatch.Domain.Clubs;

public sealed class Club
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = null!;
    public string Address { get; private set; } = null!;
    public string? CityOrZone { get; private set; }
    public double? Latitude { get; private set; }
    public double? Longitude { get; private set; }
    public ClubStatus Status { get; private set; }
    public Guid? SubmittedByPlayerId { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }

    private Club()
    {
    }

    /// <summary>Official club, seeded for development. Not exposed through any endpoint.</summary>
    public static Club Create(
        string name, string address, string? cityOrZone, double? latitude, double? longitude, DateTimeOffset nowUtc)
    {
        RequireNameAndAddress(name, address);

        return new Club
        {
            Id = Guid.NewGuid(),
            Name = name,
            Address = address,
            CityOrZone = cityOrZone,
            Latitude = latitude,
            Longitude = longitude,
            Status = ClubStatus.Official,
            SubmittedByPlayerId = null,
            CreatedAtUtc = nowUtc
        };
    }

    /// <summary>Club contributed by a player. Never has coordinates: the contributing player doesn't know them.</summary>
    public static Club SubmitByPlayer(string name, string address, string? cityOrZone, Guid submittedByPlayerId, DateTimeOffset nowUtc)
    {
        RequireNameAndAddress(name, address);

        return new Club
        {
            Id = Guid.NewGuid(),
            Name = name,
            Address = address,
            CityOrZone = cityOrZone,
            Latitude = null,
            Longitude = null,
            Status = ClubStatus.UserSubmitted,
            SubmittedByPlayerId = submittedByPlayerId,
            CreatedAtUtc = nowUtc
        };
    }

    private static void RequireNameAndAddress(string name, string address)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name is required.", nameof(name));
        }

        if (string.IsNullOrWhiteSpace(address))
        {
            throw new ArgumentException("Address is required.", nameof(address));
        }
    }
}
