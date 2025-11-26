using UnityEngine;

[DisallowMultipleComponent]
public class WallVisibilityController : MonoBehaviour
{
    [Tooltip("If true, the MeshRenderer will be hidden when the scene enters Play mode.")]
    public bool hideDuringPlay = true;

    [Tooltip("If true, a small editor gizmo will be drawn in the Scene view to indicate the wall bounds.")]
    public bool drawEditorGizmo = true;

    MeshRenderer meshRenderer;

    void Awake()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer == null) return;

        if (hideDuringPlay && Application.isPlaying)
        {
            meshRenderer.enabled = false;
        }
        else
        {
            meshRenderer.enabled = true;
        }
    }

    void OnValidate()
    {
        // Keep the renderer enabled in the editor so you can see/edit the wall
        if (!Application.isPlaying)
        {
            meshRenderer = GetComponent<MeshRenderer>();
            if (meshRenderer != null) meshRenderer.enabled = true;
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (!drawEditorGizmo) return;
        var col = new Color(1f, 0f, 0f, 0.25f);
        Gizmos.color = col;
        var boxCollider = GetComponent<BoxCollider>();
        if (boxCollider != null)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(boxCollider.center, boxCollider.size);
        }
        else
        {
            // fallback - draw bounds of the mesh renderer
            var mr = GetComponent<MeshRenderer>();
            if (mr != null)
            {
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawCube(mr.bounds.center - transform.position, mr.bounds.size);
            }
        }
    }
#endif
}
