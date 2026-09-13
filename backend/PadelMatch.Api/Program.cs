using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using PadelMatch.Api.Auth;
using PadelMatch.Api.Clubs;
using PadelMatch.Api.Players;
using PadelMatch.Application;
using PadelMatch.Infrastructure;
using PadelMatch.Infrastructure.Auth;
using PadelMatch.Infrastructure.Clubs;
using PadelMatch.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("PadelMatch");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("Configure ConnectionStrings__PadelMatch before starting the API.");
}

var authSettings = new AuthSettings(
    RequireConfigurationValue(builder.Configuration, "Auth:Google:Audience"),
    RequireConfigurationValue(builder.Configuration, "Auth:Apple:Audience"),
    RequireConfigurationValue(builder.Configuration, "Auth:SessionSigningKey"));

builder.Services.AddInfrastructure(connectionString, authSettings);
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(authSettings.SessionSigningKey))
        };
    });
builder.Services.AddAuthorization();

var app = builder.Build();
app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    if (app.Configuration.GetValue("Development:SeedClubs", defaultValue: false))
    {
        using var seedScope = app.Services.CreateScope();
        var dbContext = seedScope.ServiceProvider.GetRequiredService<PadelMatchDbContext>();
        var clock = seedScope.ServiceProvider.GetRequiredService<TimeProvider>();
        await DevelopmentClubSeeder.SeedIfEmptyAsync(dbContext, clock, CancellationToken.None);
    }
}

app.MapAuthEndpoints();
app.MapPlayerEndpoints();
app.MapClubEndpoints();

app.MapGet("/health/live", (TimeProvider clock) =>
        TypedResults.Ok(new HealthResponse("healthy", clock.GetUtcNow())))
    .WithName("GetLiveness")
    .WithSummary("Checks whether the API process is running.");

app.MapGet("/health/ready", async Task<Results<Ok<HealthResponse>, ProblemHttpResult>> (
        IDatabaseReadiness database, TimeProvider clock, CancellationToken cancellationToken) =>
    {
        if (!await database.IsReadyAsync(cancellationToken))
        {
            return TypedResults.Problem(statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "Service unavailable");
        }

        return TypedResults.Ok(new HealthResponse("healthy", clock.GetUtcNow()));
    })
    .WithName("GetReadiness")
    .WithSummary("Checks PostgreSQL connectivity and applied migrations.")
    .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

app.Run();

static string RequireConfigurationValue(IConfiguration configuration, string key) =>
    configuration[key] is { Length: > 0 } value
        ? value
        : throw new InvalidOperationException($"Configure {key.Replace(":", "__")} before starting the API.");

public sealed record HealthResponse(string Status, DateTimeOffset CheckedAtUtc);

public partial class Program;
