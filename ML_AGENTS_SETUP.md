# ML-Agents Training Environment Setup Guide

## Green Reward Checkpoints

### Creating Waypoints in Unity:

1. **Create Checkpoint GameObject:**
   - Right-click in Hierarchy → 3D Object → Sphere
   - Name it "Checkpoint"
   - Scale: Set to (2, 2, 2) for visibility

2. **Add Trigger Collider:**
   - Select the Checkpoint
   - In Inspector, check "Is Trigger" on the Sphere Collider

3. **Attach WaypointReward Script:**
   - Add Component → Scripts → WaypointReward
   - Configure settings:
     - **Reward Amount:** 10 (default) - points given when collected
     - **Respawn After Collection:** ✓ (checked)
     - **Respawn Delay:** 2 seconds
     - **Respawn Area Min/Max:** Define the volume where waypoints respawn
     - **Waypoint Color:** Green (default)
     - **Glow Intensity:** 2 (for visibility)
     - **Rotation Speed:** 50 (visual effect)

4. **Create Multiple Checkpoints:**
   - Duplicate the checkpoint (Ctrl+D)
   - Place them throughout your training area
   - Recommended: 5-10 waypoints for initial training

### Waypoint Behavior:
- **Green glow** with emission shader for high visibility
- **Rotates** continuously for dynamic appearance
- **Respawns** at random positions within defined area after collection
- **Awards +10 points** (configurable) to the agent

---

## Invisible Boundary Walls

### Creating Boundary Walls in Unity:

1. **Create Wall GameObject:**
   - Right-click in Hierarchy → 3D Object → Cube
   - Name it "BoundaryWall"
   - Scale to create a large wall (e.g., X=200, Y=100, Z=1 for a vertical wall)

2. **Make it Invisible:**
   - Select the BoundaryWall
   - In Inspector, uncheck the **Mesh Renderer** component (disable it)
   - The collider remains active but invisible

3. **Configure as Trigger:**
   - Select the Box Collider component
   - Check "Is Trigger"

4. **Attach BoundaryWall Script:**
   - Add Component → Scripts → BoundaryWall
   - Configure settings:
     - **Penalty Amount:** -5 (default) - negative reward when hit
     - **End Episode On Hit:** ☐ (unchecked for soft boundaries) or ✓ (checked for hard boundaries)

5. **Create Full Boundary Box:**
   - Create 6 walls: Front, Back, Left, Right, Top, Bottom
   - Position them to enclose your training area
   - Example positions for 200x100x200 area:
     - Front Wall: Z=100
     - Back Wall: Z=-100
     - Left Wall: X=-100
     - Right Wall: X=100
     - Top Wall: Y=100
     - Bottom Wall: Y=0

### Boundary Types:

**Soft Boundaries (End Episode On Hit = unchecked):**
- Agent gets -5 penalty but continues flying
- Good for teaching avoidance without harsh resets
- Recommended for intermediate training

**Hard Boundaries (End Episode On Hit = checked):**
- Agent gets -5 penalty AND episode ends
- Forces agent to stay strictly within bounds
- Recommended for advanced training

---

## Training Area Setup Example

### Complete Environment Layout:

```
Training Area: 200m x 100m x 200m
├── Terrain (at Y=0)
├── Runway (500m long, at Y=0.25)
├── Drone (starting at X=0, Y=10, Z=0)
├── Checkpoints (5-10 green spheres)
│   └── Respawn area: X=[-100,100], Y=[10,50], Z=[-100,100]
└── Boundary Walls (6 invisible planes)
    ├── Front (Z=100)
    ├── Back (Z=-100)
    ├── Left (X=-100)
    ├── Right (X=100)
    ├── Top (Y=100)
    └── Bottom (Y=0, soft boundary)
```

### Recommended Settings for Training:

**DroneAgent.yaml:**
```yaml
behaviors:
  DroneAgent:
    trainer_type: ppo
    max_steps: 500000
    time_horizon: 64
```

**Checkpoint Configuration:**
- Reward Amount: 10
- Respawn: Yes
- Rotation: 50 deg/s for visibility

**Boundary Configuration:**
- Side walls: Penalty -5, End Episode = No
- Ground: Penalty -10, End Episode = Yes (crash)
- Top: Penalty -3, End Episode = No

---

## Testing the Setup

### Manual Test (Heuristic Mode):

1. In Unity, click Play
2. Control the drone with keyboard:
   - Arrow Keys: Roll/Pitch
   - Space: Throttle
3. Fly through green checkpoints - watch console for "+10 reward"
4. Hit boundary walls - watch console for "-5 penalty"

### Training Test:

Run from terminal in project directory:
```bash
mlagents-learn Assets/DroneAgent.yaml --run-id=CheckpointTest --force
```

Watch for:
- "Waypoint collected!" messages in Unity console
- "Boundary hit!" warnings
- Increasing cumulative reward in terminal output

---

## Tips for Effective Training

1. **Start with more checkpoints** (8-10) for easier initial learning
2. **Use soft boundaries** during early training phases
3. **Gradually reduce checkpoint density** as agent improves
4. **Monitor waypoint collection rate** - aim for 1-2 per episode initially
5. **Adjust reward amounts** if agent prioritizes wrong behaviors
6. **Use curriculum learning:** Start with close checkpoints, increase spacing

---

## Troubleshooting

**Checkpoints not detected:**
- Ensure both Checkpoint and Drone have colliders
- Verify "Is Trigger" is checked on checkpoint
- Check DroneAgent script is attached to drone

**Boundaries not working:**
- Verify BoundaryWall script is attached
- Check collider "Is Trigger" is enabled
- Ensure Mesh Renderer is disabled (invisible)

**Waypoints not respawning:**
- Check "Respawn After Collection" is enabled
- Verify respawn area min/max values are valid (min < max)
- Look for errors in Unity Console
