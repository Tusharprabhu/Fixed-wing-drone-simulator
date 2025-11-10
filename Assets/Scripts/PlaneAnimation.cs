using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlaneAnimation : MonoBehaviour {
    [SerializeField]
    List<GameObject> afterburnerGraphics;
    [SerializeField]
    float afterburnerThreshold;
    [SerializeField]
    float afterburnerMinSize;
    [SerializeField]
    float afterburnerMaxSize;
    [SerializeField]
    float maxAileronDeflection;
    [SerializeField]
    float maxElevatorDeflection;
    [SerializeField]
    float maxRudderDeflection;
    [SerializeField]
    float airbrakeDeflection;
    [SerializeField]
    float flapsDeflection;
    [SerializeField]
    float deflectionSpeed;
    [SerializeField]
    Transform rightAileron;
    [SerializeField]
    Transform leftAileron;
    [SerializeField]
    List<Transform> elevators;
    [SerializeField]
    List<Transform> rudders;
    [SerializeField]
    Transform airbrake;
    [SerializeField]
    List<Transform> flaps;

    [Header("Propeller (for drones)")]
    [SerializeField]
    Transform propeller;
    [SerializeField]
    float propellerMaxRPM = 2000f;
    [SerializeField]
    Vector3 propellerAxis = Vector3.forward;

    Plane plane;
    List<Transform> afterburnersTransforms;
    Dictionary<Transform, Quaternion> neutralPoses;
    Vector3 deflection;
    float airbrakePosition;
    float flapsPosition;
    float propellerRotation;

    void Start() {
        plane = GetComponent<Plane>();
        afterburnersTransforms = new List<Transform>();
        neutralPoses = new Dictionary<Transform, Quaternion>();

        // Safely initialize afterburner graphics (avoid null reference)
        if (afterburnerGraphics != null) {
            foreach (var go in afterburnerGraphics) {
                if (go != null) {
                    afterburnersTransforms.Add(go.GetComponent<Transform>());
                }
            }
        }

        AddNeutralPose(leftAileron);
        AddNeutralPose(rightAileron);
        AddNeutralPose(propeller);

        if (elevators != null) {
            foreach (var t in elevators) {
                AddNeutralPose(t);
            }
        }

        if (rudders != null) {
            foreach (var t in rudders) {
                AddNeutralPose(t);
            }
        }

        AddNeutralPose(airbrake);

        if (flaps != null) {
            foreach (var t in flaps) {
                AddNeutralPose(t);
            }
        }
    }

    void AddNeutralPose(Transform transform) {
        if (transform != null) {
            neutralPoses.Add(transform, transform.localRotation);
        }
    }

    Quaternion CalculatePose(Transform transform, Quaternion offset) {
        if (transform != null && neutralPoses.ContainsKey(transform)) {
            return neutralPoses[transform] * offset;
        }
        return Quaternion.identity;
    }

    void UpdateAfterburners() {
        // Skip if no afterburner graphics assigned (for drones)
        if (afterburnerGraphics == null || afterburnerGraphics.Count == 0) {
            return;
        }

        float throttle = plane.Throttle;
        float afterburnerT = Mathf.Clamp01(Mathf.InverseLerp(afterburnerThreshold, 1, throttle));
        float size = Mathf.Lerp(afterburnerMinSize, afterburnerMaxSize, afterburnerT);

        if (throttle >= afterburnerThreshold) {
            for (int i = 0; i < afterburnerGraphics.Count && i < afterburnersTransforms.Count; i++) {
                if (afterburnerGraphics[i] != null) {
                    afterburnerGraphics[i].SetActive(true);
                    if (afterburnersTransforms[i] != null) {
                        afterburnersTransforms[i].localScale = new Vector3(size, size, size);
                    }
                }
            }
        } else {
            for (int i = 0; i < afterburnerGraphics.Count; i++) {
                if (afterburnerGraphics[i] != null) {
                    afterburnerGraphics[i].SetActive(false);
                }
            }
        }
    }

    void UpdateControlSurfaces(float dt) {
        var input = plane.EffectiveInput;

        deflection.x = Utilities.MoveTo(deflection.x, input.x, deflectionSpeed, dt, -1, 1);
        deflection.y = Utilities.MoveTo(deflection.y, input.y, deflectionSpeed, dt, -1, 1);
        deflection.z = Utilities.MoveTo(deflection.z, input.z, deflectionSpeed, dt, -1, 1);

        if (rightAileron != null) {
            rightAileron.localRotation = CalculatePose(rightAileron, Quaternion.Euler(deflection.z * maxAileronDeflection, 0, 0));
        }
        if (leftAileron != null) {
            leftAileron.localRotation = CalculatePose(leftAileron, Quaternion.Euler(-deflection.z * maxAileronDeflection, 0, 0));
        }

        if (elevators != null) {
            foreach (var t in elevators) {
                if (t != null) {
                    t.localRotation = CalculatePose(t, Quaternion.Euler(deflection.x * maxElevatorDeflection, 0, 0));
                }
            }
        }

        if (rudders != null) {
            foreach (var t in rudders) {
                if (t != null) {
                    t.localRotation = CalculatePose(t, Quaternion.Euler(0, -deflection.y * maxRudderDeflection, 0));
                }
            }
        }
    }

    void UpdatePropeller(float dt) {
        if (propeller == null) return;

        // Calculate RPM based on throttle
        float targetRPM = plane.Throttle * propellerMaxRPM;
        
        // Convert RPM to degrees per second
        float degreesPerSecond = (targetRPM / 60f) * 360f;
        
        // Update rotation
        propellerRotation += degreesPerSecond * dt;
        
        // Apply rotation
        propeller.Rotate(propellerAxis * degreesPerSecond * dt, Space.Self);
    }

    void UpdateAirbrakes(float dt) {
        var target = plane.AirbrakeDeployed ? 1 : 0;

        airbrakePosition = Utilities.MoveTo(airbrakePosition, target, deflectionSpeed, dt);

        if (airbrake != null) {
            airbrake.localRotation = CalculatePose(airbrake, Quaternion.Euler(-airbrakePosition * airbrakeDeflection, 0, 0));
        }
    }

    void UpdateFlaps(float dt) {
        var target = plane.FlapsDeployed ? 1 : 0;

        flapsPosition = Utilities.MoveTo(flapsPosition, target, deflectionSpeed, dt);

        if (flaps != null) {
            foreach (var t in flaps) {
                if (t != null) {
                    t.localRotation = CalculatePose(t, Quaternion.Euler(flapsPosition * flapsDeflection, 0, 0));
                }
            }
        }
    }

    void LateUpdate() {
        float dt = Time.deltaTime;

        UpdateAfterburners();
        UpdateControlSurfaces(dt);
        UpdateAirbrakes(dt);
        UpdateFlaps(dt);
        UpdatePropeller(dt);
    }
}
