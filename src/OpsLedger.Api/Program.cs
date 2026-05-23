using OpsLedger.Api.Modules.Health;
using OpsLedger.Api.Modules.ServiceRequests;
using OpsLedger.Api.Modules.ServiceRequests.Services;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddSingleton<IServiceRequestStore, InMemoryServiceRequestStore>();

WebApplication app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapHealthEndpoints();
app.MapServiceRequestEndpoints();

app.Run();

public partial class Program;
