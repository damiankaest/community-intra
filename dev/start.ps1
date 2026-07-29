[CmdletBinding()]
param()

. (Join-Path $PSScriptRoot "common.ps1")

$repositoryRoot = Get-RepositoryRoot
$environmentFile = Join-Path $repositoryRoot ".env"
$runtimeDirectory = Join-Path $repositoryRoot ".runtime"
$processFile = Join-Path $runtimeDirectory "processes.json"
$logsDirectory = Join-Path $repositoryRoot "logs"
$frontendDirectory = Join-Path $repositoryRoot "frontend"

foreach ($command in @("docker", "dotnet", "npm")) {
    if (-not (Get-Command $command -ErrorAction SilentlyContinue)) {
        throw "Required command '$command' was not found."
    }
}

if (-not (Test-Path $environmentFile)) {
    Copy-Item (Join-Path $repositoryRoot ".env.example") $environmentFile
    Write-Warning "Created .env from .env.example. The values are intended for local development only."
}

Import-DotEnv -Path $environmentFile

if ([string]::IsNullOrWhiteSpace($env:Jwt__SigningKey)) {
    $jwtKeyBytes = New-Object byte[] 48
    [Security.Cryptography.RandomNumberGenerator]::Fill($jwtKeyBytes)
    $env:Jwt__SigningKey = [Convert]::ToBase64String($jwtKeyBytes)
}

New-Item -ItemType Directory -Force -Path $runtimeDirectory, $logsDirectory | Out-Null

if (Test-Path $processFile) {
    $existing = Get-Content $processFile -Raw | ConvertFrom-Json
    if ((Test-ProcessRunning $existing.backendPid) -or (Test-ProcessRunning $existing.frontendPid)) {
        throw "Community Intranet is already running. Use ./dev/status.ps1 or ./dev/stop.ps1."
    }
}

Write-Host "Starting PostgreSQL and the save parser..."
& docker compose --file (Join-Path $repositoryRoot "docker-compose.yml") up -d postgres save-parser
if ($LASTEXITCODE -ne 0) {
    throw "Docker Compose could not start PostgreSQL and the save parser."
}

$databaseReady = $false
for ($attempt = 1; $attempt -le 30; $attempt++) {
    & docker compose --file (Join-Path $repositoryRoot "docker-compose.yml") exec -T postgres `
        pg_isready -U $env:POSTGRES_USER -d $env:POSTGRES_DB *> $null

    if ($LASTEXITCODE -eq 0) {
        $databaseReady = $true
        break
    }

    Start-Sleep -Seconds 1
}

if (-not $databaseReady) {
    throw "PostgreSQL did not become ready in time."
}

if (-not (Test-Path (Join-Path $frontendDirectory "node_modules"))) {
    Write-Host "Installing frontend dependencies..."
    & npm --prefix $frontendDirectory install
    if ($LASTEXITCODE -ne 0) {
        throw "npm install failed."
    }
}

$backendLog = Join-Path $logsDirectory "backend.log"
$backendErrorLog = Join-Path $logsDirectory "backend.error.log"
$frontendLog = Join-Path $logsDirectory "frontend.log"
$frontendErrorLog = Join-Path $logsDirectory "frontend.error.log"

Write-Host "Starting backend on http://localhost:5080..."
$backend = Start-Process `
    -FilePath (Get-Command dotnet).Source `
    -ArgumentList @(
        "run",
        "--project",
        (Join-Path $repositoryRoot "backend/CommunityIntranet.Api/CommunityIntranet.Api.csproj"),
        "--no-launch-profile",
        "--urls",
        "http://localhost:5080"
    ) `
    -WorkingDirectory $repositoryRoot `
    -RedirectStandardOutput $backendLog `
    -RedirectStandardError $backendErrorLog `
    -PassThru

Write-Host "Starting frontend on http://localhost:5173..."
$npmExecutable = if ($IsWindows) {
    (Get-Command npm.cmd -ErrorAction Stop).Source
}
else {
    (Get-Command npm -ErrorAction Stop).Source
}

$frontend = Start-Process `
    -FilePath $npmExecutable `
    -ArgumentList @("run", "dev", "--", "--host", "0.0.0.0") `
    -WorkingDirectory $frontendDirectory `
    -RedirectStandardOutput $frontendLog `
    -RedirectStandardError $frontendErrorLog `
    -PassThru

@{
    backendPid = $backend.Id
    frontendPid = $frontend.Id
    startedAt = [DateTimeOffset]::UtcNow.ToString("O")
} | ConvertTo-Json | Set-Content $processFile

if (-not (Wait-ForUrl "http://localhost:5080/api/system/info")) {
    Write-Warning "Backend did not become reachable. Inspect ./dev/logs.ps1 backend."
}

if (-not (Wait-ForUrl "http://localhost:5173")) {
    Write-Warning "Frontend did not become reachable. Inspect ./dev/logs.ps1 frontend."
}

Write-Host ""
Write-Host "Community Intranet is running."
Write-Host "Frontend: http://localhost:5173"
Write-Host "Backend:  http://localhost:5080"
Write-Host "Swagger:  http://localhost:5080/swagger"
Write-Host "Save parser health: http://localhost:5091/health"
