using PadelMatch.Domain.Players;

namespace PadelMatch.Application.Players;

public sealed class PlayerAuthenticator(
    IExternalIdentityVerifier verifier,
    IPlayerRepository repository,
    IPlayerSessionTokenIssuer tokenIssuer,
    TimeProvider clock) : IPlayerAuthenticator
{
    public async Task<PlayerAuthenticationResult> AuthenticateAsync(
        AuthProvider provider, string idToken, string? displayNameOverride, CancellationToken cancellationToken)
    {
        var identity = await verifier.VerifyAsync(provider, idToken, cancellationToken);
        var player = await repository.FindByExternalIdentityAsync(provider, identity.Subject, cancellationToken);
        if (player is null)
        {
            var displayName = displayNameOverride ?? identity.DisplayName ?? identity.Email ?? "Player";
            player = Player.Register(displayName, identity.Email, provider, identity.Subject, clock.GetUtcNow());
            await repository.AddAsync(player, cancellationToken);
            await repository.SaveChangesAsync(cancellationToken);
        }

        var sessionToken = tokenIssuer.Issue(player);
        return new PlayerAuthenticationResult(sessionToken, player);
    }
}
