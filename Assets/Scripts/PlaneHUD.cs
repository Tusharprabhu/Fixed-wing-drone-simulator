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
        float aoa = plane.AngleOfAttack * Mathf.Rad2Deg;
        float verticalAccelProper = plane.LocalGForce.y; // m/s^2 (proper)
        float verticalAccelWorld = plane.LocalGForceWorld.y; // m/s^2 (includes gravity)
        float verticalGProper = verticalAccelProper / 9.80665f;
        float verticalGWorld = verticalAccelWorld / 9.80665f;
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
        
        GUI.Label(new Rect(x, y, 230, lineHeight), $"Vert Proper: {verticalAccelProper:0.1} m/s²  ({verticalGProper:0.00} g)", style);
        y += lineHeight;
        GUI.Label(new Rect(x, y, 230, lineHeight), $"Vert World:  {verticalAccelWorld:0.1} m/s²  ({verticalGWorld:0.00} g)", style);
        y += lineHeight;
        
        GUI.Label(new Rect(x, y, 230, lineHeight), $"Throttle: {throttle:0}%", style);
        y += lineHeight;
        
        GUI.Label(new Rect(x, y, 230, lineHeight), $"Heading: {heading:0}°", style);
    }
}
