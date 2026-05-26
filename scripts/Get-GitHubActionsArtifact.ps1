[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$Repository,
    [Parameter(Mandatory = $true)]
    [string]$RunId,
    [Parameter(Mandatory = $true)]
    [string]$ArtifactName,
    [Parameter(Mandatory = $true)]
    [string]$Token,
    [string]$OutputRoot = "artifacts/azure-devops/github-actions",
    [switch]$SetAzurePipelineVariables
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Invoke-GitHubApi {
    param(
        [string]$Uri,
        [hashtable]$Headers
    )

    return Invoke-RestMethod `
        -Method Get `
        -Uri $Uri `
        -Headers $Headers
}

if ([string]::IsNullOrWhiteSpace($Token)) {
    throw "A GitHub token is required to download the workflow artifact."
}

if ($Repository -notmatch "^[^/]+/[^/]+$") {
    throw "Repository must use the OWNER/REPO format."
}

$headers = @{
    Accept = "application/vnd.github+json"
    Authorization = "Bearer $Token"
    "X-GitHub-Api-Version" = "2022-11-28"
    "User-Agent" = "OpsLedger-AzureValidation"
}

$artifactsUri = "https://api.github.com/repos/$Repository/actions/runs/$RunId/artifacts?per_page=100"
$artifactsResponse = Invoke-GitHubApi -Uri $artifactsUri -Headers $headers
$matchingArtifact = $artifactsResponse.artifacts |
    Where-Object { $_.name -eq $ArtifactName } |
    Select-Object -First 1

if ($null -eq $matchingArtifact) {
    throw "GitHub Actions artifact '$ArtifactName' was not found for run '$RunId' in '$Repository'."
}

New-Item -ItemType Directory -Path $OutputRoot -Force | Out-Null

$artifactPath = Join-Path $OutputRoot $ArtifactName
$zipPath = Join-Path $OutputRoot "$ArtifactName.zip"
$extractPath = Join-Path $OutputRoot "$ArtifactName-extracted"

Remove-Item -LiteralPath $artifactPath -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item -LiteralPath $zipPath -Force -ErrorAction SilentlyContinue
Remove-Item -LiteralPath $extractPath -Recurse -Force -ErrorAction SilentlyContinue

Invoke-WebRequest `
    -Method Get `
    -Uri $matchingArtifact.archive_download_url `
    -Headers $headers `
    -OutFile $zipPath

Expand-Archive -LiteralPath $zipPath -DestinationPath $extractPath -Force

$nestedArtifactPath = Join-Path $extractPath $ArtifactName
if (Test-Path -LiteralPath (Join-Path $extractPath "opsledger-artifact.json") -PathType Leaf) {
    Move-Item -LiteralPath $extractPath -Destination $artifactPath
}
elseif (Test-Path -LiteralPath (Join-Path $nestedArtifactPath "opsledger-artifact.json") -PathType Leaf) {
    Move-Item -LiteralPath $nestedArtifactPath -Destination $artifactPath
    Remove-Item -LiteralPath $extractPath -Recurse -Force -ErrorAction SilentlyContinue
}
else {
    throw "Downloaded artifact '$ArtifactName' did not contain opsledger-artifact.json."
}

Remove-Item -LiteralPath $zipPath -Force -ErrorAction SilentlyContinue

$resolvedArtifactPath = (Resolve-Path -LiteralPath $artifactPath).Path
Write-Host "Downloaded GitHub Actions artifact '$ArtifactName' to '$resolvedArtifactPath'."

if ($SetAzurePipelineVariables) {
    Write-Host "##vso[task.setvariable variable=OPSLEDGER_DOWNLOADED_ARTIFACT_PATH]$resolvedArtifactPath"
}
