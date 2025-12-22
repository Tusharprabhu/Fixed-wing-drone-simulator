using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlaneCamera : MonoBehaviour {
    [SerializeField]
    new Camera camera;
    [SerializeField]
    Vector3 cameraOffset;
    [SerializeField]
    Vector2 lookAngle;
    [SerializeField]
    float movementScale;
    [SerializeField]
    float lookAlpha;
    [SerializeField]
    float movementAlpha;
    [SerializeField]
    Vector3 deathOffset;
    [SerializeField]
    float deathSensitivity;
    
    [Header("Auto-Find Target")]
    [SerializeField]
    bool autoFindDrone = true;
    [SerializeField]
    string droneTag = "Player";
    [SerializeField]
    int targetEnvironmentID = 0; // Which environment to follow

    Transform cameraTransform;
    Plane plane;
    Transform planeTransform;
    Vector2 lookInput;
    bool dead;

    Vector2 look;
    Vector2 lookAverage;
    Vector3 avAverage;

    void Awake() {
        cameraTransform = camera.GetComponent<Transform>();
        
        // Auto-find the drone and its Plane component
        if (autoFindDrone) {
            FindAndSetDrone();
        }
    }

    void FindAndSetDrone() {
        GameObject droneObject = null;
        
        // Find drone by tag first
        if (droneObject == null) {
            droneObject = GameObject.FindWithTag(droneTag);
        }
        
        // Fallback: look for any object with Plane component
        if (droneObject == null) {
            Plane foundPlane = FindFirstObjectByType<Plane>();
            if (foundPlane != null) {
                droneObject = foundPlane.gameObject;
            }
        }

        if (droneObject != null) {
            Plane dronePlane = droneObject.GetComponent<Plane>();
            if (dronePlane != null) {
                SetPlane(dronePlane);
                Debug.Log($"PlaneCamera: Auto-connected to {droneObject.name}");
            } else {
                Debug.LogWarning($"PlaneCamera: Found drone {droneObject.name} but it has no Plane component!");
            }
        } else {
            Debug.LogWarning("PlaneCamera: Could not find drone automatically!");
        }
    }

    public void SetPlane(Plane plane) {
        this.plane = plane;

        if (plane == null) {
            planeTransform = null;
        } else {
            planeTransform = plane.GetComponent<Transform>();
        }

        // Don't parent the camera to allow independent movement
        if (planeTransform != null) {
            Debug.Log($"PlaneCamera: Connected to plane at {planeTransform.name}");
        }
    }

    public void SetInput(Vector2 input) {
        lookInput = input;
    }

    void LateUpdate() {
        if (plane == null) return;

        var cameraOffset = this.cameraOffset;

        if (plane.Dead) {
            look += lookInput * deathSensitivity * Time.deltaTime;
            look.x = (look.x + 360f) % 360f;
            look.y = Mathf.Clamp(look.y, -lookAngle.y, lookAngle.y);

            lookAverage = look;
            avAverage = new Vector3();

            cameraOffset = deathOffset;
        } else {
            var targetLookAngle = Vector2.Scale(lookInput, lookAngle);
            lookAverage = (lookAverage * (1 - lookAlpha)) + (targetLookAngle * lookAlpha);

            var angularVelocity = plane.LocalAngularVelocity;
            angularVelocity.z = -angularVelocity.z;

            avAverage = (avAverage * (1 - movementAlpha)) + (angularVelocity * movementAlpha);
        }

        var rotation = Quaternion.Euler(-lookAverage.y, lookAverage.x, 0);  //get rotation from camera input
        var turningRotation = Quaternion.Euler(new Vector3(-avAverage.x, -avAverage.y, avAverage.z) * movementScale);   //get rotation from plane's AV

        // Calculate world position instead of local position
        if (planeTransform != null) {
            var targetPosition = planeTransform.position + planeTransform.rotation * (rotation * turningRotation * cameraOffset);
            var targetRotation = planeTransform.rotation * rotation * turningRotation;

            cameraTransform.position = targetPosition;
            cameraTransform.rotation = targetRotation;
        }
    }

    // Add method to change target environment
    [ContextMenu("Reconnect to Drone")]
    public void ReconnectToDrone() {
        FindAndSetDrone();
    }
    
    public void SetTargetEnvironment(int environmentID) {
        targetEnvironmentID = environmentID;
        FindAndSetDrone();
    }
}
