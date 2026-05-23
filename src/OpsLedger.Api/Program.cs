using OpsLedger.Api;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/health", () => Results.Ok(new HealthResponse("Healthy", "OpsLedger.Api")))
    .WithName("GetHealth");

app.Run();

namespace OpsLedger.Api
{
    public sealed record HealthResponse(string Status, string Service);
}
