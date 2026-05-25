using Microsoft.AspNetCore.Mvc.Testing;
using OpsLedger.Api.Configuration;

namespace OpsLedger.IntegrationTests.ServiceRequests;

public sealed class InMemoryApiFactory : WebApplicationFactory<Program>
{
    private readonly string? originalEnvironment;
    private readonly string? originalStorageProvider;

    public InMemoryApiFactory()
    {
        originalEnvironment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        originalStorageProvider = Environment.GetEnvironmentVariable(OpsLedgerStorageConfiguration.StorageProviderKey);

        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");
        Environment.SetEnvironmentVariable(
            OpsLedgerStorageConfiguration.StorageProviderKey,
            OpsLedgerStorageConfiguration.InMemoryStorageProvider);
    }

    protected override void Dispose(bool disposing)
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", originalEnvironment);
        Environment.SetEnvironmentVariable(OpsLedgerStorageConfiguration.StorageProviderKey, originalStorageProvider);
        base.Dispose(disposing);
    }
}
