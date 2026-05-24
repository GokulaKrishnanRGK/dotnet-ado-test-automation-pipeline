using Microsoft.EntityFrameworkCore;
using OpsLedger.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace OpsLedger.IntegrationTests.PostgreSql;

public sealed class PostgreSqlFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer container = new PostgreSqlBuilder("postgres:16-alpine").Build();

    public PostgreSqlApiFactory Factory { get; private set; } = null!;

    public string ConnectionString => container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await container.StartAsync();

        DbContextOptionsBuilder<OpsLedgerDbContext> optionsBuilder = new();
        optionsBuilder.UseNpgsql(ConnectionString);

        await using OpsLedgerDbContext dbContext = new(optionsBuilder.Options);
        await dbContext.Database.MigrateAsync();

        Factory = new PostgreSqlApiFactory(ConnectionString);
    }

    public async Task DisposeAsync()
    {
        await Factory.DisposeAsync();
        await container.DisposeAsync();
    }
}
