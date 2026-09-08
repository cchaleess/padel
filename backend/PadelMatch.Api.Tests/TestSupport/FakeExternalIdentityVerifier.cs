using PadelMatch.Application.Players;
using PadelMatch.Domain.Players;

namespace PadelMatch.Api.Tests.TestSupport;

/// <summary>
/// Deterministic stand-in for real Google/Apple verification, registered in place of
/// <see cref="IExternalIdentityVerifier"/> for tests so they never call out to a real network.
/// Synthetic tokens are created with <see cref="CreateToken"/>.
/// </summary>
public sealed class FakeExternalIdentityVerifier : IExternalIdentityVerifier
{
    public Task<ExternalIdentity> VerifyAsync(AuthProvider provider, string idToken, CancellationToken cancellationToken)
    {
        var segments = idToken.Split('|');
        if (segments.Length != 4 || !Enum.TryParse<AuthProvider>(segments[0], out var tokenProvider) || tokenProvider != provider)
        {
            throw new InvalidExternalIdentityException("Synthetic token is invalid or does not match the requested provider.");
        }

        var subject = segments[1];
        var email = segments[2].Length == 0 ? null : segments[2];
        var displayName = segments[3].Length == 0 ? null : segments[3];
        return Task.FromResult(new ExternalIdentity(subject, email, displayName));
    }

    public static string CreateToken(AuthProvider provider, string subject, string? email = null, string? displayName = null) =>
        $"{provider}|{subject}|{email}|{displayName}";
}
