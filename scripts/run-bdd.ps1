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

$arguments = @(
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

& (Join-Path $PSScriptRoot "Run-BddAgainstArtifact.ps1") @arguments
