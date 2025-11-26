using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
#endif

public class WaypointReward : MonoBehaviour
{
    [Header("Reward Settings")]
    [SerializeField] private float rewardAmount = 10f;

    [Header("Visual Settings")]
    [SerializeField] private Color waypointColor = Color.green;
    [SerializeField] private float glowIntensity = 2f;
    [SerializeField] private float rotationSpeed = 50f;

    private MeshRenderer meshRenderer;
    private Material waypointMaterial;

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

#if UNITY_EDITOR
    // In the Editor, ensure this GameObject is tagged as 'Reward' if the tag exists.
    // Use delayCall to avoid "SendMessage cannot be called during OnValidate" error.
    void OnValidate()
    {
        EditorApplication.delayCall += () =>
        {
            if (this == null || gameObject == null) return; // object may have been destroyed
            var tags = InternalEditorUtility.tags;
            if (System.Array.IndexOf(tags, "Reward") >= 0)
            {
                if (!gameObject.CompareTag("Reward"))
                {
                    Undo.RecordObject(gameObject, "Set Reward tag");
                    gameObject.tag = "Reward";
                    EditorUtility.SetDirty(gameObject);
                }
            }
        };
    }
#endif

    Mesh CreateSphereMesh()
    {
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Mesh mesh = sphere.GetComponent<MeshFilter>().mesh;
        Destroy(sphere);
        return mesh;
    }

    void OnTriggerEnter(Collider other)
    {
        DroneAgent agent = other.GetComponent<DroneAgent>();
        if (agent != null)
        {
            // Award points to the agent (sphere stays in place)
            agent.CollectWaypoint(rewardAmount);
        }
    }
}
