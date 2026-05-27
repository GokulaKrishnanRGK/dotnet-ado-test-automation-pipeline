[CmdletBinding()]
param(
    [string]$InstallerUrl = "https://get.enterprisedb.com/postgresql/postgresql-17.10-1-windows-x64.exe",
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
    $installLogPath = Join-Path $env:TEMP "install-postgresql.log"

    if (Test-Path -LiteralPath $installLogPath -PathType Leaf) {
        Write-Host "PostgreSQL install log:"
        Get-Content -LiteralPath $installLogPath | Write-Host
    }
}

function Install-PostgreSql {
    param(
        [string]$Url,
        [string]$Password
    )

    [string]$installerPath = Join-Path $env:TEMP "postgresql-windows-x64.exe"
    [string]$installLogPath = Join-Path $env:TEMP "install-postgresql.log"

    Remove-Item -LiteralPath $installerPath -Force -ErrorAction SilentlyContinue
    Remove-Item -LiteralPath $installLogPath -Force -ErrorAction SilentlyContinue

    Write-Host "Downloading PostgreSQL installer from $Url"
    Invoke-WebRequest -Uri $Url -OutFile $installerPath

    [string[]]$installerArguments = @(
        "--mode"
        "unattended"
        "--unattendedmodeui"
        "none"
        "--superpassword"
        $Password
        "--serverport"
        $Port.ToString()
        "--debuglevel"
        "2"
        "--debugtrace"
        $installLogPath
    )

    Write-Host "Installing PostgreSQL with unattended installer."
    Write-Host "Installer arguments: $($installerArguments -join ' ')"

    $installerProcess = Start-Process `
        -FilePath $installerPath `
        -ArgumentList $installerArguments `
        -Wait `
        -PassThru

    if ($installerProcess.ExitCode -ne 0) {
        Write-PostgreSqlInstallLog
        throw "PostgreSQL installer failed with exit code '$($installerProcess.ExitCode)'."
    }
}

function Get-PostgreSqlService {
    $services = @(Get-Service -ErrorAction SilentlyContinue |
        Where-Object { $_.Name -like "postgresql*" -or $_.DisplayName -like "postgresql*" } |
        Sort-Object -Property Name)

    return $services | Select-Object -First 1
}

function Start-PostgreSqlService {
    $service = Get-PostgreSqlService

    if ($null -eq $service) {
        Write-Host "No PostgreSQL Windows service was found."
        return
    }

    Write-Host "PostgreSQL service '$($service.Name)' status: $($service.Status)"

    if ($service.Status -ne "Running") {
        Start-Service -Name $service.Name
        $service.WaitForStatus("Running", [TimeSpan]::FromSeconds(60))
    }

    $service = Get-Service -Name $service.Name
    Write-Host "PostgreSQL service '$($service.Name)' status after start attempt: $($service.Status)"
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
    Install-PostgreSql -Url $InstallerUrl -Password $SuperuserPassword
}

Start-PostgreSqlService

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
