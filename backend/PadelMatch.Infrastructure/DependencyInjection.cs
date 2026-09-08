using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PadelMatch.Application;
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

        services.AddSingleton(authSettings);
        services.AddSingleton<IProviderIdentityVerifier, GoogleIdentityVerifier>();
        services.AddSingleton<IProviderIdentityVerifier, AppleIdentityVerifier>();
        services.AddSingleton<IExternalIdentityVerifier, ExternalIdentityVerifier>();
        services.AddSingleton<IPlayerSessionTokenIssuer, PlayerSessionTokenIssuer>();

        return services;
    }
}
