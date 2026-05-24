# OpsLedger Database

PostgreSQL is the system of record for OpsLedger.

Write workflows that mutate request state will be implemented through stored procedures with transactional behavior. Schema and stored procedure scripts will be added here as the request workflow is developed.

## Migration Strategy

EF Core migrations are the authoritative database deployment mechanism.

Expected structure:

```text
src/OpsLedger.Infrastructure/
  Persistence/
    OpsLedgerDbContext.cs
    Configurations/
    Migrations/
  Database/
    README.md
    migrations/
    procedures/
```

Restore local tools before running migration commands:

```bash
dotnet tool restore
```

Create migrations from the infrastructure project and use the API project as startup configuration:

```bash
dotnet ef migrations add <MigrationName> \
  --project src/OpsLedger.Infrastructure \
  --startup-project src/OpsLedger.Api \
  --context OpsLedgerDbContext \
  --output-dir Persistence/Migrations
```

Apply migrations:

```bash
dotnet ef database update \
  --project src/OpsLedger.Infrastructure \
  --startup-project src/OpsLedger.Api \
  --context OpsLedgerDbContext
```

Generate idempotent SQL for CI/Azure DevOps environment setup:

```bash
dotnet ef migrations script --idempotent \
  --project src/OpsLedger.Infrastructure \
  --startup-project src/OpsLedger.Api \
  --context OpsLedgerDbContext \
  --output src/OpsLedger.Infrastructure/Database/migrations/<timestamp>_<migration-name>.sql
```

Rules:

- Schema changes belong in EF Core migrations.
- Stored procedure/function changes are applied by migrations with `migrationBuilder.Sql(...)`.
- Readable SQL mirrors may live in `Database/procedures/`, but migrations remain the deployment source of truth.
- Every stored procedure write migration needs integration tests for successful commit and rollback behavior.
- Do not commit real connection strings or environment-specific database names.

## PostgreSQL Integration Tests

PostgreSQL Testcontainers tests are opt-in locally because they require Docker.

```bash
OPSLEDGER_RUN_POSTGRES_TESTS=true dotnet test tests/OpsLedger.IntegrationTests/OpsLedger.IntegrationTests.csproj
```

The default integration test command skips PostgreSQL container tests when `OPSLEDGER_RUN_POSTGRES_TESTS` is not set to `true`.
