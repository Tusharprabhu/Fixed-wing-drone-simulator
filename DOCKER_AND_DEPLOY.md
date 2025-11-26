# Docker & Deployment Quickstart

This repo uses a trainer-only Docker container to run ML-Agents training with a mounted workspace.

## Build the trainer container

```powershell
cd path\to\Fixed-wing-drone-simulator
# Build image (trainer-only)
docker build -t drone-trainer .
```

## Run with Docker Compose

```powershell
# Start trainer as detached service
docker compose up --build -d

# Stop
docker compose down
```

## Run Trainer locally (no Docker)

```powershell
python -m venv mlagents_venv
.\mlagents_venv\Scripts\Activate.ps1
pip install -r requirements.txt
python -m mlagents.trainers.learn Assets/DroneAgent.yaml --run-id=MyRun
```

## Notes
- The `Dockerfile` installs Python dependencies from `requirements.txt`. If you add new packages, update `requirements.txt` and rebuild the image.
- If you require ONNX export or GPU acceleration, add `onnxscript` to `requirements.txt` and use a GPU-compatible base image (NVIDIA CUDA).
- Unity Editor itself is not inside the container by default — run Unity locally and press Play to connect to the trainer. If you need fully headless Unity builds, build a Linux headless player and run it separately or inside Docker.
