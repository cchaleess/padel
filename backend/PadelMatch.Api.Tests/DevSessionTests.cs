using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PadelMatch.Api.Auth;
using PadelMatch.Api.Dev;
using PadelMatch.Api.Players;
using PadelMatch.Api.Tests.TestSupport;
using PadelMatch.Infrastructure.Persistence;

namespace PadelMatch.Api.Tests;

public sealed class DevSessionTests : PlayerApiTestBase
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    [Fact]
    public async Task DevSessionSignsInAsTheSameFictionalPlayerForTheSameName()
    {
        var first = await CreateSessionAsync("Ana");
        var second = await CreateSessionAsync("Ana");
        var other = await CreateSessionAsync("Bruno");

        Assert.Equal("Ana", first.Player.DisplayName);
        // Fictional players take the level survey on creation, so they show a level like real ones.
        Assert.NotNull(first.Player.Level);
        Assert.Equal(first.Player.Id, second.Player.Id);
        Assert.NotEqual(first.Player.Id, other.Player.Id);

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", first.SessionToken);
        var me = await Client.GetFromJsonAsync<PlayerProfileResponse>("/api/players/me", JsonOptions);
        Assert.NotNull(me);
        Assert.Equal(first.Player.Id, me.Id);
    }

    [Fact]
    public async Task DevSessionRequiresAName()
    {
        var response = await Client.PostAsJsonAsync("/api/dev/session", new DevSessionRequest(" "), JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task DevSessionDoesNotExistOutsideDevelopment()
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var connectionString = scope.ServiceProvider.GetRequiredService<PadelMatchDbContext>().Database.GetConnectionString()!;
        await using var production = new PadelMatchWebApplicationFactory(connectionString, "Production");
        using var client = production.CreateClient();

        var response = await client.PostAsJsonAsync("/api/dev/session", new DevSessionRequest("Ana"), JsonOptions);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private async Task<AuthResponse> CreateSessionAsync(string name)
    {
        var response = await Client.PostAsJsonAsync("/api/dev/session", new DevSessionRequest(name), JsonOptions);
        response.EnsureSuccessStatusCode();
        var session = await response.Content.ReadFromJsonAsync<AuthResponse>(JsonOptions);
        Assert.NotNull(session);
        return session;
    }
}
