using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace OpsLedger.Infrastructure.Persistence;

public sealed class OpsLedgerDbContextFactory : IDesignTimeDbContextFactory<OpsLedgerDbContext>
{
    public OpsLedgerDbContext CreateDbContext(string[] args)
    {
        DbContextOptionsBuilder<OpsLedgerDbContext> optionsBuilder = new();

        string databaseConfiguration = Environment.GetEnvironmentVariable("OPSLEDGER_CONNECTION_STRING") ??
            throw new InvalidOperationException("OPSLEDGER_CONNECTION_STRING must be set to run EF Core design-time commands.");

        optionsBuilder.UseNpgsql(databaseConfiguration);

        return new OpsLedgerDbContext(optionsBuilder.Options);
    }
}
