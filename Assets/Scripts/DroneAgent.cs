using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using System.Linq;

public class DroneAgent : Agent
{
    private Plane plane;
    private Rigidbody rb;
    private Transform environmentParent;
    private Vector3 episodeStartPosition;
    
    [Header("Start Position - Local Coordinates")]
    [SerializeField] private Vector3 localStartPosition = new Vector3(0f, 10f, 0f);
    [SerializeField] private float initialSpeed = 20f;
    
    [Header("Goal Tracking")]
    private Transform targetGoal;
    private Transform[] allGoals;
    private int totalGoals = 0;
    private int goalsCollectedThisEpisode = 0;
    private int consecutiveRewards = 0;
    
    [Header("Training Area")]
    [SerializeField] private Transform trainingArea;
    [SerializeField] private float maxDistanceFromStart = 500f; // Max distance from start position

    [Header("Stuck Detection")]
    [SerializeField] private float stuckSpeedThreshold = 2f;
    [SerializeField] private float stuckResetTime = 5f;
    private float stuckTimer = 0f;

    [Header("Interaction Timeout")]
    [SerializeField] private float interactionTimeout = 60f; // Increased for multi-goal collection
    private float timeSinceLastInteraction = 0f;

    public override void Initialize()
    {
        // Only reset time scale to 1x during inference (when no Python trainer is connected)
        // During training, mlagents-learn sets --time-scale (e.g., 20x) which we should NOT override
        var academy = Academy.Instance;
        if (!academy.IsCommunicatorOn)
        {
            Time.timeScale = 1f;
        }
        
        plane = GetComponent<Plane>();
        rb = GetComponent<Rigidbody>();
        
        if (plane == null)
            Debug.LogError($"DroneAgent: Plane component not found on {gameObject.name}");
        if (rb == null)
            Debug.LogError($"DroneAgent: Rigidbody component not found on {gameObject.name}");
        
        // Find parent environment transform
        environmentParent = transform.parent;
        while (environmentParent != null && !environmentParent.name.Contains("Environment") && !environmentParent.name.Contains("Area"))
        {
            environmentParent = environmentParent.parent;
        }
        
        if (environmentParent != null)
        {
            trainingArea = environmentParent;
            Debug.Log($"<color=cyan>[{gameObject.name}] Found environment parent: {environmentParent.name}</color>");
        }
        
        RefreshGoals();
        
        if (totalGoals == 0)
        {
            Debug.LogWarning($"<color=orange>[{gameObject.name}] No reward spheres found! Make sure spheres are tagged with 'Reward'</color>");
        }
    }
    
    void RefreshGoals()
    {
        if (trainingArea != null)
        {
            // IMPORTANT: Use (true) to include inactive objects so totalGoals is always correct
            var rewardObjects = trainingArea.GetComponentsInChildren<Transform>(true)
                .Where(t => t.CompareTag("Reward") && t != trainingArea)
                .ToArray();
            
            allGoals = rewardObjects;
            totalGoals = allGoals.Length;
            
            Debug.Log($"<color=cyan>[{gameObject.name}] Found {totalGoals} reward spheres in local environment</color>");
        }
        else
        {
            // Fallback: Find all reward objects in scene (only active ones with this method)
            GameObject[] rewardObjects = GameObject.FindGameObjectsWithTag("Reward");
            allGoals = new Transform[rewardObjects.Length];
            
            for (int i = 0; i < rewardObjects.Length; i++)
            {
                allGoals[i] = rewardObjects[i].transform;
            }
            
            totalGoals = allGoals.Length;
            Debug.LogWarning($"<color=orange>[{gameObject.name}] Using global reward search - found {totalGoals} spheres</color>");
        }
    }

    public override void OnEpisodeBegin()
    {
        if (plane == null) plane = GetComponent<Plane>();
        if (rb == null) rb = GetComponent<Rigidbody>();
        
        if (plane == null || rb == null) return;

        Vector3 worldStartPosition;
        Quaternion startRotation;
        
        if (environmentParent != null)
        {
            worldStartPosition = environmentParent.TransformPoint(localStartPosition);
            startRotation = environmentParent.rotation;
        }
        else
        {
            worldStartPosition = localStartPosition;
            startRotation = Quaternion.identity;
        }
        
        // Store the start position for bounds checking
        episodeStartPosition = worldStartPosition;
        
        plane.ResetTo(worldStartPosition, startRotation, initialSpeed);
        rb.isKinematic = false;
        
        consecutiveRewards = 0;
        goalsCollectedThisEpisode = 0;
        
        // Re-enable all reward spheres for new episode
        ReactivateAllGoals();
        RefreshGoals();
        UpdateTargetGoal();
        stuckTimer = 0f;
        timeSinceLastInteraction = 0f;
        
        string envId = environmentParent != null ? environmentParent.name : "Unknown";
        Debug.Log($"<color=cyan>[{gameObject.name}] Episode reset complete - Env: {envId}, Goals: {totalGoals}, Start: {worldStartPosition}</color>");
    }
    
    void ReactivateAllGoals()
    {
        // Re-enable all reward spheres in this environment
        if (trainingArea != null)
        {
            var allRewards = trainingArea.GetComponentsInChildren<Transform>(true) // true = include inactive
                .Where(t => t.CompareTag("Reward") && t != trainingArea);
            
            foreach (var reward in allRewards)
            {
                reward.gameObject.SetActive(true);
            }
        }
    }

    void FixedUpdate()
    {
        if (rb == null || plane == null) return;

        if (plane.Dead)
        {
            Debug.Log($"<color=red>[{gameObject.name}] Plane dead - ending episode</color>");
            EndEpisode();
            return;
        }
        
        if (rb.isKinematic) return;

        // Bounds check - if too far from episode start position
        float distanceFromStart = Vector3.Distance(transform.position, episodeStartPosition);
        if (distanceFromStart > maxDistanceFromStart)
        {
            Debug.Log($"<color=red>[{gameObject.name}] Out of bounds ({distanceFromStart:F0}m from start) - ending episode</color>");
            AddReward(-0.5f);
            EndEpisode();
            return;
        }
        
        // Check if fell below ground
        if (transform.position.y < 0f)
        {
            Debug.Log($"<color=red>[{gameObject.name}] Below ground level - ending episode</color>");
            AddReward(-0.5f);
            EndEpisode();
            return;
        }

        float speed = rb.linearVelocity.magnitude;
        if (speed < stuckSpeedThreshold && transform.position.y < 20f)
        {
            stuckTimer += Time.fixedDeltaTime;
            if (stuckTimer > stuckResetTime)
            {
                Debug.Log($"<color=orange>[{gameObject.name}] Stuck too long ({stuckTimer:F1}s) - restarting episode</color>");
                stuckTimer = 0f;
                EndEpisode();
                return;
            }
        }
        else
        {
            stuckTimer = 0f;
        }
        
        timeSinceLastInteraction += Time.fixedDeltaTime;
        if (timeSinceLastInteraction > interactionTimeout)
        {
            Debug.Log($"<color=yellow>[{gameObject.name}] No interaction for {interactionTimeout}s - timeout penalty</color>");
            AddReward(-0.1f);
            EndEpisode();
        }
        
        // Update target goal periodically
        UpdateTargetGoal();
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        Vector3 localVelocity = rb.linearVelocity;
        sensor.AddObservation(localVelocity);
        
        if (targetGoal != null)
        {
            Vector3 directionToGoal = (targetGoal.position - transform.position).normalized;
            Vector3 goalForward = targetGoal.forward;
            
            sensor.AddObservation(directionToGoal);
            sensor.AddObservation(goalForward);
        }
        else
        {
            sensor.AddObservation(Vector3.zero);
            sensor.AddObservation(Vector3.zero);
        }
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        int pitchAction = actions.DiscreteActions[0];

        float pitch = 0f;
        if (pitchAction == 0) pitch = 1f;      // Pitch up
        else if (pitchAction == 2) pitch = -1f; // Pitch down

        if (plane != null)
        {
            plane.SetControlInput(new Vector3(pitch, 0f, 0f));
            plane.SetThrottleInput(1f);
        }
        
        // Small reward for flying toward goal
        if (targetGoal != null && rb != null)
        {
            Vector3 toGoal = (targetGoal.position - transform.position).normalized;
            Vector3 velocity = rb.linearVelocity.normalized;
            float alignment = Vector3.Dot(velocity, toGoal);
            
            // Small reward for flying toward goal (max +0.01 per step)
            AddReward(alignment * 0.01f);
        }
        
        // Small penalty to encourage faster completion
        AddReward(-0.001f);
    }

    private void OnTriggerEnter(Collider other) {
        if (other.CompareTag("Reward")) {
            consecutiveRewards++;
            goalsCollectedThisEpisode++;
            
            // Reward increases with each consecutive goal: 1.0, 1.1, 1.2, 1.3, 1.4 for goals 1-5
            float rewardAmount = 1f + (consecutiveRewards - 1) * 0.1f;
            AddReward(rewardAmount); // Use AddReward for accumulation
            timeSinceLastInteraction = 0f;
            
            string envId = environmentParent != null ? environmentParent.name : "?";
            Debug.Log($"<color=green>[{gameObject.name}] Goal {goalsCollectedThisEpisode}/{totalGoals} collected! Env: {envId}, Reward: +{rewardAmount:F1}</color>");
            
            // Disable this reward sphere
            other.gameObject.SetActive(false);
            
            // End episode when 5 rewards have been collected (scene contains exactly 5 reward spheres)
            if (goalsCollectedThisEpisode >= 5)
            {
                // Bonus for completing the 5 required goals
                AddReward(2f);
                EndEpisode();
                return;
            }
            else
            {
                // Update to next closest goal
                UpdateTargetGoal();
            }
        }
        if (other.TryGetComponent<Wall>(out Wall wall)) {
            consecutiveRewards = 0;
            AddReward(-1f); // Use AddReward for accumulation
            timeSinceLastInteraction = 0f;
            string envId = environmentParent != null ? environmentParent.name : "?";
            Debug.Log($"<color=red>[{gameObject.name}] Wall hit! Env: {envId}, Penalty: -1, Goals collected: {goalsCollectedThisEpisode}/{totalGoals}</color>");
            EndEpisode();
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discreteActionsOut = actionsOut.DiscreteActions;
        
#if ENABLE_INPUT_SYSTEM
        try
        {
            if (Keyboard.current != null)
            {
                if (Keyboard.current.upArrowKey.isPressed)
                    discreteActionsOut[0] = 0; // Pitch up
                else if (Keyboard.current.downArrowKey.isPressed)
                    discreteActionsOut[0] = 2; // Pitch down
                else
                    discreteActionsOut[0] = 1; // Neutral (nothing)
                return;
            }
        }
        catch
        {
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKey(KeyCode.UpArrow))
            discreteActionsOut[0] = 0; // Pitch up
        else if (Input.GetKey(KeyCode.DownArrow))
            discreteActionsOut[0] = 2; // Pitch down
        else
            discreteActionsOut[0] = 1; // Neutral (nothing)
#endif
    }

    void UpdateTargetGoal()
    {
        targetGoal = null;
        float minDistance = float.MaxValue;
        
        foreach (var goal in allGoals)
        {
            if (goal != null && goal.gameObject.activeInHierarchy)
            {
                float distance = Vector3.Distance(transform.position, goal.position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    targetGoal = goal;
                }
            }
        }
    }
}