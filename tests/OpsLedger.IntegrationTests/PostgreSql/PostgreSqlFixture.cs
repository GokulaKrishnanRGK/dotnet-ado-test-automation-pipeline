using Testcontainers.PostgreSql;

namespace OpsLedger.IntegrationTests.PostgreSql;

public sealed class PostgreSqlFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer container = new PostgreSqlBuilder("postgres:16-alpine").Build();

    public PostgreSqlApiFactory Factory { get; private set; } = null!;

    public string DatabaseConfiguration => container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await container.StartAsync();

        Factory = new PostgreSqlApiFactory(DatabaseConfiguration);
    }

    public async Task DisposeAsync()
    {
        await Factory.DisposeAsync();
        await container.DisposeAsync();
    }
}
