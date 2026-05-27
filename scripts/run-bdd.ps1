[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$ArtifactPath,
    [string]$Configuration = "Release",
    [string]$ResultsRoot = "artifacts/test-results/bdd",
    [string]$TestFilter = "",
    [switch]$IncludeUi,
    [switch]$NoRestore,
    [switch]$NoBuild
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

[string]$runnerPath = Join-Path $PSScriptRoot "Run-BddAgainstArtifact.ps1"
[string[]]$requiredParameters = @("ArtifactPath", "Configuration", "ResultsRoot", "TestFilter")
[System.Management.Automation.CommandInfo]$runnerCommand = Get-Command $runnerPath

foreach ($requiredParameter in $requiredParameters) {
    [string]$parameterName = $requiredParameter

    if (-not $runnerCommand.Parameters.ContainsKey($parameterName)) {
        throw "BDD artifact runner '$runnerPath' is missing the '$parameterName' parameter. Ensure scripts/run-bdd.ps1 and scripts/Run-BddAgainstArtifact.ps1 come from the same repository revision."
    }
}

[string[]]$arguments = @(
    "-ArtifactPath"
    $ArtifactPath
    "-Configuration"
    $Configuration
    "-ResultsRoot"
    $ResultsRoot
)

if ($TestFilter) {
    $arguments += "-TestFilter"
    $arguments += $TestFilter
}

if ($IncludeUi) {
    $arguments += "-IncludeUi"
}

if ($NoRestore) {
    $arguments += "-NoRestore"
}

if ($NoBuild) {
    $arguments += "-NoBuild"
}

& $runnerPath @arguments
