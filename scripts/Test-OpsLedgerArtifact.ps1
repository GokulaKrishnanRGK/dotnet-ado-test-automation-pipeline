[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$ArtifactPath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Assert-MetadataField {
    param(
        [object]$Metadata,
        [string]$FieldName
    )

    if ($Metadata.PSObject.Properties.Name -notcontains $FieldName) {
        throw "Artifact metadata is missing required field '$FieldName'."
    }

    $fieldValue = $Metadata.$FieldName
    if ($null -eq $fieldValue -or $fieldValue.ToString().Trim().Length -eq 0) {
        throw "Artifact metadata field '$FieldName' must not be empty."
    }
}

if (-not (Test-Path -LiteralPath $ArtifactPath -PathType Container)) {
    throw "Artifact directory '$ArtifactPath' does not exist."
}

$metadataPath = Join-Path $ArtifactPath "opsledger-artifact.json"
if (-not (Test-Path -LiteralPath $metadataPath -PathType Leaf)) {
    throw "Artifact metadata file '$metadataPath' does not exist."
}

$metadata = Get-Content -LiteralPath $metadataPath -Raw | ConvertFrom-Json
$requiredFields = @(
    "schemaVersion",
    "artifactName",
    "target",
    "framework",
    "runtime",
    "configuration",
    "commitSha",
    "commitShortSha",
    "repository",
    "sourceBranch",
    "expectedPayload",
    "publishedAtUtc"
)

foreach ($requiredField in $requiredFields) {
    Assert-MetadataField -Metadata $metadata -FieldName $requiredField
}

if ($metadata.schemaVersion -ne 1) {
    throw "Unsupported artifact metadata schema version '$($metadata.schemaVersion)'."
}

$artifactDirectoryName = Split-Path -Path $ArtifactPath -Leaf
if ($artifactDirectoryName -ne $metadata.artifactName) {
    throw "Artifact directory '$artifactDirectoryName' does not match metadata artifactName '$($metadata.artifactName)'."
}

$expectedArtifactName = "opsledger-app-$($metadata.target)-$($metadata.commitShortSha)"
if ($metadata.artifactName -ne $expectedArtifactName) {
    throw "Artifact name '$($metadata.artifactName)' does not match expected name '$expectedArtifactName'."
}

if (-not $metadata.commitSha.ToString().StartsWith($metadata.commitShortSha.ToString(), [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "commitShortSha '$($metadata.commitShortSha)' is not a prefix of commitSha '$($metadata.commitSha)'."
}

if ($metadata.target -eq "maccatalyst-arm64") {
    $appBundlePath = Join-Path $ArtifactPath "OpsLedger.app"
    if (-not (Test-Path -LiteralPath $appBundlePath -PathType Container)) {
        throw "Mac Catalyst artifact is missing expected app bundle '$appBundlePath'."
    }
}
elseif ($metadata.target -eq "win-x64") {
    $windowsExecutable = Get-ChildItem -LiteralPath $ArtifactPath -Filter "OpsLedger*.exe" -Recurse | Select-Object -First 1
    if ($null -eq $windowsExecutable) {
        throw "Windows artifact is missing an OpsLedger executable."
    }
}
else {
    throw "Unsupported artifact target '$($metadata.target)'."
}

Write-Host "Artifact metadata validated: $($metadata.artifactName)"
