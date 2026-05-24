using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace OpsLedger.Infrastructure.Persistence;

public sealed class OpsLedgerDbContextFactory : IDesignTimeDbContextFactory<OpsLedgerDbContext>
{
    public OpsLedgerDbContext CreateDbContext(string[] args)
    {
        DbContextOptionsBuilder<OpsLedgerDbContext> optionsBuilder = new();

        string connectionString = Environment.GetEnvironmentVariable("OPSLEDGER_CONNECTION_STRING") ??
            "Host=localhost;Port=5432;Database=opsledger;Username=opsledger;Password=opsledger";

        optionsBuilder.UseNpgsql(connectionString);

        return new OpsLedgerDbContext(optionsBuilder.Options);
    }
}
