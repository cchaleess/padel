namespace PadelMatch.Application.Matches;

public sealed class PlayerAlreadyHasSeatException : Exception
{
    public PlayerAlreadyHasSeatException() : base("Ya tienes una plaza en este partido.")
    {
    }
}
