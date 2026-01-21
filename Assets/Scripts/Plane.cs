using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Plane : MonoBehaviour {
    [SerializeField]
    float maxThrust;
    [SerializeField]
    float throttleSpeed;
    

    [Header("Lift")]
    [SerializeField]
    float liftPower;
    [SerializeField]
    AnimationCurve liftAOACurve;
    [SerializeField]
    float inducedDrag;
    [SerializeField]
    AnimationCurve inducedDragCurve;
    [SerializeField]
    float rudderPower;
    [SerializeField]
    AnimationCurve rudderAOACurve;
    [SerializeField]
    AnimationCurve rudderInducedDragCurve;

    [Header("Steering")]
    [SerializeField]
    Vector3 turnSpeed;
    [SerializeField]
    Vector3 turnAcceleration;
    [SerializeField]
    AnimationCurve steeringCurve;

    [Header("Drag")]
    [SerializeField]
    AnimationCurve dragForward;
    [SerializeField]
    AnimationCurve dragBack;
    [SerializeField]
    AnimationCurve dragLeft;
    [SerializeField]
    AnimationCurve dragRight;
    [SerializeField]
    AnimationCurve dragTop;
    [SerializeField]
    AnimationCurve dragBottom;
    [SerializeField]
    Vector3 angularDrag;
    
    [Header("Aerodynamic Stability")]
    [SerializeField]
    float sideslipStabilityCoefficient = 2.0f;  // Weather vane effect strength
    [SerializeField]
    float adverseYawCoefficient = 0.5f;         // Aileron-induced yaw strength
    [SerializeField]
    float stabilityDamping = 1.0f;              // Overall stability damping

    [Header("Glide Performance")]
    [SerializeField]
    float stallWarningThreshold = 0.7f;         // Lift/Weight ratio below which stall warning triggers
    [SerializeField]
    float minLiftSpeed = 5f;                    // Minimum speed for lift generation (m/s)

    [Header("Misc")]
    [SerializeField]
    List<Collider> landingGear;
    [SerializeField]
    PhysicsMaterial landingGearBrakesMaterial;
    [SerializeField]
    List<GameObject> graphics;
    [SerializeField]
    GameObject damageEffect;
    [SerializeField]
    GameObject deathEffect;
    [SerializeField]
    float initialSpeed;

    new PlaneAnimation animation;

    float throttleInput;
    Vector3 controlInput;

    Vector3 lastVelocity;
    
    // Display-only calculation variables (don't affect flight physics)
    Vector3 lastVelocityDisplay;
    Vector3 lastGForceDisplay;
    PhysicsMaterial landingGearDefaultMaterial;

    // Glide metrics tracking
    float currentLiftMagnitude;
    float currentDragMagnitude;
    Vector3 lastWorldLiftForce;
    Vector3 lastInducedDragForce;

    public bool Dead { get; private set; }

    public Rigidbody Rigidbody { get; private set; }
    public float Throttle { get; private set; }
    public Vector3 EffectiveInput { get; private set; }
    public Vector3 Velocity { get; private set; }
    public Vector3 LocalVelocity { get; private set; }
    public Vector3 LocalGForce { get; private set; }
    public Vector3 LocalAngularVelocity { get; private set; }
    public float AngleOfAttack { get; private set; }
    public float AngleOfAttackYaw { get; private set; }
    public float SideslipAngle { get; private set; }  // Beta - sideways sliding angle

    // Glide performance properties
    public float LiftToDragRatio { get; private set; }    // L/D ratio - higher = better glide
    public float SinkRate { get; private set; }           // Vertical descent rate (m/s, positive = descending)
    public float VerticalLiftComponent { get; private set; } // Vertical lift force (N)
    public float RequiredLiftForLevel { get; private set; } // Lift needed to maintain altitude (N)
    public bool IsStalling { get; private set; }          // True when lift insufficient for level flight
    public float BankAngle { get; private set; }          // Current roll angle in degrees (-180 to 180)

    // Initialize references, store landing gear default material and set initial linear velocity
    void Start() {
        animation = GetComponent<PlaneAnimation>();
        Rigidbody = GetComponent<Rigidbody>();

        if (landingGear != null && landingGear.Count > 0 && landingGear[0] != null) {
            landingGearDefaultMaterial = landingGear[0].sharedMaterial;
        }

        Rigidbody.linearVelocity = Rigidbody.rotation * new Vector3(0, 0, initialSpeed);
    }

    /// <summary>
    /// Normalize an angle from 0-360 range to -180 to +180 range
    /// </summary>
    static float NormalizeAngle(float angle) {
        return angle > 180f ? angle - 360f : angle;
    }

    // Set throttle input from player/controller. Input is ignored if plane is dead.
    public void SetThrottleInput(float input) {
        if (Dead) return;
        throttleInput = input;
    }

    // Set control input (pitch/yaw/roll) from player/controller. Clamped to magnitude 1. Ignored if dead.
    public void SetControlInput(Vector3 input) {
        if (Dead) return;
        controlInput = Vector3.ClampMagnitude(input, 1);
    }

    // Reset controls for agent training
    public void ResetControls() {
        throttleInput = 0;
        Throttle = 0;
        controlInput = Vector3.zero;
        Dead = false;
    }

    // Full reset to a given position/rotation and initial forward speed
    public void ResetTo(Vector3 position, Quaternion rotation, float forwardSpeed) {
        // Restore kinematic/physics behaviour
        Rigidbody.isKinematic = false;
        Rigidbody.position = position;
        Rigidbody.rotation = rotation;
        Rigidbody.linearVelocity = rotation * new Vector3(0, 0, forwardSpeed);
        Rigidbody.angularVelocity = Vector3.zero;

        // Reset internal state and control inputs
        throttleInput = 0f;
        Throttle = 0f;
        controlInput = Vector3.zero;
        Dead = false;

        // Clear visual/damage/death effects
        if (damageEffect != null) {
            var ps = damageEffect.GetComponent<ParticleSystem>();
            if (ps != null) {
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }
        if (deathEffect != null) {
            deathEffect.SetActive(false);
        }

        // Re-enable mesh graphics that are disabled after crash
        if (graphics != null) {
            foreach (var go in graphics) {
                if (go != null) go.SetActive(true);
            }
        }

        // Reset last velocity and local values
        lastVelocity = Vector3.zero;
        LocalGForce = Vector3.zero;
    }

    // Trigger death: stop engine, mark dead, pause damage effect and enable death effect
    void Die() {
        throttleInput = 0;
        Throttle = 0;
        Dead = true;

        damageEffect.GetComponent<ParticleSystem>().Pause();
        deathEffect.SetActive(true);
    }

    // Smoothly move throttle value toward target based on throttle input and throttleSpeed
    void UpdateThrottle(float dt) {
        float target = 0;
        if (throttleInput > 0) target = 1;

        //throttle input is [-1, 1]
        //throttle is [0, 1]
        Throttle = Utilities.MoveTo(Throttle, target, throttleSpeed * Mathf.Abs(throttleInput), dt);
    }

    // Compute angle of attack (pitch) and yaw angle of attack from local velocity
    void CalculateAngleOfAttack() {
        // If very slow, set AOA to zero
        if (LocalVelocity.sqrMagnitude < 0.1f) {
            AngleOfAttack = 0;
            AngleOfAttackYaw = 0;
            return;
        }

        float forwardSpeed = LocalVelocity.z;
        float verticalSpeed = -LocalVelocity.y;  // Note: Y is inverted for proper AOA
        float sideSpeed = LocalVelocity.x;
        
        // Prevent division by zero and extreme values
        if (Mathf.Abs(forwardSpeed) < 0.01f) {
            AngleOfAttack = 0;
            AngleOfAttackYaw = 0;
            SideslipAngle = 0;
            return;
        }
        
        // Calculate AOA with proper clamping to prevent extreme values
        AngleOfAttack = Mathf.Clamp(Mathf.Atan2(verticalSpeed, Mathf.Abs(forwardSpeed)), -1.57f, 1.57f); // ±90°
        AngleOfAttackYaw = Mathf.Clamp(Mathf.Atan2(sideSpeed, Mathf.Abs(forwardSpeed)), -1.57f, 1.57f);
        
        // Calculate sideslip angle (Beta) - how much the plane is sliding sideways
        SideslipAngle = Mathf.Atan2(LocalVelocity.x, Mathf.Abs(LocalVelocity.z));
    }

    // Estimate local G-force by differentiating velocity, applying guards and smoothing
    void CalculateGForce(float dt) {
        // Avoid bogus spikes: when dead/kinematic or bad dt, reset and exit
        if (Dead || dt <= 0f) {
            lastVelocity = Velocity;
            LocalGForce = Vector3.zero;
            return;
        }

        var invRotation = Quaternion.Inverse(Rigidbody.rotation);
        var acceleration = (Velocity - lastVelocity) / dt;

        // Guard against NaN/Infinity due to sudden engine state changes
        if (
            float.IsNaN(acceleration.x) || float.IsNaN(acceleration.y) || float.IsNaN(acceleration.z) ||
            float.IsInfinity(acceleration.x) || float.IsInfinity(acceleration.y) || float.IsInfinity(acceleration.z)
        ) {
            lastVelocity = Velocity;
            return;
        }

        // Light smoothing and clamping to avoid visual spikes during abrupt contacts
        var localA = invRotation * acceleration;
        const float maxAbs = 200f; // m/s^2 (~20g)
        localA.x = Mathf.Clamp(localA.x, -maxAbs, maxAbs);
        localA.y = Mathf.Clamp(localA.y, -maxAbs, maxAbs);
        localA.z = Mathf.Clamp(localA.z, -maxAbs, maxAbs);

        // Exponential smoothing
        LocalGForce = Vector3.Lerp(LocalGForce, localA, 0.25f);
        lastVelocity = Velocity;
    }

    // Display-only G-force calculation for HUD (doesn't affect flight physics)
    Vector3 CalculateGForceForDisplay(float dt) {
        // Simple acceleration calculation for display
        Vector3 currentVelocity = Rigidbody.linearVelocity;
        
        // Initialize or handle bad data
        if (lastVelocityDisplay == Vector3.zero || dt <= 0f) {
            lastVelocityDisplay = currentVelocity;
            return Vector3.zero;
        }
        
        // Calculate world acceleration
        Vector3 worldAcceleration = (currentVelocity - lastVelocityDisplay) / dt;
        
        // Subtract gravity to get net acceleration due to aircraft forces
        Vector3 netAcceleration = worldAcceleration - Physics.gravity;
        
        // Transform to local aircraft space for proper G-force display
        Vector3 localGForce = Quaternion.Inverse(transform.rotation) * netAcceleration;
        
        // Smooth the result for display
        localGForce = Vector3.Lerp(lastGForceDisplay, localGForce, 0.2f);
        
        // Update for next frame
        lastVelocityDisplay = currentVelocity;
        lastGForceDisplay = localGForce;
        
        return localGForce;
    }
    
    // AOA is just the pitch angle - simple and direct
    float GetAOAForDisplay() {
        float pitchAngle = NormalizeAngle(transform.eulerAngles.x);
        // Invert sign for display: user wants positive to become negative and vice-versa.
        // This affects HUD/console display only and does not modify physics calculations.
        return -pitchAngle;
    }
    
    // Public property for HUD
    public float DisplayAOA => GetAOAForDisplay();

    // Display Load Factor (G-load) using push-over formula for negative Gs
    // Formula: G = 1 - 4(v^2)/(r * 9.81)
    // This allows negative values during push-over maneuvers (diving over hills)
    public float DisplayLoadFactor {
        get {
            return CalculateLoadFactorForDisplay();
        }
    }

    float CalculateLoadFactorForDisplay() {
        // Get current velocity magnitude (m/s)
        float v = Velocity.magnitude;
        
        // Get pitch angular velocity (rad/s) - use raw value to detect direction
        float omega = LocalAngularVelocity.x;
        
        // Use smaller threshold for more sensitive detection
        if (Mathf.Abs(omega) < 1e-4f) {
            return 1.0f; // Level flight, normal gravity
        }
        
        // Calculate radius: r = v / |omega|
        float r = v / Mathf.Abs(omega);
        
        // Clamp radius to reasonable flight values (tighter turns = stronger G forces)
        r = Mathf.Clamp(r, 5f, 500f); // 5m minimum radius for tight maneuvers
        
        // Push-over formula: G = 1 - (v^2)/(r * 9.81)
        const float g = 9.81f;
        // Use the exact physics centripetal term (no arbitrary multiplier)
        float centripetalTerm = (4*v * v) / (r * g);
        
        // Apply direction: positive omega (nose down) = negative G, negative omega (nose up) = positive G (push-over)
        float gLoad;
        if (omega > 0) {
            // Nose up: push-over formula (1 - centripetal)
            gLoad = 1f - centripetalTerm;
        } else {
            // Nose down: positive G (1 + centripetal)
            gLoad = 1f + centripetalTerm;
        }
        
        // Clamp to realistic values
        return Mathf.Clamp(gLoad, -10f, 15f);
    }    // Update cached kinematic state: world velocity, local velocity, local angular velocity and AOA
    void CalculateState(float dt) {
        var invRotation = Quaternion.Inverse(Rigidbody.rotation);
        Velocity = Rigidbody.linearVelocity;
        
        // Clamp velocity to prevent physics explosion (max 200 m/s)
        if (Velocity.magnitude > 200f) {
            Velocity = Velocity.normalized * 200f;
            Rigidbody.linearVelocity = Velocity;
        }
        
        LocalVelocity = invRotation * Velocity;  //transform world velocity into local space
        LocalAngularVelocity = invRotation * Rigidbody.angularVelocity;  //transform into local space

        CalculateAngleOfAttack();
    }

    // Apply forward thrust based on Throttle and maxThrust (relative to plane)
    void UpdateThrust() {
        float thrustForce = Throttle * maxThrust;
        // Clamp thrust to prevent runaway acceleration
        thrustForce = Mathf.Clamp(thrustForce, 0, 100f); // Max 100N
        Rigidbody.AddRelativeForce(thrustForce * Vector3.forward);
    }

    // Compute aerodynamic drag from directional drag curves and apply as relative force
    void UpdateDrag() {
        var lv = LocalVelocity;
        var lv2 = lv.sqrMagnitude;  //velocity squared
        
        // Skip if velocity is too small to avoid division by zero
        if (lv2 < 0.01f) return;

        //calculate coefficient of drag depending on direction on velocity
        var coefficient = Utilities.Scale6(
            lv.normalized,
            dragRight.Evaluate(Mathf.Abs(lv.x)), dragLeft.Evaluate(Mathf.Abs(lv.x)),
            dragTop.Evaluate(Mathf.Abs(lv.y)), dragBottom.Evaluate(Mathf.Abs(lv.y)),
            dragForward.Evaluate(Mathf.Abs(lv.z)),
            dragBack.Evaluate(Mathf.Abs(lv.z))
        );

        var drag = coefficient.magnitude * lv2 * -lv.normalized;    //drag is opposite direction of velocity
        
        // Clamp drag force to prevent extreme values
        if (drag.magnitude > 1000f) {
            drag = drag.normalized * 1000f;
        }

        Rigidbody.AddRelativeForce(drag);
    }

    // Calculate lift (and induced drag) for a given angle, axis and lift curves; returns force in local space
    // LEGACY METHOD - kept for rudder lift which still uses local space
    Vector3 CalculateLiftLocal(float angleOfAttack, Vector3 rightAxis, float liftPower, AnimationCurve aoaCurve, AnimationCurve inducedDragCurve) {
        var liftVelocity = Vector3.ProjectOnPlane(LocalVelocity, rightAxis);    //project velocity onto YZ plane
        var v2 = liftVelocity.sqrMagnitude;                                     //square of velocity

        //lift = velocity^2 * coefficient * liftPower
        //coefficient varies with AOA
        var liftCoefficient = aoaCurve.Evaluate(angleOfAttack * Mathf.Rad2Deg);
        var liftForce = v2 * liftCoefficient * liftPower;

        //lift is perpendicular to velocity
        var liftDirection = Vector3.Cross(liftVelocity.normalized, rightAxis);
        var lift = liftDirection * liftForce;

        //induced drag varies with square of lift coefficient
        var dragForce = liftCoefficient * liftCoefficient;
        var dragDirection = -liftVelocity.normalized;
        var inducedDrag = dragDirection * v2 * dragForce * this.inducedDrag * inducedDragCurve.Evaluate(Mathf.Max(0, LocalVelocity.z));

        return lift + inducedDrag;
    }

    /// <summary>
    /// Calculate wing lift using world-space cross-product method.
    /// Lift is always perpendicular to BOTH the wing surface AND the relative wind.
    /// This naturally handles bank angles:
    /// - Level flight (0° roll): lift purely vertical (opposes gravity)
    /// - Banked turn (45°): lift tilted (reduced vertical component, aircraft descends without AOA increase)
    /// - Knife edge (90°): lift horizontal only (aircraft falls under gravity)
    /// - Inverted (180°): lift points downward (requires negative AOA to fly)
    /// </summary>
    /// <returns>Tuple of (worldLiftForce, localInducedDrag)</returns>
    (Vector3 worldLift, Vector3 localInducedDrag) CalculateWingLiftWorldSpace() {
        // Get world-space velocity (airflow relative to aircraft)
        Vector3 worldVelocity = Velocity;
        float speedSqr = worldVelocity.sqrMagnitude;
        
        // No lift at very low speeds
        if (speedSqr < minLiftSpeed * minLiftSpeed) {
            return (Vector3.zero, Vector3.zero);
        }
        
        // Get the wing's right axis in world space
        Vector3 wingRight = transform.right;
        
        // Project velocity onto plane perpendicular to wing (remove spanwise component)
        Vector3 liftVelocity = Vector3.ProjectOnPlane(worldVelocity, wingRight);
        float liftSpeedSqr = liftVelocity.sqrMagnitude;
        
        if (liftSpeedSqr < 0.01f) {
            return (Vector3.zero, Vector3.zero);
        }
        
        // Calculate lift coefficient from AOA curve
        float liftCoefficient = liftAOACurve.Evaluate(AngleOfAttack * Mathf.Rad2Deg);
        
        // Lift magnitude: L = 0.5 * rho * v^2 * S * Cl (simplified: v^2 * Cl * liftPower)
        float liftMagnitude = liftSpeedSqr * liftCoefficient * liftPower;
        
        // CRITICAL: Cross product gives lift direction perpendicular to both velocity AND wing
        // This automatically tilts lift based on bank angle!
        Vector3 liftDirection = Vector3.Cross(liftVelocity.normalized, wingRight).normalized;
        
        // Handle edge case where cross product is zero (velocity parallel to wing)
        if (liftDirection.sqrMagnitude < 0.01f) {
            liftDirection = transform.up; // Fallback to aircraft up
        }
        
        Vector3 worldLift = liftDirection * liftMagnitude;
        
        // Clamp lift force to prevent physics explosion
        if (worldLift.magnitude > 500f) {
            worldLift = worldLift.normalized * 500f;
        }
        
        // Induced drag stays in LOCAL body frame (opposes motion relative to aircraft structure)
        // D_induced = Cl^2 * v^2 * k (drag increases with square of lift coefficient)
        float dragCoefficient = liftCoefficient * liftCoefficient;
        Vector3 localDragDirection = -LocalVelocity.normalized;
        Vector3 localInducedDrag = localDragDirection * liftSpeedSqr * dragCoefficient * 
                                   this.inducedDrag * inducedDragCurve.Evaluate(Mathf.Max(0, LocalVelocity.z));
        
        // Clamp induced drag
        if (localInducedDrag.magnitude > 200f) {
            localInducedDrag = localInducedDrag.normalized * 200f;
        }
        
        return (worldLift, localInducedDrag);
    }

    // Compute and apply lift for wings and rudder (yaw), skipping at very low speeds
    // UPDATED: Wing lift now uses world-space cross-product for realistic bank behavior
    void UpdateLift() {
        if (LocalVelocity.sqrMagnitude < 1f) return;

        // WING LIFT - World space cross-product method
        // Automatically handles bank angle: tilted wings = tilted lift vector
        var (worldLift, localInducedDrag) = CalculateWingLiftWorldSpace();
        
        // Store for metrics calculation
        lastWorldLiftForce = worldLift;
        lastInducedDragForce = localInducedDrag;
        currentLiftMagnitude = worldLift.magnitude;

        // RUDDER LIFT - Still uses local space (yaw forces don't need world decomposition)
        var yawForce = CalculateLiftLocal(AngleOfAttackYaw, Vector3.up, rudderPower, rudderAOACurve, rudderInducedDragCurve);

        // Clamp rudder force
        if (yawForce.magnitude > 200f) {
            yawForce = yawForce.normalized * 200f;
        }

        // Apply wing lift in WORLD SPACE - critical for bank angle behavior!
        // When banked, lift vector tilts with the aircraft:
        // - At 0° bank: lift is vertical (full gravity opposition)
        // - At 45° bank: lift is tilted 45° (only 70% vertical, aircraft descends)
        // - At 90° bank: lift is horizontal (zero gravity opposition, aircraft falls)
        Rigidbody.AddForce(worldLift, ForceMode.Force);
        
        // DEBUG: Log lift forces
        if (Time.frameCount % 60 == 0) { // Log once per second (at 60 FPS)
            float liftAngleFromHorizontal = Mathf.Asin(worldLift.y / (worldLift.magnitude + 0.01f)) * Mathf.Rad2Deg;
            Debug.Log($"[LIFT] Magnitude: {worldLift.magnitude:F1}N, Y-component: {worldLift.y:F1}N, " +
                      $"Lift angle: {liftAngleFromHorizontal:F1}°, Speed: {Velocity.magnitude:F1}m/s, " +
                      $"AOA: {AngleOfAttack * Mathf.Rad2Deg:F1}°, Bank: {BankAngle:F1}°");
        }
        
        // Apply induced drag in LOCAL space (opposes aircraft-relative motion)
        Rigidbody.AddRelativeForce(localInducedDrag);
        
        // Apply rudder force in local space
        Rigidbody.AddRelativeForce(yawForce);
    }

    // Apply angular damping torque based on local angular velocity and configured angularDrag
    void UpdateAngularDrag() {
        var av = LocalAngularVelocity;
        var drag = av.sqrMagnitude * -av.normalized;    //squared, opposite direction of angular velocity
        Rigidbody.AddRelativeTorque(Vector3.Scale(drag, angularDrag), ForceMode.Acceleration);  //ignore rigidbody mass
    }

    // Compute a small change (clamped by acceleration * dt) to move angular velocity toward target
    float CalculateSteering(float dt, float angularVelocity, float targetVelocity, float acceleration) {
        var error = targetVelocity - angularVelocity;
        var accel = acceleration * dt;
        return Mathf.Clamp(error, -accel, accel);
    }

    // Convert player control input into torques applied to the rigidbody, compute EffectiveInput for animation/UI
    void UpdateSteering(float dt) {
        var speed = Mathf.Max(0, LocalVelocity.z);
        var steeringPower = steeringCurve.Evaluate(speed);

        // Asymmetric pitch: pitch up = 1.5x, pitch down = 0.75x
        float pitchMultiplier = controlInput.x > 0f ? 0.75f : 1.5f;
        float pitchTargetAV = controlInput.x * turnSpeed.x * steeringPower * pitchMultiplier;
        
        var targetAV = new Vector3(
            pitchTargetAV,                                    // Pitch (asymmetric)
            controlInput.y * turnSpeed.y * steeringPower,    // Yaw (symmetric)
            controlInput.z * turnSpeed.z * steeringPower     // Roll (symmetric)
        );
        var av = LocalAngularVelocity * Mathf.Rad2Deg;

        // Calculate basic steering corrections for pitch and roll
        var pitchCorrection = CalculateSteering(dt, av.x, targetAV.x, turnAcceleration.x * steeringPower);
        var rollCorrection = CalculateSteering(dt, av.z, targetAV.z, turnAcceleration.z * steeringPower);
        
        // AERODYNAMIC YAW FUSION - Combine three yaw torque components
        float yawCorrection = CalculateAerodynamicYawTorque(dt, av.y, targetAV.y, steeringPower, rollCorrection);

        var correction = new Vector3(pitchCorrection, yawCorrection, rollCorrection);
        Rigidbody.AddRelativeTorque(correction * Mathf.Deg2Rad, ForceMode.VelocityChange);    //ignore rigidbody mass

        var correctionInput = new Vector3(
            Mathf.Clamp((targetAV.x - av.x) / turnAcceleration.x, -1, 1),
            Mathf.Clamp((targetAV.y - av.y) / turnAcceleration.y, -1, 1),
            Mathf.Clamp((targetAV.z - av.z) / turnAcceleration.z, -1, 1)
        );

        var effectiveInput = (correctionInput + controlInput);

        EffectiveInput = new Vector3(
            Mathf.Clamp(effectiveInput.x, -1, 1),
            Mathf.Clamp(effectiveInput.y, -1, 1),
            Mathf.Clamp(effectiveInput.z, -1, 1)
        );
    }
    
    /// <summary>
    /// Calculate glide performance metrics based on current flight state.
    /// Updates: LiftToDragRatio, SinkRate, VerticalLiftComponent, IsStalling, BankAngle
    /// </summary>
    void CalculateGlideMetrics() {
        // Calculate bank angle from roll
        BankAngle = NormalizeAngle(transform.eulerAngles.z);
        
        // Calculate vertical component of lift (how much opposes gravity)
        VerticalLiftComponent = lastWorldLiftForce.y;
        
        // Required lift for level flight = Weight = mass * g
        RequiredLiftForLevel = Rigidbody.mass * 9.81f;
        
        // Stall detection: vertical lift < threshold * required lift
        // At high bank angles, less vertical lift is available, so stall threshold matters more
        float liftRatio = RequiredLiftForLevel > 0.01f ? VerticalLiftComponent / RequiredLiftForLevel : 0f;
        IsStalling = liftRatio < stallWarningThreshold && Velocity.magnitude > minLiftSpeed;
        
        // Sink rate = negative vertical velocity (positive = descending)
        SinkRate = -Rigidbody.linearVelocity.y;
        
        // Calculate total drag (parasitic + induced)
        // Note: This is approximate since we don't track parasitic drag separately
        currentDragMagnitude = lastInducedDragForce.magnitude;
        
        // Lift-to-Drag ratio
        // L/D = Lift magnitude / Total Drag magnitude
        // Higher L/D = better glide performance (gliders: 20-50, powered aircraft: 8-15)
        if (currentDragMagnitude > 0.01f) {
            LiftToDragRatio = currentLiftMagnitude / currentDragMagnitude;
            // Clamp to reasonable range
            LiftToDragRatio = Mathf.Clamp(LiftToDragRatio, 0f, 50f);
        } else {
            LiftToDragRatio = 0f;
        }
    }

    // Calculate aerodynamically-correct yaw torque combining pilot input + adverse yaw + sideslip stability
    float CalculateAerodynamicYawTorque(float dt, float currentYawVelocity, float targetYawVelocity, float steeringPower, float rollCorrection) {
        // 1. PILOT RUDDER INPUT - Basic yaw control
        float pilotYawTorque = CalculateSteering(dt, currentYawVelocity, targetYawVelocity, turnAcceleration.y * steeringPower);
        
        // 2. ADVERSE YAW - Ailerons create opposite yaw due to differential drag
        // When rolling right (positive roll), left wing goes down (more lift+drag), nose yaws left (negative)
        float adverseYawTorque = -rollCorrection * adverseYawCoefficient;
        
        // 3. SIDESLIP STABILITY - Weather vane effect fights sideways sliding
        // If sliding right (positive beta), apply left yaw torque (negative) to realign nose
        float sideslipCorrectionTorque = -SideslipAngle * sideslipStabilityCoefficient * steeringPower;
        
        // 4. TOTAL FUSED YAW TORQUE
        float totalYawTorque = pilotYawTorque + adverseYawTorque + sideslipCorrectionTorque;
        
        // Apply stability damping to prevent oscillations
        totalYawTorque *= stabilityDamping;
        
        return totalYawTorque;
    }

    // Main physics update: compute state, apply input (thrust, lift, steering, drag, angular drag), and handle dead alignment
    void FixedUpdate() {
        float dt = Time.fixedDeltaTime;

        //calculate at start, to capture any changes that happened externally
        CalculateState(dt);
        CalculateGForce(dt);
        
        // Update display-only values (for HUD consumption)
        CalculateGForceForDisplay(dt);
        
        UpdateThrottle(dt);

        if (!Dead) {
            //apply updates
            UpdateThrust();
            UpdateLift();
            UpdateSteering(dt);
            
            // Calculate glide performance metrics after forces applied
            CalculateGlideMetrics();
            
            // DEBUG: Log glide metrics once per second
            if (Time.frameCount % 60 == 0) {
                Debug.Log($"[GLIDE] L/D: {LiftToDragRatio:F1}, SinkRate: {SinkRate:F2}m/s, " +
                          $"VertLift: {VerticalLiftComponent:F1}N, Required: {RequiredLiftForLevel:F1}N, " +
                          $"Stalling: {IsStalling}");
            }
        } else {
            //align with velocity
            Vector3 up = Rigidbody.rotation * Vector3.up;
            Vector3 forward = Rigidbody.linearVelocity.normalized;
            Rigidbody.rotation = Quaternion.LookRotation(forward, up);
            
            // Reset glide metrics when dead
            LiftToDragRatio = 0f;
            SinkRate = -Rigidbody.linearVelocity.y;
            IsStalling = true;
        }

        UpdateDrag();
        UpdateAngularDrag();

        //calculate again, so that other systems can read this plane's state
        CalculateState(dt);
    }

    // Handle collisions: ignore contacts from landing gear, otherwise trigger crash/Die, make rigidbody kinematic and disable graphics
    void OnCollisionEnter(Collision collision) {
        for (int i = 0; i < collision.contactCount; i++) {
            var contact = collision.contacts[i];

            if (landingGear != null && landingGear.Contains(contact.thisCollider)) {
                continue;
            }

            // Crash - trigger death
            Die();

            Rigidbody.isKinematic = true;
            Rigidbody.position = contact.point;
            Rigidbody.rotation = Quaternion.Euler(0, Rigidbody.rotation.eulerAngles.y, 0);

            // Reset G-force after crash to avoid large spikes from velocity drop
            lastVelocity = Rigidbody.linearVelocity;
            LocalGForce = Vector3.zero;

            if (graphics != null) {
                foreach (var go in graphics) {
                    if (go != null) {
                        go.SetActive(false);
                    }
                }
            }

            return;
        }
    }
}
