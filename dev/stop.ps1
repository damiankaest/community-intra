[CmdletBinding()]
param(
    [switch] $StopDatabase
)

. (Join-Path $PSScriptRoot "common.ps1")

$repositoryRoot = Get-RepositoryRoot
$processFile = Join-Path $repositoryRoot ".runtime/processes.json"

if (Test-Path $processFile) {
    $processes = Get-Content $processFile -Raw | ConvertFrom-Json

    foreach ($process in @(
        @{ Name = "backend"; Id = $processes.backendPid },
        @{ Name = "frontend"; Id = $processes.frontendPid }
    )) {
        if (Test-ProcessRunning $process.Id) {
            Write-Host "Stopping $($process.Name) (PID $($process.Id))..."
            Stop-ManagedProcess -TargetId ([int] $process.Id)
        }
        else {
            Write-Host "$($process.Name) is not running."
        }
    }

    Remove-Item $processFile -Force
}
else {
    Write-Host "No managed backend or frontend processes were found."
}

if ($StopDatabase) {
    Write-Host "Stopping PostgreSQL and the save parser..."
    & docker compose --file (Join-Path $repositoryRoot "docker-compose.yml") stop postgres save-parser
}
else {
    Write-Host "PostgreSQL and the save parser remain running. Use -StopDatabase to stop them as well."
}
