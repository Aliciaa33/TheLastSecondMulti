// Place in Assets/Editor/GameOverPanelBuilder.cs
#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor;

public class GameOverPanelBuilder : Editor
{
    static readonly Color NeonBlue = new Color(0.10f, 0.89f, 1.00f, 1f);
    static readonly Color PanelBg  = new Color(0.04f, 0.08f, 0.16f, 0.97f);
    static readonly Color Cream    = new Color(0.96f, 0.93f, 0.85f, 1f);
    static readonly Color BtnRed   = new Color(0.88f, 0.76f, 0.75f, 1f);
    static readonly Color TextDark = new Color(0.07f, 0.12f, 0.22f, 1f);

    [MenuItem("Tools/Rebuild Game Over Panel")]
    public static void Build()
    {
        GameObject existing = GameObject.Find("GameOverCanvas");
        if (existing != null)
        {
            if (!EditorUtility.DisplayDialog("Rebuild Game Over Panel",
                "GameOverCanvas already exists. Replace it?", "Replace", "Cancel"))
                return;
            DestroyImmediate(existing);
        }

        // ── Canvas ────────────────────────────────────────────────────────
        GameObject canvasGO = new GameObject("GameOverCanvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 90;
        CanvasScaler cs = canvasGO.AddComponent<CanvasScaler>();
        cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920, 1080);
        canvasGO.AddComponent<GraphicRaycaster>();
        canvasGO.AddComponent<GameOverPanelStyler>();

        // Full-screen overlay
        GameObject overlay = MakeImage(canvasGO, "GameOverOverlay",
            new Color(0.02f, 0.04f, 0.10f, 0.85f));
        StretchFull(overlay);

        // ── Panel ─────────────────────────────────────────────────────────
        GameObject panel = new GameObject("GameOverPanel");
        panel.transform.SetParent(canvasGO.transform, false);
        RectTransform panelRT = panel.AddComponent<RectTransform>();
        panelRT.anchorMin = panelRT.anchorMax = new Vector2(0.5f, 0.5f);
        panelRT.pivot     = new Vector2(0.5f, 0.5f);
        panelRT.sizeDelta = new Vector2(520, 600);
        panelRT.anchoredPosition = Vector2.zero;
        panel.AddComponent<Image>().color = PanelBg;

        // Use a plain vertical stack — no LayoutGroup on panel itself.
        // Each child is absolutely positioned so nothing fights.
        VerticalLayoutGroup vlg = panel.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(52, 52, 48, 40);
        vlg.spacing = 0;
        vlg.childAlignment        = TextAnchor.UpperCenter;
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;
        vlg.childControlWidth  = true;
        vlg.childControlHeight = false;

        MakeTMP(panel, "GOGameTitle", "THE LAST SECOND", 11,
            new Color(NeonBlue.r, NeonBlue.g, NeonBlue.b, 0.4f),
            new Vector2(0, 20), spacing: 7f);
        AddSpace(panel, 12);

        // ── Icon container ────────────────────────────────────────────────
        GameObject iconContainer = new GameObject("GOIconContainer");
        iconContainer.transform.SetParent(panel.transform, false);
        iconContainer.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 88);
        LayoutElement icle = iconContainer.AddComponent<LayoutElement>();
        icle.preferredHeight = 88; icle.flexibleWidth = 1;
        iconContainer.AddComponent<GameOverIconRenderer>();
        MakeCentredIconEmpty(iconContainer, "GOWinIcon",  88);
        MakeCentredIconEmpty(iconContainer, "GOLossIcon", 88);

        AddSpace(panel, 18);
        MakeTMP(panel, "ResultTagText", "// MISSION FAILED //", 13,
            new Color(1f, 0.24f, 0.24f, 1f), new Vector2(0, 20), spacing: 4f);
        AddSpace(panel, 4);
        MakeTMP(panel, "ResultHeadingText", "GAME OVER", 42,
            new Color(1f, 0.24f, 0.24f, 1f), new Vector2(0, 54), spacing: 2f, bold: true);
        AddSpace(panel, 6);
        MakeTMP(panel, "ResultSubText",
            "All operatives lost. The bomb was not defused in time.",
            14, new Color(1, 1, 1, 0.45f), new Vector2(0, 38));
        AddSpace(panel, 20);

        // ── Stats row — FIXED: use absolute-height row, no nested VLG ────
        // The root stats container is a fixed-height HorizontalLayoutGroup.
        // Each cell is a plain GO with two TMP children stacked top-to-bottom
        // using a ContentSizeFitter, avoiding the height-fighting bug.
        GameObject statsRow = new GameObject("StatsContainer");
        statsRow.transform.SetParent(panel.transform, false);
        statsRow.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 80);
        LayoutElement srle = statsRow.AddComponent<LayoutElement>();
        srle.preferredHeight = 80; srle.minHeight = 80; srle.flexibleWidth = 1;
        statsRow.AddComponent<Image>().color =
            new Color(NeonBlue.r, NeonBlue.g, NeonBlue.b, 0.06f);

        HorizontalLayoutGroup shlg = statsRow.AddComponent<HorizontalLayoutGroup>();
        shlg.childAlignment        = TextAnchor.MiddleCenter;
        shlg.childForceExpandWidth  = true;
        shlg.childForceExpandHeight = true;  // cells stretch to full row height
        shlg.childControlWidth  = true;
        shlg.childControlHeight = true;      // cells fill the 80px row height
        shlg.padding = new RectOffset(4, 4, 0, 0);

        MakeStatCell(statsRow, "StatHP",    "0",     "HP REMAINING");
        MakeStatDivider(statsRow);
        MakeStatCell(statsRow, "StatBombs", "0 / 3", "BOMBS DEFUSED");
        MakeStatDivider(statsRow);
        MakeStatCell(statsRow, "StatHints", "0",     "HINTS FOUND");

        AddSpace(panel, 20);
        MakeButton(panel, "RestartButton",  "RESTART MISSION",
            Cream,   TextDark,
            new Color(0.35f, 0.07f, 0.07f, 1f), false, new Vector2(0, 52));
        AddSpace(panel, 10);
        MakeButton(panel, "QuitMenuButton", "QUIT TO MENU",
            BtnRed,  new Color(0.35f, 0.07f, 0.07f, 1f),
            new Color(1f, 0.55f, 0.55f, 1f), true,  new Vector2(0, 52));

        if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        canvasGO.SetActive(false);
        EditorUtility.DisplayDialog("Done",
            "GameOverCanvas built.\n\n" +
            "Assign GameOverCanvas to UIManager.gameOverCanvas.", "OK");
    }

    // ── Stat cell: two TMP labels centred inside the cell ─────────────────
    // Both value and label are children of the cell with anchors set to
    // upper/lower halves — no nested LayoutGroup, so nothing fights the HLG.
    static void MakeStatCell(GameObject parent, string valName,
        string valText, string labelText)
    {
        GameObject cell = new GameObject(valName + "Cell");
        cell.transform.SetParent(parent.transform, false);
        RectTransform cellRT = cell.AddComponent<RectTransform>();
        // HLG will size it; we just need the RectTransform to exist
        cellRT.sizeDelta = Vector2.zero;

        // Value text — anchored to upper half of cell
        GameObject valGO = new GameObject(valName);
        valGO.transform.SetParent(cell.transform, false);
        RectTransform valRT = valGO.AddComponent<RectTransform>();
        valRT.anchorMin = new Vector2(0, 0.45f);
        valRT.anchorMax = new Vector2(1, 1f);
        valRT.offsetMin = valRT.offsetMax = Vector2.zero;
        TextMeshProUGUI vt = valGO.AddComponent<TextMeshProUGUI>();
        vt.text      = valText;
        vt.fontSize  = 26;
        vt.fontStyle = FontStyles.Bold;
        vt.color     = NeonBlue;
        vt.alignment = TextAlignmentOptions.Bottom;
        vt.enableAutoSizing = false;

        // Label text — anchored to lower half of cell
        string labelGoName = valName + "Label";
        GameObject lblGO = new GameObject(labelGoName);
        lblGO.transform.SetParent(cell.transform, false);
        RectTransform lblRT = lblGO.AddComponent<RectTransform>();
        lblRT.anchorMin = new Vector2(0, 0f);
        lblRT.anchorMax = new Vector2(1, 0.5f);
        lblRT.offsetMin = lblRT.offsetMax = Vector2.zero;
        TextMeshProUGUI lt = lblGO.AddComponent<TextMeshProUGUI>();
        lt.text      = labelText;
        lt.fontSize  = 10;
        lt.color     = new Color(NeonBlue.r, NeonBlue.g, NeonBlue.b, 0.45f);
        lt.alignment = TextAlignmentOptions.Top;
        lt.characterSpacing = 1.5f;
        lt.enableAutoSizing = false;
    }

    static void MakeStatDivider(GameObject parent)
    {
        GameObject div = new GameObject("StatDivider");
        div.transform.SetParent(parent.transform, false);
        LayoutElement le = div.AddComponent<LayoutElement>();
        le.preferredWidth = 1; le.flexibleWidth = 0;
        div.AddComponent<Image>().color =
            new Color(NeonBlue.r, NeonBlue.g, NeonBlue.b, 0.15f);
    }

    static void MakeButton(GameObject parent, string name, string label,
        Color bg, Color textCol, Color hoverCol, bool isQuit, Vector2 sizeDelta)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        go.AddComponent<RectTransform>().sizeDelta = sizeDelta;
        LayoutElement le = go.AddComponent<LayoutElement>();
        le.preferredHeight = sizeDelta.y; le.flexibleWidth = 1;
        go.AddComponent<Image>().color = bg;
        Button btn = go.AddComponent<Button>();
        ColorBlock cb      = btn.colors;
        cb.normalColor     = bg;
        cb.highlightedColor= hoverCol;
        cb.pressedColor    = new Color(0.68f, 0.68f, 0.68f);
        cb.colorMultiplier = 1f;
        btn.colors = cb;

        GameObject lbl = new GameObject("Label");
        lbl.transform.SetParent(go.transform, false);
        RectTransform lrt = lbl.AddComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
        lrt.sizeDelta = Vector2.zero;
        TextMeshProUGUI tmp = lbl.AddComponent<TextMeshProUGUI>();
        tmp.text      = label;
        tmp.fontSize  = 20;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color     = textCol;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.characterSpacing = 3f;
    }

    static void MakeCentredIconEmpty(GameObject parent, string name, float size)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(size, size);
        rt.anchoredPosition = Vector2.zero;
    }

    static void MakeTMP(GameObject parent, string name, string text,
        float size, Color color, Vector2 sizeDelta,
        float spacing = 0f, bool bold = false)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        go.AddComponent<RectTransform>().sizeDelta = sizeDelta;
        LayoutElement le = go.AddComponent<LayoutElement>();
        le.preferredHeight = sizeDelta.y; le.flexibleWidth = 1;
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = size;
        tmp.color     = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.characterSpacing = spacing;
        tmp.enableAutoSizing = false;
        if (bold) tmp.fontStyle = FontStyles.Bold;
    }

    static GameObject MakeImage(GameObject parent, string name, Color color)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        go.AddComponent<RectTransform>();
        go.AddComponent<Image>().color = color;
        return go;
    }

    static void AddSpace(GameObject parent, float height)
    {
        GameObject sp = new GameObject("Space");
        sp.transform.SetParent(parent.transform, false);
        sp.AddComponent<RectTransform>().sizeDelta = new Vector2(0, height);
        LayoutElement le = sp.AddComponent<LayoutElement>();
        le.preferredHeight = height; le.flexibleWidth = 1;
    }

    static void StretchFull(GameObject go)
    {
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }
}
#endif