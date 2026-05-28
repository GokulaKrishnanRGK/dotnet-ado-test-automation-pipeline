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

if ($ProcessId.StartsWith('$(')) {
    Write-Host "OpsLedger API process id was not resolved by the pipeline."
    return
}

[int]$apiProcessId = 0
if (-not [int]::TryParse($ProcessId, [ref]$apiProcessId)) {
    Write-Host "OpsLedger API process id '$ProcessId' is not numeric."
    return
}

$apiProcess = Get-Process -Id $apiProcessId -ErrorAction SilentlyContinue

if ($null -eq $apiProcess) {
    Write-Host "OpsLedger API process '$apiProcessId' is not running."
    return
}

Stop-Process -Id $apiProcessId -Force
$apiProcess.WaitForExit(30000)

if (-not $apiProcess.HasExited) {
    throw "OpsLedger API process '$apiProcessId' did not exit within 30 seconds."
}

Write-Host "Stopped OpsLedger API process '$apiProcessId'."
