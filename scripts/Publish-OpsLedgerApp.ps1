[CmdletBinding()]
param(
    [ValidateSet("maccatalyst-arm64", "win-x64")]
    [string]$Target = "maccatalyst-arm64",
    [string]$Configuration = "Release",
    [string]$OutputRoot = "artifacts",
    [string]$CommitSha = "",
    [switch]$NoRestore
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Get-CommitSha {
    if ($CommitSha) {
        return $CommitSha
    }

    if ($env:GITHUB_SHA) {
        return $env:GITHUB_SHA
    }

    $gitSha = (& git rev-parse --short=12 HEAD 2>$null)
    if ($LASTEXITCODE -eq 0 -and $gitSha) {
        return $gitSha.Trim()
    }

    return "local"
}

function Get-RepositoryName {
    if ($env:GITHUB_REPOSITORY) {
        return $env:GITHUB_REPOSITORY
    }

    $remoteUrl = (& git config --get remote.origin.url 2>$null)
    if ($LASTEXITCODE -eq 0 -and $remoteUrl) {
        return $remoteUrl.Trim()
    }

    return "local"
}

function Get-SourceBranch {
    if ($env:GITHUB_REF_NAME) {
        return $env:GITHUB_REF_NAME
    }

    $branchName = (& git branch --show-current 2>$null)
    if ($LASTEXITCODE -eq 0 -and $branchName) {
        return $branchName.Trim()
    }

    return "local"
}

function Assert-HostSupportsTarget {
    param([string]$PublishTarget)

    if ($PublishTarget -eq "maccatalyst-arm64" -and -not $IsMacOS) {
        throw "Target '$PublishTarget' must be published on macOS with Xcode and MAUI workloads installed."
    }

    if ($PublishTarget -eq "win-x64" -and -not $IsWindows) {
        throw "Target '$PublishTarget' must be published on Windows with the MAUI Windows workload installed."
    }
}

function Get-PublishSettings {
    param([string]$PublishTarget)

    if ($PublishTarget -eq "maccatalyst-arm64") {
        return @{
            Framework = "net10.0-maccatalyst"
            Runtime = "maccatalyst-arm64"
            ExtraProperties = @(
                "-p:CreatePackage=false"
            )
        }
    }

    return @{
        Framework = "net10.0-windows10.0.19041.0"
        Runtime = "win-x64"
        ExtraProperties = @(
            "-p:WindowsPackageType=None"
        )
    }
}

$resolvedCommitSha = Get-CommitSha
$shortCommitSha = if ($resolvedCommitSha.Length -gt 12) { $resolvedCommitSha.Substring(0, 12) } else { $resolvedCommitSha }
$artifactName = "opsledger-app-$Target-$shortCommitSha"
$artifactDirectory = Join-Path $OutputRoot $artifactName
$settings = Get-PublishSettings -PublishTarget $Target
$projectPublishDirectory = Join-Path "src/OpsLedger.App/bin" (Join-Path $Configuration (Join-Path $settings.Framework $settings.Runtime))

Assert-HostSupportsTarget -PublishTarget $Target

if (Test-Path -LiteralPath $artifactDirectory) {
    Remove-Item -LiteralPath $artifactDirectory -Recurse -Force
}

New-Item -ItemType Directory -Path $artifactDirectory | Out-Null

$publishArguments = @(
    "publish"
    "src/OpsLedger.App/OpsLedger.App.csproj"
    "--configuration"
    $Configuration
    "--framework"
    $settings.Framework
    "--runtime"
    $settings.Runtime
    "--output"
    $artifactDirectory
    "-p:SourceRevisionId=$resolvedCommitSha"
)

if ($NoRestore) {
    $publishArguments += "--no-restore"
}

$publishArguments += $settings.ExtraProperties

Write-Host "Publishing OpsLedger app artifact '$artifactName'"
Write-Host "Target: $Target"
Write-Host "Commit: $resolvedCommitSha"

& dotnet @publishArguments

if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed for target '$Target'."
}

if ($Target -eq "maccatalyst-arm64") {
    $appBundle = Join-Path $projectPublishDirectory "OpsLedger.app"
    if (-not (Test-Path -LiteralPath $appBundle)) {
        throw "Expected Mac Catalyst app bundle was not found at '$appBundle'."
    }

    Copy-Item -LiteralPath $appBundle -Destination $artifactDirectory -Recurse -Force
}

$expectedPayload = if ($Target -eq "maccatalyst-arm64") { "OpsLedger.app" } else { "OpsLedger*.exe" }

$metadata = [ordered]@{
    schemaVersion = 1
    artifactName = $artifactName
    target = $Target
    framework = $settings.Framework
    runtime = $settings.Runtime
    configuration = $Configuration
    commitSha = $resolvedCommitSha
    commitShortSha = $shortCommitSha
    repository = Get-RepositoryName
    sourceBranch = Get-SourceBranch
    expectedPayload = $expectedPayload
    publishedAtUtc = (Get-Date).ToUniversalTime().ToString("O")
}

$metadataPath = Join-Path $artifactDirectory "opsledger-artifact.json"
$metadata | ConvertTo-Json | Set-Content -LiteralPath $metadataPath -Encoding utf8

& (Join-Path $PSScriptRoot "Test-OpsLedgerArtifact.ps1") -ArtifactPath $artifactDirectory

Write-Host "Published artifact to $artifactDirectory"
Write-Host "Metadata written to $metadataPath"
