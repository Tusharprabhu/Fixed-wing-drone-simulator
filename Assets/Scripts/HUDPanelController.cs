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

    const float metersToKnots = 1.94384f;
    const float metersToFeet = 3.28084f;

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

        // Debug what we found
        Debug.Log($"Found texts: Speed={airspeedText != null}, Alt={altitudeText != null}, AOA={aoaText != null}, G={gforceText != null}");
    }

    void Update()
    {
        if (plane == null) return;

        // Calculate all values
        float airspeed = plane.LocalVelocity.z * metersToKnots;
        float altitude = plane.Rigidbody.position.y * metersToFeet;
        float aoa = plane.AngleOfAttack * Mathf.Rad2Deg;
        // G-Force: magnitude of acceleration divided by gravity
        float gForce = plane.LocalGForce.magnitude / 9.81f;
        float throttle = plane.Throttle * 100f;
        float heading = plane.transform.eulerAngles.y;

        // Update UI Text elements
        if (airspeedText != null)
        {
            airspeedText.text = $"{airspeed:0.0} kts";
        }

        if (altitudeText != null)
        {
            altitudeText.text = $"{altitude:0.0} ft";
        }

        if (aoaText != null)
        {
            aoaText.text = $"{aoa:0.00}°";
        }

        if (gforceText != null)
        {
            gforceText.text = $"{gForce:0.00} G";
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
