using UnityEngine;
using UnityEngine.UI;

public class HUDPanelController : MonoBehaviour
{
    public Plane plane;

    // UI Text elements
    Text airspeedText;
    Text altitudeText;
    Text aoaText;
    Text gforceText;
    Bar throttleBar;
    Text compassText;

    // HUD uses SI units: meters and meters/second

    void Start()
    {
        // Find the plane if not assigned
        if (plane == null)
        {
            plane = FindFirstObjectByType<Plane>();
        }

        // Find Text components in children
        FindTextComponents();
    }

    void FindTextComponents()
    {
        // Search for Text components under Airspeed, Altitude, etc.
        Transform airspeed = transform.Find("Airspeed");
        if (airspeed != null)
        {
            airspeedText = airspeed.GetComponentInChildren<Text>();
        }

        Transform altitude = transform.Find("Altitude");
        if (altitude != null)
        {
            altitudeText = altitude.GetComponentInChildren<Text>();
        }

        Transform aoa = transform.Find("AOA");
        if (aoa != null)
        {
            aoaText = aoa.GetComponentInChildren<Text>();
        }

        Transform gforce = transform.Find("GForce");
        if (gforce != null)
        {
            gforceText = gforce.GetComponentInChildren<Text>();
        }

        Transform throttle = transform.Find("ThrottleBar");
        if (throttle != null)
        {
            throttleBar = throttle.GetComponent<Bar>();
        }

        Transform compass = transform.Find("Compass");
        if (compass != null)
        {
            compassText = compass.GetComponentInChildren<Text>();
        }

        // Check if all components were found
        // GForce element is optional: removed from HUD calculation by default.
        bool allFound = airspeedText != null && altitudeText != null && 
                aoaText != null && throttleBar != null && compassText != null;
        
        if (allFound)
        {
            Debug.Log("✅ HUD Panel Controller: All UI elements found and connected");
        }
        else
        {
            Debug.LogWarning($"⚠️ HUD missing elements: Speed={airspeedText != null}, Alt={altitudeText != null}, AOA={aoaText != null}, Throttle={throttleBar != null}, Compass={compassText != null}");
        }
    }

    void Update()
    {
        if (plane == null)
        {
            if (Time.frameCount % 300 == 0) // Every 5 seconds
            {
                Debug.LogWarning("HUD: Plane reference is missing!");
            }
            return;
        }

        // Safety check for Rigidbody
        if (plane.Rigidbody == null)
        {
            Debug.LogError("HUD: Plane's Rigidbody is null!");
            return;
        }

        // Calculate all values
        // LocalVelocity.z is in meters per second (m/s)
        float airspeed = plane.LocalVelocity.z;
        // Altitude in meters
        float altitude = plane.Rigidbody.position.y;
        float aoa = plane.DisplayAOA; // Use display AOA (pitch angle)
        // GForce display removed for now; will be replaced with user-provided calculation when available.
        float throttle = plane.Throttle * 100f;
        float heading = plane.transform.eulerAngles.y;

        // Update UI Text elements
        if (airspeedText != null)
        {
            airspeedText.text = $"{airspeed:0.0} m/s";
        }

        if (altitudeText != null)
        {
            altitudeText.text = $"{altitude:0.0} m";
        }

        if (aoaText != null)
        {
            aoaText.text = $"{aoa:0.00}°";
        }

        // Clear GForce until user supplies a replacement calculation.
        if (gforceText != null)
        {
            gforceText.text = string.Empty;
        }

        if (throttleBar != null)
        {
            throttleBar.SetValue(plane.Throttle);
        }

        if (compassText != null)
        {
            compassText.text = $"{heading:0}°";
        }
    }
}
