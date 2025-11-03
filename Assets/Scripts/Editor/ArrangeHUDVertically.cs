using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

public class ArrangeHUDVertically : EditorWindow
{
    [MenuItem("Tools/Arrange HUD Vertically")]
    static void ArrangeHUD()
    {
        // Find HUDPanel
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("Canvas not found!");
            return;
        }

        Transform hudPanel = canvas.transform.Find("HUDPanel");
        if (hudPanel == null)
        {
            Debug.LogError("HUDPanel not found!");
            return;
        }

        // Set HUDPanel position
        RectTransform panelRect = hudPanel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(1, 1);
        panelRect.anchorMax = new Vector2(1, 1);
        panelRect.pivot = new Vector2(1, 1);
        panelRect.anchoredPosition = new Vector2(-20, -20);
        panelRect.sizeDelta = new Vector2(250, 400);

        // Arrange elements vertically
        float yPos = -10;
        float spacing = 45;

        ArrangeElement(hudPanel, "Airspeed", ref yPos, spacing, 230, 35);
        ArrangeElement(hudPanel, "Altitude", ref yPos, spacing, 230, 35);
        ArrangeElement(hudPanel, "AOA", ref yPos, spacing, 230, 35);
        ArrangeElement(hudPanel, "GForce", ref yPos, spacing, 230, 35);
        ArrangeElement(hudPanel, "ThrottleBar", ref yPos, spacing, 230, 30);
        ArrangeElement(hudPanel, "HealthBar", ref yPos, spacing, 230, 30);
        ArrangeElement(hudPanel, "Compass", ref yPos, spacing, 230, 35);

        Debug.Log("HUD arranged vertically in top-right corner!");
        EditorUtility.SetDirty(hudPanel.gameObject);
    }

    static void ArrangeElement(Transform parent, string name, ref float yPos, float spacing, float width, float height)
    {
        Transform element = parent.Find(name);
        if (element == null)
        {
            Debug.LogWarning($"Element {name} not found");
            return;
        }

        RectTransform rect = element.GetComponent<RectTransform>();
        if (rect == null) return;

        // Anchor to top-right
        rect.anchorMin = new Vector2(1, 1);
        rect.anchorMax = new Vector2(1, 1);
        rect.pivot = new Vector2(1, 1);
        rect.anchoredPosition = new Vector2(-10, yPos);
        rect.sizeDelta = new Vector2(width, height);

        yPos -= spacing;

        Debug.Log($"Positioned {name} at Y: {yPos + spacing}");
    }
}
