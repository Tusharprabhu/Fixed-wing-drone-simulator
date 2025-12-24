# Fixed-wing Drone Simulator - Unity Project

Lightweight fixed-wing drone flight simulator in Unity, tuned to an RC-like 2 kg vehicle with SI units (meters, m/s) and a simplified HUD for testing and experimentation.

## Overview

This project focuses on flight dynamics and a clean, testable flying experience. It started from a larger flight sim base but was intentionally simplified for drone-style control and quick iteration. Weapons and most non-essential visual/weapon systems were removed to concentrate on flight behaviors.

## Features

- **Physics & Tuning** - Aerodynamic lift/drag, thrust, and G-force diagnostics; tuned for an RC-style 2 kg vehicle and stable behavior
- **Keyboard Controls** - Full flight control using W/S for throttle, Arrow keys for pitch/roll, Q/E for yaw
- **Runway Start** - Plane begins stationary on the runway for realistic takeoff experience
- **Real-time HUD** - Displays airspeed (m/s), altitude (m), angle of attack (display-only, pitch), G-load (displayed using a push-over formula), throttle, and heading
-- **Control Surfaces** - Working ailerons, elevators, and rudder (flaps/airbrakes/afterburners removed for this simplified build)

## Controls

| Key | Action |
|-----|--------|
| W/S | Throttle Up/Down |
| ↑/↓ | Pitch Up/Down |
| ←/→ | Roll Left/Right |
| Q/E | Yaw Left/Right |
| F | Toggle Flaps |
| H | Toggle Help |

## Quick Start

1. Open project in Unity 2021.3.10f1 or later
2. Load the Main scene
3. Press Play
4. Use W to throttle up; the vehicle is tuned for RC agility (target cruise speed ≈ 20–25 m/s)
5. Press Up Arrow to take off
6. Fly!

## HUD Information (summary)

HUD shows essential flight data in SI units. Key behaviors:
- Airspeed in m/s
- Altitude in meters (m)
- Angle of Attack (AOA) is simplified to be the aircraft's pitch angle (display only) — the code uses a dedicated display method so flight physics are not affected.
- G-load (display only) is computed using a push-over model: G = 1 - v^2/(r × 9.81) for nose-down dynamics and G = 1 + v^2/(r × 9.81) for nose-up; displayed value can be negative for push-overs.

Note: HUD variables are computed for display only; the underlying physics uses `LocalGForce` and standard aerodynamics.

## Credits

Base project derived from the original FlightSim project by [vazgriz](https://github.com/vazgriz/FlightSim). This fork adapts the simulator for simpler RC-style drone testing and learning.


## pip list
(venv_mlagents) PS D:\unity_stack\Fixed-wing-drone-simulator> pip list
Package                 Version
----------------------- ----------
absl-py                 2.3.1     
anyio                   4.12.0    
attrs                   25.4.0    
cattrs                  1.5.0
certifi                 2025.11.12
charset-normalizer      3.4.4
click                   8.3.1
cloudpickle             3.1.2
colorama                0.4.6
exceptiongroup          1.3.1
filelock                3.20.1
fsspec                  2025.12.0
grpcio                  1.48.2
gym                     0.26.2
gym-notices             0.1.0
h11                     0.16.0
h5py                    3.15.1
hf-xet                  1.2.0
httpcore                1.0.9
httpx                   0.28.1
huggingface_hub         1.2.3
idna                    3.11
Jinja2                  3.1.6
Markdown                3.10
MarkupSafe              3.0.3
mlagents                1.1.0
mlagents-envs           1.1.0
mpmath                  1.3.0
networkx                3.4.2
numpy                   1.23.5
onnx                    1.15.0
packaging               25.0
PettingZoo              1.15.0
pillow                  12.0.0
pip                     23.0.1
protobuf                3.20.3
pypiwin32               223
pywin32                 311
PyYAML                  6.0.3
requests                2.32.5
setuptools              65.5.0
shellingham             1.5.4
six                     1.17.0
sympy                   1.14.0
tensorboard             2.20.0
tensorboard-data-server 0.7.2
torch                   2.0.0
torchvision             0.15.1
tqdm                    4.67.1
typer-slim              0.20.1
typing_extensions       4.15.0
urllib3                 2.6.2
Werkzeug                3.1.4