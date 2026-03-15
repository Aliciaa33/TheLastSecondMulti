// ⚠️ Place inside Assets/Editor/PausePanelBuilder.cs
// Then run: Tools > Create Pause Panel

#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor;

/// <summary>
/// Generates the full Pause Panel hierarchy in the currently open scene.
/// Run via Tools > Create Pause Panel.
/// All wiring is done at runtime by PauseManager via FindObjectOfType / GameObject.Find.
/// </summary>
public class PausePanelBuilder : Editor
{
    // ── Colour palette matching The Last Second ────────────────────────────
    static readonly Color BgDark      = new Color(0.04f, 0.08f, 0.16f, 1f);
    static readonly Color NeonBlue    = new Color(0.10f, 0.89f, 1.00f, 1f);
    static readonly Color NeonRed     = new Color(1.00f, 0.26f, 0.26f, 1f);
    static readonly Color ButtonCream = new Color(0.96f, 0.93f, 0.85f, 1f);
    static readonly Color ButtonRed   = new Color(0.88f, 0.76f, 0.75f, 1f);
    static readonly Color PanelBg     = new Color(0.04f, 0.08f, 0.16f, 0.97f);
    static readonly Color OverlayBg   = new Color(0.02f, 0.04f, 0.10f, 0.75f);

    [MenuItem("Tools/Create Pause Panel")]
    public static void Build()
    {
        // ── 1. PauseCanvas (Screen Space Overlay, starts hidden) ─────────────
        GameObject canvasGO = new GameObject("PauseCanvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100; // always on top
        CanvasScaler cs = canvasGO.AddComponent<CanvasScaler>();
        cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920, 1080);
        canvasGO.AddComponent<GraphicRaycaster>();
        canvasGO.SetActive(false); // hidden until paused

        // ── 2. Dark overlay that covers the whole screen ──────────────────────
        GameObject overlay = MakeImage(canvasGO, "Overlay", OverlayBg);
        StretchFull(overlay);

        // ── 3. Panel (centred, fixed size) ────────────────────────────────────
        GameObject panel = new GameObject("PausePanel");
        panel.transform.SetParent(canvasGO.transform, false);
        RectTransform panelRT = panel.AddComponent<RectTransform>();
        panelRT.anchorMin = new Vector2(0.5f, 0.5f);
        panelRT.anchorMax = new Vector2(0.5f, 0.5f);
        panelRT.pivot     = new Vector2(0.5f, 0.5f);
        panelRT.sizeDelta = new Vector2(520, 560);
        panelRT.anchoredPosition = Vector2.zero;

        // Panel background image (assign your circuit-border sprite in Inspector for full effect)
        Image panelImg = panel.AddComponent<Image>();
        panelImg.color = PanelBg;

        // Vertical layout for children
        VerticalLayoutGroup vlg = panel.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(48, 48, 44, 40);
        vlg.spacing = 16;
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;
        vlg.childControlWidth  = true;
        vlg.childControlHeight = false;

        // ── 4. Title ──────────────────────────────────────────────────────────
        MakeTMP(panel, "TitleText", "// PAUSED //", 36, Color.white, new Vector2(0, 56));

        // ── 5. Buttons ────────────────────────────────────────────────────────
        MakePauseButton(panel, "ResumeButton",   "RESUME",       ButtonCream, false);
        MakePauseButton(panel, "RestartButton",  "RESTART",      ButtonCream, false);
        MakePauseButton(panel, "SettingsButton", "SETTINGS",     ButtonCream, false);

        // ── 6. Settings section (hidden by default) ───────────────────────────
        GameObject settingsSection = new GameObject("SettingsSection");
        settingsSection.transform.SetParent(panel.transform, false);
        settingsSection.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 110);
        Image sImg = settingsSection.AddComponent<Image>();
        sImg.color = new Color(0.10f, 0.89f, 1.00f, 0.06f);
        LayoutElement sLE = settingsSection.AddComponent<LayoutElement>();
        sLE.preferredHeight = 110;

        VerticalLayoutGroup svlg = settingsSection.AddComponent<VerticalLayoutGroup>();
        svlg.padding = new RectOffset(20, 20, 14, 14);
        svlg.spacing = 10;
        svlg.childForceExpandWidth  = true;
        svlg.childForceExpandHeight = false;
        svlg.childControlHeight = false;

        MakeSliderRow(settingsSection, "MusicSlider", "MUSIC", 0.70f);
        MakeSliderRow(settingsSection, "SFXSlider",   "SFX",   0.85f);
        settingsSection.SetActive(false);

        // ── 7. Quit button (red tint) ─────────────────────────────────────────
        MakePauseButton(panel, "QuitButton", "QUIT TO MENU", ButtonRed, true);

        // ── 8. Hint label ─────────────────────────────────────────────────────
        MakeTMP(panel, "HintText", "Press P or ESC to resume", 14,
            new Color(0.10f, 0.89f, 1.00f, 0.45f), new Vector2(0, 28));

        // ── 9. PausePanelStyler — applies neon visuals at runtime ─────────────
        // Attach to the canvas so it runs as soon as the panel activates
        canvasGO.AddComponent<PausePanelStyler>();

        // ── 10. PauseManager on its own root GO ──────────────────────────────
        if (GameObject.Find("PauseManager") == null)
        {
            GameObject pmGO = new GameObject("PauseManager");
            pmGO.AddComponent<PauseManager>();
        }

        EditorUtility.DisplayDialog("Done",
            "Pause Panel created in the current scene!\n\n" +
            "Tip: Select 'PausePanel' and assign your circuit-border sprite to the Image component for the full neon frame look.",
            "OK");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    static GameObject MakeImage(GameObject parent, string name, Color color)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        go.AddComponent<RectTransform>();
        go.AddComponent<Image>().color = color;
        return go;
    }

    static GameObject MakeTMP(GameObject parent, string name, string text,
        float size, Color color, Vector2 sizeDelta)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = sizeDelta;
        LayoutElement le = go.AddComponent<LayoutElement>();
        le.preferredHeight = sizeDelta.y;
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = size;
        tmp.color     = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontStyle = FontStyles.Bold;
        return go;
    }

    static void MakePauseButton(GameObject parent, string name,
        string label, Color bgColor, bool isQuit)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0, 58);
        LayoutElement le = go.AddComponent<LayoutElement>();
        le.preferredHeight = 58;

        Image img = go.AddComponent<Image>();
        img.color = bgColor;

        Button btn = go.AddComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.highlightedColor = isQuit
            ? new Color(1f, 0.5f, 0.5f, 1f)
            : new Color(0.85f, 1f, 1f, 1f);
        cb.pressedColor = new Color(0.7f, 0.7f, 0.7f, 1f);
        btn.colors = cb;

        // Label
        GameObject labelGO = new GameObject("Label");
        labelGO.transform.SetParent(go.transform, false);
        RectTransform lrt = labelGO.AddComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero;
        lrt.anchorMax = Vector2.one;
        lrt.sizeDelta = Vector2.zero;
        TextMeshProUGUI tmp = labelGO.AddComponent<TextMeshProUGUI>();
        tmp.text      = label;
        tmp.fontSize  = 20;
        tmp.color     = isQuit ? new Color(0.35f, 0.07f, 0.07f) : new Color(0.07f, 0.12f, 0.22f);
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontStyle = FontStyles.Bold;
    }

    static void MakeSliderRow(GameObject parent, string sliderName,
        string labelText, float defaultValue)
    {
        GameObject row = new GameObject(sliderName + "Row");
        row.transform.SetParent(parent.transform, false);
        RectTransform rrt = row.AddComponent<RectTransform>();
        rrt.sizeDelta = new Vector2(0, 36);
        LayoutElement rle = row.AddComponent<LayoutElement>();
        rle.preferredHeight = 36;
        HorizontalLayoutGroup hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 12;
        hlg.childForceExpandWidth  = false;
        hlg.childForceExpandHeight = true;
        hlg.childAlignment = TextAnchor.MiddleLeft;

        // Label
        GameObject lbl = new GameObject("Label");
        lbl.transform.SetParent(row.transform, false);
        lbl.AddComponent<RectTransform>().sizeDelta = new Vector2(60, 0);
        LayoutElement lle = lbl.AddComponent<LayoutElement>();
        lle.preferredWidth = 60; lle.flexibleWidth = 0;
        TextMeshProUGUI lt = lbl.AddComponent<TextMeshProUGUI>();
        lt.text = labelText; lt.fontSize = 14; lt.fontStyle = FontStyles.Bold;
        lt.color = NeonBlue; lt.alignment = TextAlignmentOptions.MidlineLeft;

        // Slider
        GameObject sliderGO = new GameObject(sliderName);
        sliderGO.transform.SetParent(row.transform, false);
        sliderGO.AddComponent<RectTransform>();
        LayoutElement sle = sliderGO.AddComponent<LayoutElement>();
        sle.flexibleWidth = 1;
        Slider slider = sliderGO.AddComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value    = defaultValue;

        // Slider needs Background and Fill Area children to render
        // (Unity's default Slider setup)
        GameObject slBg = new GameObject("Background");
        slBg.transform.SetParent(sliderGO.transform, false);
        RectTransform slBgRT = slBg.AddComponent<RectTransform>();
        slBgRT.anchorMin = new Vector2(0, 0.25f);
        slBgRT.anchorMax = new Vector2(1, 0.75f);
        slBgRT.sizeDelta = Vector2.zero;
        slBg.AddComponent<Image>().color = new Color(0.1f, 0.9f, 1f, 0.2f);

        GameObject fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(sliderGO.transform, false);
        RectTransform faRT = fillArea.AddComponent<RectTransform>();
        faRT.anchorMin = new Vector2(0, 0.25f);
        faRT.anchorMax = new Vector2(1, 0.75f);
        faRT.offsetMin = new Vector2(5, 0);
        faRT.offsetMax = new Vector2(-15, 0);

        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(fillArea.transform, false);
        RectTransform fillRT = fill.AddComponent<RectTransform>();
        fillRT.sizeDelta = new Vector2(10, 0);
        fill.AddComponent<Image>().color = NeonBlue;
        slider.fillRect = fillRT;

        GameObject handleArea = new GameObject("Handle Slide Area");
        handleArea.transform.SetParent(sliderGO.transform, false);
        RectTransform haRT = handleArea.AddComponent<RectTransform>();
        haRT.anchorMin = Vector2.zero; haRT.anchorMax = Vector2.one;
        haRT.offsetMin = new Vector2(10, 0); haRT.offsetMax = new Vector2(-10, 0);

        GameObject handle = new GameObject("Handle");
        handle.transform.SetParent(handleArea.transform, false);
        RectTransform hdRT = handle.AddComponent<RectTransform>();
        hdRT.sizeDelta = new Vector2(20, 20);
        Image hdImg = handle.AddComponent<Image>();
        hdImg.color = Color.white;
        slider.handleRect = hdRT;
        slider.targetGraphic = hdImg;
    }

    static void StretchFull(GameObject go)
    {
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
    }
}
#endif
