# Trainer-only Dockerfile for CPU
# Use this to run the ML-Agents Python trainer inside Docker. Mount the repo into /workspace.

FROM python:3.10-slim

ENV DEBIAN_FRONTEND=noninteractive
WORKDIR /workspace

# Copy only the requirements first to allow cache-friendly builds
COPY requirements.txt /workspace/requirements.txt

RUN apt-get update \
    && apt-get install -y --no-install-recommends build-essential git curl \
    && python -m pip install --upgrade pip setuptools wheel \
    && pip install --no-cache-dir -r /workspace/requirements.txt \
    && apt-get remove -y build-essential \
    && apt-get autoremove -y \
    && rm -rf /var/lib/apt/lists/* /root/.cache/pip

EXPOSE 5004

# Default command: run trainer.learn with the config file in the mounted workspace
ENTRYPOINT ["python", "-m", "mlagents.trainers.learn"]
CMD ["Assets/DroneAgent.yaml", "--run-id=DroneAgent_DockerRun"]
