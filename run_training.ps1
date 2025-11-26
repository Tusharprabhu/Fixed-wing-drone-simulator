param(
    [string]$RunId = "DroneAgent_Run1",
    [string]$Config = "Assets/DroneAgent.yaml",
    [switch]$NoActivate
)

# Path to project root
$projRoot = Split-Path -Parent $MyInvocation.MyCommand.Definition

# Virtualenv activation script
$activate = Join-Path $projRoot "mlagents_venv\Scripts\Activate.ps1"
$python = Join-Path $projRoot "mlagents_venv\Scripts\python.exe"

if (-not $NoActivate) {
    if (Test-Path $activate) {
        Write-Host "Activating virtualenv..."
        & $activate
    } else {
        Write-Warning "Activation script not found at $activate. Continuing without activating.";
    }
}

if (-not (Test-Path $python)) {
    Write-Error "Python executable not found at $python. Make sure your venv is created and path is correct.";
    exit 1
}

$fullConfig = if ($Config -like "*:*") { $Config } else { Join-Path $projRoot $Config }

Write-Host "Starting ML-Agents trainer"
Write-Host "Config: $fullConfig"
Write-Host "RunId: $RunId"

& $python -m mlagents.trainers.learn $fullConfig --run-id=$RunId
