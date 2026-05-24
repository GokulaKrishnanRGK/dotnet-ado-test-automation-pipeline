using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace OpsLedger.IntegrationTests.PostgreSql;

public sealed class PostgreSqlApiFactory(string connectionString) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration(configuration =>
        {
            Dictionary<string, string?> settings = new()
            {
                ["ConnectionStrings:OpsLedger"] = connectionString
            };

            configuration.AddInMemoryCollection(settings);
        });
    }
}
