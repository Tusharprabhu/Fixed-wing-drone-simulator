# Complete ML-Agents Training Pipeline: Imitation Learning → PPO

## 🎯 Overview
This guide walks you through:
1. **Setting up visible checkpoints and boundaries** in your scene
2. **Recording human demonstrations** (imitation learning)
3. **Training with GAIL** (Generative Adversarial Imitation Learning)
4. **Fine-tuning with PPO** (Proximal Policy Optimization)

---

## ✅ Current Setup Status

### Code Status
- ✅ `DroneAgent.cs` - Discrete actions (pitch/roll), waypoint tracking, observations
- ✅ `WaypointReward.cs` - Green checkpoint collection with rewards
- ✅ `BoundaryWall.cs` - Invisible penalty walls
- ✅ `DroneAgent.yaml` - PPO configuration (256 units × 2 layers)
- ✅ `DroneAgent_Imitation.yaml` - GAIL configuration for imitation phase
- ✅ ML-Agents Python package installed

### What You Need to Do in Unity Editor
1. Create visible green checkpoints in the scene
2. Create semi-transparent red boundary walls
3. Attach scripts to GameObjects
4. Record demonstrations
5. Run training

---

## 📍 Part 1: Create Visible Checkpoints in Unity

### Step 1: Create Checkpoint Prefab

1. **Create a new Sphere:**
   - Hierarchy → Right-click → 3D Object → Sphere
   - Name it: `Checkpoint`

2. **Make it green and glowing:**
   - Select `Checkpoint`
   - In Inspector, click on `Material` (under Mesh Renderer)
   - Create → Material → Name it `CheckpointMaterial`
   - Set properties:
     - **Albedo Color:** Green (R=0, G=255, B=0)
     - **Emission:** Check the box
     - **Emission Color:** Bright green (R=0, G=255, B=0)
     - **Emission Intensity:** 2.0

3. **Add trigger collider:**
   - Select `Checkpoint`
   - In Sphere Collider component, check **Is Trigger**

4. **Add WaypointReward script:**
   - Select `Checkpoint`
   - Add Component → Scripts → `WaypointReward`
   - Set parameters:
     - Reward Amount: `10`
     - Respawn After Collection: ✓ (checked)
     - Respawn Delay: `2`
     - Respawn Area Min: `(-100, 10, -100)`
     - Respawn Area Max: `(100, 50, 100)`
     - Waypoint Color: Green
     - Glow Intensity: `2`
     - Rotation Speed: `50`

5. **Scale for visibility:**
   - Transform → Scale: `(3, 3, 3)` or larger

6. **Make it a prefab:**
   - Drag `Checkpoint` from Hierarchy to Project panel (Assets/Prefabs folder)
   - Now you have a reusable prefab!

### Step 2: Place Checkpoints in Scene

**Beginner Circuit (10 checkpoints):**

Create 10 instances of your checkpoint prefab at these positions:

```
Checkpoint_1:  Position (0, 10, 50)     - Just ahead
Checkpoint_2:  Position (30, 15, 80)    - Right turn, climb
Checkpoint_3:  Position (60, 20, 80)    - Continue right
Checkpoint_4:  Position (80, 20, 50)    - Turn back
Checkpoint_5:  Position (80, 20, 0)     - Heading home
Checkpoint_6:  Position (60, 25, -30)   - Climb left
Checkpoint_7:  Position (30, 30, -50)   - Peak altitude
Checkpoint_8:  Position (0, 25, -50)    - Level center
Checkpoint_9:  Position (-30, 20, -30)  - Descend left
Checkpoint_10: Position (0, 15, 0)      - Return to start
```

**Quick Method:**
- Select your `Checkpoint` prefab in Project
- Drag into scene
- Set position in Transform
- Repeat 10 times

---

## 🧱 Part 2: Create Visible Boundary Walls

### Step 1: Create Boundary Wall Prefab

1. **Create a cube:**
   - Hierarchy → 3D Object → Cube
   - Name it: `BoundaryWall`

2. **Make it semi-transparent red:**
   - Create → Material → `BoundaryMaterial`
   - Set **Rendering Mode** to `Transparent`
   - **Albedo Color:** Red with alpha (R=255, G=0, B=0, A=100) — semi-transparent!
   - **Emission:** Optional light red glow

3. **Configure collider:**
   - Box Collider → Check **Is Trigger**

4. **Add BoundaryWall script:**
   - Add Component → Scripts → `BoundaryWall`
   - Set parameters:
     - Penalty Amount: `-5`
     - End Episode On Hit: ☐ (unchecked for soft boundaries)

5. **Scale to wall size:**
   - For vertical walls: Scale `(200, 100, 1)` — thin flat wall
   - For horizontal walls: Scale `(200, 1, 200)` — thin floor/ceiling

### Step 2: Place 6 Boundary Walls (Box around training area)

**Training area: 200m × 100m × 200m**

Create 6 walls:

```
Wall_Front:  Position (0, 50, 100),   Scale (200, 100, 1)   - North boundary
Wall_Back:   Position (0, 50, -100),  Scale (200, 100, 1)   - South boundary
Wall_Left:   Position (-100, 50, 0),  Scale (1, 100, 200)   - West boundary
Wall_Right:  Position (100, 50, 0),   Scale (1, 100, 200)   - East boundary
Wall_Top:    Position (0, 100, 0),    Scale (200, 1, 200)   - Ceiling
Wall_Bottom: Position (0, 0, 0),      Scale (200, 1, 200)   - Ground (hard boundary)
```

**For the ground wall (Wall_Bottom):**
- Set `End Episode On Hit` to ✓ (checked) — crash ends episode

---

## 🎮 Part 3: Test the Environment Visually

1. **Press Play in Unity**
2. **Heuristic controls (manual flying):**
   - W / Up Arrow: Pitch up
   - S / Down Arrow: Pitch down
   - A / Left Arrow: Roll left
   - D / Right Arrow: Roll right

3. **What you should see:**
   - ✅ Green glowing checkpoints rotating in the air
   - ✅ Semi-transparent red walls marking boundaries
   - ✅ Console messages: "Waypoint collected! Total: X, Reward: +10"
   - ✅ Console warnings: "Boundary hit! Penalty: -5"

4. **If checkpoints are too small:** Increase Scale to (5, 5, 5)
5. **If walls are invisible:** Check material Rendering Mode = Transparent, Alpha < 255

---

## 📹 Part 4: Record Human Demonstrations (Imitation Learning)

### Why Imitation Learning First?
- **Faster initial training** — agent learns basic flight from your demos
- **Better exploration** — starts with reasonable behaviors
- **Reduces random failures** — fewer crashes during early PPO

### Step 1: Enable Demo Recording

1. **Open Unity**
2. **Select your Drone GameObject**
3. **In DroneAgent component:**
   - Find `Behavior Type` dropdown
   - Set to: **Heuristic Only**
   - This lets you fly manually while recording

4. **Add Demonstration Recorder component:**
   - Select Drone
   - Add Component → ML Agents → `Demonstration Recorder`
   - Set parameters:
     - **Demo Name:** `DroneAgent_demos`
     - **Record:** ✓ (checked)
     - **Num Steps To Record:** `10000` (will record multiple episodes)

### Step 2: Record Good Flight Demonstrations

1. **Press Play**
2. **Fly the drone yourself** using W/A/S/D:
   - Collect 5-10 checkpoints per episode
   - Avoid crashing
   - Fly smoothly (not erratic)
3. **Record 3-5 episodes** (crashes will reset, keep recording)
4. **Press Stop**

### Step 3: Find Your Demo File

- Look in: `Assets/Demonstrations/DroneAgent_demos.demo`
- This file contains your recorded episodes

---

## 🧠 Part 5: Train with Imitation Learning (GAIL)

### Step 1: Prepare for Training

1. **Set agent back to ML-Agents control:**
   - Select Drone
   - DroneAgent component → `Behavior Type`: **Default**
   - Remove or disable `Demonstration Recorder`

2. **Save your scene** (Ctrl+S)

### Step 2: Start Imitation Training

Open PowerShell in your project root:

```powershell
cd D:\unity_ws\Fixed-wing-drone-simulator

mlagents-learn Assets/DroneAgent_Imitation.yaml --run-id=Drone_Imitation --force
```

When you see: **"Start training by pressing the Play button in the Unity Editor"**

3. **Press Play in Unity**

### What Happens:
- Agent learns from your demonstrations using GAIL
- Training runs for ~200,000 steps (10-20 minutes)
- Watch terminal for:
  - `Mean Reward` increasing
  - `Policy Loss` decreasing
  - Checkpoints collected per episode

### Step 3: Monitor Training

**In terminal:**
```powershell
# In a second PowerShell window
tensorboard --logdir results
```

Open browser: http://localhost:6006

**Watch these metrics:**
- Mean Cumulative Reward (should increase)
- Episode Length (should stabilize)
- GAIL.Policy Loss (should decrease)

### Step 4: When to Stop

Stop when:
- Mean reward plateaus (stops improving)
- Agent consistently collects 3-5 checkpoints per episode
- Usually after ~150,000-200,000 steps

**To stop:**
- Press Stop in Unity
- Ctrl+C in terminal

**Your trained model is saved at:**
`results/Drone_Imitation/DroneAgent.onnx`

---

## 🚀 Part 6: Fine-Tune with PPO (Reinforcement Learning)

### Why PPO After Imitation?
- **Optimize beyond demos** — learns better strategies than human
- **Handles edge cases** — discovers recovery maneuvers
- **Maximizes reward** — fine-tunes for checkpoint speed and efficiency

### Step 1: Resume Training with PPO

Use the imitation model as initialization:

```powershell
mlagents-learn Assets/DroneAgent.yaml --run-id=Drone_PPO --initialize-from=Drone_Imitation --force
```

When prompted:

3. **Press Play in Unity**

### What Happens:
- Agent starts with your imitation-learned policy
- PPO refines it through trial-and-error
- Training runs for ~500,000 steps (30-60 minutes)

### Step 2: Monitor PPO Training

**Watch for:**
- Mean Reward continuing to increase
- Agent collecting 8-10+ checkpoints per episode
- Smooth flight through the circuit

### Step 3: Training Complete

**Final model saved at:**
`results/Drone_PPO/DroneAgent.onnx`

---

## 🎓 Part 7: Deploy Trained Model

### Load Model for Inference

1. **In Unity, select Drone**
2. **DroneAgent component:**
   - `Behavior Type`: **Inference Only**
   - `Model`: Drag `DroneAgent.onnx` from `results/Drone_PPO/`
3. **Press Play** — watch the trained agent fly!

---

## 📊 Expected Results

### After Imitation (GAIL):
- Collects 3-5 checkpoints per episode
- Basic flight stability
- Follows general circuit path

### After PPO:
- Collects 8-10+ checkpoints per episode
- Smooth, efficient navigation
- Recovers from disturbances
- Near-optimal flight paths

---

## 🐛 Troubleshooting

### Checkpoints not visible:
- Check Scale (increase to 5,5,5)
- Verify Emission enabled on material
- Check Glow Intensity = 2

### Walls not visible:
- Material Rendering Mode = Transparent
- Albedo Alpha < 255
- Check walls are in camera view

### Agent not collecting checkpoints:
- Verify both agent and checkpoint have colliders
- Check "Is Trigger" enabled on checkpoints
- Confirm WaypointReward script attached

### Demo recording not working:
- Behavior Type = Heuristic Only
- Demonstration Recorder → Record ✓
- Fly for at least 30 seconds

### Training not starting:
- Unity must be in Play mode
- Check terminal for errors
- Verify .yaml path is correct

---

## 📁 File Structure After Setup

```
Fixed-wing-drone-simulator/
├── Assets/
│   ├── Scenes/
│   │   └── Main.unity (with checkpoints + walls)
│   ├── Prefabs/
│   │   ├── Checkpoint.prefab
│   │   └── BoundaryWall.prefab
│   ├── Materials/
│   │   ├── CheckpointMaterial.mat
│   │   └── BoundaryMaterial.mat
│   ├── Demonstrations/
│   │   └── DroneAgent_demos.demo
│   ├── Scripts/
│   │   ├── DroneAgent.cs
│   │   ├── WaypointReward.cs
│   │   └── BoundaryWall.cs
│   ├── DroneAgent.yaml (PPO config)
│   └── DroneAgent_Imitation.yaml (GAIL config)
├── results/
│   ├── Drone_Imitation/
│   │   └── DroneAgent.onnx
│   └── Drone_PPO/
│       └── DroneAgent.onnx (final model)
```

---

## 🎯 Quick Command Reference

```powershell
# Record demos (Unity: Behavior = Heuristic, Recorder attached)
# Fly manually with WASD, collect checkpoints

# Train with imitation learning
mlagents-learn Assets/DroneAgent_Imitation.yaml --run-id=Drone_Imitation --force

# Monitor training
tensorboard --logdir results

# Fine-tune with PPO
mlagents-learn Assets/DroneAgent.yaml --run-id=Drone_PPO --initialize-from=Drone_Imitation --force

# Test trained model (Unity: Behavior = Inference, load .onnx)
```

---

## ✅ Pre-Training Checklist

- [ ] Green checkpoints visible and glowing in scene (10+ placed)
- [ ] Red boundary walls visible and semi-transparent (6 walls)
- [ ] Drone has DroneAgent component attached
- [ ] Plane has Rigidbody and Plane script
- [ ] All scripts compile (no errors)
- [ ] Heuristic controls work (W/A/S/D fly the drone)
- [ ] Checkpoints trigger "Waypoint collected!" messages
- [ ] Boundaries trigger "Boundary hit!" messages
- [ ] Demo file created in Assets/Demonstrations/
- [ ] ML-Agents Python installed (`pip list | grep mlagents`)

**You're ready to train!** 🚀
