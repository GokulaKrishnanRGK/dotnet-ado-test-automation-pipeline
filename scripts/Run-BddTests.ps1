[CmdletBinding()]
param(
    [string]$Configuration = "Debug",
    [string]$ResultsDirectory = "artifacts/test-results/bdd",
    [switch]$NoRestore,
    [switch]$NoBuild
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$projectPath = "tests/OpsLedger.BddTests/OpsLedger.BddTests.csproj"
$trxFileName = "OpsLedger.BddTests.trx"

New-Item -ItemType Directory -Path $ResultsDirectory -Force | Out-Null

if ($env:OPSLEDGER_BDD_ARTIFACT_NAME) {
    Write-Host "BDD artifact: $env:OPSLEDGER_BDD_ARTIFACT_NAME"
}

if ($env:OPSLEDGER_BDD_COMMIT_SHA) {
    Write-Host "BDD artifact commit: $env:OPSLEDGER_BDD_COMMIT_SHA"
}

$testArguments = @(
    "test"
    $projectPath
    "--configuration"
    $Configuration
    "--logger"
    "trx;LogFileName=$trxFileName"
    "--results-directory"
    $ResultsDirectory
)

if ($NoRestore) {
    $testArguments += "--no-restore"
}

if ($NoBuild) {
    $testArguments += "--no-build"
}

& dotnet @testArguments

if ($LASTEXITCODE -ne 0) {
    throw "BDD test execution failed."
}

Write-Host "BDD test results written to $(Join-Path $ResultsDirectory $trxFileName)"
