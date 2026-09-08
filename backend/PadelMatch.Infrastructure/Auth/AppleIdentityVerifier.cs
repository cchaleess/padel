using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using PadelMatch.Application.Players;
using PadelMatch.Domain.Players;

namespace PadelMatch.Infrastructure.Auth;

internal sealed class AppleIdentityVerifier : IProviderIdentityVerifier
{
    private const string Issuer = "https://appleid.apple.com";
    private const string MetadataAddress = "https://appleid.apple.com/.well-known/openid-configuration";

    private readonly AuthSettings settings;
    private readonly ConfigurationManager<OpenIdConnectConfiguration> configurationManager;

    public AppleIdentityVerifier(AuthSettings settings)
    {
        this.settings = settings;
        configurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(MetadataAddress, new OpenIdConnectConfigurationRetriever());
    }

    public AuthProvider Provider => AuthProvider.Apple;

    public async Task<ExternalIdentity> VerifyAsync(string idToken, CancellationToken cancellationToken)
    {
        var configuration = await configurationManager.GetConfigurationAsync(cancellationToken);
        var validationParameters = new TokenValidationParameters
        {
            ValidIssuer = Issuer,
            ValidAudience = settings.AppleAudience,
            IssuerSigningKeys = configuration.SigningKeys,
            ValidateLifetime = true
        };

        try
        {
            var principal = new JwtSecurityTokenHandler().ValidateToken(idToken, validationParameters, out _);
            var subject = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                ?? throw new InvalidExternalIdentityException("The Apple ID token has no subject.");
            var email = principal.FindFirst(JwtRegisteredClaimNames.Email)?.Value;
            return new ExternalIdentity(subject, email, DisplayName: null);
        }
        catch (SecurityTokenException ex)
        {
            throw new InvalidExternalIdentityException("The Apple ID token is invalid.", ex);
        }
    }
}
