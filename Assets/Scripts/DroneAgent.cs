using UnityEngine;
// Support both the Legacy Input and the new Input System for heuristic controls
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

public class DroneAgent : Agent
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
    }

    public override void OnEpisodeBegin()
    {
        // Use Plane.ResetTo for a full reset including effects
        plane.ResetTo(startPosition, Quaternion.identity, initialSpeed);
        // Ensure rigidbody physics are active again and clear any Kinematic state
        if (rb != null) rb.isKinematic = false;
        
        // Reset waypoint counter
        waypointsCollected = 0;
        
        // Refresh waypoints within training area
        RefreshWaypoints();
        
        // Find nearest waypoint
        UpdateTargetWaypoint();
        
        // Reset stuck timer
        stuckTimer = 0f;
        
        // Reset interaction timer
        timeSinceLastInteraction = 0f;

        Debug.Log("<color=blue>*** EPISODE STARTED ***</color> Drone reset to starting position");
    }

    void FixedUpdate()
    {
        if (rb == null) return;
        // Avoid stuck detection while kinematic or dead
        if (rb.isKinematic || plane == null) return;

        // If plane reports dead (crashed), end the episode to ensure a proper reset
        if (plane.Dead)
        {
            Debug.Log("<color=red>*** PLANE DEAD - ENDING EPISODE ***</color>");
            EndEpisode();
            return;
        }

        float speed = rb.linearVelocity.magnitude;
        if (speed < stuckSpeedThreshold && transform.position.y < 20f)
        {
            stuckTimer += Time.fixedDeltaTime;
            if (stuckTimer > stuckResetTime)
            {
                Debug.Log($"<color=orange>*** STUCK TOO LONG ({stuckTimer:F1}s) - RESTARTING EPISODE ***</color>");
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
            Debug.Log($"<color=yellow>*** NO INTERACTION FOR {interactionTimeout}s - RESTARTING EPISODE ***</color>");
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
        
        // Total observations: 3 + 3 + 3 = 9 base observations
        // Add raycasts if needed to reach 33 total
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        // Discrete actions: 2 branches, 3 choices each
        // Branch 0: Pitch (0=up, 1=neutral, 2=down)
        // Branch 1: Roll (0=left, 1=neutral, 2=right) - agent controls roll directly

        int pitchAction = actions.DiscreteActions[0];
        int rollAction = actions.DiscreteActions[1];

        // Convert discrete actions to control inputs
        float pitch = 0f;
        if (pitchAction == 0) pitch = 1f;      // Pitch up
        else if (pitchAction == 2) pitch = -1f; // Pitch down

        float roll = 0f;
        if (rollAction == 0) roll = -1f;        // Roll left
        else if (rollAction == 2) roll = 1f;    // Roll right

        // Apply controls (roll is the agent-controlled channel)
        // Plane.SetControlInput expects Vector3(pitch, yawFromRoll, roll)
        // We place roll into the z channel; yawFromRoll (y) remains 0 for agent.
        plane.SetControlInput(new Vector3(pitch, 0f, roll * 0.5f));
        plane.SetThrottleInput(1f); // Full throttle for now

        // Rewards
        float reward = 0f;

        // Small time penalty to encourage efficiency
        reward -= 0.001f;
        
        // Reward for moving toward waypoint
        if (targetWaypoint != null)
        {
            Vector3 dirToWaypoint = (targetWaypoint.transform.position - transform.position).normalized;
            Vector3 velocity = rb.linearVelocity.normalized;
            float alignment = Vector3.Dot(velocity, dirToWaypoint);
            reward += alignment * 0.1f;
        }

        // Reward for staying airborne
        if (transform.position.y > 5f)
        {
            reward += 0.01f;
        }

        // Penalty for crashing into terrain (-10)
        if (transform.position.y < 0)
        {
            reward -= 10f;
            timeSinceLastInteraction = 0f; // Reset interaction timer
            Debug.Log("<color=red>*** TERRAIN CRASH! ***</color> Drone hit ground (-10) - Episode restarting");
            EndEpisode();
        }
        
        // Also check for other crash conditions
        if (rb.linearVelocity.magnitude < 5f && transform.position.y < 20f)
        {
            // Stalled too close to ground
            reward -= 5f;
            Debug.Log("<color=orange>*** STALL! ***</color> Drone stalled - Episode restarting");
            EndEpisode();
        }

        // Penalty for excessive G-force
        float gForce = plane.DisplayLoadFactor;
        if (Mathf.Abs(gForce) > 5f)
        {
            reward -= 0.05f;
        }

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
                // Pitch control: W/S or Up/Down arrows
                if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed)
                    discreteActionsOut[0] = 0; // Pitch up
                else if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed)
                    discreteActionsOut[0] = 2; // Pitch down
                else
                    discreteActionsOut[0] = 1; // Neutral

                // Roll control: A/D or Left/Right arrows
                if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
                    discreteActionsOut[1] = 0; // Roll left
                else if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
                    discreteActionsOut[1] = 2; // Roll right
                else
                    discreteActionsOut[1] = 1; // Neutral

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

            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
                discreteActionsOut[1] = 0; // Roll left
            else if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
                discreteActionsOut[1] = 2; // Roll right
            else
                discreteActionsOut[1] = 1; // Neutral
        }
#endif
    }

    // Called by WaypointReward when collected
    public void CollectWaypoint(float rewardAmount)
    {
        waypointsCollected++;
        AddReward(rewardAmount);
        timeSinceLastInteraction = 0f; // Reset interaction timer
        Debug.Log($"<color=green>REWARD SPHERE TOUCHED</color> - Waypoints: {waypointsCollected} | Reward: +{rewardAmount}");
        
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
        Debug.Log($"<color=red>WALL TOUCHED</color> - Penalty: {penaltyAmount}");
        
        if (endEpisode)
        {
            Debug.Log("<color=red>*** BOUNDARY VIOLATION ***</color> Episode restarting");
            EndEpisode();
        }
    }
}