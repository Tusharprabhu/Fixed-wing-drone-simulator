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
    PhysicsMaterial landingGearDefaultMaterial;

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

    // Initialize references, store landing gear default material and set initial linear velocity
    void Start() {
        animation = GetComponent<PlaneAnimation>();
        Rigidbody = GetComponent<Rigidbody>();

        if (landingGear != null && landingGear.Count > 0 && landingGear[0] != null) {
            landingGearDefaultMaterial = landingGear[0].sharedMaterial;
        }

        Rigidbody.linearVelocity = Rigidbody.rotation * new Vector3(0, 0, initialSpeed);
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
            return;
        }
        
        // Calculate AOA with proper clamping to prevent extreme values
        AngleOfAttack = Mathf.Clamp(Mathf.Atan2(verticalSpeed, Mathf.Abs(forwardSpeed)), -1.57f, 1.57f); // ±90°
        AngleOfAttackYaw = Mathf.Clamp(Mathf.Atan2(sideSpeed, Mathf.Abs(forwardSpeed)), -1.57f, 1.57f);
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

    // Update cached kinematic state: world velocity, local velocity, local angular velocity and AOA
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
    Vector3 CalculateLift(float angleOfAttack, Vector3 rightAxis, float liftPower, AnimationCurve aoaCurve, AnimationCurve inducedDragCurve) {
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

    // Compute and apply lift for wings and rudder (yaw), skipping at very low speeds
    void UpdateLift() {
        if (LocalVelocity.sqrMagnitude < 1f) return;

        var liftForce = CalculateLift(
            AngleOfAttack, Vector3.right,
            liftPower,
            liftAOACurve,
            inducedDragCurve
        );

        var yawForce = CalculateLift(AngleOfAttackYaw, Vector3.up, rudderPower, rudderAOACurve, rudderInducedDragCurve);

        // Clamp lift forces to prevent physics explosion
        if (liftForce.magnitude > 500f) {
            liftForce = liftForce.normalized * 500f;
        }
        if (yawForce.magnitude > 200f) {
            yawForce = yawForce.normalized * 200f;
        }

        Rigidbody.AddRelativeForce(liftForce);
        Rigidbody.AddRelativeForce(yawForce);
    }

    // Apply angular damping torque based on local angular velocity and configured angularDrag
    void UpdateAngularDrag() {
        var av = LocalAngularVelocity;
        var drag = av.sqrMagnitude * -av.normalized;    //squared, opposite direction of angular velocity
        Rigidbody.AddRelativeTorque(Vector3.Scale(drag, angularDrag), ForceMode.Acceleration);  //ignore rigidbody mass
    }

    // Estimate a G-force vector from angular velocity and velocity (auxiliary calculation)
    Vector3 CalculateGForce(Vector3 angularVelocity, Vector3 velocity) {
        //estiamte G Force from angular velocity and velocity
        //Velocity = AngularVelocity * Radius
        //G = Velocity^2 / R
        //G = (Velocity * AngularVelocity * Radius) / Radius
        //G = Velocity * AngularVelocity
        //G = V cross A
        return Vector3.Cross(angularVelocity, velocity);
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

        var targetAV = Vector3.Scale(controlInput, turnSpeed * steeringPower);
        var av = LocalAngularVelocity * Mathf.Rad2Deg;

        var correction = new Vector3(
            CalculateSteering(dt, av.x, targetAV.x, turnAcceleration.x * steeringPower),
            CalculateSteering(dt, av.y, targetAV.y, turnAcceleration.y * steeringPower),
            CalculateSteering(dt, av.z, targetAV.z, turnAcceleration.z * steeringPower)
        );

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

    // Main physics update: compute state, apply input (thrust, lift, steering, drag, angular drag), and handle dead alignment
    void FixedUpdate() {
        float dt = Time.fixedDeltaTime;

        //calculate at start, to capture any changes that happened externally
        CalculateState(dt);
        CalculateGForce(dt);
        
        // Continuous speed and angle logging
        Vector3 eulerAngles = transform.eulerAngles;
        Debug.Log($"Speed: {Rigidbody.linearVelocity.magnitude:F1} m/s | AOA: {AngleOfAttack*Mathf.Rad2Deg:F1}° | Angles X:{eulerAngles.x:F1}° Y:{eulerAngles.y:F1}° Z:{eulerAngles.z:F1}°");

        //handle user input
        UpdateThrottle(dt);

        if (!Dead) {
            //apply updates
            UpdateThrust();
            UpdateLift();
            UpdateSteering(dt);
        } else {
            //align with velocity
            Vector3 up = Rigidbody.rotation * Vector3.up;
            Vector3 forward = Rigidbody.linearVelocity.normalized;
            Rigidbody.rotation = Quaternion.LookRotation(forward, up);
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
