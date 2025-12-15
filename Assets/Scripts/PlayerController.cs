using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour {
    [SerializeField]
    new Camera camera;
    [SerializeField]
    Plane plane;

    Vector3 controlInput;
    PlaneCamera planeCamera;

    void Start() {
        planeCamera = GetComponent<PlaneCamera>();
        SetPlane(plane);    //SetPlane if var is set in inspector
    }

    void SetPlane(Plane plane) {
        this.plane = plane;
        planeCamera.SetPlane(plane);
    }
    
    public void OnToggleHelp(InputAction.CallbackContext context) {
        // Help dialogs removed
    }

    public void SetThrottleInput(InputAction.CallbackContext context) {
        if (plane == null) return;

        plane.SetThrottleInput(context.ReadValue<float>());
    }

    public void OnRollPitchInput(InputAction.CallbackContext context) {
        if (plane == null) return;

        var input = context.ReadValue<Vector2>();
        float rollInput = input.x;   // Roll input (left/right) - FIXED: removed negative
        float pitchInput = -input.y; // Pitch input (up/down) - FIXED: added negative for correct direction
        
        // Add fixed 0.1 yaw when rolling
        float yawFromRoll = rollInput * 0.1f;
        
        controlInput = new Vector3(pitchInput, yawFromRoll, rollInput);
    }

    public void OnCameraInput(InputAction.CallbackContext context) {
        if (plane == null) return;

        var input = context.ReadValue<Vector2>();
        planeCamera.SetInput(input);
    }

    void Update() {
        if (plane == null) return;

        plane.SetControlInput(controlInput);
    }
}
