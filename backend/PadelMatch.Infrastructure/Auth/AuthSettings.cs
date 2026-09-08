namespace PadelMatch.Infrastructure.Auth;

public sealed record AuthSettings(string GoogleAudience, string AppleAudience, string SessionSigningKey);
