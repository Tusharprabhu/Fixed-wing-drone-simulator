using UnityEngine;

/// <summary>
/// Simple Wall component for collision detection with DroneAgent
/// Attach this to any object that should act as a boundary/wall
/// </summary>
public class Wall : MonoBehaviour
{
    [Header("Wall Settings")]
    [SerializeField] private float penaltyAmount = -1f;
    [SerializeField] private bool destroyOnHit = false;
    [SerializeField] private bool logCollisions = true;
    
    void Start()
    {
        // Ensure the object has a trigger collider
        Collider collider = GetComponent<Collider>();
        if (collider == null)
        {
            collider = gameObject.AddComponent<BoxCollider>();
            Debug.LogWarning($"Wall: Added BoxCollider to {gameObject.name} - consider adding a proper collider manually");
        }
        
        // Make sure it's set as a trigger
        collider.isTrigger = true;
    }
    
    void OnTriggerEnter(Collider other)
    {
        // The DroneAgent handles the collision logic in its OnTriggerEnter method
        // This component just marks the object as a wall
        
        if (logCollisions)
        {
            var droneAgent = other.GetComponent<DroneAgent>();
            if (droneAgent != null)
            {
                Debug.Log($"<color=red>Wall {gameObject.name} hit by {other.gameObject.name}</color>");
            }
        }
        
        if (destroyOnHit)
        {
            var droneAgent = other.GetComponent<DroneAgent>();
            if (droneAgent != null)
            {
                // Wait a frame to let the DroneAgent handle the collision first
                StartCoroutine(DestroyAfterFrame());
            }
        }
    }
    
    System.Collections.IEnumerator DestroyAfterFrame()
    {
        yield return null; // Wait one frame
        Destroy(gameObject);
    }
    
    // Utility method to get penalty amount (in case other systems need it)
    public float GetPenaltyAmount()
    {
        return penaltyAmount;
    }
}
