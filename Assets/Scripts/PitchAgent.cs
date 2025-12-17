using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using System.Collections.Generic;

/// <summary>
/// Simplified agent that learns PITCH CONTROL ONLY
/// Master one movement at a time - start with pitch, then add roll/yaw later
/// </summary>
public class PitchAgent : Agent
{
    private Plane plane;
    private Rigidbody rb;
    
    [Header("Training Stats")]
    [SerializeField] private int waypointsCollected = 0;
    
    [Header("Start Position")]
    [SerializeField] private Vector3 startPosition = new Vector3(0f, 15f, 0f);
    [SerializeField] private float initialSpeed = 25f;
    
    [Header("Waypoint Tracking")]
    private WaypointReward targetWaypoint;
    private WaypointReward[] allWaypoints;
    private int totalWaypoints = 0;
    private HashSet<WaypointReward> collectedWaypoints = new HashSet<WaypointReward>();
    
    [Header("Training Area")]
    [SerializeField] private Transform trainingArea;

    [Header("Episode Settings")]
    [SerializeField] private float maxEpisodeTime = 30f;
    private float episodeTimer = 0f;

    public override void Initialize()
    {
        plane = GetComponent<Plane>();
        rb = GetComponent<Rigidbody>();
        
        // Auto-detect training area
        if (trainingArea == null)
        {
            Transform parent = transform.parent;
            while (parent != null)
            {
                if (parent.name.Contains("TrainingArea") || parent.name.Contains("Area"))
                {
                    trainingArea = parent;
                    break;
                }
                parent = parent.parent;
            }
        }
        
        RefreshWaypoints();
        
        if (totalWaypoints == 0)
        {
            totalWaypoints = 3; // Start with just 3 waypoints for pitch practice
            Debug.LogWarning($"<color=orange>No waypoints found! Using default: {totalWaypoints}</color>");
        }
    }
    
    void RefreshWaypoints()
    {
        if (trainingArea != null)
        {
            allWaypoints = trainingArea.GetComponentsInChildren<WaypointReward>();
        }
        else
        {
            allWaypoints = FindObjectsByType<WaypointReward>(FindObjectsSortMode.None);
        }
        
        totalWaypoints = allWaypoints != null ? allWaypoints.Length : 0;
        Debug.Log($"<color=cyan>[PitchAgent] Found {totalWaypoints} waypoints</color>");
    }

    public override void OnEpisodeBegin()
    {
        // Reset plane to starting position
        plane.ResetTo(startPosition, Quaternion.identity, initialSpeed);
        if (rb != null) rb.isKinematic = false;
        
        // Reset waypoint tracking
        waypointsCollected = 0;
        collectedWaypoints.Clear();
        RefreshWaypoints();
        UpdateTargetWaypoint();
        
        // Reset episode timer
        episodeTimer = 0f;

        Debug.Log("<color=blue>[PitchAgent] Episode started - Learning PITCH only</color>");
    }

    void FixedUpdate()
    {
        if (rb == null || plane == null) return;
        if (rb.isKinematic || plane.Dead) return;

        // End episode if plane crashes
        if (plane.Dead)
        {
            Debug.Log("<color=red>[PitchAgent] Crashed - ending episode</color>");
            AddReward(-10f);
            EndEpisode();
            return;
        }

        // Episode timeout
        episodeTimer += Time.fixedDeltaTime;
        if (episodeTimer > maxEpisodeTime)
        {
            Debug.Log($"<color=yellow>[PitchAgent] Episode timeout ({maxEpisodeTime}s)</color>");
            EndEpisode();
        }

        // Ground collision check
        if (transform.position.y < 0f)
        {
            Debug.Log("<color=red>[PitchAgent] Hit ground</color>");
            AddReward(-10f);
            EndEpisode();
        }
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // ULTRA-SIMPLE OBSERVATIONS for imitation learning (3 values only)
        
        // 1. Vertical direction to waypoint (do I need to pitch up or down?)
        float verticalDir = 0f;
        if (targetWaypoint != null)
        {
            Vector3 toWaypoint = targetWaypoint.transform.position - transform.position;
            verticalDir = toWaypoint.y / toWaypoint.magnitude; // -1 to 1 (down to up)
        }
        sensor.AddObservation(verticalDir);
        
        // 2. Current height (normalized)
        sensor.AddObservation(transform.position.y / 30f);
        
        // 3. Vertical velocity (am I climbing or descending?)
        sensor.AddObservation(rb.linearVelocity.y / 15f);
        
        // Total: 3 observations (minimal - easy to learn from demos)
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        // SINGLE ACTION BRANCH: Pitch control only
        // Branch 0: Pitch (0=up, 1=neutral, 2=down)
        int pitchAction = actions.DiscreteActions[0];

        float pitch = 0f;
        if (pitchAction == 0) pitch = 1f;      // Pitch up
        else if (pitchAction == 2) pitch = -1f; // Pitch down
        // pitchAction == 1 means neutral (pitch = 0)

        // Apply ONLY pitch control
        // Roll and yaw are kept at 0 (agent doesn't control them)
        plane.SetControlInput(new Vector3(pitch, 0f, 0f));
        plane.SetThrottleInput(1f); // Full throttle

        // SIMPLE REWARDS for imitation learning
        // Let the human demo handle the details, just reward success
        
        float reward = -0.01f; // Small time penalty
        
        // Main reward: collect waypoints (human will show how)
        // Waypoint collection adds +10 in OnWaypointCollected
        
        // Penalize crashes (handled in FixedUpdate)
        // Everything else learned from demonstrations
        
        AddReward(reward);
    }

    void UpdateTargetWaypoint()
    {
        if (allWaypoints == null || allWaypoints.Length == 0) return;
        
        float closestDistance = float.MaxValue;
        WaypointReward closestWaypoint = null;
        
        foreach (var waypoint in allWaypoints)
        {
            if (waypoint == null || collectedWaypoints.Contains(waypoint)) continue;
            
            float distance = Vector3.Distance(transform.position, waypoint.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestWaypoint = waypoint;
            }
        }
        
        targetWaypoint = closestWaypoint;
    }

    public void OnWaypointCollected(WaypointReward waypoint)
    {
        if (collectedWaypoints.Contains(waypoint)) return;
        
        collectedWaypoints.Add(waypoint);
        waypointsCollected++;
        
        // Big reward for collecting waypoint
        AddReward(10f);
        
        Debug.Log($"<color=green>[PitchAgent] Waypoint collected! ({waypointsCollected}/{totalWaypoints}) +10</color>");
        
        // Update to next waypoint
        UpdateTargetWaypoint();
        
        // If all waypoints collected, bonus and end episode
        if (waypointsCollected >= totalWaypoints)
        {
            AddReward(20f);
            Debug.Log("<color=green>[PitchAgent] ALL WAYPOINTS! Bonus +20</color>");
            EndEpisode();
        }
    }

    public void OnBoundaryHit()
    {
        AddReward(-5f);
        Debug.Log("<color=orange>[PitchAgent] Boundary hit -5</color>");
        EndEpisode();
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discreteActions = actionsOut.DiscreteActions;
        
#if ENABLE_INPUT_SYSTEM
        // New Input System
        if (UnityEngine.InputSystem.Keyboard.current.upArrowKey.isPressed)
            discreteActions[0] = 0; // Pitch up
        else if (UnityEngine.InputSystem.Keyboard.current.downArrowKey.isPressed)
            discreteActions[0] = 2; // Pitch down
        else
            discreteActions[0] = 1; // Neutral
#else
        // Legacy Input (fallback)
        if (Input.GetKey(KeyCode.UpArrow))
            discreteActions[0] = 0; // Pitch up
        else if (Input.GetKey(KeyCode.DownArrow))
            discreteActions[0] = 2; // Pitch down
        else
            discreteActions[0] = 1; // Neutral
#endif
    }
}
