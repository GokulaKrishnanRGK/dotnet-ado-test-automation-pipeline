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

    Write-Host "Checking for PostgreSQL tool '$ToolName'."

    $command = Get-Command $ToolName -ErrorAction SilentlyContinue
    if ($null -ne $command -and $command.Source -notlike "*\pgAdmin 4\runtime\*") {
        Write-Host "Found PostgreSQL tool '$ToolName' on PATH: $($command.Source)"
        return $command.Source
    }

    $candidate = Get-ChildItem -Path "C:\Program Files\PostgreSQL" -Filter $ToolName -Recurse -ErrorAction SilentlyContinue |
        Where-Object { $_.FullName -like "*\bin\$ToolName" -and $_.FullName -notlike "*\pgAdmin 4\runtime\*" } |
        Sort-Object -Property FullName -Descending |
        Select-Object -First 1

    if ($null -eq $candidate) {
        Write-Host "PostgreSQL tool '$ToolName' was not found."
        throw "$ToolName was not found after PostgreSQL installation."
    }

    Write-Host "Found PostgreSQL tool '$ToolName' under Program Files: $($candidate.FullName)"
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

    if ([string]::IsNullOrWhiteSpace($Password) -or (Test-UnresolvedAzureMacro -Value $Password)) {
        throw "OPSLEDGER_POSTGRES_SUPERUSER_PASSWORD must be set before installing PostgreSQL."
    }

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
    Write-Host "Checking for PostgreSQL Windows service."

    $services = @(Get-Service -ErrorAction SilentlyContinue |
        Where-Object { $_.Name -like "postgresql*" -or $_.DisplayName -like "postgresql*" } |
        Sort-Object -Property Name)

    return $services | Select-Object -First 1
}

function Start-PostgreSqlServiceIfPresent {
    $service = Get-PostgreSqlService

    if ($null -eq $service) {
        Write-Host "No PostgreSQL Windows service was found."
        return $false
    }
 
    Write-Host "PostgreSQL service '$($service.Name)' status: $($service.Status)"

    if ($service.Status -ne "Running") {
        try {
            Start-Service -Name $service.Name
            $service.WaitForStatus("Running", [TimeSpan]::FromSeconds(60))
        }
        catch {
            Write-Host "PostgreSQL service '$($service.Name)' could not be started: $($_.Exception.Message)"
            return $false
        }
    }

    $service = Get-Service -Name $service.Name
    Write-Host "PostgreSQL service '$($service.Name)' status after start attempt: $($service.Status)"
    return $service.Status -eq "Running"
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

function Test-PostgreSqlPassword {
    param(
        [string]$PsqlPath,
        [AllowNull()]
        [string]$Password,
        [string]$Label
    )

    $previousPassword = $env:PGPASSWORD

    if ($null -eq $Password) {
        Remove-Item Env:\PGPASSWORD -ErrorAction SilentlyContinue
    }
    else {
        $env:PGPASSWORD = $Password
    }

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
        if ($null -eq $previousPassword) {
            Remove-Item Env:\PGPASSWORD -ErrorAction SilentlyContinue
        }
        else {
            $env:PGPASSWORD = $previousPassword
        }
    }
}

function Find-WorkingPostgreSqlPassword {
    param(
        [string]$PsqlPath,
        [string]$ProvidedPassword
    )

    Write-Host "Checking PostgreSQL superuser authentication with known local credentials."

    if (Test-PostgreSqlPassword -PsqlPath $PsqlPath -Password $null -Label "no password") {
        return ""
    }

    if (Test-PostgreSqlPassword -PsqlPath $PsqlPath -Password "root" -Label "password 'root'") {
        return "root"
    }

    if (Test-PostgreSqlPassword -PsqlPath $PsqlPath -Password "postgres" -Label "password 'postgres'") {
        return "postgres"
    }

    if (-not [string]::IsNullOrWhiteSpace($ProvidedPassword) -and -not (Test-UnresolvedAzureMacro -Value $ProvidedPassword)) {
        if (Test-PostgreSqlPassword -PsqlPath $PsqlPath -Password $ProvidedPassword -Label "provided password") {
            return $ProvidedPassword
        }
    }
    else {
        Write-Host "No provided PostgreSQL superuser password is available to test."
    }

    return $null
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

[string]$applicationPassword = New-LocalPassword

Write-Host "Checking whether PostgreSQL server tools are already installed."
$psqlCommand = Get-Command psql -ErrorAction SilentlyContinue
$psqlIsOnPath = $null -ne $psqlCommand -and $psqlCommand.Source -notlike "*\pgAdmin 4\runtime\*"
$existingPsql = Get-ChildItem -Path "C:\Program Files\PostgreSQL" -Filter "psql.exe" -Recurse -ErrorAction SilentlyContinue |
    Where-Object { $_.FullName -like "*\bin\psql.exe" -and $_.FullName -notlike "*\pgAdmin 4\runtime\*" } |
    Sort-Object -Property FullName -Descending |
    Select-Object -First 1
$postgresInstalledByThisRun = $false

if ($psqlIsOnPath) {
    Write-Host "PostgreSQL appears to be pre-installed because psql is available on PATH: $($psqlCommand.Source)"
}
elseif ($null -ne $psqlCommand) {
    Write-Host "Ignoring pgAdmin-bundled psql on PATH: $($psqlCommand.Source)"
}

if (-not $psqlIsOnPath -and $null -ne $existingPsql) {
    Write-Host "PostgreSQL server tools appear to be pre-installed under Program Files: $($existingPsql.FullName)"
}

if ($psqlIsOnPath -or $null -ne $existingPsql) {
    [bool]$serviceStarted = Start-PostgreSqlServiceIfPresent

    if (-not $serviceStarted) {
        Write-Host "Existing PostgreSQL server tools were found, but no usable running PostgreSQL service is available."
        Write-Host "Installing a fresh PostgreSQL server for validation."
        Install-PostgreSql -Url $InstallerUrl -Password $SuperuserPassword
        $postgresInstalledByThisRun = $true
    }
}
else {
    Write-Host "PostgreSQL server tools were not found. Downloading and installing PostgreSQL."
    Install-PostgreSql -Url $InstallerUrl -Password $SuperuserPassword
    $postgresInstalledByThisRun = $true
}

if ($postgresInstalledByThisRun) {
    [bool]$installedServiceStarted = Start-PostgreSqlServiceIfPresent
    if (-not $installedServiceStarted) {
        Write-PostgreSqlInstallLog
        throw "PostgreSQL was installed, but its Windows service could not be started."
    }
}

$psqlPath = Find-PostgreSqlTool -ToolName "psql.exe"
$createdbPath = Find-PostgreSqlTool -ToolName "createdb.exe"

$detectedSuperuserPassword = Find-WorkingPostgreSqlPassword -PsqlPath $psqlPath -ProvidedPassword $SuperuserPassword

if ($null -eq $detectedSuperuserPassword -and $postgresInstalledByThisRun) {
    Write-Host "PostgreSQL was installed by this run, but no tested password worked yet. Re-running service start and authentication checks."
    Start-PostgreSqlServiceIfPresent | Out-Null
    $detectedSuperuserPassword = Find-WorkingPostgreSqlPassword -PsqlPath $psqlPath -ProvidedPassword $SuperuserPassword
}

if ($null -ne $detectedSuperuserPassword) {
    $SuperuserPassword = $detectedSuperuserPassword
}
elseif ($null -ne $psqlCommand -or $null -ne $existingPsql) {
    Write-Host "PostgreSQL was already installed, but none of the tested superuser credentials worked."
    Write-Host "PostgreSQL installation will be skipped because an existing installation is present."
    throw "Unable to authenticate to the existing PostgreSQL instance."
}
else {
    Write-PostgreSqlInstallLog
    throw "PostgreSQL was installed, but none of the tested superuser credentials worked."
}

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
