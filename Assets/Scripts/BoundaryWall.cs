using UnityEngine;

public class BoundaryWall : MonoBehaviour
{
    [Header("Penalty Settings")]
    [SerializeField] private float penaltyAmount = -5f;
    [SerializeField] private bool endEpisodeOnHit = false;

    void OnTriggerEnter(Collider other)
    {
        DroneAgent agent = other.GetComponent<DroneAgent>();
        if (agent != null)
        {
            agent.HitBoundary(penaltyAmount, endEpisodeOnHit);
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
