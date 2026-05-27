[CmdletBinding()]
param(
    [string]$PackageName = "postgresql17",
    [string]$HostName = "localhost",
    [int]$Port = 5432,
    [string]$SuperuserName = "postgres",
    [string]$SuperuserPassword = $env:OPSLEDGER_POSTGRES_SUPERUSER_PASSWORD,
    [string]$DatabaseName = "opsledger_validation",
    [string]$ApplicationUsername = "opsledger_app",
    [switch]$SetAzurePipelineVariables
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function New-LocalPassword {
    return [Convert]::ToBase64String([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(32))
}

function Test-UnresolvedAzureMacro {
    param([string]$Value)

    return $Value -match '^\$\([^)]+\)$'
}

function ConvertTo-PostgreSqlLiteral {
    param([string]$Value)

    return "'" + $Value.Replace("'", "''") + "'"
}

function ConvertTo-PostgreSqlIdentifier {
    param([string]$Value)

    return '"' + $Value.Replace('"', '""') + '"'
}

function Find-PostgreSqlTool {
    param([string]$ToolName)

    $command = Get-Command $ToolName -ErrorAction SilentlyContinue
    if ($null -ne $command) {
        return $command.Source
    }

    $candidate = Get-ChildItem -Path "C:\Program Files\PostgreSQL" -Filter $ToolName -Recurse -ErrorAction SilentlyContinue |
        Sort-Object -Property FullName -Descending |
        Select-Object -First 1

    if ($null -eq $candidate) {
        throw "$ToolName was not found after PostgreSQL installation."
    }

    return $candidate.FullName
}

function Write-PostgreSqlInstallLog {
    $installLogPath = Join-Path $env:TEMP "chocolatey\install-postgresql.log"

    if (Test-Path -LiteralPath $installLogPath -PathType Leaf) {
        Write-Host "PostgreSQL install log:"
        Get-Content -LiteralPath $installLogPath | Write-Host
    }
}

function Invoke-PostgreSqlCommand {
    param(
        [string]$PsqlPath,
        [string]$Password,
        [string]$Sql,
        [string]$Database = "postgres"
    )

    $previousPassword = $env:PGPASSWORD
    $env:PGPASSWORD = $Password

    try {
        & $PsqlPath `
            --host $HostName `
            --port $Port `
            --username $SuperuserName `
            --dbname $Database `
            --no-password `
            --tuples-only `
            --command $Sql

        if ($LASTEXITCODE -ne 0) {
            throw "PostgreSQL command failed."
        }
    }
    finally {
        $env:PGPASSWORD = $previousPassword
    }
}

function Invoke-PostgreSqlCreateDatabase {
    param(
        [string]$CreatedbPath,
        [string]$Password
    )

    $previousPassword = $env:PGPASSWORD
    $env:PGPASSWORD = $Password

    try {
        & $CreatedbPath `
            --host $HostName `
            --port $Port `
            --username $SuperuserName `
            --owner $ApplicationUsername `
            $DatabaseName

        if ($LASTEXITCODE -ne 0) {
            throw "Database creation failed."
        }
    }
    finally {
        $env:PGPASSWORD = $previousPassword
    }
}

if ([string]::IsNullOrWhiteSpace($SuperuserPassword) -or (Test-UnresolvedAzureMacro -Value $SuperuserPassword)) {
    throw "OPSLEDGER_POSTGRES_SUPERUSER_PASSWORD must be set for local PostgreSQL installation."
}

[string]$applicationPassword = New-LocalPassword

$psqlCommand = Get-Command psql -ErrorAction SilentlyContinue
if ($null -eq $psqlCommand) {
    $chocolateyCommand = Get-Command choco -ErrorAction SilentlyContinue
    if ($null -eq $chocolateyCommand) {
        throw "PostgreSQL was not found and Chocolatey is unavailable for local installation."
    }

    $packageParameters = "/Password:$SuperuserPassword /Port:$Port"

    choco install $PackageName --yes --no-progress --params $packageParameters --ia "--enable-components server,commandlinetools"

    if ($LASTEXITCODE -ne 0) {
        throw "PostgreSQL installation failed."
    }
}

$psqlPath = Find-PostgreSqlTool -ToolName "psql.exe"
$createdbPath = Find-PostgreSqlTool -ToolName "createdb.exe"

$deadline = (Get-Date).AddSeconds(90)
do {
    try {
        Invoke-PostgreSqlCommand -PsqlPath $psqlPath -Password $SuperuserPassword -Sql "SELECT 1;"
        break
    }
    catch {
        if ((Get-Date) -ge $deadline) {
            Write-PostgreSqlInstallLog
            throw "PostgreSQL did not become available on ${HostName}:$Port."
        }

        Start-Sleep -Seconds 3
    }
} while ($true)

$applicationRoleIdentifier = ConvertTo-PostgreSqlIdentifier -Value $ApplicationUsername
$applicationPasswordLiteral = ConvertTo-PostgreSqlLiteral -Value $applicationPassword
$applicationRoleLiteral = ConvertTo-PostgreSqlLiteral -Value $ApplicationUsername
$databaseLiteral = ConvertTo-PostgreSqlLiteral -Value $DatabaseName

$roleSql = @"
DO `$`$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = $applicationRoleLiteral) THEN
        CREATE ROLE $applicationRoleIdentifier LOGIN PASSWORD $applicationPasswordLiteral;
    ELSE
        ALTER ROLE $applicationRoleIdentifier WITH LOGIN PASSWORD $applicationPasswordLiteral;
    END IF;
END
`$`$;
"@

Invoke-PostgreSqlCommand -PsqlPath $psqlPath -Password $SuperuserPassword -Sql $roleSql

$previousPassword = $env:PGPASSWORD
$env:PGPASSWORD = $SuperuserPassword

try {
    $databaseExists = & $psqlPath `
        --host $HostName `
        --port $Port `
        --username $SuperuserName `
        --dbname postgres `
        --no-password `
        --tuples-only `
        --command "SELECT 1 FROM pg_database WHERE datname = $databaseLiteral;"

    if ($LASTEXITCODE -ne 0) {
        throw "Database existence check failed."
    }
}
finally {
    $env:PGPASSWORD = $previousPassword
}

if ([string]::IsNullOrWhiteSpace(($databaseExists | Out-String).Trim())) {
    Invoke-PostgreSqlCreateDatabase -CreatedbPath $createdbPath -Password $SuperuserPassword
}

$databaseIdentifier = ConvertTo-PostgreSqlIdentifier -Value $DatabaseName
Invoke-PostgreSqlCommand `
    -PsqlPath $psqlPath `
    -Password $SuperuserPassword `
    -Sql "GRANT ALL PRIVILEGES ON DATABASE $databaseIdentifier TO $applicationRoleIdentifier;"

$connectionString = "Host=$HostName;Port=$Port;Database=$DatabaseName;Username=$ApplicationUsername;Password=$applicationPassword;Include Error Detail=true"

Write-Host "PostgreSQL is ready on ${HostName}:$Port."
Write-Host "Database '$DatabaseName' is ready for validation."

if ($SetAzurePipelineVariables) {
    Write-Host "##vso[task.setvariable variable=OPSLEDGER_CONNECTION_STRING;issecret=true]$connectionString"
    Write-Host "##vso[task.setvariable variable=OPSLEDGER_POSTGRES_DATABASE]$DatabaseName"
}
