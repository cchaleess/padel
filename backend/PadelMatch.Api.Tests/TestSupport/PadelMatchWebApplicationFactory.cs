using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PadelMatch.Application.Players;

namespace PadelMatch.Api.Tests.TestSupport;

public sealed class PadelMatchWebApplicationFactory(string connectionString, string environment = "Development")
    : WebApplicationFactory<Program>
{
    public MutableTimeProvider Clock { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(environment);
        builder.UseSetting("ConnectionStrings:PadelMatch", connectionString);
        builder.UseSetting("Auth:Google:Audience", "test-google-audience");
        builder.UseSetting("Auth:Apple:Audience", "test-apple-audience");
        builder.UseSetting("Auth:SessionSigningKey", "test-only-session-signing-key-32-bytes-minimum");
        builder.UseSetting("Development:SeedClubs", "false");
        // Many test classes create and migrate their own database in parallel: 5 s (the app default) isn't enough
        // under that load.
        builder.UseSetting("Database:CommandTimeoutSeconds", "30");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IExternalIdentityVerifier>();
            services.AddSingleton<IExternalIdentityVerifier, FakeExternalIdentityVerifier>();
            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(Clock);
        });
    }
}
