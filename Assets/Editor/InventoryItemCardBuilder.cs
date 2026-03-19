// Place in Assets/Editor/InventoryItemCardBuilder.cs
// Run via: Tools > Rebuild Inventory Item Card Prefab
#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor;

public class InventoryItemCardBuilder : Editor
{
    static readonly Color NeonBlue  = new Color(0.10f, 0.89f, 1.00f, 1f);
    static readonly Color CardBg    = new Color(0.10f, 0.89f, 1.00f, 0.07f);
    static readonly Color CardBorder= new Color(0.10f, 0.89f, 1.00f, 0.28f);

    [MenuItem("Tools/Rebuild Inventory Item Card Prefab")]
    public static void Build()
    {
        // ── Build the card hierarchy in the scene first ───────────────────
        // Root: fixed size matching GridLayoutGroup cell (180 x 120)
        GameObject card = new GameObject("InventoryItemCard");
        RectTransform cardRT = card.AddComponent<RectTransform>();
        cardRT.sizeDelta = new Vector2(180, 120);

        // Background image
        Image bg = card.AddComponent<Image>();
        bg.color = CardBg;

        // Outline border
        Outline outline = card.AddComponent<Outline>();
        outline.effectColor    = CardBorder;
        outline.effectDistance = new Vector2(1.5f, -1.5f);

        // InventoryItemUI component — holds refs to the two TMP children
        InventoryItemUI itemUI = card.AddComponent<InventoryItemUI>();

        // ── VerticalLayoutGroup keeps number + divider + hint stacked ─────
        VerticalLayoutGroup vlg = card.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(8, 8, 10, 10);
        vlg.spacing = 4;
        vlg.childAlignment        = TextAnchor.UpperCenter;
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;
        vlg.childControlWidth  = true;
        vlg.childControlHeight = false;

        // ── Number text (large, top half) ─────────────────────────────────
        GameObject numGO = new GameObject("NumberText");
        numGO.transform.SetParent(card.transform, false);
        numGO.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 42);
        LayoutElement nle = numGO.AddComponent<LayoutElement>();
        nle.preferredHeight = 42; nle.flexibleWidth = 1;
        TextMeshProUGUI numTMP = numGO.AddComponent<TextMeshProUGUI>();
        numTMP.text             = "0";
        numTMP.fontSize         = 34;
        numTMP.fontStyle        = FontStyles.Bold;
        numTMP.color            = Color.white;
        numTMP.alignment        = TextAlignmentOptions.Center;
        numTMP.enableAutoSizing = false;
        numTMP.outlineWidth     = 0.1f;
        numTMP.outlineColor     = NeonBlue;

        // ── Thin divider ──────────────────────────────────────────────────
        GameObject divGO = new GameObject("Divider");
        divGO.transform.SetParent(card.transform, false);
        divGO.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 1);
        LayoutElement dle = divGO.AddComponent<LayoutElement>();
        dle.preferredHeight = 1; dle.flexibleWidth = 1;
        divGO.AddComponent<Image>().color =
            new Color(NeonBlue.r, NeonBlue.g, NeonBlue.b, 0.3f);

        // ── Hint text (smaller, bottom half) ──────────────────────────────
        GameObject hintGO = new GameObject("HintText");
        hintGO.transform.SetParent(card.transform, false);
        hintGO.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 44);
        LayoutElement hle = hintGO.AddComponent<LayoutElement>();
        hle.preferredHeight = 44; hle.flexibleWidth = 1;
        TextMeshProUGUI hintTMP = hintGO.AddComponent<TextMeshProUGUI>();
        hintTMP.text             = "Hint text";
        hintTMP.fontSize         = 12;
        hintTMP.color            =
            new Color(NeonBlue.r, NeonBlue.g, NeonBlue.b, 0.8f);
        hintTMP.alignment        = TextAlignmentOptions.Center;
        hintTMP.enableAutoSizing = false;
        hintTMP.enableWordWrapping = true;

        // ── Wire InventoryItemUI Inspector references ─────────────────────
        // (Done via SerializedObject so it survives into the prefab asset)
        SerializedObject so = new SerializedObject(itemUI);
        so.FindProperty("numberText").objectReferenceValue = numTMP;
        so.FindProperty("hintText").objectReferenceValue   = hintTMP;
        so.ApplyModifiedPropertiesWithoutUndo();

        // ── Save as prefab ────────────────────────────────────────────────
        string folder = "Assets/Prefabs";
        if (!System.IO.Directory.Exists(folder))
        {
            System.IO.Directory.CreateDirectory(folder);
            AssetDatabase.Refresh();
        }

        string path   = folder + "/InventoryItemCard.prefab";
        bool   exists = System.IO.File.Exists(path);

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(card, path);
        DestroyImmediate(card); // remove from scene — we only needed it to build

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            exists ? "Prefab Updated" : "Prefab Created",
            "Saved to: " + path + "\n\n" +
            "Now assign this prefab to InventoryUI.inventoryItem in the Inspector.",
            "OK");

        // Ping the prefab in the Project window
        EditorGUIUtility.PingObject(prefab);
    }
}
#endif