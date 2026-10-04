using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PadelMatch.Application;
using PadelMatch.Application.Clubs;
using PadelMatch.Application.Matches;
using PadelMatch.Application.Players;
using PadelMatch.Infrastructure.Auth;
using PadelMatch.Infrastructure.Persistence;

namespace PadelMatch.Infrastructure;

public static class DependencyInjection
{
    /// <param name="commandTimeoutSeconds">EF command timeout. 5 s by default; the test suite raises it, since many
    /// test classes create and migrate their own database at the same time.</param>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, string connectionString, AuthSettings authSettings, int commandTimeoutSeconds = 5)
    {
        services.AddDbContext<PadelMatchDbContext>(options =>
            options.UseNpgsql(connectionString, postgres => postgres.CommandTimeout(commandTimeoutSeconds)));
        services.AddScoped<IDatabaseReadiness, DatabaseReadiness>();
        services.AddScoped<IPlayerRepository, PlayerRepository>();
        services.AddScoped<IPlayerAuthenticator, PlayerAuthenticator>();
        services.AddScoped<IPlayerProfileService, PlayerProfileService>();
        services.AddScoped<IClubRepository, ClubRepository>();
        services.AddScoped<IClubDiscoveryService, ClubDiscoveryService>();
        services.AddScoped<IClubSubmissionService, ClubSubmissionService>();
        services.AddScoped<IMatchRepository, MatchRepository>();
        services.AddScoped<IMatchCreationService, MatchCreationService>();
        services.AddScoped<IMatchFeedService, MatchFeedService>();
        services.AddScoped<IMatchSeatRepository, MatchSeatRepository>();
        services.AddScoped<IMatchSeatService, MatchSeatService>();
        services.AddScoped<IMatchAccessRepository, MatchAccessRepository>();
        services.AddScoped<IMatchAccessService, MatchAccessService>();

        services.AddSingleton(authSettings);
        services.AddSingleton<IProviderIdentityVerifier, GoogleIdentityVerifier>();
        services.AddSingleton<IProviderIdentityVerifier, AppleIdentityVerifier>();
        services.AddSingleton<IExternalIdentityVerifier, ExternalIdentityVerifier>();
        services.AddSingleton<IPlayerSessionTokenIssuer, PlayerSessionTokenIssuer>();

        return services;
    }
}
