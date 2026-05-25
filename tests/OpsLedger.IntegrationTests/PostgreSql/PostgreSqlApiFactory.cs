using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using OpsLedger.Api.Configuration;

namespace OpsLedger.IntegrationTests.PostgreSql;

public sealed class PostgreSqlApiFactory : WebApplicationFactory<Program>
{
    private readonly string? originalDatabaseConfiguration;
    private readonly string? originalStorageProvider;

    public PostgreSqlApiFactory(string databaseConfiguration)
    {
        originalDatabaseConfiguration = Environment.GetEnvironmentVariable("OPSLEDGER_CONNECTION_STRING");
        originalStorageProvider = Environment.GetEnvironmentVariable(OpsLedgerStorageConfiguration.StorageProviderKey);
        Environment.SetEnvironmentVariable("OPSLEDGER_CONNECTION_STRING", databaseConfiguration);
        Environment.SetEnvironmentVariable(
            OpsLedgerStorageConfiguration.StorageProviderKey,
            OpsLedgerStorageConfiguration.PostgreSqlStorageProvider);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
    }

    protected override void Dispose(bool disposing)
    {
        Environment.SetEnvironmentVariable("OPSLEDGER_CONNECTION_STRING", originalDatabaseConfiguration);
        Environment.SetEnvironmentVariable(OpsLedgerStorageConfiguration.StorageProviderKey, originalStorageProvider);
        base.Dispose(disposing);
    }
}
