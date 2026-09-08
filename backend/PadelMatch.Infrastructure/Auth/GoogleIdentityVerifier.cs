using Google.Apis.Auth;
using PadelMatch.Application.Players;
using PadelMatch.Domain.Players;

namespace PadelMatch.Infrastructure.Auth;

internal sealed class GoogleIdentityVerifier(AuthSettings settings) : IProviderIdentityVerifier
{
    public AuthProvider Provider => AuthProvider.Google;

    public async Task<ExternalIdentity> VerifyAsync(string idToken, CancellationToken cancellationToken)
    {
        try
        {
            var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = [settings.GoogleAudience]
            });
            return new ExternalIdentity(payload.Subject, payload.Email, payload.Name);
        }
        catch (InvalidJwtException ex)
        {
            throw new InvalidExternalIdentityException("The Google ID token is invalid.", ex);
        }
    }
}
