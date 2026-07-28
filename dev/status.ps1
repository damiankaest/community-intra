[CmdletBinding()]
param()

. (Join-Path $PSScriptRoot "common.ps1")

$repositoryRoot = Get-RepositoryRoot
$processFile = Join-Path $repositoryRoot ".runtime/processes.json"
$backendPid = $null
$frontendPid = $null

if (Test-Path $processFile) {
    $processes = Get-Content $processFile -Raw | ConvertFrom-Json
    $backendPid = $processes.backendPid
    $frontendPid = $processes.frontendPid
}

$runningServices = & docker compose `
    --file (Join-Path $repositoryRoot "docker-compose.yml") `
    ps --status running --services 2> $null
$databaseRunning = $runningServices -contains "postgres"

$rows = @(
    [PSCustomObject]@{
        Service = "Backend"
        Status = if (Test-ProcessRunning $backendPid) { "Running" } else { "Stopped" }
        Port = 5080
        PID = $backendPid
    },
    [PSCustomObject]@{
        Service = "Frontend"
        Status = if (Test-ProcessRunning $frontendPid) { "Running" } else { "Stopped" }
        Port = 5173
        PID = $frontendPid
    },
    [PSCustomObject]@{
        Service = "PostgreSQL"
        Status = if ($databaseRunning) { "Running" } else { "Stopped" }
        Port = if ($env:POSTGRES_PORT) { $env:POSTGRES_PORT } else { 5432 }
        PID = "-"
    }
)

$rows | Format-Table -AutoSize
