namespace PadelMatch.Domain.Clubs;

/// <summary>Closed list of court slot durations allowed by the plan (§9): 60, 90 (default) or 120 minutes.</summary>
public enum SlotDuration
{
    SixtyMinutes = 60,
    NinetyMinutes = 90,
    OneTwentyMinutes = 120
}
