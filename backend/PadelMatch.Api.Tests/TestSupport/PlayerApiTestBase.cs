using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PadelMatch.Infrastructure.Persistence;

namespace PadelMatch.Api.Tests.TestSupport;

public abstract class PlayerApiTestBase : IAsyncLifetime
{
    private readonly EphemeralDatabase database = new();

    protected PadelMatchWebApplicationFactory Factory { get; private set; } = null!;
    protected HttpClient Client { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await database.InitializeAsync();
        Factory = new PadelMatchWebApplicationFactory(database.ConnectionString);

        await using var scope = Factory.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<PadelMatchDbContext>().Database.MigrateAsync();

        Client = Factory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        Client.Dispose();
        await Factory.DisposeAsync();
        await database.DisposeAsync();
    }
}
