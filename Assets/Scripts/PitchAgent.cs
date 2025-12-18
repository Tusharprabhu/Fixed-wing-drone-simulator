using UnityEngine;
// Support both the Legacy Input and the new Input System for heuristic controls
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
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
    [SerializeField] private Vector3 startPosition = new Vector3(0f, 10f, 0f); // Default to air for safety
    [SerializeField] private float initialSpeed = 20f;
    [SerializeField] private bool startOnRunway = false;
    
    [Header("Waypoint Tracking")]
    private WaypointReward targetWaypoint;
    private WaypointReward[] allWaypoints;
    private int totalWaypoints = 0;
    private HashSet<WaypointReward> collectedWaypoints = new HashSet<WaypointReward>();
    private float previousDistanceToWaypoint = float.MaxValue;
    
    [Header("Training Area")]
    [Tooltip("Parent transform containing this agent's waypoints/boundaries. If null, uses global search.")]
    [SerializeField] private Transform trainingArea;

    [Header("Stuck Detection")]
    [SerializeField] private float stuckSpeedThreshold = 2f; // below this speed is considered stuck
    [SerializeField] private float stuckResetTime = 5f; // seconds before auto-reset
    private float stuckTimer = 0f;

    [Header("Interaction Timeout")]
    [SerializeField] private float interactionTimeout = 30f; // seconds without reward/penalty before restart
    private float timeSinceLastInteraction = 0f;

    public override void Initialize()
    {
        plane = GetComponent<Plane>();
        rb = GetComponent<Rigidbody>();
        
        // Auto-detect training area if not set (look for parent with "TrainingArea" in name)
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
        
        // Find waypoints within training area only (or global if no area set)
        RefreshWaypoints();
        
        // If no totalWaypoints found, set a default (user must have 5 waypoints)
        if (totalWaypoints == 0)
        {
            totalWaypoints = 5; // Hardcode as fallback
            Debug.LogWarning($"<color=orange>[PitchAgent] No waypoints found automatically! Using default: {totalWaypoints}</color>");
        }
    }
    
    void RefreshWaypoints()
    {
        if (trainingArea != null)
        {
            // Get only waypoints within this training area
            allWaypoints = trainingArea.GetComponentsInChildren<WaypointReward>();
        }
        else
        {
            // Fallback to global search
            allWaypoints = FindObjectsByType<WaypointReward>(FindObjectsSortMode.None);
        }
        
        totalWaypoints = allWaypoints != null ? allWaypoints.Length : 0;
        Debug.Log($"<color=cyan>[PitchAgent] Found {totalWaypoints} waypoints for episode</color>");
    }

    public override void OnEpisodeBegin()
    {
        // Use Plane.ResetTo for a full reset including effects
        plane.ResetTo(startPosition, Quaternion.identity, initialSpeed);
        // Ensure rigidbody physics are active again and clear any Kinematic state
        if (rb != null) rb.isKinematic = false;
        
        // Reset waypoint counter and collected set
        waypointsCollected = 0;
        collectedWaypoints.Clear();
        
        // Refresh waypoints within training area
        RefreshWaypoints();
        
        // Find nearest waypoint
        UpdateTargetWaypoint();
        
        // Reset stuck timer
        stuckTimer = 0f;
        
        // Reset interaction timer
        timeSinceLastInteraction = 0f;
        
        // Reset distance tracking
        previousDistanceToWaypoint = float.MaxValue;

        Debug.Log("<color=blue>[PitchAgent] *** EPISODE STARTED - PITCH ONLY ***</color> Drone reset to starting position");
    }

    void FixedUpdate()
    {
        if (rb == null) return;
        // Avoid stuck detection while kinematic or dead
        if (rb.isKinematic || plane == null) return;

        // If plane reports dead (crashed), end the episode to ensure a proper reset
        if (plane.Dead)
        {
            Debug.Log("<color=red>[PitchAgent] *** PLANE DEAD - ENDING EPISODE ***</color>");
            EndEpisode();
            return;
        }

        float speed = rb.linearVelocity.magnitude;
        if (speed < stuckSpeedThreshold && transform.position.y < 20f)
        {
            stuckTimer += Time.fixedDeltaTime;
            if (stuckTimer > stuckResetTime)
            {
                Debug.Log($"<color=orange>[PitchAgent] *** STUCK TOO LONG ({stuckTimer:F1}s) - RESTARTING EPISODE ***</color>");
                stuckTimer = 0f;
                EndEpisode();
                return;
            }
        }
        else
        {
            stuckTimer = 0f;
        }
        
        // Check interaction timeout - restart if no reward/penalty for too long
        timeSinceLastInteraction += Time.fixedDeltaTime;
        if (timeSinceLastInteraction > interactionTimeout)
        {
            Debug.Log($"<color=yellow>[PitchAgent] *** NO INTERACTION FOR {interactionTimeout}s - RESTARTING EPISODE ***</color>");
            AddReward(-5f); // Small penalty for timing out
            EndEpisode();
        }
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // Agent's own velocity (3 values: X, Y, Z)
        sensor.AddObservation(rb.linearVelocity);
        
        // Direction to next checkpoint (3 values: X, Y, Z) - normalized
        if (targetWaypoint != null)
        {
            Vector3 directionToWaypoint = (targetWaypoint.transform.position - transform.position).normalized;
            sensor.AddObservation(directionToWaypoint);
            
            // Direction the checkpoint is facing (3 values: X, Y, Z) - forward vector
            sensor.AddObservation(targetWaypoint.transform.forward);
        }
        else
        {
            // No waypoint - use zero vectors
            sensor.AddObservation(Vector3.zero);
            sensor.AddObservation(Vector3.zero);
        }
        
        // Total observations: 3 + 3 + 3 = 9 observations (same as DroneAgent)
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        // Discrete actions: 1 branch, 3 choices
        // Branch 0: Pitch (0=up, 1=neutral, 2=down) - PITCH ONLY!

        int pitchAction = actions.DiscreteActions[0];

        // Convert discrete actions to control inputs
        float pitch = 0f;
        if (pitchAction == 0) pitch = 1f;      // Pitch up
        else if (pitchAction == 2) pitch = -1f; // Pitch down

        // Apply ONLY pitch control - roll and yaw locked at 0
        // Plane.SetControlInput expects Vector3(pitch, yaw, roll)
        plane.SetControlInput(new Vector3(pitch, 0f, 0f));
        plane.SetThrottleInput(1f); // Full throttle for now

        // SIMPLIFIED REWARDS FOR IMITATION LEARNING
        // Let the human demonstrations teach the behavior
        // Only use sparse rewards for major events
        float reward = 0f;

        // Tiny time penalty to encourage episode completion
        reward -= 0.001f;

        // Crash penalties (handled in FixedUpdate and by boundaries)
        // Waypoint rewards (handled in CollectWaypoint method)
        // Let imitation learning figure out the rest from demos
        
        AddReward(reward);
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discreteActionsOut = actionsOut.DiscreteActions;
        
        // Heuristic support: Prefer Unity Input System if enabled, otherwise use legacy Input.
        bool usedInput = false;
#if ENABLE_INPUT_SYSTEM
        try
        {
            if (Keyboard.current != null)
            {
                // Pitch control: W/S or Up/Down arrows - PITCH ONLY!
                if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed)
                    discreteActionsOut[0] = 0; // Pitch up
                else if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed)
                    discreteActionsOut[0] = 2; // Pitch down
                else
                    discreteActionsOut[0] = 1; // Neutral

                usedInput = true;
            }
        }
        catch
        {
            // If anything goes wrong with the input system, fallback to legacy Input below
            usedInput = false;
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        if (!usedInput)
        {
            // Legacy Input fallback (works if "Input Manager (Old)" or "Both" are enabled in Player Settings)
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
                discreteActionsOut[0] = 0; // Pitch up
            else if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
                discreteActionsOut[0] = 2; // Pitch down
            else
                discreteActionsOut[0] = 1; // Neutral
        }
#endif
    }

    // Called by WaypointReward when collected
    public void CollectWaypoint(WaypointReward waypoint, float rewardAmount)
    {
        // Check if already collected this episode
        if (collectedWaypoints.Contains(waypoint))
        {
            return; // Already collected, ignore
        }
        
        // Mark as collected
        collectedWaypoints.Add(waypoint);
        waypointsCollected++;
        AddReward(rewardAmount);
        timeSinceLastInteraction = 0f; // Reset interaction timer
        
        // Display progress (use actual required waypoints for display)
        int displayTotal = totalWaypoints > 0 ? totalWaypoints : 5;
        Debug.Log($"<color=green>[PitchAgent] ✓ WAYPOINT COLLECTED</color> - Progress: {waypointsCollected}/{displayTotal} | Reward: +{rewardAmount}");
        
        // Check if all waypoints collected (use 5 as minimum if totalWaypoints is wrong)
        int requiredWaypoints = totalWaypoints > 0 ? totalWaypoints : 5;
        if (waypointsCollected >= requiredWaypoints)
        {
            AddReward(20f); // Bonus for completing all waypoints!
            Debug.Log($"<color=lime>[PitchAgent] ★★★ ALL {waypointsCollected} WAYPOINTS COLLECTED ★★★</color> Completion Bonus: +20 | Episode restarting!");
            EndEpisode();
            return;
        }
        
        // Find next waypoint
        UpdateTargetWaypoint();
    }
    
    // Find nearest uncollected waypoint
    void UpdateTargetWaypoint()
    {
        targetWaypoint = null;
        float minDistance = float.MaxValue;
        
        foreach (var waypoint in allWaypoints)
        {
            if (waypoint != null && waypoint.gameObject.activeInHierarchy)
            {
                float distance = Vector3.Distance(transform.position, waypoint.transform.position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    targetWaypoint = waypoint;
                }
            }
        }
    }

    // Called by BoundaryWall when hit
    public void HitBoundary(float penaltyAmount, bool endEpisode)
    {
        AddReward(penaltyAmount);
        timeSinceLastInteraction = 0f; // Reset interaction timer
        Debug.Log($"<color=red>[PitchAgent] WALL TOUCHED</color> - Penalty: {penaltyAmount}");
        
        if (endEpisode)
        {
            Debug.Log("<color=red>[PitchAgent] *** BOUNDARY VIOLATION ***</color> Episode restarting");
            EndEpisode();
        }
    }
}
