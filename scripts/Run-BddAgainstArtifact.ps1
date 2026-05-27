[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$ArtifactPath,
    [string]$Configuration = "Debug",
    [string]$ResultsRoot = "artifacts/test-results/bdd",
    [string]$TestFilter = "",
    [switch]$IncludeUi,
    [switch]$NoRestore,
    [switch]$NoBuild
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Get-WindowsExecutablePath {
    param([string]$ValidatedArtifactPath)

    $windowsExecutable = Get-ChildItem -LiteralPath $ValidatedArtifactPath -Filter "OpsLedger*.exe" -Recurse |
        Select-Object -First 1

    if ($null -eq $windowsExecutable) {
        throw "Windows artifact is missing an OpsLedger executable."
    }

    return $windowsExecutable.FullName
}

& (Join-Path $PSScriptRoot "Test-OpsLedgerArtifact.ps1") -ArtifactPath $ArtifactPath

$metadataPath = Join-Path $ArtifactPath "opsledger-artifact.json"
$metadata = Get-Content -LiteralPath $metadataPath -Raw | ConvertFrom-Json

if ($metadata.target -ne "win-x64") {
    throw "BDD artifact execution expects a win-x64 artifact. Found '$($metadata.target)'."
}

$executablePath = Get-WindowsExecutablePath -ValidatedArtifactPath $ArtifactPath
$artifactResultsDirectory = Join-Path $ResultsRoot $metadata.artifactName
$startedAtUtc = (Get-Date).ToUniversalTime().ToString("O")

$env:OPSLEDGER_BDD_ARTIFACT_PATH = (Resolve-Path -LiteralPath $ArtifactPath).Path
$env:OPSLEDGER_BDD_ARTIFACT_METADATA_PATH = (Resolve-Path -LiteralPath $metadataPath).Path
$env:OPSLEDGER_BDD_APP_EXECUTABLE_PATH = $executablePath
$env:OPSLEDGER_BDD_ARTIFACT_NAME = $metadata.artifactName
$env:OPSLEDGER_BDD_COMMIT_SHA = $metadata.commitSha
$env:OPSLEDGER_RUN_UI_BDD = if ($IncludeUi) { "true" } else { "false" }

Write-Host "Running BDD tests against artifact '$($metadata.artifactName)'"
Write-Host "Executable: $executablePath"
Write-Host "Commit: $($metadata.commitSha)"

[hashtable]$runBddArguments = @{
    Configuration = $Configuration
    ResultsDirectory = $artifactResultsDirectory
}

if ($NoRestore) {
    $runBddArguments.NoRestore = $true
}

if ($NoBuild) {
    $runBddArguments.NoBuild = $true
}

if ($TestFilter) {
    $runBddArguments.TestFilter = $TestFilter
}

if ($IncludeUi) {
    if (-not $IsWindows) {
        throw "Interactive Windows UI BDD can only run on a Windows agent."
    }

    [string]$uiEvidenceDirectory = Join-Path $artifactResultsDirectory "ui-evidence"
    New-Item -ItemType Directory -Path $uiEvidenceDirectory -Force | Out-Null
    $env:OPSLEDGER_UI_EVIDENCE_DIR = (Resolve-Path -LiteralPath $uiEvidenceDirectory).Path
    $runBddArguments.Framework = "net10.0-windows10.0.19041.0"
    $runBddArguments.DotNetProperties = @("EnableWindowsUiAutomation=true")

    Write-Host "UI BDD evidence directory: $env:OPSLEDGER_UI_EVIDENCE_DIR"
}

& (Join-Path $PSScriptRoot "Run-BddTests.ps1") @runBddArguments

if ($LASTEXITCODE -ne 0) {
    throw "BDD artifact validation failed for '$($metadata.artifactName)'."
}

$completedAtUtc = (Get-Date).ToUniversalTime().ToString("O")
$runMetadata = [ordered]@{
    schemaVersion = 1
    artifactName = $metadata.artifactName
    artifactPath = $env:OPSLEDGER_BDD_ARTIFACT_PATH
    artifactMetadataPath = $env:OPSLEDGER_BDD_ARTIFACT_METADATA_PATH
    target = $metadata.target
    commitSha = $metadata.commitSha
    commitShortSha = $metadata.commitShortSha
    executablePath = $env:OPSLEDGER_BDD_APP_EXECUTABLE_PATH
    resultsDirectory = (Resolve-Path -LiteralPath $artifactResultsDirectory).Path
    trxPath = (Join-Path (Resolve-Path -LiteralPath $artifactResultsDirectory).Path "OpsLedger.BddTests.trx")
    startedAtUtc = $startedAtUtc
    completedAtUtc = $completedAtUtc
}

$runMetadataPath = Join-Path $artifactResultsDirectory "opsledger-bdd-artifact-run.json"
$runMetadata | ConvertTo-Json | Set-Content -LiteralPath $runMetadataPath -Encoding utf8

Write-Host "BDD artifact validation completed for '$($metadata.artifactName)'"
Write-Host "BDD artifact run metadata written to $runMetadataPath"
