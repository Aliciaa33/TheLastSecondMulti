// Place in Assets/Editor/LoadingSceneBuilder.cs
// Run via: Tools > Create Loading Scene

#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;

public class LoadingSceneBuilder : Editor
{
    // Palette
    static readonly Color BgDark = new Color(0.027f, 0.035f, 0.059f, 1f);
    static readonly Color NeonBlue = new Color(0.10f, 0.89f, 1.00f, 1f);
    static readonly Color NeonGreen = new Color(0.22f, 1.00f, 0.43f, 1f);
    static readonly Color NeonRed = new Color(1.00f, 0.24f, 0.24f, 1f);
    static readonly Color PanelBg = new Color(0.04f, 0.08f, 0.157f, 0.93f);
    static readonly Color Transparent = new Color(0, 0, 0, 0);

    [MenuItem("Tools/Create Loading Scene")]
    public static void Build()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // ── Root + LoadingSceneManager ────────────────────────────────────
        GameObject root = new GameObject("LoadingScene");
        root.AddComponent<LoadingSceneManager>();

        // ── Camera with dark background ───────────────────────────────────
        GameObject camGO = new GameObject("Main Camera");
        Camera cam = camGO.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = BgDark;
        camGO.tag = "MainCamera";

        // ── Canvas ────────────────────────────────────────────────────────
        GameObject canvasGO = new GameObject("Canvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler cs = canvasGO.AddComponent<CanvasScaler>();
        cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920, 1080);
        canvasGO.AddComponent<GraphicRaycaster>();

        // Background fill
        GameObject bgFill = MakeImage(canvasGO, "Background", BgDark);
        StretchFull(bgFill);

        // ── Centre card panel ─────────────────────────────────────────────
        GameObject card = new GameObject("LoadingCard");
        card.transform.SetParent(canvasGO.transform, false);
        RectTransform cardRT = card.AddComponent<RectTransform>();
        cardRT.anchorMin = new Vector2(0.5f, 0.5f);
        cardRT.anchorMax = new Vector2(0.5f, 0.5f);
        cardRT.pivot = new Vector2(0.5f, 0.5f);
        cardRT.sizeDelta = new Vector2(580, 620);
        cardRT.anchoredPosition = Vector2.zero;
        Image cardImg = card.AddComponent<Image>();
        cardImg.color = PanelBg;

        VerticalLayoutGroup vlg = card.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(52, 52, 56, 48);
        vlg.spacing = 0;
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;

        // Neon border (sibling of card, behind it).
        // MakeImage already adds RectTransform, so use GetComponent — never AddComponent again.
        GameObject border = MakeImage(canvasGO, "CardBorder", NeonBlue);
        RectTransform borderRT = border.GetComponent<RectTransform>();
        borderRT.anchorMin = cardRT.anchorMin;
        borderRT.anchorMax = cardRT.anchorMax;
        borderRT.pivot = cardRT.pivot;
        borderRT.anchoredPosition = cardRT.anchoredPosition;
        borderRT.sizeDelta = cardRT.sizeDelta + new Vector2(4, 4);
        border.transform.SetSiblingIndex(card.transform.GetSiblingIndex());

        // ── Game title label ──────────────────────────────────────────────
        MakeTMP(card, "GameTitle", "THE LAST SECOND", 13,
            new Color(NeonBlue.r, NeonBlue.g, NeonBlue.b, 0.5f),
            new Vector2(0, 28), spacing: 8f);
        AddSpace(card, 32);

        // ── Icon group (spinner / success / failed) ───────────────────────
        // They overlap — use a fixed-height container with no layout
        GameObject iconContainer = new GameObject("IconContainer");
        iconContainer.transform.SetParent(card.transform, false);
        RectTransform icRT = iconContainer.AddComponent<RectTransform>();
        icRT.sizeDelta = new Vector2(0, 96);
        LayoutElement icLE = iconContainer.AddComponent<LayoutElement>();
        icLE.preferredHeight = 96; icLE.flexibleWidth = 1;

        // Spinner, SuccessIcon, FailedIcon — created as plain GOs with NO Image component.
        // LoadingIconRenderer.Awake() adds RawImage and draws the textures itself.
        MakeCentredIconEmpty(iconContainer, "Spinner", 88);
        MakeCentredIconEmpty(iconContainer, "SuccessIcon", 88);
        MakeCentredIconEmpty(iconContainer, "FailedIcon", 88);

        AddSpace(card, 28);

        // ── Status text ───────────────────────────────────────────────────
        MakeTMP(card, "StatusText", "CONNECTING.", 22, Color.white,
            new Vector2(0, 34), spacing: 3f, bold: true);

        MakeTMP(card, "SubStatusText",
            "Establishing connection to master server", 13,
            new Color(NeonBlue.r, NeonBlue.g, NeonBlue.b, 0.5f),
            new Vector2(0, 26), spacing: 1.5f);

        AddSpace(card, 24);

        // ── Progress bar ──────────────────────────────────────────────────
        GameObject pbTrack = MakeImage(card, "ProgressTrack",
            new Color(NeonBlue.r, NeonBlue.g, NeonBlue.b, 0.1f));
        pbTrack.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 4);
        LayoutElement pbLE = pbTrack.AddComponent<LayoutElement>();
        pbLE.preferredHeight = 4; pbLE.flexibleWidth = 1;

        // Fill (child of track, use Image.Type.Filled)
        GameObject pbFill = MakeImage(pbTrack, "ProgressBar", NeonBlue);
        Image pbImg = pbFill.GetComponent<Image>();
        pbImg.type = Image.Type.Filled;
        pbImg.fillMethod = Image.FillMethod.Horizontal;
        pbImg.fillAmount = 0f;
        StretchFull(pbFill);

        AddSpace(card, 28);

        // ── Step rows ─────────────────────────────────────────────────────
        string[] stepLabels = {
            "INITIALISING NETWORK",
            "CONNECTING TO PHOTON",
            "AUTHENTICATING",
            "READY"
        };
        for (int i = 0; i < stepLabels.Length; i++)
        {
            MakeStepRow(card, i, stepLabels[i]);
            if (i < stepLabels.Length - 1) AddSpace(card, 8);
        }

        AddSpace(card, 24);

        // ── Retry button ──────────────────────────────────────────────────
        GameObject retryGO = new GameObject("RetryButton");
        retryGO.transform.SetParent(card.transform, false);
        retryGO.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 52);
        LayoutElement rle = retryGO.AddComponent<LayoutElement>();
        rle.preferredHeight = 52; rle.flexibleWidth = 1;
        Image rImg = retryGO.AddComponent<Image>();
        rImg.color = NeonRed;
        retryGO.AddComponent<Button>();
        GameObject rLabel = new GameObject("Label");
        rLabel.transform.SetParent(retryGO.transform, false);
        RectTransform rLRT = rLabel.AddComponent<RectTransform>();
        rLRT.anchorMin = Vector2.zero; rLRT.anchorMax = Vector2.one; rLRT.sizeDelta = Vector2.zero;
        TextMeshProUGUI rTmp = rLabel.AddComponent<TextMeshProUGUI>();
        rTmp.text = "RETRY CONNECTION";
        rTmp.fontSize = 16; rTmp.fontStyle = FontStyles.Bold;
        rTmp.color = new Color(0.05f, 0.05f, 0.05f);
        rTmp.alignment = TextAlignmentOptions.Center;
        rTmp.characterSpacing = 3f;
        retryGO.SetActive(false);

        // ── LoadingIconRenderer on the IconContainer ─────────────────────
        // Draws the spinner ring, checkmark, and X entirely in code
        iconContainer.AddComponent<LoadingIconRenderer>();

        // ── LoadingSceneUI on canvas ──────────────────────────────────────
        canvasGO.AddComponent<LoadingSceneUI>();

        // ── EventSystem ───────────────────────────────────────────────────
        GameObject es = new GameObject("EventSystem");
        es.AddComponent<UnityEngine.EventSystems.EventSystem>();
        es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

        // ── Save ──────────────────────────────────────────────────────────
        System.IO.Directory.CreateDirectory("Assets/Scenes");
        string path = "Assets/Scenes/Loading.unity";
        EditorSceneManager.SaveScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene(), path);
        AssetDatabase.Refresh();
        AddToBuildSettings(path);

        EditorUtility.DisplayDialog("Done",
            "Loading scene saved to: " + path +
            "\n\nAdded as index 0 in Build Settings.", "OK");
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    static void MakeStepRow(GameObject parent, int index, string labelText)
    {
        GameObject row = new GameObject("StepRow" + index);
        row.transform.SetParent(parent.transform, false);
        row.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 28);
        LayoutElement le = row.AddComponent<LayoutElement>();
        le.preferredHeight = 28; le.flexibleWidth = 1;
        HorizontalLayoutGroup hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 14;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = true;
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childControlHeight = false;

        // Dot
        GameObject dot = new GameObject("StepDot" + index);
        dot.transform.SetParent(row.transform, false);
        dot.AddComponent<RectTransform>().sizeDelta = new Vector2(10, 10);
        LayoutElement dle = dot.AddComponent<LayoutElement>();
        dle.preferredWidth = 10; dle.preferredHeight = 10; dle.flexibleWidth = 0;
        dot.AddComponent<Image>().color = new Color(1, 1, 1, 0.18f);

        // Label
        GameObject lbl = new GameObject("StepLabel" + index);
        lbl.transform.SetParent(row.transform, false);
        lbl.AddComponent<RectTransform>().sizeDelta = new Vector2(260, 28);
        LayoutElement lle = lbl.AddComponent<LayoutElement>();
        lle.preferredWidth = 260; lle.flexibleWidth = 0;
        TextMeshProUGUI lt = lbl.AddComponent<TextMeshProUGUI>();
        lt.text = labelText; lt.fontSize = 12;
        lt.color = new Color(1, 1, 1, 0.28f);
        lt.alignment = TextAlignmentOptions.MidlineLeft;
        lt.characterSpacing = 2f;

        // Status
        GameObject stat = new GameObject("StepStatus" + index);
        stat.transform.SetParent(row.transform, false);
        stat.AddComponent<RectTransform>().sizeDelta = new Vector2(140, 28);
        LayoutElement sle = stat.AddComponent<LayoutElement>();
        sle.flexibleWidth = 1;
        TextMeshProUGUI st = stat.AddComponent<TextMeshProUGUI>();
        st.text = "---"; st.fontSize = 11;
        st.color = new Color(1, 1, 1, 0.25f);
        st.alignment = TextAlignmentOptions.MidlineRight;
        st.characterSpacing = 1.5f;
    }

    // Creates a centred RectTransform with NO Image — LoadingIconRenderer adds RawImage itself.
    static GameObject MakeCentredIconEmpty(GameObject parent, string name, float size)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(size, size);
        rt.anchoredPosition = Vector2.zero;
        return go;
    }

    static GameObject MakeTMP(GameObject parent, string name, string text,
        float size, Color color, Vector2 sizeDelta,
        float spacing = 0f, bool bold = false)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        go.AddComponent<RectTransform>().sizeDelta = sizeDelta;
        LayoutElement le = go.AddComponent<LayoutElement>();
        le.preferredHeight = sizeDelta.y; le.flexibleWidth = 1;
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text; tmp.fontSize = size; tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.characterSpacing = spacing;
        if (bold) tmp.fontStyle = FontStyles.Bold;
        return go;
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
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
    }

    static void AddToBuildSettings(string path)
    {
        var existing = EditorBuildSettings.scenes;
        foreach (var s in existing) if (s.path == path) return;
        var updated = new EditorBuildSettingsScene[existing.Length + 1];
        updated[0] = new EditorBuildSettingsScene(path, true);
        existing.CopyTo(updated, 1);
        EditorBuildSettings.scenes = updated;
    }
}
#endif