Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Get-RepositoryRoot {
    return (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
}

function Import-DotEnv {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Path
    )

    if (-not (Test-Path $Path)) {
        throw "Environment file not found: $Path"
    }

    foreach ($line in Get-Content $Path) {
        $trimmed = $line.Trim()
        if (-not $trimmed -or $trimmed.StartsWith("#")) {
            continue
        }

        $separator = $trimmed.IndexOf("=")
        if ($separator -le 0) {
            continue
        }

        $name = $trimmed.Substring(0, $separator).Trim()
        $value = $trimmed.Substring($separator + 1).Trim()
        [Environment]::SetEnvironmentVariable($name, $value, "Process")
    }
}

function Test-ProcessRunning {
    param(
        [AllowNull()]
        [object] $ProcessId
    )

    if ($null -eq $ProcessId) {
        return $false
    }

    return $null -ne (Get-Process -Id ([int] $ProcessId) -ErrorAction SilentlyContinue)
}

function Stop-ManagedProcess {
    param(
        [Parameter(Mandatory = $true)]
        [int] $TargetId
    )

    if (-not (Test-ProcessRunning $TargetId)) {
        return
    }

    if ($IsWindows) {
        & taskkill /PID $TargetId /T /F | Out-Null
        return
    }

    Stop-Process -Id $TargetId -Force -ErrorAction SilentlyContinue
}

function Wait-ForUrl {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Url,
        [int] $Attempts = 40
    )

    for ($attempt = 1; $attempt -le $Attempts; $attempt++) {
        try {
            $response = Invoke-WebRequest -Uri $Url -UseBasicParsing -TimeoutSec 2
            if ($response.StatusCode -ge 200 -and $response.StatusCode -lt 500) {
                return $true
            }
        }
        catch {
            Start-Sleep -Milliseconds 500
        }
    }

    return $false
}
