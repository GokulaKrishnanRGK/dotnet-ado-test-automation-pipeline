using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
namespace OpsLedger.IntegrationTests.PostgreSql;

public sealed class PostgreSqlApiFactory : WebApplicationFactory<Program>
{
    private readonly string? originalDatabaseConfiguration;

    public PostgreSqlApiFactory(string databaseConfiguration)
    {
        originalDatabaseConfiguration = Environment.GetEnvironmentVariable("OPSLEDGER_CONNECTION_STRING");
        Environment.SetEnvironmentVariable("OPSLEDGER_CONNECTION_STRING", databaseConfiguration);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
    }

    protected override void Dispose(bool disposing)
    {
        Environment.SetEnvironmentVariable("OPSLEDGER_CONNECTION_STRING", originalDatabaseConfiguration);
        base.Dispose(disposing);
    }
}
