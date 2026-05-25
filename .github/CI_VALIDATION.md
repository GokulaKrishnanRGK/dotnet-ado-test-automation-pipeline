# CI Validation

GitHub Actions runs fast validation on pull requests and pushes to `main`.

## Pull Request Validation

The PR validation workflow runs on Ubuntu and covers:

- API and presentation project restore/build.
- Unit tests.
- API/in-memory integration tests.
- API BDD scenarios, excluding Windows UI automation.
- TRX test result publication.
- Cobertura and HTML coverage reports for unit and API/in-memory integration tests.

Local parity commands:

```bash
dotnet restore src/OpsLedger.Api/OpsLedger.Api.csproj
dotnet restore src/OpsLedger.Presentation/OpsLedger.Presentation.csproj
dotnet restore tests/OpsLedger.UnitTests/OpsLedger.UnitTests.csproj
dotnet restore tests/OpsLedger.IntegrationTests/OpsLedger.IntegrationTests.csproj
dotnet restore tests/OpsLedger.BddTests/OpsLedger.BddTests.csproj

dotnet build src/OpsLedger.Api/OpsLedger.Api.csproj --configuration Release --no-restore
dotnet build src/OpsLedger.Presentation/OpsLedger.Presentation.csproj --configuration Release --no-restore

dotnet test tests/OpsLedger.UnitTests/OpsLedger.UnitTests.csproj --configuration Release --no-restore
ASPNETCORE_ENVIRONMENT=Testing OPSLEDGER_STORAGE_PROVIDER=InMemory dotnet test tests/OpsLedger.IntegrationTests/OpsLedger.IntegrationTests.csproj --configuration Release --no-restore
ASPNETCORE_ENVIRONMENT=Testing OPSLEDGER_STORAGE_PROVIDER=InMemory OPSLEDGER_RUN_UI_BDD=false dotnet test tests/OpsLedger.BddTests/OpsLedger.BddTests.csproj --configuration Release --no-restore --filter "Category!=ui"
```

## Coverage

Coverage is collected from unit and API/in-memory integration tests. The workflow excludes generated migrations, designer files, MAUI platform bootstrap code, generated `obj` files, and test assemblies.

The generated coverage artifact is named `opsledger-coverage-report`.

## Windows Artifact

Pushes to `main` publish a `win-x64` MAUI artifact after validation succeeds.

Artifact naming comes from `scripts/Publish-OpsLedgerApp.ps1`:

```text
opsledger-app-win-x64-<short-commit-sha>
```

The artifact payload includes `opsledger-artifact.json`, which records the full commit SHA, short SHA, source branch, repository, runtime, framework, and expected payload. Azure DevOps should use the GitHub run id plus this artifact name when downloading the artifact for Windows BDD validation.

Uploaded GitHub Actions artifacts use 14-day retention.
