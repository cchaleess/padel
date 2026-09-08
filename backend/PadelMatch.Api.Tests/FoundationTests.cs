using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using PadelMatch.Api.Tests.TestSupport;
using PadelMatch.Infrastructure.Persistence;

namespace PadelMatch.Api.Tests;

public sealed class FoundationTests : IAsyncLifetime
{
    private readonly EphemeralDatabase database = new();

    public Task InitializeAsync() => database.InitializeAsync();

    public Task DisposeAsync() => database.DisposeAsync();

    [Fact]
    public async Task ReadinessRequiresMigrationAndRepeatedMigrationIsANoOp()
    {
        await using var factory = CreateFactory(database.ConnectionString);
        using var client = factory.CreateClient();
        Assert.Equal(HttpStatusCode.ServiceUnavailable, (await client.GetAsync("/health/ready")).StatusCode);

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<PadelMatchDbContext>();
        await db.Database.MigrateAsync();
        var applied = (await db.Database.GetAppliedMigrationsAsync()).ToArray();
        Assert.NotEmpty(applied);
        await db.Database.MigrateAsync();
        Assert.Equal(applied, (await db.Database.GetAppliedMigrationsAsync()).ToArray());
        Assert.Empty(await db.Database.GetPendingMigrationsAsync());

        using var response = await client.GetAsync("/health/ready");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var health = await response.Content.ReadFromJsonAsync<HealthResponse>();
        Assert.NotNull(health);
        Assert.Equal("healthy", health.Status);
        Assert.Equal(TimeSpan.Zero, health.CheckedAtUtc.Offset);
    }

    [Fact]
    public async Task MissingDatabaseDoesNotBreakLivenessOrExposeConnectionDetails()
    {
        var unavailable = new NpgsqlConnectionStringBuilder(database.ConnectionString)
        {
            Database = $"{database.DatabaseName}_missing"
        };
        await using var factory = CreateFactory(unavailable.ConnectionString);
        using var client = factory.CreateClient();
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health/live")).StatusCode);
        using var ready = await client.GetAsync("/health/ready");
        Assert.Equal(HttpStatusCode.ServiceUnavailable, ready.StatusCode);
        var body = await ready.Content.ReadAsStringAsync();
        Assert.DoesNotContain(database.DatabaseName, body);
        Assert.DoesNotContain("Password", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DevelopmentOpenApiDescribesHealthEndpoints()
    {
        await using var factory = CreateFactory(database.ConnectionString);
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/openapi/v1.json");
        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.StartsWith("3.", document.RootElement.GetProperty("openapi").GetString());
        var paths = document.RootElement.GetProperty("paths");
        Assert.True(paths.TryGetProperty("/health/live", out _));
        var responses = paths.GetProperty("/health/ready").GetProperty("get").GetProperty("responses");
        Assert.True(responses.TryGetProperty("200", out _));
        Assert.True(responses.TryGetProperty("503", out _));
    }

    [Fact]
    public async Task ProductionDoesNotPublishOpenApi()
    {
        await using var factory = CreateFactory(database.ConnectionString, "Production");
        using var client = factory.CreateClient();
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/openapi/v1.json")).StatusCode);
    }

    private static PadelMatchWebApplicationFactory CreateFactory(string connectionString, string environment = "Development") =>
        new(connectionString, environment);
}
