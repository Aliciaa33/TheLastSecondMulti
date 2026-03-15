// Place in Assets/Editor/InventoryPanelBuilder.cs
#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor;

public class InventoryPanelBuilder : Editor
{
    static readonly Color NeonBlue  = new Color(0.10f, 0.89f, 1.00f, 1f);
    static readonly Color NeonAmber = new Color(1.00f, 0.75f, 0.30f, 1f);
    static readonly Color PanelBg   = new Color(0.04f, 0.08f, 0.16f, 0.97f);
    static readonly Color Cream     = new Color(0.96f, 0.93f, 0.85f, 1f);
    static readonly Color TextDark  = new Color(0.07f, 0.12f, 0.22f, 1f);

    [MenuItem("Tools/Rebuild Inventory Panel")]
    public static void Build()
    {
        GameObject existing = GameObject.Find("InventoryCanvas");
        if (existing != null)
        {
            if (!EditorUtility.DisplayDialog("Rebuild Inventory Panel",
                "InventoryCanvas already exists. Replace it?", "Replace", "Cancel"))
                return;
            DestroyImmediate(existing);
        }

        // ── Canvas ────────────────────────────────────────────────────────
        GameObject canvasGO = new GameObject("InventoryCanvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;
        CanvasScaler cs = canvasGO.AddComponent<CanvasScaler>();
        cs.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920, 1080);
        canvasGO.AddComponent<GraphicRaycaster>();
        canvasGO.AddComponent<InventoryPanelStyler>();

        // Dim overlay
        GameObject overlay = MakeImage(canvasGO, "InventoryOverlay",
            new Color(0.02f, 0.04f, 0.10f, 0.55f));
        StretchFull(overlay);

        // ── Panel ─────────────────────────────────────────────────────────
        // Fixed size panel — content inside scrolls via ScrollRect
        GameObject panel = new GameObject("InventoryPanel");
        panel.transform.SetParent(canvasGO.transform, false);
        RectTransform panelRT = panel.AddComponent<RectTransform>();
        panelRT.anchorMin        = panelRT.anchorMax = new Vector2(0.5f, 0.5f);
        panelRT.pivot            = new Vector2(0.5f, 0.5f);
        panelRT.sizeDelta        = new Vector2(780, 640);
        panelRT.anchoredPosition = Vector2.zero;
        panel.AddComponent<Image>().color = PanelBg;

        VerticalLayoutGroup panelVLG = panel.AddComponent<VerticalLayoutGroup>();
        panelVLG.padding               = new RectOffset(40, 40, 32, 28);
        panelVLG.spacing               = 0;
        panelVLG.childAlignment        = TextAnchor.UpperCenter;
        panelVLG.childForceExpandWidth  = true;
        panelVLG.childForceExpandHeight = false;
        panelVLG.childControlWidth  = true;
        panelVLG.childControlHeight = false;

        // ── Top bar: game title (left-pad) + X button (right) ─────────────
        // This is a RELATIVE layout: title floats centre, X pins to right.
        // We put them inside a fixed-height overlay container using anchors,
        // NOT a HorizontalLayoutGroup, so X always stays top-right.
        GameObject topBar = new GameObject("TopBar");
        topBar.transform.SetParent(panel.transform, false);
        RectTransform tbRT = topBar.AddComponent<RectTransform>();
        tbRT.sizeDelta = new Vector2(0, 40);
        LayoutElement tble = topBar.AddComponent<LayoutElement>();
        tble.preferredHeight = 40; tble.flexibleWidth = 1;

        // Game subtitle — centred inside top bar
        GameObject gameTitleGO = new GameObject("InvGameTitle");
        gameTitleGO.transform.SetParent(topBar.transform, false);
        RectTransform gtRT = gameTitleGO.AddComponent<RectTransform>();
        gtRT.anchorMin = Vector2.zero; gtRT.anchorMax = Vector2.one;
        gtRT.offsetMin = gtRT.offsetMax = Vector2.zero;
        TextMeshProUGUI gtTMP = gameTitleGO.AddComponent<TextMeshProUGUI>();
        gtTMP.text      = "THE LAST SECOND";
        gtTMP.fontSize  = 11;
        gtTMP.color     = new Color(NeonBlue.r, NeonBlue.g, NeonBlue.b, 0.4f);
        gtTMP.alignment = TextAlignmentOptions.Center;
        gtTMP.characterSpacing = 7f;

        // Close button — anchored to right centre of top bar
        GameObject closeGO = new GameObject("InventoryCloseButton");
        closeGO.transform.SetParent(topBar.transform, false);
        RectTransform closeRT = closeGO.AddComponent<RectTransform>();
        closeRT.anchorMin        = new Vector2(1f, 0.5f);
        closeRT.anchorMax        = new Vector2(1f, 0.5f);
        closeRT.pivot            = new Vector2(1f, 0.5f);
        closeRT.sizeDelta        = new Vector2(36, 36);
        closeRT.anchoredPosition = new Vector2(0, 0);
        Image closeImg = closeGO.AddComponent<Image>();
        closeImg.color = new Color(NeonBlue.r, NeonBlue.g, NeonBlue.b, 0.12f);
        closeGO.AddComponent<Button>();
        GameObject closeLbl = new GameObject("Label");
        closeLbl.transform.SetParent(closeGO.transform, false);
        RectTransform clRT = closeLbl.AddComponent<RectTransform>();
        clRT.anchorMin = Vector2.zero; clRT.anchorMax = Vector2.one;
        clRT.sizeDelta = Vector2.zero;
        TextMeshProUGUI closeTMP = closeLbl.AddComponent<TextMeshProUGUI>();
        closeTMP.text      = "X";
        closeTMP.fontSize  = 16;
        closeTMP.fontStyle = FontStyles.Bold;
        closeTMP.color     = NeonBlue;
        closeTMP.alignment = TextAlignmentOptions.Center;

        AddSpace(panel, 6);

        // ── Inventory title — centred ──────────────────────────────────────
        MakeTMP(panel, "InventoryTitleText", "// INVENTORY //", 24,
            Color.white, new Vector2(0, 36), spacing: 3f, bold: true,
            align: TextAlignmentOptions.Center);

        AddSpace(panel, 14);

        // Divider
        MakeDivider(panel, "InventoryDivider");

        AddSpace(panel, 14);

        // ── Hints section label ───────────────────────────────────────────
        MakeTMP(panel, "HintsSectionLabel", "HINTS", 11,
            new Color(NeonBlue.r, NeonBlue.g, NeonBlue.b, 0.45f),
            new Vector2(0, 16), spacing: 5f,
            align: TextAlignmentOptions.Center);
        AddSpace(panel, 8);

        // ── Scroll view for hints grid ────────────────────────────────────
        // Using a ScrollRect means any number of cards will scroll cleanly
        // instead of overflowing or shrinking.
        GameObject scrollGO = new GameObject("HintsScrollView");
        scrollGO.transform.SetParent(panel.transform, false);
        scrollGO.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 260);
        LayoutElement sle = scrollGO.AddComponent<LayoutElement>();
        sle.preferredHeight = 260; sle.flexibleWidth = 1;
        scrollGO.AddComponent<Image>().color =
            new Color(NeonBlue.r, NeonBlue.g, NeonBlue.b, 0.04f);
        ScrollRect sr = scrollGO.AddComponent<ScrollRect>();
        sr.horizontal = false;
        sr.vertical   = true;
        sr.scrollSensitivity = 20f;

        // Viewport — use RectMask2D instead of Mask+Image.
        // Mask requires an Image; if that Image is clear Unity still clips
        // against the wrong rect. RectMask2D clips purely by RectTransform
        // with no Image dependency, so it works correctly with clear backgrounds.
        GameObject viewport = new GameObject("Viewport");
        viewport.transform.SetParent(scrollGO.transform, false);
        RectTransform vpRT = viewport.AddComponent<RectTransform>();
        vpRT.anchorMin = Vector2.zero; vpRT.anchorMax = Vector2.one;
        vpRT.offsetMin = vpRT.offsetMax = Vector2.zero;
        viewport.AddComponent<RectMask2D>();
        sr.viewport = vpRT;

        // Content — this grows to fit all cards
        GameObject content = new GameObject("InventoryGrid");
        content.transform.SetParent(viewport.transform, false);
        RectTransform contentRT = content.AddComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0, 1);
        contentRT.anchorMax = new Vector2(1, 1);
        contentRT.pivot     = new Vector2(0.5f, 1f);
        contentRT.sizeDelta = new Vector2(0, 0);
        contentRT.anchoredPosition = Vector2.zero;
        sr.content = contentRT;

        // GridLayoutGroup on the content
        GridLayoutGroup glg = content.AddComponent<GridLayoutGroup>();
        glg.cellSize       = new Vector2(200, 130);
        glg.spacing        = new Vector2(12, 12);
        glg.padding        = new RectOffset(12, 12, 12, 12);
        glg.childAlignment = TextAnchor.UpperLeft;
        glg.constraint     = GridLayoutGroup.Constraint.FixedColumnCount;
        glg.constraintCount = 3;

        // ContentSizeFitter makes the content grow as cards are added
        ContentSizeFitter csf = content.AddComponent<ContentSizeFitter>();
        csf.verticalFit   = ContentSizeFitter.FitMode.PreferredSize;
        csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        AddSpace(panel, 14);

        // ── Divider ───────────────────────────────────────────────────────
        MakeDivider(panel, "PotionDivider");
        AddSpace(panel, 10);

        // ── Potions section ───────────────────────────────────────────────
        MakeTMP(panel, "PotionSectionLabel", "POTIONS", 11,
            new Color(NeonBlue.r, NeonBlue.g, NeonBlue.b, 0.45f),
            new Vector2(0, 16), spacing: 5f,
            align: TextAlignmentOptions.Center);
        AddSpace(panel, 8);

        // Potion row — compact, fixed height
        GameObject potionRow = new GameObject("PotionRow");
        potionRow.transform.SetParent(panel.transform, false);
        potionRow.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 52);
        LayoutElement prle = potionRow.AddComponent<LayoutElement>();
        prle.preferredHeight = 52; prle.flexibleWidth = 1;
        potionRow.AddComponent<Image>().color =
            new Color(NeonBlue.r, NeonBlue.g, NeonBlue.b, 0.04f);

        HorizontalLayoutGroup phlg = potionRow.AddComponent<HorizontalLayoutGroup>();
        phlg.padding = new RectOffset(16, 16, 0, 0);
        phlg.spacing = 10;
        phlg.childAlignment        = TextAnchor.MiddleLeft;
        phlg.childForceExpandHeight = true;
        phlg.childForceExpandWidth  = false;
        phlg.childControlHeight = false;

        // Potion flask icon — small circle coloured amber (no sprite needed)
        GameObject potionIcon = new GameObject("PotionIcon");
        potionIcon.transform.SetParent(potionRow.transform, false);
        potionIcon.AddComponent<RectTransform>().sizeDelta = new Vector2(28, 28);
        LayoutElement pile = potionIcon.AddComponent<LayoutElement>();
        pile.preferredWidth = 28; pile.preferredHeight = 28; pile.flexibleWidth = 0;
        Image piImg = potionIcon.AddComponent<Image>();
        piImg.color = NeonAmber;
        // Circle shape via code — same as PausePanelStyler pattern
        // (InventoryPanelStyler.Awake will apply the circle sprite at runtime)

        // "x N" count
        GameObject potionCountGO = new GameObject("PotionCountText");
        potionCountGO.transform.SetParent(potionRow.transform, false);
        LayoutElement pcle = potionCountGO.AddComponent<LayoutElement>();
        pcle.flexibleWidth = 1;
        TextMeshProUGUI pct = potionCountGO.AddComponent<TextMeshProUGUI>();
        pct.text      = "x 0";
        pct.fontSize  = 26;
        pct.fontStyle = FontStyles.Bold;
        pct.color     = NeonAmber;
        pct.alignment = TextAlignmentOptions.MidlineLeft;
        pct.enableAutoSizing = false;

        // USE button — slim, right-aligned
        GameObject useBtn = new GameObject("UsePotionButton");
        useBtn.transform.SetParent(potionRow.transform, false);
        useBtn.AddComponent<RectTransform>().sizeDelta = new Vector2(120, 38);
        LayoutElement uble = useBtn.AddComponent<LayoutElement>();
        uble.preferredWidth = 120; uble.preferredHeight = 38; uble.flexibleWidth = 0;
        useBtn.AddComponent<Image>().color = Cream;
        useBtn.AddComponent<Button>();
        GameObject useLbl = new GameObject("Label");
        useLbl.transform.SetParent(useBtn.transform, false);
        RectTransform ulRT = useLbl.AddComponent<RectTransform>();
        ulRT.anchorMin = Vector2.zero; ulRT.anchorMax = Vector2.one;
        ulRT.sizeDelta = Vector2.zero;
        TextMeshProUGUI useTMP = useLbl.AddComponent<TextMeshProUGUI>();
        useTMP.text      = "USE";
        useTMP.fontSize  = 16;
        useTMP.fontStyle = FontStyles.Bold;
        useTMP.color     = TextDark;
        useTMP.alignment = TextAlignmentOptions.Center;
        useTMP.characterSpacing = 3f;
        useTMP.enableAutoSizing = false;

        AddSpace(panel, 10);

        // ── Footer ────────────────────────────────────────────────────────
        MakeDivider(panel, "FooterDivider");
        AddSpace(panel, 8);

        GameObject footerRow = new GameObject("FooterRow");
        footerRow.transform.SetParent(panel.transform, false);
        footerRow.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 22);
        LayoutElement frle = footerRow.AddComponent<LayoutElement>();
        frle.preferredHeight = 22; frle.flexibleWidth = 1;
        HorizontalLayoutGroup fhlg = footerRow.AddComponent<HorizontalLayoutGroup>();
        fhlg.childAlignment        = TextAnchor.MiddleLeft;
        fhlg.childForceExpandWidth  = false;
        fhlg.childForceExpandHeight = true;

        GameObject countGO = new GameObject("InventoryCountText");
        countGO.transform.SetParent(footerRow.transform, false);
        LayoutElement ctle = countGO.AddComponent<LayoutElement>();
        ctle.flexibleWidth = 1;
        TextMeshProUGUI countTMP = countGO.AddComponent<TextMeshProUGUI>();
        countTMP.text      = "HINTS COLLECTED: 0";
        countTMP.fontSize  = 12;
        countTMP.color     = new Color(NeonBlue.r, NeonBlue.g, NeonBlue.b, 0.5f);
        countTMP.alignment = TextAlignmentOptions.MidlineLeft;
        countTMP.characterSpacing  = 2f;
        countTMP.enableAutoSizing  = false;

        GameObject tipGO = new GameObject("TipText");
        tipGO.transform.SetParent(footerRow.transform, false);
        LayoutElement tple = tipGO.AddComponent<LayoutElement>();
        tple.preferredWidth = 160; tple.flexibleWidth = 0;
        TextMeshProUGUI tipTMP = tipGO.AddComponent<TextMeshProUGUI>();
        tipTMP.text      = "[ TAB ] TOGGLE";
        tipTMP.fontSize  = 11;
        tipTMP.color     = new Color(NeonBlue.r, NeonBlue.g, NeonBlue.b, 0.25f);
        tipTMP.alignment = TextAlignmentOptions.MidlineRight;
        tipTMP.characterSpacing = 2f;
        tipTMP.enableAutoSizing = false;

        // ── EventSystem ───────────────────────────────────────────────────
        if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        canvasGO.SetActive(false);

        EditorUtility.DisplayDialog("Done",
            "InventoryCanvas created.\n\n" +
            "Assign in InventoryUI Inspector:\n" +
            "  inventoryPanel       -> InventoryCanvas\n" +
            "  inventoryGrid        -> InventoryGrid  (inside HintsScrollView/Viewport)\n" +
            "  inventoryItem        -> InventoryItemCard prefab\n" +
            "  inventoryCountText   -> InventoryCountText\n" +
            "  inventoryCloseButton -> InventoryCloseButton\n" +
            "  potionCountText      -> PotionCountText\n" +
            "  usePotionButton      -> UsePotionButton", "OK");
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    static void MakeTMP(GameObject parent, string name, string text,
        float size, Color color, Vector2 sizeDelta,
        float spacing = 0f, bool bold = false,
        TextAlignmentOptions align = TextAlignmentOptions.Center)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        go.AddComponent<RectTransform>().sizeDelta = sizeDelta;
        LayoutElement le = go.AddComponent<LayoutElement>();
        le.preferredHeight = sizeDelta.y; le.flexibleWidth = 1;
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = text; tmp.fontSize = size; tmp.color = color;
        tmp.alignment = align;
        tmp.characterSpacing = spacing;
        tmp.enableAutoSizing = false;
        if (bold) tmp.fontStyle = FontStyles.Bold;
    }

    static void MakeDivider(GameObject parent, string name)
    {
        GameObject go = MakeImage(parent, name,
            new Color(0.10f, 0.89f, 1.00f, 0.2f));
        go.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 1);
        LayoutElement le = go.AddComponent<LayoutElement>();
        le.preferredHeight = 1; le.flexibleWidth = 1;
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