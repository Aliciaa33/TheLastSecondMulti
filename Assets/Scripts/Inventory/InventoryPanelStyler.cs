using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Attach to the InventoryCanvas GameObject.
/// Applies the same neon circuit aesthetic as PausePanelStyler at runtime.
/// No external sprites needed — all generated in code.
/// </summary>
[RequireComponent(typeof(Canvas))]
public class InventoryPanelStyler : MonoBehaviour
{
    // ── Palette (matches PausePanelStyler exactly) ────────────────────────
    static readonly Color NeonBlue = new Color(0.10f, 0.89f, 1.00f, 1f);
    static readonly Color NeonBlueDim = new Color(0.10f, 0.89f, 1.00f, 0.08f);
    static readonly Color PanelBg = new Color(0.04f, 0.08f, 0.16f, 0.97f);
    static readonly Color OverlayBg = new Color(0.02f, 0.04f, 0.10f, 0.76f);
    static readonly Color ButtonCream = new Color(0.96f, 0.93f, 0.85f, 1f);
    static readonly Color TextDark = new Color(0.07f, 0.12f, 0.22f, 1f);
    static readonly Color ItemBg = new Color(0.10f, 0.89f, 1.00f, 0.05f);
    static readonly Color ItemBorder = new Color(0.10f, 0.89f, 1.00f, 0.22f);

    void Awake() => ApplyStyle();

    public void ApplyStyle()
    {
        StyleOverlay();
        StylePanel();
        StyleHeader();
        StyleCloseButton();
        StyleGrid();
        StyleFooter();
    }

    void StyleOverlay()
    {
        Image img = FindImage("InventoryOverlay");
        if (img) img.color = OverlayBg;
    }

    void StylePanel()
    {
        GameObject panel = GameObject.Find("InventoryPanel");
        if (panel == null) return;

        Image bg = panel.GetComponent<Image>();
        if (bg) { bg.color = PanelBg; bg.sprite = RoundedSprite(); bg.type = Image.Type.Sliced; }

        // Neon border — sibling on Canvas so VerticalLayoutGroup can't touch it
        GameObject canvasGO = gameObject;
        Transform oldBorder = canvasGO.transform.Find("InvNeonBorder");
        if (oldBorder) Destroy(oldBorder.gameObject);

        GameObject border = new GameObject("InvNeonBorder");
        border.transform.SetParent(canvasGO.transform, false);
        RectTransform panelRT = panel.GetComponent<RectTransform>();
        RectTransform borderRT = border.AddComponent<RectTransform>();
        borderRT.anchorMin = panelRT.anchorMin;
        borderRT.anchorMax = panelRT.anchorMax;
        borderRT.pivot = panelRT.pivot;
        borderRT.anchoredPosition = panelRT.anchoredPosition;
        borderRT.sizeDelta = panelRT.sizeDelta + new Vector2(8, 8);
        Image borderImg = border.AddComponent<Image>();
        borderImg.color = NeonBlue;
        borderImg.sprite = RoundedSprite();
        borderImg.type = Image.Type.Sliced;
        border.transform.SetSiblingIndex(panel.transform.GetSiblingIndex());

        AddCornerDots(canvasGO, panelRT);
    }

    void StyleHeader()
    {
        // Title: "// INVENTORY //"
        GameObject titleGO = GameObject.Find("InventoryTitleText");
        if (titleGO != null)
        {
            TMP_Text t = titleGO.GetComponent<TMP_Text>();
            if (t)
            {
                t.text = "// INVENTORY //";
                t.color = Color.white;
                t.fontStyle = FontStyles.Bold;
                t.fontSize = 22;
                t.characterSpacing = 3f;
                t.outlineWidth = 0.1f;
                t.outlineColor = NeonBlue;
            }
        }

        // Divider line
        Image divider = FindImage("InventoryDivider");
        if (divider) divider.color = new Color(NeonBlue.r, NeonBlue.g, NeonBlue.b, 0.25f);
    }

    void StyleCloseButton()
    {
        GameObject go = GameObject.Find("InventoryCloseButton");
        if (go == null) return;

        Image img = go.GetComponent<Image>();
        if (img) { img.color = new Color(NeonBlue.r, NeonBlue.g, NeonBlue.b, 0f); }

        Button btn = go.GetComponent<Button>();
        if (btn)
        {
            ColorBlock cb = btn.colors;
            cb.normalColor = new Color(NeonBlue.r, NeonBlue.g, NeonBlue.b, 0f);
            cb.highlightedColor = new Color(NeonBlue.r, NeonBlue.g, NeonBlue.b, 0.18f);
            cb.pressedColor = new Color(NeonBlue.r, NeonBlue.g, NeonBlue.b, 0.35f);
            cb.colorMultiplier = 1f;
            btn.colors = cb;
        }

        // Add a neon border to the close button
        foreach (var old in go.GetComponents<Outline>()) Destroy(old);
        Outline outline = go.AddComponent<Outline>();
        outline.effectColor = new Color(NeonBlue.r, NeonBlue.g, NeonBlue.b, 0.5f);
        outline.effectDistance = new Vector2(1, -1);

        TMP_Text label = go.GetComponentInChildren<TMP_Text>();
        if (label) { label.color = NeonBlue; label.fontStyle = FontStyles.Bold; label.fontSize = 16; }
    }

    void StyleGrid()
    {
        // Style the grid background
        Image gridBg = FindImage("InventoryGrid");
        if (gridBg)
        {
            gridBg.color = NeonBlueDim;
            gridBg.sprite = RoundedSprite();
            gridBg.type = Image.Type.Sliced;
        }

        // Style any existing item cards
        RefreshItemCards();
    }

    /// Call this every time inventory contents change (from InventoryUI.ShowInventory)
    public void RefreshItemCards()
    {
        GameObject gridGO = GameObject.Find("InventoryGrid");
        if (gridGO == null) return;

        foreach (Transform child in gridGO.transform)
        {
            StyleItemCard(child.gameObject);
        }
    }

    void StyleItemCard(GameObject card)
    {
        Image bg = card.GetComponent<Image>();
        if (bg) { bg.color = ItemBg; bg.sprite = RoundedSprite(); bg.type = Image.Type.Sliced; }

        // Neon border on card via Outline (small cards — Outline is fine here)
        foreach (var old in card.GetComponents<Outline>()) Destroy(old);
        Outline outline = card.AddComponent<Outline>();
        outline.effectColor = ItemBorder;
        outline.effectDistance = new Vector2(1, -1);

        // Number text (large)
        TMP_Text[] texts = card.GetComponentsInChildren<TMP_Text>();
        foreach (TMP_Text t in texts)
        {
            if (t.gameObject.name == "NumberText" || t.gameObject.name == "ItemNumber")
            {
                t.color = Color.white;
                t.fontStyle = FontStyles.Bold;
                t.fontSize = 36;
                t.outlineWidth = 0.12f;
                t.outlineColor = NeonBlue;
                t.alignment = TextAlignmentOptions.Center;
            }
            else if (t.gameObject.name == "HintText" || t.gameObject.name == "ItemHint")
            {
                t.color = new Color(NeonBlue.r, NeonBlue.g, NeonBlue.b, 0.75f);
                t.fontStyle = FontStyles.Normal;
                t.fontSize = 18;
                t.alignment = TextAlignmentOptions.Center;
            }
        }
    }

    void StyleFooter()
    {
        // "Total: N" count text
        GameObject countGO = GameObject.Find("InventoryCountText");
        if (countGO != null)
        {
            TMP_Text t = countGO.GetComponent<TMP_Text>();
            if (t)
            {
                t.color = new Color(NeonBlue.r, NeonBlue.g, NeonBlue.b, 0.5f);
                t.fontSize = 13;
                t.characterSpacing = 2f;
            }
        }

        // Optional footer divider
        Image footerDiv = FindImage("FooterDivider");
        if (footerDiv) footerDiv.color = new Color(NeonBlue.r, NeonBlue.g, NeonBlue.b, 0.15f);
    }

    // ── Corner dots ───────────────────────────────────────────────────────
    void AddCornerDots(GameObject canvasGO, RectTransform panelRT)
    {
        for (int i = canvasGO.transform.childCount - 1; i >= 0; i--)
        {
            Transform c = canvasGO.transform.GetChild(i);
            if (c.name.StartsWith("InvCornerDot")) Destroy(c.gameObject);
        }

        Vector2 centre = panelRT.anchoredPosition;
        Vector2 half = panelRT.sizeDelta * 0.5f;

        Vector2[] positions = {
            centre + new Vector2(-half.x, +half.y),
            centre + new Vector2(+half.x, +half.y),
            centre + new Vector2(-half.x, -half.y),
            centre + new Vector2(+half.x, -half.y),
        };
        string[] names = { "InvCornerDotTL", "InvCornerDotTR", "InvCornerDotBL", "InvCornerDotBR" };

        for (int i = 0; i < 4; i++)
        {
            GameObject dot = new GameObject(names[i]);
            dot.transform.SetParent(canvasGO.transform, false);
            RectTransform rt = dot.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(14, 14);
            rt.anchoredPosition = positions[i];
            Image img = dot.AddComponent<Image>();
            img.color = NeonBlue;
            img.sprite = CircleSprite();
        }
    }

    // ── Sprite helpers (same as PausePanelStyler) ─────────────────────────
    Image FindImage(string name)
    {
        GameObject go = GameObject.Find(name);
        return go ? go.GetComponent<Image>() : null;
    }

    static Sprite _roundedSprite;
    static Sprite RoundedSprite()
    {
        if (_roundedSprite != null) return _roundedSprite;
        _roundedSprite = MakeRoundedRectSprite(128, 128, 16);
        return _roundedSprite;
    }

    static Sprite _circleSprite;
    static Sprite CircleSprite()
    {
        if (_circleSprite != null) return _circleSprite;
        _circleSprite = MakeCircleSprite(64);
        return _circleSprite;
    }

    static Sprite MakeRoundedRectSprite(int w, int h, int radius)
    {
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        Color[] px = new Color[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float cx = Mathf.Clamp(x, radius, w - radius - 1);
                float cy = Mathf.Clamp(y, radius, h - radius - 1);
                float dx = x - cx, dy = y - cy;
                float alpha = Mathf.Clamp01(radius - Mathf.Sqrt(dx * dx + dy * dy) + 0.5f);
                px[y * w + x] = new Color(1, 1, 1, alpha);
            }
        tex.SetPixels(px); tex.Apply();
        float b = radius;
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f),
            100f, 0, SpriteMeshType.FullRect, new Vector4(b, b, b, b));
    }

    static Sprite MakeCircleSprite(int size)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        Color[] px = new Color[size * size];
        float half = size * 0.5f;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = x - half + 0.5f, dy = y - half + 0.5f;
                float alpha = Mathf.Clamp01(half - Mathf.Sqrt(dx * dx + dy * dy) + 0.5f);
                px[y * size + x] = new Color(1, 1, 1, alpha);
            }
        tex.SetPixels(px); tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }
}
