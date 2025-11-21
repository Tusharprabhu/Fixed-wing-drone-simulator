using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

public class DroneAgent : Agent
{
    private Plane plane;
    private Rigidbody rb;
    
    [Header("Training Stats")]
    [SerializeField] private int waypointsCollected = 0;
    
    [Header("Waypoint Tracking")]
    private WaypointReward targetWaypoint;
    private WaypointReward[] allWaypoints;

    public override void Initialize()
    {
        plane = GetComponent<Plane>();
        rb = GetComponent<Rigidbody>();
        allWaypoints = FindObjectsByType<WaypointReward>(FindObjectsSortMode.None);
    }

    public override void OnEpisodeBegin()
    {
        // Reset the plane to initial position
        transform.position = new Vector3(0, 10, 0);
        transform.rotation = Quaternion.identity;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        plane.ResetControls();
        
        // Reset waypoint counter
        waypointsCollected = 0;
        
        // Respawn all waypoints
        allWaypoints = FindObjectsByType<WaypointReward>(FindObjectsSortMode.None);
        foreach (var waypoint in allWaypoints)
        {
            waypoint.Respawn();
        }
        
        // Find nearest waypoint
        UpdateTargetWaypoint();
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

        // Penalty for crashing
        if (transform.position.y < 0)
        {
            reward -= 10f;
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
        
        // Pitch control: W/S or Up/Down arrows
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
            discreteActionsOut[0] = 0; // Pitch up
        else if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
            discreteActionsOut[0] = 2; // Pitch down
        else
            discreteActionsOut[0] = 1; // Neutral
        
        // Roll control: A/D or Left/Right arrows
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
            discreteActionsOut[1] = 0; // Roll left
        else if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
            discreteActionsOut[1] = 2; // Roll right
        else
            discreteActionsOut[1] = 1; // Neutral
    }

    // Called by WaypointReward when collected
    public void CollectWaypoint(float rewardAmount)
    {
        waypointsCollected++;
        AddReward(rewardAmount);
        Debug.Log($"Waypoint collected! Total: {waypointsCollected}, Reward: +{rewardAmount}");
        
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
        Debug.Log($"Boundary hit! Penalty: {penaltyAmount}");
        
        if (endEpisode)
        {
            EndEpisode();
        }
    }
}