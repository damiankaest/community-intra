[CmdletBinding()]
param(
    [Parameter(Mandatory = $true, Position = 0)]
    [ValidateSet("backend", "frontend")]
    [string] $Service,
    [int] $Lines = 100,
    [switch] $NoFollow
)

. (Join-Path $PSScriptRoot "common.ps1")

$repositoryRoot = Get-RepositoryRoot
$logFile = Join-Path $repositoryRoot "logs/$Service.log"

if (-not (Test-Path $logFile)) {
    throw "Log file does not exist yet: $logFile"
}

Get-Content $logFile -Tail $Lines -Wait:(-not $NoFollow)
