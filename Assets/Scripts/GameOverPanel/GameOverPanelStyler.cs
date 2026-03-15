using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Attach to the GameOverCanvas GameObject.
/// Renders neon Game Over (red) and Victory (green) states.
/// Called by UIManager.ShowGameOver(bool win).
/// No external sprites needed.
/// </summary>
[RequireComponent(typeof(Canvas))]
public class GameOverPanelStyler : MonoBehaviour
{
    // ── Palette ───────────────────────────────────────────────────────────
    static readonly Color NeonBlue   = new Color(0.10f, 0.89f, 1.00f, 1f);
    static readonly Color NeonGreen  = new Color(0.22f, 1.00f, 0.43f, 1f);
    static readonly Color NeonRed    = new Color(1.00f, 0.24f, 0.24f, 1f);
    static readonly Color PanelBg    = new Color(0.04f, 0.08f, 0.16f, 0.97f);
    static readonly Color OverlayBg  = new Color(0.02f, 0.04f, 0.10f, 0.85f);
    static readonly Color ButtonCream= new Color(0.96f, 0.93f, 0.85f, 1f);
    static readonly Color ButtonRed  = new Color(0.88f, 0.76f, 0.75f, 1f);
    static readonly Color TextDark   = new Color(0.07f, 0.12f, 0.22f, 1f);
    static readonly Color TextDarkRed= new Color(0.35f, 0.07f, 0.07f, 1f);

    void Awake() => ApplyBaseStyle();

    // ── Called once at Awake to wire up everything ────────────────────────
    void ApplyBaseStyle()
    {
        Image overlay = FindImage("GameOverOverlay");
        if (overlay) overlay.color = OverlayBg;

        GameObject panel = GameObject.Find("GameOverPanel");
        if (panel == null) return;

        Image bg = panel.GetComponent<Image>();
        if (bg) { bg.color = PanelBg; bg.sprite = RoundedSprite(); bg.type = Image.Type.Sliced; }

        // Buttons wired here — colours updated in ApplyResultStyle
        StyleButton("RestartButton",  ButtonCream, TextDark,    false);
        StyleButton("QuitMenuButton", ButtonRed,   TextDarkRed, true);
    }

    /// <summary>
    /// Call this from UIManager.ShowGameOver(win) to apply
    /// the correct win/lose visual state.
    /// </summary>
    public void ApplyResultStyle(bool win, int currentHP, int maxHP,
        int defusedBombs, int goal, int hintsCollected)
    {
        Color accent = win ? NeonGreen : NeonRed;

        // ── Panel border ──────────────────────────────────────────────────
        GameObject canvasGO = gameObject;
        GameObject panel    = GameObject.Find("GameOverPanel");
        if (panel != null)
        {
            // Remove old border
            Transform oldBorder = canvasGO.transform.Find("GONeonBorder");
            if (oldBorder) Destroy(oldBorder.gameObject);

            GameObject border  = new GameObject("GONeonBorder");
            border.transform.SetParent(canvasGO.transform, false);
            RectTransform panelRT  = panel.GetComponent<RectTransform>();
            RectTransform borderRT = border.AddComponent<RectTransform>();
            borderRT.anchorMin        = panelRT.anchorMin;
            borderRT.anchorMax        = panelRT.anchorMax;
            borderRT.pivot            = panelRT.pivot;
            borderRT.anchoredPosition = panelRT.anchoredPosition;
            borderRT.sizeDelta        = panelRT.sizeDelta + new Vector2(8, 8);
            Image borderImg  = border.AddComponent<Image>();
            borderImg.color  = accent;
            borderImg.sprite = RoundedSprite();
            borderImg.type   = Image.Type.Sliced;
            border.transform.SetSiblingIndex(panel.transform.GetSiblingIndex());

            AddCornerDots(canvasGO, panelRT, accent);
        }

        // ── Icon (drawn by GameOverIconRenderer) ──────────────────────────
        GameOverIconRenderer iconRenderer = FindObjectOfType<GameOverIconRenderer>();
        if (iconRenderer != null)
            iconRenderer.ShowResult(win);

        // ── Tag line: "// MISSION FAILED //" or "// MISSION ACCOMPLISHED //" ──
        SetTMP("ResultTagText",
            win ? "// MISSION ACCOMPLISHED //" : "// MISSION FAILED //",
            accent, 13, FontStyles.Normal, spacing: 3f);

        // ── Heading: "GAME OVER" or "VICTORY" ────────────────────────────
        SetTMP("ResultHeadingText",
            win ? "VICTORY" : "GAME OVER",
            accent, 42, FontStyles.Bold, spacing: 2f,
            outline: true, outlineColor: new Color(accent.r, accent.g, accent.b, 0.3f));

        // ── Sub text ──────────────────────────────────────────────────────
        SetTMP("ResultSubText",
            win ? "All bombs defused. The city has been saved."
                : "All operatives lost. The bomb was not defused in time.",
            new Color(1,1,1,0.45f), 14, FontStyles.Normal);

        // ── Stats ─────────────────────────────────────────────────────────
        // HP stat
        SetTMP("StatHP",    currentHP.ToString(),
            win ? NeonGreen : NeonRed, 28, FontStyles.Bold);
        SetTMP("StatHPLabel", "HP REMAINING",
            new Color(NeonBlue.r, NeonBlue.g, NeonBlue.b, 0.4f), 11, FontStyles.Normal, spacing:2f);

        // Bombs stat
        SetTMP("StatBombs", $"{defusedBombs} / {goal}",
            NeonBlue, 28, FontStyles.Bold);
        SetTMP("StatBombsLabel", "BOMBS DEFUSED",
            new Color(NeonBlue.r, NeonBlue.g, NeonBlue.b, 0.4f), 11, FontStyles.Normal, spacing:2f);

        // Hints stat
        SetTMP("StatHints", hintsCollected.ToString(),
            NeonBlue, 28, FontStyles.Bold);
        SetTMP("StatHintsLabel", "HINTS FOUND",
            new Color(NeonBlue.r, NeonBlue.g, NeonBlue.b, 0.4f), 11, FontStyles.Normal, spacing:2f);

        // Stats container border colour
        Image statsBg = FindImage("StatsContainer");
        if (statsBg) statsBg.color = new Color(accent.r, accent.g, accent.b, 0.08f);

        // ── Restart button: green on win, cream on loss ───────────────────
        if (win)
        {
            StyleButton("RestartButton", NeonGreen,
                new Color(0.04f, 0.25f, 0.10f), false, isWinRestart: true);
        }
        else
        {
            StyleButton("RestartButton", ButtonCream, TextDark, false);
        }
    }

    // ── Button styling ────────────────────────────────────────────────────
    void StyleButton(string goName, Color bg, Color textCol, bool isQuit,
        bool isWinRestart = false)
    {
        GameObject go = GameObject.Find(goName);
        if (go == null) return;

        Image img = go.GetComponent<Image>();
        if (img) { img.color = bg; img.sprite = RoundedSprite(); img.type = Image.Type.Sliced; }

        Button btn = go.GetComponent<Button>();
        if (btn)
        {
            ColorBlock cb      = btn.colors;
            cb.normalColor     = bg;
            cb.highlightedColor= isQuit         ? new Color(1f,0.55f,0.55f)
                               : isWinRestart   ? new Color(0.55f,1f,0.70f)
                               :                  new Color(0.80f,0.97f,1f);
            cb.pressedColor    = new Color(0.68f,0.68f,0.68f);
            cb.colorMultiplier = 1f;
            btn.colors = cb;
        }

        foreach (var old in go.GetComponents<Outline>()) Destroy(old);
        foreach (var old in go.GetComponents<Shadow>())  Destroy(old);

        TMP_Text label = go.GetComponentInChildren<TMP_Text>();
        if (label)
        {
            label.color     = textCol;
            label.fontStyle = FontStyles.Bold;
            label.fontSize  = 20;
            string dot = isQuit         ? "<color=#FF6666>•</color>"
                       : isWinRestart   ? "<color=#05190A>•</color>"
                       :                  "<color=#1AE4FF>•</color>";
            string raw = label.text.Replace("•","").Trim();
            if (!raw.Contains("//"))
                label.text = $"{dot}  {raw}  {dot}";
        }
    }

    // ── Corner dots ───────────────────────────────────────────────────────
    void AddCornerDots(GameObject canvasGO, RectTransform panelRT, Color accent)
    {
        for (int i = canvasGO.transform.childCount - 1; i >= 0; i--)
        {
            Transform c = canvasGO.transform.GetChild(i);
            if (c.name.StartsWith("GOCornerDot")) Destroy(c.gameObject);
        }

        Vector2 centre = panelRT.anchoredPosition;
        Vector2 half   = panelRT.sizeDelta * 0.5f;
        Vector2[] positions = {
            centre + new Vector2(-half.x, +half.y),
            centre + new Vector2(+half.x, +half.y),
            centre + new Vector2(-half.x, -half.y),
            centre + new Vector2(+half.x, -half.y),
        };
        string[] names = { "GOCornerDotTL","GOCornerDotTR","GOCornerDotBL","GOCornerDotBR" };

        for (int i = 0; i < 4; i++)
        {
            GameObject dot = new GameObject(names[i]);
            dot.transform.SetParent(canvasGO.transform, false);
            RectTransform rt   = dot.AddComponent<RectTransform>();
            rt.anchorMin       = new Vector2(0.5f, 0.5f);
            rt.anchorMax       = new Vector2(0.5f, 0.5f);
            rt.pivot           = new Vector2(0.5f, 0.5f);
            rt.sizeDelta       = new Vector2(14, 14);
            rt.anchoredPosition= positions[i];
            Image img  = dot.AddComponent<Image>();
            img.color  = accent;
            img.sprite = CircleSprite();
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────
    void SetTMP(string goName, string text, Color color, float size,
        FontStyles style, float spacing = 0f,
        bool outline = false, Color outlineColor = default)
    {
        GameObject go = GameObject.Find(goName);
        if (go == null) return;
        TMP_Text t = go.GetComponent<TMP_Text>();
        if (t == null) return;
        t.text      = text;
        t.color     = color;
        t.fontSize  = size;
        t.fontStyle = style;
        t.characterSpacing = spacing;
        if (outline) { t.outlineWidth = 0.14f; t.outlineColor = outlineColor; }
    }

    Image FindImage(string name)
    {
        GameObject go = GameObject.Find(name);
        return go ? go.GetComponent<Image>() : null;
    }

    // ── Sprite helpers ────────────────────────────────────────────────────
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
            float alpha = Mathf.Clamp01(radius - Mathf.Sqrt(dx*dx+dy*dy) + 0.5f);
            px[y * w + x] = new Color(1, 1, 1, alpha);
        }
        tex.SetPixels(px); tex.Apply();
        float b = radius;
        return Sprite.Create(tex, new Rect(0,0,w,h), new Vector2(0.5f,0.5f),
            100f, 0, SpriteMeshType.FullRect, new Vector4(b,b,b,b));
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
            float alpha = Mathf.Clamp01(half - Mathf.Sqrt(dx*dx+dy*dy) + 0.5f);
            px[y * size + x] = new Color(1, 1, 1, alpha);
        }
        tex.SetPixels(px); tex.Apply();
        return Sprite.Create(tex, new Rect(0,0,size,size), new Vector2(0.5f,0.5f), 100f);
    }
}
