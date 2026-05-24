using Microsoft.EntityFrameworkCore;
using OpsLedger.Api.Modules.Health;
using OpsLedger.Api.Modules.ServiceRequests;
using OpsLedger.Api.Modules.ServiceRequests.Services;
using OpsLedger.Infrastructure.Persistence;
using OpsLedger.Infrastructure.Persistence.Repositories;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

string? databaseConfiguration = Environment.GetEnvironmentVariable("OPSLEDGER_CONNECTION_STRING");

if (string.IsNullOrWhiteSpace(databaseConfiguration))
{
    builder.Services.AddSingleton<IServiceRequestStore, InMemoryServiceRequestStore>();
}
else
{
    builder.Services.AddDbContext<OpsLedgerDbContext>(options =>
        options.UseNpgsql(databaseConfiguration));
    builder.Services.AddScoped<PostgreSqlServiceRequestRepository>();
    builder.Services.AddScoped<IServiceRequestStore, PostgreSqlServiceRequestStore>();
}

WebApplication app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapHealthEndpoints();
app.MapServiceRequestEndpoints();

app.Run();

public partial class Program;
