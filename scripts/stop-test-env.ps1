[CmdletBinding()]
param(
    [string]$ProcessId = $env:OPSLEDGER_API_PROCESS_ID
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($ProcessId)) {
    Write-Host "No OpsLedger API process id was provided."
    return
}

[int]$apiProcessId = [int]$ProcessId
$apiProcess = Get-Process -Id $apiProcessId -ErrorAction SilentlyContinue

if ($null -eq $apiProcess) {
    Write-Host "OpsLedger API process '$apiProcessId' is not running."
    return
}

Stop-Process -Id $apiProcessId -Force
Write-Host "Stopped OpsLedger API process '$apiProcessId'."
