namespace PadelMatch.Domain.Matches;

public sealed class Match
{
    private const int MaxNoteLength = 500;

    public Guid Id { get; private set; }
    public Guid CourtSlotId { get; private set; }
    public Guid OrganizerId { get; private set; }
    public MatchType Type { get; private set; }
    public MatchStatus Status { get; private set; }
    public decimal? OrganizerLevelAtCreation { get; private set; }
    public decimal? MinLevel { get; private set; }
    public decimal? MaxLevel { get; private set; }
    public int? MinMatchesRequired { get; private set; }
    public string? Note { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }

    private Match()
    {
    }

    /// <summary>Friendly matches never carry a level range, minimum, or organizer snapshot (proposal.md,
    /// "Un jugador sin puntaje ... puede crear uno Friendly").</summary>
    public static Match CreateFriendly(Guid courtSlotId, Guid organizerId, string? note, DateTimeOffset nowUtc)
    {
        RequireValidNote(note);

        return new Match
        {
            Id = Guid.NewGuid(),
            CourtSlotId = courtSlotId,
            OrganizerId = organizerId,
            Type = MatchType.Friendly,
            Status = MatchStatus.Open,
            Note = note,
            CreatedAtUtc = nowUtc
        };
    }

    public static Match CreateCompetitive(
        Guid courtSlotId,
        Guid organizerId,
        decimal organizerLevelAtCreation,
        decimal minLevel,
        decimal maxLevel,
        int? minMatchesRequired,
        string? note,
        DateTimeOffset nowUtc)
    {
        if (minLevel > maxLevel)
        {
            throw new ArgumentException("MinLevel must not be greater than MaxLevel.", nameof(minLevel));
        }

        if (organizerLevelAtCreation < minLevel || organizerLevelAtCreation > maxLevel)
        {
            throw new ArgumentException(
                "OrganizerLevelAtCreation must fall within [MinLevel, MaxLevel].", nameof(organizerLevelAtCreation));
        }

        if (minMatchesRequired is < 0)
        {
            throw new ArgumentException("MinMatchesRequired must not be negative.", nameof(minMatchesRequired));
        }

        RequireValidNote(note);

        return new Match
        {
            Id = Guid.NewGuid(),
            CourtSlotId = courtSlotId,
            OrganizerId = organizerId,
            Type = MatchType.Competitive,
            Status = MatchStatus.Open,
            OrganizerLevelAtCreation = organizerLevelAtCreation,
            MinLevel = minLevel,
            MaxLevel = maxLevel,
            MinMatchesRequired = minMatchesRequired,
            Note = note,
            CreatedAtUtc = nowUtc
        };
    }

    private static void RequireValidNote(string? note)
    {
        if (note is { Length: > MaxNoteLength })
        {
            throw new ArgumentException($"Note must not exceed {MaxNoteLength} characters.", nameof(note));
        }
    }
}
