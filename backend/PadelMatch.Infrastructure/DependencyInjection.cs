using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PadelMatch.Application;
using PadelMatch.Infrastructure.Persistence;

namespace PadelMatch.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<PadelMatchDbContext>(options =>
            options.UseNpgsql(connectionString, postgres => postgres.CommandTimeout(5)));
        services.AddScoped<IDatabaseReadiness, DatabaseReadiness>();
        return services;
    }
}
