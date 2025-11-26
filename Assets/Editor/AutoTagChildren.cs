#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEditorInternal;

public static class AutoTagChildren
{
    private static void AddTagIfNotExists(string tag)
    {
        if (string.IsNullOrEmpty(tag)) return;

        SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty tagsProp = tagManager.FindProperty("tags");

        // check if tag already exists
        for (int i = 0; i < tagsProp.arraySize; i++)
        {
            if (tagsProp.GetArrayElementAtIndex(i).stringValue == tag)
                return;
        }

        tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
        SerializedProperty newTag = tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1);
        newTag.stringValue = tag;
        tagManager.ApplyModifiedProperties();

        Debug.Log($"Tag '{tag}' added to TagManager");
    }

    private static void TagChildrenOf(GameObject parent, string tag)
    {
        if (parent == null) return;
        var allChildren = parent.GetComponentsInChildren<Transform>(true);
        if (allChildren == null || allChildren.Length <= 1) return; // only itself

        Undo.RecordObjects(allChildren, "Tag Children Recursively");

        foreach (var t in allChildren)
        {
            if (t == null) continue;
            if (t.gameObject == parent) continue;
            var child = t.gameObject;
            if (child.tag != tag)
            {
                child.tag = tag;
                EditorUtility.SetDirty(child);
            }
        }
    }

    private static string[] GetAllRootGameObjectNames()
    {
        var roots = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
        string[] names = new string[roots.Length];
        for (int i = 0; i < roots.Length; i++) names[i] = roots[i].name;
        return names;
    }

    public static void TagChildrenByParentName(string nameContains, string tagName)
    {
        AddTagIfNotExists(tagName);

        var roots = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
        int count = 0;
        foreach (var root in roots)
        {
            // Check root object name
            if (root.name.ToLower().Contains(nameContains.ToLower()))
            {
                TagChildrenOf(root, tagName);
                count++;
            }

            // Also check deeper objects (parents of the children could be nested)
            var parents = root.GetComponentsInChildren<Transform>(true);
            foreach (var t in parents)
            {
                if (t.childCount > 0 && t.name.ToLower().Contains(nameContains.ToLower()))
                {
                    TagChildrenOf(t.gameObject, tagName);
                    count++;
                }
            }
        }

        Debug.Log($"Tagged children of {count} parent GameObject(s) that contain '{nameContains}' with tag '{tagName}'");
    }

    [MenuItem("Tools/Auto Tag/Tag Reward Spheres")] 
    public static void Menu_TagRewardSpheres()
    {
        TagChildrenByParentName("reward", "Reward");
        TagChildrenByParentName("sphere", "Reward");
    }

    [MenuItem("Tools/Auto Tag/Tag Boundary Walls")] 
    public static void Menu_TagBoundaryWalls()
    {
        TagChildrenByParentName("wall", "Boundary");
    }

    [MenuItem("Tools/Auto Tag/Tag Rewards and Walls")] 
    public static void Menu_TagBoth()
    {
        Menu_TagRewardSpheres();
        Menu_TagBoundaryWalls();
    }

    [MenuItem("Tools/Auto Tag/Tag Selected Parent's Children")] 
    public static void Menu_TagSelectedParentChildren()
    {
        var selected = Selection.activeGameObject;
        if (selected == null)
        {
            Debug.LogWarning("No GameObject selected.");
            return;
        }

        // try to determine tag from parent name
        string tag = null;
        string name = selected.name.ToLower();
        if (name.Contains("reward") || name.Contains("sphere")) tag = "Reward";
        if (name.Contains("wall")) tag = "Boundary";

        if (tag == null)
        {
            // ask user choice
            if (EditorUtility.DisplayDialog("Choose Tag", "Assign 'Reward' or 'Boundary' tag to the children of the selected GameObject?", "Reward", "Boundary"))
            {
                tag = "Reward";
            }
            else
            {
                tag = "Boundary";
            }
        }

        AddTagIfNotExists(tag);
        TagChildrenOf(selected, tag);
        Debug.Log($"Tagged children of '{selected.name}' with '{tag}' tag.");
    }
}
#endif
