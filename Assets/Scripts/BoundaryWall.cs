using UnityEngine;

public class BoundaryWall : MonoBehaviour
{
    [Header("Penalty Settings")]
    [SerializeField] private float penaltyAmount = -5f;
    [SerializeField] private bool endEpisodeOnHit = false;

    void OnTriggerEnter(Collider other)
    {
        // Check for DroneAgent
        DroneAgent droneAgent = other.GetComponent<DroneAgent>();
        if (droneAgent != null)
        {
            droneAgent.HitBoundary(penaltyAmount, endEpisodeOnHit);
            return;
        }
        
        // Check for PitchAgent
        PitchAgent pitchAgent = other.GetComponent<PitchAgent>();
        if (pitchAgent != null)
        {
            pitchAgent.HitBoundary(penaltyAmount, endEpisodeOnHit);
        }
    }

    void OnValidate()
    {
        // Ensure the collider is a trigger
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
        }
    }
}
