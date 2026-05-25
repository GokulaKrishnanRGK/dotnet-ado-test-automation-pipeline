using Microsoft.EntityFrameworkCore;
using OpsLedger.Api.Modules.ServiceRequests.Services;
using OpsLedger.Infrastructure.Persistence;
using OpsLedger.Infrastructure.Persistence.Repositories;

namespace OpsLedger.Api.Configuration;

public static class OpsLedgerStorageConfiguration
{
    public const string ConnectionStringEnvironmentVariable = "OPSLEDGER_CONNECTION_STRING";
    public const string StorageProviderKey = "OPSLEDGER_STORAGE_PROVIDER";
    public const string InMemoryStorageProvider = "InMemory";
    public const string PostgreSqlStorageProvider = "PostgreSql";

    public static Boolean UsesInMemoryStorage(IConfiguration configuration, IWebHostEnvironment environment)
    {
        string? storageProvider = configuration[StorageProviderKey];

        if (!String.Equals(storageProvider, InMemoryStorageProvider, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!environment.IsEnvironment("Testing"))
        {
            throw new InvalidOperationException("In-memory storage is only allowed in the Testing environment.");
        }

        return true;
    }

    public static string GetRequiredDatabaseConfiguration()
    {
        string? databaseConfiguration = Environment.GetEnvironmentVariable(ConnectionStringEnvironmentVariable);

        if (String.IsNullOrWhiteSpace(databaseConfiguration))
        {
            throw new InvalidOperationException($"{ConnectionStringEnvironmentVariable} must be set before OpsLedger starts.");
        }

        return databaseConfiguration;
    }

    public static IServiceCollection AddOpsLedgerStorage(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        if (UsesInMemoryStorage(configuration, environment))
        {
            services.AddSingleton<IServiceRequestStore, InMemoryServiceRequestStore>();
            return services;
        }

        string databaseConfiguration = GetRequiredDatabaseConfiguration();

        services.AddDbContext<OpsLedgerDbContext>(options =>
            options.UseNpgsql(databaseConfiguration));
        services.AddScoped<PostgreSqlServiceRequestRepository>();
        services.AddScoped<IServiceRequestStore, PostgreSqlServiceRequestStore>();

        return services;
    }

    public static async Task MigrateOpsLedgerDatabaseAsync(this IServiceProvider services)
    {
        using IServiceScope scope = services.CreateScope();
        OpsLedgerDbContext dbContext = scope.ServiceProvider.GetRequiredService<OpsLedgerDbContext>();
        await dbContext.Database.MigrateAsync();
    }
}
