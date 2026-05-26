[CmdletBinding()]
param(
    [string]$ApiProjectPath = "src/OpsLedger.Api/OpsLedger.Api.csproj",
    [string]$ApiBaseAddress = "http://localhost:5184",
    [string]$ConnectionString = $env:OPSLEDGER_CONNECTION_STRING,
    [string]$StorageProvider = "PostgreSql",
    [int]$StartupTimeoutSeconds = 90,
    [string]$LogDirectory = "artifacts/azure-devops/logs",
    [string]$MetadataPath = "artifacts/azure-devops/opsledger-test-env.json",
    [switch]$NoRestore
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

if (-not (Test-Path -LiteralPath $ApiProjectPath -PathType Leaf)) {
    throw "API project '$ApiProjectPath' was not found."
}

if (-not $NoRestore) {
    dotnet restore $ApiProjectPath

    if ($LASTEXITCODE -ne 0) {
        throw "API project restore failed."
    }
}

if ($StorageProvider -eq "PostgreSql" -and [string]::IsNullOrWhiteSpace($ConnectionString)) {
    throw "OPSLEDGER_CONNECTION_STRING must be set for PostgreSQL validation."
}

New-Item -ItemType Directory -Path $LogDirectory -Force | Out-Null
New-Item -ItemType Directory -Path (Split-Path -Path $MetadataPath -Parent) -Force | Out-Null

$apiLogPath = Join-Path $LogDirectory "opsledger-api.log"
$apiErrorLogPath = Join-Path $LogDirectory "opsledger-api-error.log"
Remove-Item -LiteralPath $apiLogPath -Force -ErrorAction SilentlyContinue
Remove-Item -LiteralPath $apiErrorLogPath -Force -ErrorAction SilentlyContinue

$env:ASPNETCORE_ENVIRONMENT = "Production"
$env:ASPNETCORE_URLS = $ApiBaseAddress
$env:OPSLEDGER_STORAGE_PROVIDER = $StorageProvider
$env:OPSLEDGER_CONNECTION_STRING = $ConnectionString

$apiArguments = @(
    "run"
    "--project"
    $ApiProjectPath
    "--configuration"
    "Release"
    "--no-launch-profile"
)

if ($NoRestore) {
    $apiArguments += "--no-restore"
}

$apiProcess = Start-Process `
    -FilePath "dotnet" `
    -ArgumentList $apiArguments `
    -RedirectStandardOutput $apiLogPath `
    -RedirectStandardError $apiErrorLogPath `
    -PassThru `
    -WindowStyle Hidden

$healthUri = "$ApiBaseAddress/health"
$deadline = (Get-Date).AddSeconds($StartupTimeoutSeconds)

while ((Get-Date) -lt $deadline) {
    if ($apiProcess.HasExited) {
        [string]$apiOutputLog = if (Test-Path -LiteralPath $apiLogPath -PathType Leaf) { Get-Content -LiteralPath $apiLogPath -Raw } else { "" }
        [string]$apiErrorLog = if (Test-Path -LiteralPath $apiErrorLogPath -PathType Leaf) { Get-Content -LiteralPath $apiErrorLogPath -Raw } else { "" }
        [string]$apiLog = $apiOutputLog + [Environment]::NewLine + $apiErrorLog

        throw "OpsLedger API exited before health check succeeded. Log: $apiLog"
    }

    try {
        $healthResponse = Invoke-RestMethod -Method Get -Uri $healthUri -TimeoutSec 5
        if ($healthResponse.status -eq "Healthy") {
            [ordered]@{
                schemaVersion = 1
                apiBaseAddress = $ApiBaseAddress
                healthUri = $healthUri
                processId = $apiProcess.Id
                logPath = (Resolve-Path -LiteralPath $apiLogPath).Path
                startedAtUtc = (Get-Date).ToUniversalTime().ToString("O")
            } | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $MetadataPath -Encoding utf8

            Write-Host "OpsLedger API is healthy at $healthUri."
            Write-Host "Environment metadata written to $MetadataPath."
            Write-Host "##vso[task.setvariable variable=OPSLEDGER_API_PROCESS_ID]$($apiProcess.Id)"
            Write-Host "##vso[task.setvariable variable=OPSLEDGER_API_BASE_ADDRESS]$ApiBaseAddress"
            return
        }
    }
    catch {
        Start-Sleep -Seconds 2
    }
}

Stop-Process -Id $apiProcess.Id -Force -ErrorAction SilentlyContinue
throw "OpsLedger API did not become healthy at $healthUri within $StartupTimeoutSeconds seconds."
