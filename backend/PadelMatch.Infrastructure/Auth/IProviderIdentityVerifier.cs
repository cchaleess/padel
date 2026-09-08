using PadelMatch.Application.Players;
using PadelMatch.Domain.Players;

namespace PadelMatch.Infrastructure.Auth;

internal interface IProviderIdentityVerifier
{
    AuthProvider Provider { get; }
    Task<ExternalIdentity> VerifyAsync(string idToken, CancellationToken cancellationToken);
}
