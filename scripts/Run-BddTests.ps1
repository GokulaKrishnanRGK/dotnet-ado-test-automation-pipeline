[CmdletBinding()]
param(
    [string]$Configuration = "Debug",
    [string]$ResultsDirectory = "artifacts/test-results/bdd",
    [string]$Framework = "",
    [string]$TestFilter = "",
    [string[]]$DotNetProperties = @(),
    [switch]$NoRestore,
    [switch]$NoBuild,
    [switch]$ContinueOnTestFailure
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

if ($Framework) {
    $testArguments += "--framework"
    $testArguments += $Framework
}

if ($TestFilter) {
    $testArguments += "--filter"
    $testArguments += $TestFilter
}

foreach ($dotNetProperty in $DotNetProperties) {
    $testArguments += "-p:$dotNetProperty"
}

& dotnet @testArguments
[int]$testExitCode = $LASTEXITCODE

if ($testExitCode -ne 0 -and -not $ContinueOnTestFailure) {
    throw "BDD test execution failed."
}

if ($testExitCode -ne 0) {
    Write-Warning "BDD test execution completed with failing tests. Test results were still written for publishing."
    $global:LASTEXITCODE = 0
}

Write-Host "BDD test results written to $(Join-Path $ResultsDirectory $trxFileName)"
