using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using PadelMatch.Application.Players;
using PadelMatch.Domain.Players;

namespace PadelMatch.Infrastructure.Auth;

internal sealed class PlayerSessionTokenIssuer(AuthSettings settings, TimeProvider clock) : IPlayerSessionTokenIssuer
{
    private static readonly TimeSpan SessionLifetime = TimeSpan.FromDays(30);

    public string Issue(Player player)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.SessionSigningKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var now = clock.GetUtcNow();

        var token = new JwtSecurityToken(
            claims: [new Claim(JwtRegisteredClaimNames.Sub, player.Id.ToString())],
            notBefore: now.UtcDateTime,
            expires: now.Add(SessionLifetime).UtcDateTime,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
