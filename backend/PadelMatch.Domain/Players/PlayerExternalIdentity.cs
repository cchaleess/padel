namespace PadelMatch.Domain.Players;

public sealed class PlayerExternalIdentity
{
    public Guid Id { get; private set; }
    public Guid PlayerId { get; private set; }
    public AuthProvider Provider { get; private set; }
    public string ProviderSubjectId { get; private set; } = null!;
    public DateTimeOffset LinkedAtUtc { get; private set; }

    private PlayerExternalIdentity()
    {
    }

    internal PlayerExternalIdentity(Guid playerId, AuthProvider provider, string providerSubjectId, DateTimeOffset linkedAtUtc)
    {
        Id = Guid.NewGuid();
        PlayerId = playerId;
        Provider = provider;
        ProviderSubjectId = providerSubjectId;
        LinkedAtUtc = linkedAtUtc;
    }
}
