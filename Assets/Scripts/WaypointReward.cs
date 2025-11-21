using UnityEngine;

public class WaypointReward : MonoBehaviour
{
    [Header("Reward Settings")]
    [SerializeField] private float rewardAmount = 10f;
    [SerializeField] private bool respawnAfterCollection = true;
    [SerializeField] private float respawnDelay = 2f;
    [SerializeField] private Vector3 respawnAreaMin = new Vector3(-100, 10, -100);
    [SerializeField] private Vector3 respawnAreaMax = new Vector3(100, 50, 100);

    [Header("Visual Settings")]
    [SerializeField] private Color waypointColor = Color.green;
    [SerializeField] private float glowIntensity = 2f;
    [SerializeField] private float rotationSpeed = 50f;

    private MeshRenderer meshRenderer;
    private Material waypointMaterial;
    private bool isCollected = false;

    void Start()
    {
        SetupVisuals();
    }

    void Update()
    {
        // Rotate the waypoint for visual effect
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
    }

    void SetupVisuals()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        
        if (meshRenderer == null)
        {
            // Add a mesh renderer if not present
            meshRenderer = gameObject.AddComponent<MeshRenderer>();
            MeshFilter meshFilter = gameObject.AddComponent<MeshFilter>();
            meshFilter.mesh = CreateSphereMesh();
        }

        // Create emission material for glow effect
        waypointMaterial = new Material(Shader.Find("Standard"));
        waypointMaterial.color = waypointColor;
        waypointMaterial.EnableKeyword("_EMISSION");
        waypointMaterial.SetColor("_EmissionColor", waypointColor * glowIntensity);
        meshRenderer.material = waypointMaterial;
    }

    Mesh CreateSphereMesh()
    {
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Mesh mesh = sphere.GetComponent<MeshFilter>().mesh;
        Destroy(sphere);
        return mesh;
    }

    void OnTriggerEnter(Collider other)
    {
        if (isCollected) return;

        DroneAgent agent = other.GetComponent<DroneAgent>();
        if (agent != null)
        {
            // Award points to the agent
            agent.CollectWaypoint(rewardAmount);
            isCollected = true;

            if (respawnAfterCollection)
            {
                Invoke(nameof(RespawnWaypoint), respawnDelay);
                // Hide the waypoint temporarily
                meshRenderer.enabled = false;
            }
            else
            {
                gameObject.SetActive(false);
            }
        }
    }

    void RespawnWaypoint()
    {
        // Respawn at random position
        Vector3 newPosition = new Vector3(
            Random.Range(respawnAreaMin.x, respawnAreaMax.x),
            Random.Range(respawnAreaMin.y, respawnAreaMax.y),
            Random.Range(respawnAreaMin.z, respawnAreaMax.z)
        );

        transform.position = newPosition;
        meshRenderer.enabled = true;
        isCollected = false;
    }

    // Manual respawn for training setup
    public void Respawn()
    {
        RespawnWaypoint();
    }
}
