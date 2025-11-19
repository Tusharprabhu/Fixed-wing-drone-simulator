using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlaneHUD : MonoBehaviour {
    Plane plane;

    // Unity uses meters (distance) and m/s (velocity). Altitude now shown directly in meters.

    void Start() {
    }

    public void SetPlane(Plane plane) {
        this.plane = plane;
    }

    public void SetCamera(Camera camera) {
        // Not needed
    }

    public void ToggleHelpDialogs() {
        // Not needed
    }

    void OnGUI() {
        if (plane == null) return;

        // Calculate values
        // LocalVelocity is in meters per second (m/s)
        float airspeed = plane.LocalVelocity.z;
        float altitude = plane.Rigidbody.position.y; // meters
        // Use display-only AOA (pitch) which is already in degrees and has been inverted as requested
        float aoa = plane.DisplayAOA;
        // G-Force using push-over formula (can show negative values)
        float gLoad = plane.DisplayLoadFactor;
        float throttle = plane.Throttle * 100f;
        float heading = plane.transform.eulerAngles.y;

        // Set up GUI style
        GUIStyle style = new GUIStyle();
        style.fontSize = 18;
        style.normal.textColor = Color.white;
        style.alignment = TextAnchor.UpperRight;

        // Position in top-right corner
        float x = Screen.width - 250;
        float y = 20;
        float lineHeight = 25;

        // Display values
        GUI.Label(new Rect(x, y, 230, lineHeight), $"Airspeed: {airspeed:0} m/s", style);
        y += lineHeight;
        
        GUI.Label(new Rect(x, y, 230, lineHeight), $"Altitude: {altitude:0} m", style);
        y += lineHeight;
        
        GUI.Label(new Rect(x, y, 230, lineHeight), $"AOA: {aoa:0.1}°", style);
        y += lineHeight;
        
        GUI.Label(new Rect(x, y, 230, lineHeight), $"G: {gLoad:0.00} g", style);
        y += lineHeight;
        
        // (G-force removed)
        
        GUI.Label(new Rect(x, y, 230, lineHeight), $"Throttle: {throttle:0}%", style);
        y += lineHeight;
        
        GUI.Label(new Rect(x, y, 230, lineHeight), $"Heading: {heading:0}°", style);
    }
}
