namespace PadelMatch.Domain.Players;

public sealed class Player
{
    private readonly List<PlayerExternalIdentity> externalIdentities = [];

    public Guid Id { get; private set; }
    public string DisplayName { get; private set; } = null!;
    public string? Email { get; private set; }
    public DateOnly? DateOfBirth { get; private set; }
    public string? CityOrZone { get; private set; }
    public string? PhotoUrl { get; private set; }
    public decimal? Level { get; private set; }
    public LevelConfidence LevelConfidence { get; private set; } = LevelConfidence.None;
    public int MatchesPlayed { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public IReadOnlyCollection<PlayerExternalIdentity> ExternalIdentities => externalIdentities;

    private Player()
    {
    }

    public static Player Register(string displayName, string? email, AuthProvider provider, string providerSubjectId, DateTimeOffset nowUtc)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException("DisplayName is required.", nameof(displayName));
        }

        if (string.IsNullOrWhiteSpace(providerSubjectId))
        {
            throw new ArgumentException("ProviderSubjectId is required.", nameof(providerSubjectId));
        }

        var player = new Player
        {
            Id = Guid.NewGuid(),
            DisplayName = displayName,
            Email = email,
            LevelConfidence = LevelConfidence.None,
            MatchesPlayed = 0,
            CreatedAtUtc = nowUtc
        };
        player.externalIdentities.Add(new PlayerExternalIdentity(player.Id, provider, providerSubjectId, nowUtc));
        return player;
    }

    public void UpdateProfile(string? cityOrZone, DateOnly? dateOfBirth, string? photoUrl)
    {
        CityOrZone = cityOrZone;
        DateOfBirth = dateOfBirth;
        PhotoUrl = photoUrl;
    }

    /// <exception cref="PlayerProfileIncompleteException">DateOfBirth is not set yet; age feeds match-quality logic.</exception>
    public void CompleteLevelSurvey(LevelSurveyAnswers answers)
    {
        if (DateOfBirth is null)
        {
            throw new PlayerProfileIncompleteException("DateOfBirth must be set before completing the level survey.");
        }

        Level = InitialLevelEstimator.Estimate(answers);
        LevelConfidence = LevelConfidence.Low;
    }
}
