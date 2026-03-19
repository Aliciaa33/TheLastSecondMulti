using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Attach to the PauseCanvas GameObject.
/// Applies neon styling at runtime. No external sprites needed.
/// </summary>
[RequireComponent(typeof(Canvas))]
public class PausePanelStyler : MonoBehaviour
{
    static readonly Color NeonBlue = new Color(0.10f, 0.89f, 1.00f, 1f);
    static readonly Color NeonBlueDim = new Color(0.10f, 0.89f, 1.00f, 0.18f);
    static readonly Color NeonRed = new Color(1.00f, 0.26f, 0.26f, 1f);
    static readonly Color PanelBg = new Color(0.04f, 0.08f, 0.16f, 0.97f);
    static readonly Color OverlayBg = new Color(0.02f, 0.04f, 0.10f, 0.76f);
    static readonly Color ButtonCream = new Color(0.96f, 0.93f, 0.85f, 1f);
    static readonly Color ButtonRed = new Color(0.88f, 0.76f, 0.75f, 1f);
    static readonly Color TextDark = new Color(0.07f, 0.12f, 0.22f, 1f);
    static readonly Color TextDarkRed = new Color(0.35f, 0.07f, 0.07f, 1f);

    void Awake() => ApplyStyle();

    public void ApplyStyle()
    {
        StyleOverlay();
        StylePanel();
        StyleTitle();
        StyleButtons();
        StyleSettingsSection();
        StyleHint();
    }

    // ── Overlay ────────────────────────────────────────────────────────────
    void StyleOverlay()
    {
        var img = FindImage("Overlay");
        if (img) img.color = OverlayBg;
    }

    // ── Panel ──────────────────────────────────────────────────────────────
    void StylePanel()
    {
        GameObject panel = GameObject.Find("PausePanel");
        if (panel == null) return;

        // Panel background
        Image bg = panel.GetComponent<Image>();
        if (bg) { bg.color = PanelBg; bg.sprite = RoundedSprite(); bg.type = Image.Type.Sliced; }

        // ── Neon border: a separate child Image that sits BEHIND content ──
        // We attach it to PauseCanvas (parent of PausePanel), not to PausePanel
        // itself, so the VerticalLayoutGroup never touches it.
        GameObject canvasGO = GameObject.Find("PauseCanvas");
        if (canvasGO == null) canvasGO = gameObject;

        // Remove stale border from previous runs
        Transform oldBorder = canvasGO.transform.Find("PanelNeonBorder");
        if (oldBorder) Destroy(oldBorder.gameObject);

        GameObject border = new GameObject("PanelNeonBorder");
        border.transform.SetParent(canvasGO.transform, false);

        // Copy panel's RectTransform exactly, then make it slightly larger
        RectTransform panelRT = panel.GetComponent<RectTransform>();
        RectTransform borderRT = border.AddComponent<RectTransform>();
        borderRT.anchorMin = panelRT.anchorMin;
        borderRT.anchorMax = panelRT.anchorMax;
        borderRT.pivot = panelRT.pivot;
        borderRT.anchoredPosition = panelRT.anchoredPosition;
        borderRT.sizeDelta = panelRT.sizeDelta + new Vector2(8, 8); // 4px each side

        Image borderImg = border.AddComponent<Image>();
        borderImg.color = NeonBlue;
        borderImg.sprite = RoundedSprite();
        borderImg.type = Image.Type.Sliced;

        // Push the border behind the panel
        border.transform.SetSiblingIndex(
            panel.transform.GetSiblingIndex()); // same level, placed first = behind

        // ── Corner dots: children of PauseCanvas, NOT PausePanel ──────────
        AddCornerDots(canvasGO, panelRT);
    }

    // ── Title ──────────────────────────────────────────────────────────────
    void StyleTitle()
    {
        GameObject go = GameObject.Find("TitleText");
        if (go == null) return;
        TMP_Text t = go.GetComponent<TMP_Text>();
        if (t == null) return;
        t.fontSize = 38;
        t.fontStyle = FontStyles.Bold;
        t.color = Color.white;
        t.text = "// <color=#FF4444>PAUSED</color> //";
        t.outlineWidth = 0.12f;
        t.outlineColor = NeonBlue;
    }

    // ── Buttons ────────────────────────────────────────────────────────────
    void StyleButtons()
    {
        StyleOneButton("ResumeButton", ButtonCream, TextDark, false);
        StyleOneButton("RestartButton", ButtonCream, TextDark, false);
        StyleOneButton("SettingsButton", ButtonCream, TextDark, false);
        StyleOneButton("QuitButton", ButtonRed, TextDarkRed, true);
    }

    void StyleOneButton(string name, Color bg, Color textCol, bool isQuit)
    {
        GameObject go = GameObject.Find(name);
        if (go == null) return;

        Image img = go.GetComponent<Image>();
        if (img) { img.color = bg; img.sprite = RoundedSprite(); img.type = Image.Type.Sliced; }

        Button btn = go.GetComponent<Button>();
        if (btn)
        {
            ColorBlock cb = btn.colors;
            cb.normalColor = bg;
            cb.highlightedColor = isQuit ? new Color(1f, 0.55f, 0.55f) : new Color(0.80f, 0.97f, 1f);
            cb.pressedColor = new Color(0.68f, 0.68f, 0.68f);
            cb.colorMultiplier = 1f;
            btn.colors = cb;
        }

        // Remove any leftover Outline/Shadow that were causing the stretched bars
        foreach (var old in go.GetComponents<Outline>()) Destroy(old);
        foreach (var old in go.GetComponents<Shadow>()) Destroy(old);

        TMP_Text label = go.GetComponentInChildren<TMP_Text>();
        if (label)
        {
            label.color = textCol;
            label.fontStyle = FontStyles.Bold;
            label.fontSize = 20;
            string dot = isQuit ? "<color=#FF6666>•</color>" : "<color=#1AE4FF>•</color>";
            string raw = label.text.Replace("•", "").Trim();
            // Guard against double-adding dots on re-runs
            if (!raw.StartsWith("//"))
                label.text = $"{dot}  {raw}  {dot}";
        }
    }

    // ── Settings section ───────────────────────────────────────────────────
    void StyleSettingsSection()
    {
        GameObject go = GameObject.Find("SettingsSection");
        if (go == null) return;

        Image img = go.GetComponent<Image>() ?? go.AddComponent<Image>();
        img.color = NeonBlueDim; img.sprite = RoundedSprite(); img.type = Image.Type.Sliced;

        foreach (var slider in go.GetComponentsInChildren<Slider>())
        {
            if (slider.fillRect?.GetComponent<Image>() is Image fill)
                fill.color = NeonBlue;
            if (slider.handleRect?.GetComponent<Image>() is Image handle)
            { handle.color = NeonBlue; handle.sprite = CircleSprite(); }
            Transform bgT = slider.transform.Find("Background");
            if (bgT?.GetComponent<Image>() is Image track)
                track.color = new Color(0.1f, 0.9f, 1f, 0.15f);
        }

        foreach (var t in go.GetComponentsInChildren<TMP_Text>())
            if (t.gameObject.name == "Label")
            { t.color = NeonBlue; t.fontStyle = FontStyles.Bold; t.fontSize = 15; }
    }

    // ── Hint text ──────────────────────────────────────────────────────────
    void StyleHint()
    {
        GameObject go = GameObject.Find("HintText");
        if (go == null) return;
        TMP_Text t = go.GetComponent<TMP_Text>();
        if (t) t.color = new Color(NeonBlue.r, NeonBlue.g, NeonBlue.b, 0.45f);
    }

    // ── Corner dots ────────────────────────────────────────────────────────
    // Parented to the Canvas, positioned to match panel corners.
    // This keeps them OUTSIDE the VerticalLayoutGroup so they can't be stretched.
    void AddCornerDots(GameObject canvasGO, RectTransform panelRT)
    {
        // Remove stale dots
        for (int i = canvasGO.transform.childCount - 1; i >= 0; i--)
        {
            Transform c = canvasGO.transform.GetChild(i);
            if (c.name.StartsWith("CornerDot")) Destroy(c.gameObject);
        }

        // Panel corners in canvas-local space
        // anchoredPosition is panel centre; sizeDelta is panel size
        Vector2 centre = panelRT.anchoredPosition;
        Vector2 half = panelRT.sizeDelta * 0.5f;

        Vector2[] positions = {
            centre + new Vector2(-half.x, +half.y), // TL
            centre + new Vector2(+half.x, +half.y), // TR
            centre + new Vector2(-half.x, -half.y), // BL
            centre + new Vector2(+half.x, -half.y), // BR
        };
        string[] dotNames = { "CornerDotTL", "CornerDotTR", "CornerDotBL", "CornerDotBR" };

        for (int i = 0; i < 4; i++)
        {
            GameObject dot = new GameObject(dotNames[i]);
            dot.transform.SetParent(canvasGO.transform, false);

            RectTransform rt = dot.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(16, 16);
            rt.anchoredPosition = positions[i];

            Image img = dot.AddComponent<Image>();
            img.color = NeonBlue;
            img.sprite = CircleSprite();
        }
    }

    // ── Helpers ────────────────────────────────────────────────────────────
    Image FindImage(string goName)
    {
        GameObject go = GameObject.Find(goName);
        return go ? go.GetComponent<Image>() : null;
    }

    // Generates sprites entirely in code — works on all Unity versions,
    // no built-in resource paths needed.
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

    // Draws a rounded rectangle and returns it as a 9-sliced Sprite.
    static Sprite MakeRoundedRectSprite(int w, int h, int radius)
    {
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        Color[] pixels = new Color[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float cx = Mathf.Clamp(x, radius, w - radius - 1);
                float cy = Mathf.Clamp(y, radius, h - radius - 1);
                float dx = x - cx, dy = y - cy;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                float alpha = Mathf.Clamp01(radius - dist + 0.5f);
                pixels[y * w + x] = new Color(1, 1, 1, alpha);
            }
        tex.SetPixels(pixels);
        tex.Apply();
        float b = radius;
        return Sprite.Create(tex,
            new Rect(0, 0, w, h),
            new Vector2(0.5f, 0.5f),
            100f, 0,
            SpriteMeshType.FullRect,
            new Vector4(b, b, b, b));
    }

    // Draws a filled circle and returns it as a Sprite.
    static Sprite MakeCircleSprite(int size)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        Color[] pixels = new Color[size * size];
        float half = size * 0.5f;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = x - half + 0.5f, dy = y - half + 0.5f;
                float alpha = Mathf.Clamp01(half - Mathf.Sqrt(dx * dx + dy * dy) + 0.5f);
                pixels[y * size + x] = new Color(1, 1, 1, alpha);
            }
        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex,
            new Rect(0, 0, size, size),
            new Vector2(0.5f, 0.5f), 100f);
    }
}