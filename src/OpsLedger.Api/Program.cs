using OpsLedger.Api.Configuration;
using OpsLedger.Api.Modules.Health;
using OpsLedger.Api.Modules.ServiceRequests;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddOpsLedgerStorage(builder.Configuration, builder.Environment);

WebApplication app = builder.Build();

if (!OpsLedgerStorageConfiguration.UsesInMemoryStorage(app.Configuration, app.Environment))
{
    await app.Services.MigrateOpsLedgerDatabaseAsync();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapHealthEndpoints();
app.MapServiceRequestEndpoints();

await app.RunAsync();

public partial class Program;
