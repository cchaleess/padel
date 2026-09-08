using PadelMatch.Domain.Players;

namespace PadelMatch.Application.Players;

public interface IExternalIdentityVerifier
{
    /// <exception cref="InvalidExternalIdentityException">
    /// The token is invalid for the given provider (bad signature, issuer, audience or expiration),
    /// or the provider is not supported.
    /// </exception>
    Task<ExternalIdentity> VerifyAsync(AuthProvider provider, string idToken, CancellationToken cancellationToken);
}
