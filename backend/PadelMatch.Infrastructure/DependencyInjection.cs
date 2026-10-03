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
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString, AuthSettings authSettings)
    {
        services.AddDbContext<PadelMatchDbContext>(options =>
            options.UseNpgsql(connectionString, postgres => postgres.CommandTimeout(5)));
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

        services.AddSingleton(authSettings);
        services.AddSingleton<IProviderIdentityVerifier, GoogleIdentityVerifier>();
        services.AddSingleton<IProviderIdentityVerifier, AppleIdentityVerifier>();
        services.AddSingleton<IExternalIdentityVerifier, ExternalIdentityVerifier>();
        services.AddSingleton<IPlayerSessionTokenIssuer, PlayerSessionTokenIssuer>();

        return services;
    }
}
