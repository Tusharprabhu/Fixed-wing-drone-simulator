using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlaneAnimation : MonoBehaviour {
    [SerializeField]
    float maxAileronDeflection;
    [SerializeField]
    float maxElevatorDeflection;
    [SerializeField]
    float maxRudderDeflection;
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

    [Header("Propeller (for drones)")]
    [SerializeField]
    Transform propeller;
    [SerializeField]
    float propellerMaxRPM = 2000f;
    [SerializeField]
    Vector3 propellerAxis = Vector3.forward;

    Plane plane;
    Dictionary<Transform, Quaternion> neutralPoses;
    Vector3 deflection;
    float propellerRotation;

    void Start() {
        plane = GetComponent<Plane>();
        neutralPoses = new Dictionary<Transform, Quaternion>();

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
                    t.localRotation = CalculatePose(t, Quaternion.Euler(0, 0, -deflection.y * maxRudderDeflection));
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





    void LateUpdate() {
        float dt = Time.deltaTime;

        UpdateControlSurfaces(dt);
        UpdatePropeller(dt);
    }
}
