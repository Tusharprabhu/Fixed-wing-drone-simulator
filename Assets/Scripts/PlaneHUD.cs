using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlaneHUD : MonoBehaviour {
    Plane plane;

    const float metersToKnots = 1.94384f;
    const float metersToFeet = 3.28084f;

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
        float airspeed = plane.LocalVelocity.z * metersToKnots;
        float altitude = plane.Rigidbody.position.y * metersToFeet;
        float aoa = plane.AngleOfAttack * Mathf.Rad2Deg;
        float gForce = plane.LocalGForce.y / 9.81f;
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
        GUI.Label(new Rect(x, y, 230, lineHeight), $"Airspeed: {airspeed:0} kts", style);
        y += lineHeight;
        
        GUI.Label(new Rect(x, y, 230, lineHeight), $"Altitude: {altitude:0} ft", style);
        y += lineHeight;
        
        GUI.Label(new Rect(x, y, 230, lineHeight), $"AOA: {aoa:0.1}°", style);
        y += lineHeight;
        
        GUI.Label(new Rect(x, y, 230, lineHeight), $"G-Force: {gForce:0.1} G", style);
        y += lineHeight;
        
        GUI.Label(new Rect(x, y, 230, lineHeight), $"Throttle: {throttle:0}%", style);
        y += lineHeight;
        
        GUI.Label(new Rect(x, y, 230, lineHeight), $"Heading: {heading:0}°", style);
    }
}
