[CmdletBinding()]
param(
    [string]$OutputPath = ".env.local",
    [switch]$Force,
    [switch]$PrintOnly
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function New-RandomSecret {
    param([int]$Bytes = 32)

    $buffer = [System.Security.Cryptography.RandomNumberGenerator]::GetBytes($Bytes)
    return [Convert]::ToBase64String($buffer)
}

$values = [ordered]@{
    AZDO_ORG_URL = "https://dev.azure.com/<organization>"
    AZDO_PROJECT = "OpsLedger"
    AZDO_PIPELINE_ID = "<pipeline-id>"
    AZDO_INCOMING_WEBHOOK_URL = "<azure-devops-incoming-webhook-url>"
    AZDO_INCOMING_WEBHOOK_SECRET = (New-RandomSecret)
    AZDO_PIPELINE_TRIGGER_PAT = "<create-in-azure-devops-and-store-in-github-actions-secret-if-needed>"
    TEAMS_WORKFLOW_WEBHOOK_URL = "<teams-workflows-webhook-url>"
    ADO_WORK_ITEM_DEDUPE_SALT = (New-RandomSecret)
    ADO_WORK_ITEM_TYPE = "Issue"
}

$content = @(
    "# OpsLedger local external integration values."
    "# This file is generated for local dry runs and must not be committed."
    "# Fill real PATs and webhook URLs manually in platform secret stores where possible."
    ""
)

foreach ($entry in $values.GetEnumerator()) {
    $content += "$($entry.Key)=$($entry.Value)"
}

if ($PrintOnly) {
    $content -join [Environment]::NewLine
    return
}

if ((Test-Path -LiteralPath $OutputPath) -and -not $Force) {
    throw "Refusing to overwrite existing '$OutputPath'. Use -Force to replace it."
}

$directory = Split-Path -Parent $OutputPath
if ($directory -and -not (Test-Path -LiteralPath $directory)) {
    New-Item -ItemType Directory -Path $directory | Out-Null
}

Set-Content -LiteralPath $OutputPath -Value $content -Encoding utf8
Write-Host "Wrote local placeholder secrets to $OutputPath"
Write-Host "Review the file, fill real values only where needed, and keep it out of git."
