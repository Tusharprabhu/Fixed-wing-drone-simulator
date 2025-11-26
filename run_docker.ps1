param(
    [string]$RunId = "DroneAgent_DockerRun",
    [string]$ComposeFile = "docker-compose.yml"
)

$root = Split-Path -Parent $MyInvocation.MyCommand.Definition
cd $root

Write-Host "Building docker image (trainer)..."
docker build -t drone-trainer .

Write-Host "Starting docker-compose ($ComposeFile) ..."
if (-not (Test-Path $ComposeFile)) {
    Write-Host "Compose file not found: $ComposeFile" -ForegroundColor Yellow
    exit 1
}

docker compose up --build -d

Write-Host "Trainer started on port 5004. Run ID: $RunId"
Write-Host "To stop: docker compose down"
