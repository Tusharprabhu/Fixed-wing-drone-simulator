# Fixed-Wing Drone Simulator - Flight Controls Guide

## Overview
This is a realistic fixed-wing RC drone simulator featuring 2kg aircraft physics, SI units (m/s, meters), and aerodynamically-correct flight dynamics.

## Flight Controls

### Primary Controls
- **W / S** - Throttle Up / Down
  - W increases throttle
  - S decreases throttle

- **Arrow Keys** - Pitch & Roll
  - Up Arrow = Pitch UP (nose up)
  - Down Arrow = Pitch DOWN (nose down)  
  - Left Arrow = Roll LEFT
  - Right Arrow = Roll RIGHT
  - *Note: Rolling automatically adds small yaw input for coordinated turns*

### Camera Controls
- **Mouse Movement** - Look around (camera control)

## Aircraft Specifications

### Physical Properties
- **Mass**: 2kg RC drone
- **Thrust**: 40N maximum
- **Speed Units**: m/s (meters per second)
- **Altitude Units**: meters
- **Max Speed**: 200 m/s (velocity clamped)

### Flight Physics Features
- ✅ Realistic aerodynamic forces (lift, drag, thrust)
- ✅ Adverse yaw effects (ailerons create opposite yaw)
- ✅ Sideslip stability (weather vane effect)
- ✅ Coordinated turns (automatic yaw when rolling)
- ✅ Angle of attack calculations
- ✅ G-force display with push-over/pull-up physics
- ✅ Separate display vs physics calculations

### HUD Information
- **Speed**: Current velocity in m/s
- **Altitude**: Height above ground in meters
- **AOA**: Angle of Attack in degrees (inverted: + becomes -, - becomes +)
- **G-Force**: Load factor with combined angular velocity calculation
- **Throttle Bar**: Visual throttle position indicator

## Setup Instructions

### Runway Start
1. Place aircraft on runway starting position
2. Set throttle to minimum (S key)
3. Gradually increase throttle (W key) for takeoff
4. Use pitch up (Up Arrow) to lift off when at speed

### Flight Tips
- Use coordinated turns: bank with arrows, let automatic yaw assist
- Monitor speed and altitude on HUD
- Watch angle of attack to avoid stalls
- G-force indicator shows turning intensity

## Technical Notes

### Removed Military Features
- ❌ Missiles and weapon systems
- ❌ Afterburner 
- ❌ Air brakes and flaps
- ❌ G-force limiter
- ❌ Manual yaw controls (Q/E keys removed)

### Aerodynamic Systems
- **Roll-Yaw Fusion**: Rolling adds 0.1 yaw automatically
- **Stability**: Aircraft naturally corrects for sideslip
- **Realistic Physics**: All forces calculated from aerodynamic principles

## Troubleshooting

### Controls Not Responding
- Check PlayerInput component has correct action asset
- Verify no AI Controller attached to aircraft
- Reimport PlayerInput.inputactions file

### Physics Issues  
- Aircraft mass set to 2kg in Rigidbody
- Velocity clamping prevents physics explosion
- Separate EffectiveInput used for animations

---

**Aircraft Type**: 2kg Fixed-Wing RC Drone  
**Physics**: Realistic aerodynamic simulation  
**Units**: SI (m/s, meters)  
**Version**: Optimized for coordinated flight