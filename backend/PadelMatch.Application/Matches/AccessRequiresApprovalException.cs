using PadelMatch.Domain.Matches;

namespace PadelMatch.Application.Matches;

/// <summary>The player doesn't meet a competitive match's criteria and has no approved access request, so they
/// can't hold a seat (plan §16). <see cref="Shortfalls"/> says which criteria they miss.</summary>
public sealed class AccessRequiresApprovalException(IReadOnlyList<AccessShortfall> shortfalls)
    : Exception("Este partido requiere la aprobación de los jugadores confirmados.")
{
    public IReadOnlyList<AccessShortfall> Shortfalls { get; } = shortfalls;
}
