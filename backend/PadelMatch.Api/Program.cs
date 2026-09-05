using Microsoft.AspNetCore.Http.HttpResults;
using PadelMatch.Application;
using PadelMatch.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("PadelMatch");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("Configure ConnectionStrings__PadelMatch before starting the API.");
}

builder.Services.AddInfrastructure(connectionString);
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();

var app = builder.Build();
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

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

public sealed record HealthResponse(string Status, DateTimeOffset CheckedAtUtc);

public partial class Program;
