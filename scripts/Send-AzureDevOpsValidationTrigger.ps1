[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$WebhookUrl,
    [Parameter(Mandatory = $true)]
    [string]$WebhookSecret,
    [Parameter(Mandatory = $true)]
    [string]$Repository,
    [Parameter(Mandatory = $true)]
    [string]$Branch,
    [Parameter(Mandatory = $true)]
    [string]$CommitSha,
    [Parameter(Mandatory = $true)]
    [string]$GithubRunId,
    [Parameter(Mandatory = $true)]
    [string]$GithubRunAttempt,
    [Parameter(Mandatory = $true)]
    [string]$GithubRunUrl,
    [Parameter(Mandatory = $true)]
    [string]$ArtifactName,
    [string]$SignatureHeaderName = "X-OpsLedger-Webhook-Signature",
    [string]$ArtifactSource = "github-actions",
    [switch]$WhatIf
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function New-OpsLedgerValidationPayload {
    param(
        [string]$Repository,
        [string]$Branch,
        [string]$CommitSha,
        [string]$GithubRunId,
        [string]$GithubRunAttempt,
        [string]$GithubRunUrl,
        [string]$ArtifactName,
        [string]$ArtifactSource
    )

    return [ordered]@{
        resource = [ordered]@{
            message = [ordered]@{
                schemaVersion = 1
                repository = $Repository
                branch = $Branch
                commitSha = $CommitSha
                githubRunId = $GithubRunId
                githubRunAttempt = $GithubRunAttempt
                githubRunUrl = $GithubRunUrl
                artifactName = $ArtifactName
                artifactSource = $ArtifactSource
            }
        }
    }
}

function New-HmacSha1Signature {
    param(
        [string]$Body,
        [string]$Secret
    )

    $encoding = [System.Text.Encoding]::UTF8
    $hmac = [System.Security.Cryptography.HMACSHA1]::new($encoding.GetBytes($Secret))
    $signatureBytes = $hmac.ComputeHash($encoding.GetBytes($Body))

    return [System.BitConverter]::ToString($signatureBytes).Replace("-", "").ToUpperInvariant()
}

if ([string]::IsNullOrWhiteSpace($WebhookUrl)) {
    throw "WebhookUrl is required."
}

if ([string]::IsNullOrWhiteSpace($WebhookSecret)) {
    throw "WebhookSecret is required."
}

$payload = New-OpsLedgerValidationPayload `
    -Repository $Repository `
    -Branch $Branch `
    -CommitSha $CommitSha `
    -GithubRunId $GithubRunId `
    -GithubRunAttempt $GithubRunAttempt `
    -GithubRunUrl $GithubRunUrl `
    -ArtifactName $ArtifactName `
    -ArtifactSource $ArtifactSource

$body = $payload | ConvertTo-Json -Depth 10 -Compress
$signature = New-HmacSha1Signature -Body $body -Secret $WebhookSecret

if ($WhatIf) {
    [ordered]@{
        body = $body
        signatureHeaderName = $SignatureHeaderName
        signature = $signature
    } | ConvertTo-Json -Depth 10

    return
}

$headers = @{
    $SignatureHeaderName = $signature
}

Invoke-RestMethod `
    -Method Post `
    -Uri $WebhookUrl `
    -Headers $headers `
    -ContentType "application/json" `
    -Body $body
