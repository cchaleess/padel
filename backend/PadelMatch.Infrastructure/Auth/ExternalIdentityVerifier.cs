using PadelMatch.Application.Players;
using PadelMatch.Domain.Players;

namespace PadelMatch.Infrastructure.Auth;

internal sealed class ExternalIdentityVerifier(IEnumerable<IProviderIdentityVerifier> verifiers) : IExternalIdentityVerifier
{
    public Task<ExternalIdentity> VerifyAsync(AuthProvider provider, string idToken, CancellationToken cancellationToken)
    {
        var verifier = verifiers.FirstOrDefault(v => v.Provider == provider)
            ?? throw new InvalidExternalIdentityException($"Provider '{provider}' is not supported.");
        return verifier.VerifyAsync(idToken, cancellationToken);
    }
}
