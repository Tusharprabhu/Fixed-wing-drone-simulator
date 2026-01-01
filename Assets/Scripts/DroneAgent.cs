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
    [Header("Start Position Randomization")]
    [Tooltip("Enable randomizing the local start Y each episode")]
    [SerializeField] private bool randomizeStartY = true;
    [Tooltip("Min start local Y (used when randomizeStartY is true)")]
    [SerializeField] private float startYMin = 10f;
    [Tooltip("Max start local Y (used when randomizeStartY is true)")]
    [SerializeField] private float startYMax = 30f;
    
    [Header("Goal Tracking")]
    private Transform targetGoal;
    private Transform[] allGoals;
    private int currentGoalIndex = 0;
    private int totalGoals = 5;
    
    [Header("Training Area")]
    [SerializeField] private Transform trainingArea;
    [SerializeField] private float maxDistanceFromStart = 500f;

    [Header("Stuck Detection")]
    [SerializeField] private float stuckSpeedThreshold = 2f;
    [SerializeField] private float stuckResetTime = 5f;
    private float stuckTimer = 0f;

    [Header("Interaction Timeout")]
    [SerializeField] private float interactionTimeout = 60f;
    private float timeSinceLastInteraction = 0f;

    public override void Initialize()
    {
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
        
        if (totalGoals != 5)
        {
            Debug.LogWarning($"<color=orange>[{gameObject.name}] Expected 5 reward spheres but found {totalGoals}! Make sure there are exactly 5 spheres named 'Sphere (1)' through 'Sphere (5)'</color>");
        }
    }
    
    void RefreshGoals()
    {
        if (trainingArea != null)
        {
            // Find only the actual sphere objects (Sphere (1), Sphere (2), etc), not the parent container
            var rewardObjects = trainingArea.GetComponentsInChildren<Transform>(true)
                .Where(t => t.name.StartsWith("Sphere (") && t.name.Contains(")"))
                .OrderBy(t => t.name)
                .ToArray();
            
            allGoals = rewardObjects;
            totalGoals = allGoals.Length;
            
            Debug.Log($"<color=cyan>[{gameObject.name}] Found {totalGoals} reward spheres in order: {string.Join(", ", allGoals.Select(g => g.name))}</color>");
        }
        else
        {
            // Fallback: find spheres globally
            var allTransforms = FindObjectsByType<Transform>(FindObjectsSortMode.None);
            allGoals = allTransforms
                .Where(t => t.name.StartsWith("Sphere (") && t.name.Contains(")"))
                .OrderBy(t => t.name)   // your own logical ordering
                .ToArray();
                
            totalGoals = allGoals.Length;
            Debug.LogWarning($"<color=orange>[{gameObject.name}] Using global reward search - found {totalGoals} spheres</color>");
        }
    }

    public override void OnEpisodeBegin()
    {
        if (plane == null) plane = GetComponent<Plane>();
        if (rb == null) rb = GetComponent<Rigidbody>();
        
        if (plane == null || rb == null) return;

        Vector3 runtimeLocalStart = localStartPosition;
        if (randomizeStartY)
        {
            runtimeLocalStart.y = Random.Range(startYMin, startYMax);
        }

        Vector3 worldStartPosition;
        Quaternion startRotation;
        
        if (environmentParent != null)
        {
            worldStartPosition = environmentParent.TransformPoint(runtimeLocalStart);
            startRotation = environmentParent.rotation;
        }
        else
        {
            worldStartPosition = runtimeLocalStart;
            startRotation = Quaternion.identity;
        }
        
        episodeStartPosition = worldStartPosition;
        
        plane.ResetTo(worldStartPosition, startRotation, initialSpeed);
        rb.isKinematic = false;
        
        currentGoalIndex = 0;
        
        ReactivateAllGoals();
        RefreshGoals();
        UpdateTargetGoal();
        
        stuckTimer = 0f;
        timeSinceLastInteraction = 0f;
        
        string envId = environmentParent != null ? environmentParent.name : "Unknown";
        Debug.Log($"<color=cyan>[{gameObject.name}] Episode START - Env: {envId}, Target: {(targetGoal != null ? targetGoal.name : "None")}</color>");
    }
    
    void ReactivateAllGoals()
    {
        if (trainingArea != null)
        {
            // Find only actual sphere objects, not parent containers
            var allRewards = trainingArea.GetComponentsInChildren<Transform>(true)
                .Where(t => t.name.StartsWith("Sphere (") && t.name.Contains(")"));
            
            foreach (var reward in allRewards)
            {
                Vector3 localPos = reward.localPosition;
                localPos.y = Random.Range(10f, 30f);
                reward.localPosition = localPos;
                
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

        float distanceFromStart = Vector3.Distance(transform.position, episodeStartPosition);
        if (distanceFromStart > maxDistanceFromStart)
        {
            Debug.Log($"<color=red>[{gameObject.name}] Out of bounds ({distanceFromStart:F0}m) - ending episode</color>");
            AddReward(-0.5f);
            EndEpisode();
            return;
        }
        
        if (transform.position.y < 0f)
        {
            Debug.Log($"<color=red>[{gameObject.name}] Below ground - ending episode</color>");
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
                Debug.Log($"<color=orange>[{gameObject.name}] Stuck - restarting episode</color>");
                AddReward(-0.3f);
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
            Debug.Log($"<color=yellow>[{gameObject.name}] Timeout - ending episode</color>");
            AddReward(-0.1f);
            EndEpisode();
        }
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        Vector3 localVelocity = rb.linearVelocity;
        sensor.AddObservation(localVelocity);
        
        // Roll-related observations
        if (plane != null)
        {
            sensor.AddObservation(plane.EffectiveInput.z);        // commanded roll [-1,1]
            sensor.AddObservation(plane.LocalAngularVelocity.z);  // roll rate (rad/s)
        }
        else
        {
            sensor.AddObservation(0f);
            sensor.AddObservation(0f);
        }
        
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
        int rollAction = actions.DiscreteActions.Length > 1 ? actions.DiscreteActions[1] : 1;

        float pitch = 0f;
        if (pitchAction == 0) pitch = 1f;
        else if (pitchAction == 2) pitch = -1f;

        float roll = 0f;
        if (rollAction == 0) roll = 1f;      // roll left
        else if (rollAction == 2) roll = -1f; // roll right

        if (plane != null)
        {
            plane.SetControlInput(new Vector3(pitch, 0f, roll));
            plane.SetThrottleInput(1f);
        }
        
        if (targetGoal != null && rb != null)
        {
            Vector3 toGoal = (targetGoal.position - transform.position).normalized;
            Vector3 velocity = rb.linearVelocity.normalized;
            float alignment = Vector3.Dot(velocity, toGoal);
            
            AddReward(alignment * 0.01f);
        }
        
        AddReward(-0.001f);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Reward"))
        {
            // Find which sphere this collider belongs to by checking parent hierarchy
            Transform sphereTransform = other.transform;
            while (sphereTransform != null && !sphereTransform.name.StartsWith("Sphere ("))
            {
                sphereTransform = sphereTransform.parent;
            }
            
            if (sphereTransform == null)
            {
                Debug.LogWarning($"<color=orange>[{gameObject.name}] Could not find sphere parent for collider {other.name}</color>");
                return;
            }
            
            // Check if this is the correct goal in sequence
            if (allGoals != null && currentGoalIndex < allGoals.Length && sphereTransform == allGoals[currentGoalIndex])
            {
                currentGoalIndex++;
                
                float rewardAmount = 1f + (currentGoalIndex - 1) * 0.1f;
                AddReward(rewardAmount);
                timeSinceLastInteraction = 0f;
                
                string envId = environmentParent != null ? environmentParent.name : "?";
                Debug.Log($"<color=green>[{gameObject.name}] Goal {currentGoalIndex}/5 ({sphereTransform.name}) collected! Env: {envId}, Reward: +{rewardAmount:F1}</color>");
                
                sphereTransform.gameObject.SetActive(false);
                
                if (currentGoalIndex >= 5)
                {
                    AddReward(2f);
                    Debug.Log($"<color=lime>[{gameObject.name}] ALL 5 GOALS COMPLETED! Episode Success!</color>");
                    EndEpisode();
                    return;
                }
                else
                {
                    UpdateTargetGoal();
                }
            }
            else
            {
                Debug.Log($"<color=yellow>[{gameObject.name}] Wrong goal! Expected {(currentGoalIndex < allGoals.Length ? allGoals[currentGoalIndex].name : "none")}, got {sphereTransform.name}</color>");
                AddReward(-0.2f);
            }
        }
        
        if (other.TryGetComponent<Wall>(out Wall wall))
        {
            AddReward(-1f);
            timeSinceLastInteraction = 0f;
            string envId = environmentParent != null ? environmentParent.name : "?";
            Debug.Log($"<color=red>[{gameObject.name}] Wall hit! Env: {envId}, Goals collected: {currentGoalIndex}/5</color>");
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
                // Pitch (up/down arrow)
                if (Keyboard.current.upArrowKey.isPressed)
                    discreteActionsOut[0] = 0;
                else if (Keyboard.current.downArrowKey.isPressed)
                    discreteActionsOut[0] = 2;
                else
                    discreteActionsOut[0] = 1;
                
                // Roll (left/right arrow)
                if (discreteActionsOut.Length > 1)
                {
                    if (Keyboard.current.leftArrowKey.isPressed)
                        discreteActionsOut[1] = 0;
                    else if (Keyboard.current.rightArrowKey.isPressed)
                        discreteActionsOut[1] = 2;
                    else
                        discreteActionsOut[1] = 1;
                }
                return;
            }
        }
        catch
        {
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        // Pitch
        if (Input.GetKey(KeyCode.UpArrow))
            discreteActionsOut[0] = 0;
        else if (Input.GetKey(KeyCode.DownArrow))
            discreteActionsOut[0] = 2;
        else
            discreteActionsOut[0] = 1;
        
        // Roll
        if (discreteActionsOut.Length > 1)
        {
            if (Input.GetKey(KeyCode.LeftArrow))
                discreteActionsOut[1] = 0;
            else if (Input.GetKey(KeyCode.RightArrow))
                discreteActionsOut[1] = 2;
            else
                discreteActionsOut[1] = 1;
        }
#endif
    }

    void UpdateTargetGoal()
    {
        if (currentGoalIndex < allGoals.Length)
        {
            targetGoal = allGoals[currentGoalIndex];
            Debug.Log($"<color=cyan>[{gameObject.name}] Next target: {targetGoal.name}</color>");
        }
        else
        {
            targetGoal = null;
        }
    }
}