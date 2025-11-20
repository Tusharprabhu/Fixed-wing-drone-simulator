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
