[CmdletBinding()]
param(
    [string]$InstallerUrl = "https://get.enterprisedb.com/postgresql/postgresql-17.10-1-windows-x64.exe",
    [string]$HostName = "localhost",
    [int]$Port = 5432,
    [string]$SuperuserName = "postgres",
    [string]$SuperuserPassword = "root",
    [string]$DatabaseName = "opsledger_validation",
    [string]$ApplicationUsername = "opsledger_app",
    [switch]$SetAzurePipelineVariables
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function New-LocalPassword {
    return [Convert]::ToBase64String([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(32))
}

function ConvertTo-PostgreSqlLiteral {
    param([string]$Value)

    return "'" + $Value.Replace("'", "''") + "'"
}

function ConvertTo-PostgreSqlIdentifier {
    param([string]$Value)

    return '"' + $Value.Replace('"', '""') + '"'
}

function Write-PostgreSqlInstallLog {
    [string]$installLogPath = Join-Path $env:TEMP "install-postgresql.log"

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
    [System.Diagnostics.Process]$installerProcess = Start-Process `
        -FilePath $installerPath `
        -ArgumentList $installerArguments `
        -Wait `
        -PassThru

    if ($installerProcess.ExitCode -ne 0) {
        Write-PostgreSqlInstallLog
        throw "PostgreSQL installer failed with exit code '$($installerProcess.ExitCode)'."
    }
}

function Find-PostgreSqlTool {
    param([string]$ToolName)

    Write-Host "Resolving PostgreSQL tool '$ToolName'."

    $candidate = Get-ChildItem -Path "C:\Program Files\PostgreSQL" -Filter $ToolName -Recurse -ErrorAction SilentlyContinue |
        Where-Object { $_.FullName -like "*\bin\$ToolName" -and $_.FullName -notlike "*\pgAdmin 4\runtime\*" } |
        Sort-Object -Property FullName -Descending |
        Select-Object -First 1

    if ($null -eq $candidate) {
        throw "$ToolName was not found after PostgreSQL installation."
    }

    Write-Host "Resolved PostgreSQL tool '$ToolName': $($candidate.FullName)"
    return $candidate.FullName
}

function Get-PostgreSqlService {
    Write-Host "Resolving PostgreSQL Windows service."

    $services = @(Get-Service -ErrorAction SilentlyContinue |
        Where-Object { $_.Name -like "postgresql*" -or $_.DisplayName -like "postgresql*" } |
        Sort-Object -Property Name)

    return $services | Select-Object -First 1
}

function Start-PostgreSqlService {
    $service = Get-PostgreSqlService

    if ($null -eq $service) {
        throw "No PostgreSQL Windows service was found after installation."
    }

    Write-Host "PostgreSQL service '$($service.Name)' status: $($service.Status)"

    if ($service.Status -ne "Running") {
        Start-Service -Name $service.Name
        $service.WaitForStatus("Running", [TimeSpan]::FromSeconds(60))
    }

    $service = Get-Service -Name $service.Name
    Write-Host "PostgreSQL service '$($service.Name)' status after start attempt: $($service.Status)"

    if ($service.Status -ne "Running") {
        throw "PostgreSQL service '$($service.Name)' did not reach Running status."
    }
}

function Invoke-PostgreSqlCommand {
    param(
        [string]$PsqlPath,
        [string]$Password,
        [string]$Sql,
        [string]$Database = "postgres"
    )

    [string]$previousPassword = $env:PGPASSWORD
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

function Test-PostgreSqlPassword {
    param(
        [string]$PsqlPath,
        [string]$Password,
        [string]$Label
    )

    [string]$previousPassword = $env:PGPASSWORD
    $env:PGPASSWORD = $Password

    try {
        & $PsqlPath `
            --host $HostName `
            --port $Port `
            --username $SuperuserName `
            --dbname postgres `
            --no-password `
            --tuples-only `
            --command "SELECT 1;" | Out-Null

        if ($LASTEXITCODE -eq 0) {
            Write-Host "PostgreSQL superuser authentication succeeded with $Label."
            return $true
        }

        Write-Host "PostgreSQL superuser authentication failed with $Label."
        return $false
    }
    finally {
        $env:PGPASSWORD = $previousPassword
    }
}

function Wait-PostgreSqlAuthentication {
    param(
        [string]$PsqlPath,
        [string]$Password
    )

    [datetime]$deadline = (Get-Date).AddSeconds(90)

    do {
        if (Test-PostgreSqlPassword -PsqlPath $PsqlPath -Password $Password -Label "configured password") {
            return
        }

        if ((Get-Date) -ge $deadline) {
            Write-PostgreSqlInstallLog
            throw "PostgreSQL did not accept the configured superuser password."
        }

        Start-Sleep -Seconds 3
    } while ($true)
}

function Invoke-PostgreSqlCreateDatabase {
    param(
        [string]$CreatedbPath,
        [string]$Password
    )

    [string]$previousPassword = $env:PGPASSWORD
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

[string]$applicationPassword = New-LocalPassword

Write-Host "Installing PostgreSQL for validation."
Install-PostgreSql -Url $InstallerUrl -Password $SuperuserPassword
Start-PostgreSqlService

[string]$psqlPath = Find-PostgreSqlTool -ToolName "psql.exe"
[string]$createdbPath = Find-PostgreSqlTool -ToolName "createdb.exe"

Wait-PostgreSqlAuthentication -PsqlPath $psqlPath -Password $SuperuserPassword

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

[string]$previousPassword = $env:PGPASSWORD
$env:PGPASSWORD = $SuperuserPassword

try {
    [object[]]$databaseExists = & $psqlPath `
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

[string]$databaseIdentifier = ConvertTo-PostgreSqlIdentifier -Value $DatabaseName
Invoke-PostgreSqlCommand `
    -PsqlPath $psqlPath `
    -Password $SuperuserPassword `
    -Sql "GRANT ALL PRIVILEGES ON DATABASE $databaseIdentifier TO $applicationRoleIdentifier;"

[string]$connectionString = "Host=$HostName;Port=$Port;Database=$DatabaseName;Username=$ApplicationUsername;Password=$applicationPassword;Include Error Detail=true"

Write-Host "PostgreSQL is ready on ${HostName}:$Port."
Write-Host "Database '$DatabaseName' is ready for validation."

if ($SetAzurePipelineVariables) {
    Write-Host "##vso[task.setvariable variable=OPSLEDGER_CONNECTION_STRING;issecret=true]$connectionString"
    Write-Host "##vso[task.setvariable variable=OPSLEDGER_POSTGRES_DATABASE]$DatabaseName"
}
