namespace OpsLedger.IntegrationTests.PostgreSql;

public sealed class PostgreSqlFactAttribute : FactAttribute
{
    public PostgreSqlFactAttribute()
    {
        if (!string.Equals(
            Environment.GetEnvironmentVariable("OPSLEDGER_RUN_POSTGRES_TESTS"),
            "true",
            StringComparison.OrdinalIgnoreCase))
        {
            Skip = "Set OPSLEDGER_RUN_POSTGRES_TESTS=true and run Docker to execute PostgreSQL Testcontainers tests.";
        }
    }
}
