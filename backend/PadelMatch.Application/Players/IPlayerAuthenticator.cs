using PadelMatch.Domain.Players;

namespace PadelMatch.Application.Players;

public sealed record PlayerAuthenticationResult(string SessionToken, Player Player);

public interface IPlayerAuthenticator
{
    Task<PlayerAuthenticationResult> AuthenticateAsync(
        AuthProvider provider, string idToken, string? displayNameOverride, CancellationToken cancellationToken);
}
