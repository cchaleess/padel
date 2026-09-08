using PadelMatch.Domain.Players;

namespace PadelMatch.Application.Players;

public interface IPlayerSessionTokenIssuer
{
    string Issue(Player player);
}
