using UnityEngine;
using UnityEditor;

public class ApplyBuiltInMaterials : MonoBehaviour
{
    [MenuItem("Tools/Apply Built-in Materials to Drones")]
    static void ApplyMaterialsToDrones()
    {
        // Load built-in materials
        Material redMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Drone_Red.mat");
        Material blueMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Drone_Blue.mat");
        Material greenMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Drone_Green.mat");
        Material yellowMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Drone_Yellow.mat");
        Material purpleMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Drone_Purple.mat");
        
        // Create additional colors programmatically
        Material cyanMat = new Material(Shader.Find("Standard"));
        cyanMat.color = new Color(0f, 1f, 1f); // Cyan
        
        Material orangeMat = new Material(Shader.Find("Standard"));
        orangeMat.color = new Color(1f, 0.5f, 0f); // Orange
        
        Material pinkMat = new Material(Shader.Find("Standard"));
        pinkMat.color = new Color(1f, 0.4f, 0.7f); // Pink

        // Map drones to unique materials
        var mappings = new (string droneName, Material material)[]
        {
            ("Drone_1", redMat),
            ("Drone_2", blueMat),
            ("Drone_3", greenMat),
            ("Drone_4", yellowMat),
            ("Drone_5", purpleMat),
            ("Drone_6", cyanMat),
            ("Drone_7", orangeMat),
            ("Drone_8", pinkMat)
        };

        int successCount = 0;
        foreach (var (droneName, material) in mappings)
        {
            GameObject drone = GameObject.Find(droneName);
            if (drone != null)
            {
                // Find Myfbx child which contains the mesh renderers
                Transform myfbx = drone.transform.Find("Myfbx");
                if (myfbx != null)
                {
                    // Create unique material instance for this drone
                    Material uniqueMaterial = new Material(material);
                    
                    // Apply to all renderers in Myfbx
                    Renderer[] renderers = myfbx.GetComponentsInChildren<Renderer>();
                    foreach (Renderer renderer in renderers)
                    {
                        // Create array of unique material instances for each renderer
                        Material[] materials = new Material[renderer.sharedMaterials.Length];
                        for (int i = 0; i < materials.Length; i++)
                        {
                            materials[i] = new Material(uniqueMaterial);
                        }
                        renderer.sharedMaterials = materials;
                    }
                    
                    Debug.Log($"Applied {material.name} to {droneName}");
                    successCount++;
                }
                else
                {
                    Debug.LogWarning($"Could not find Myfbx child in {droneName}");
                }
            }
            else
            {
                Debug.LogWarning($"Could not find {droneName}");
            }
        }

        Debug.Log($"<color=green>Successfully applied materials to {successCount} drones!</color>");
    }
}
