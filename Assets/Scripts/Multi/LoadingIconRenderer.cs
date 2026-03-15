using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Attach to the IconContainer in the Loading scene.
/// Draws the spinner ring, success checkmark, and failed X entirely in code.
/// No external sprites or assets needed.
/// </summary>
public class LoadingIconRenderer : MonoBehaviour
{
    // All three icons are RawImage components so we can draw custom textures
    private RawImage spinnerImage;
    private RawImage successImage;
    private RawImage failedImage;

    private const int TexSize = 128;   // texture resolution
    private const int Thick = 10;    // ring stroke thickness
    private const float ArcDeg = 270f;  // how many degrees the spinner arc covers

    // Palette
    static readonly Color NeonBlue = new Color(0.10f, 0.89f, 1.00f, 1f);
    static readonly Color NeonGreen = new Color(0.22f, 1.00f, 0.43f, 1f);
    static readonly Color NeonRed = new Color(1.00f, 0.24f, 0.24f, 1f);

    private float spinAngle = 0f;
    private bool isSpinning = false;

    void Awake()
    {
        // Find or create the three RawImage GameObjects inside IconContainer
        spinnerImage = GetOrCreateRawImage("Spinner");
        successImage = GetOrCreateRawImage("SuccessIcon");
        failedImage = GetOrCreateRawImage("FailedIcon");

        // Draw static textures for success and failed
        successImage.texture = DrawCheckmark(TexSize, NeonGreen);
        failedImage.texture = DrawCross(TexSize, NeonRed);

        // Spinner starts with a blank; we redraw each frame while spinning
        spinnerImage.texture = DrawArc(TexSize, NeonBlue, 0f, ArcDeg);

        // Initial visibility — spinner shown, others hidden
        ShowIcon(LoadingIconState.Connecting);
    }

    void Update()
    {
        if (!isSpinning) return;
        spinAngle = (spinAngle + 220f * Time.deltaTime) % 360f;
        // Redraw spinner arc at new rotation angle
        if (spinnerImage != null)
            spinnerImage.texture = DrawArc(TexSize, NeonBlue, spinAngle, ArcDeg);
    }

    public void ShowIcon(LoadingIconState state)
    {
        isSpinning = (state == LoadingIconState.Connecting);
        if (spinnerImage != null) spinnerImage.gameObject.SetActive(state == LoadingIconState.Connecting);
        if (successImage != null) successImage.gameObject.SetActive(state == LoadingIconState.Success);
        if (failedImage != null) failedImage.gameObject.SetActive(state == LoadingIconState.Failed);
    }

    // ── Texture drawing ───────────────────────────────────────────────────

    /// Draws a partial ring arc (the spinning loader).
    static Texture2D DrawArc(int size, Color color, float startDeg, float arcDeg)
    {
        Texture2D tex = NewTex(size);
        Color[] px = new Color[size * size];
        float cx = size * 0.5f, cy = size * 0.5f;
        float outerR = size * 0.5f - 2f;
        float innerR = outerR - Thick;

        // Draw faint track ring first
        Color trackCol = new Color(color.r, color.g, color.b, 0.12f);
        DrawRingInto(px, size, cx, cy, outerR, innerR, 0f, 360f, trackCol);
        // Draw bright arc on top
        DrawRingInto(px, size, cx, cy, outerR, innerR, startDeg, arcDeg, color);

        tex.SetPixels(px);
        tex.Apply();
        return tex;
    }

    /// Draws a circle with a checkmark inside.
    /// Texture2D Y=0 is bottom, so Y coords are flipped vs screen space.
    static Texture2D DrawCheckmark(int size, Color color)
    {
        Texture2D tex = NewTex(size);
        Color[] px = new Color[size * size];
        float cx = size * 0.5f, cy = size * 0.5f;
        float outerR = size * 0.5f - 2f;
        float innerR = outerR - Thick;

        // Ring
        DrawRingInto(px, size, cx, cy, outerR, innerR, 0f, 360f, color);

        // Checkmark points: left-bottom foot, centre-top peak, right-bottom end
        // Y is flipped (0=bottom), so "visually lower" = smaller Y value
        // Short left leg: bottom-left -> centre
        Vector2 p1 = new Vector2(cx - 0.20f * size, cy - 0.04f * size); // left foot (visually lower-left)
        Vector2 p2 = new Vector2(cx - 0.04f * size, cy - 0.18f * size); // centre-bottom of V
        // Long right leg: centre -> upper-right
        Vector2 p3 = new Vector2(cx + 0.24f * size, cy + 0.16f * size); // upper right tip

        DrawLine(px, size, p1, p2, color, 5);
        DrawLine(px, size, p2, p3, color, 5);

        tex.SetPixels(px);
        tex.Apply();
        return tex;
    }

    /// Draws a circle with an X inside.
    /// X is perfectly symmetric so Y-flip doesn't affect it.
    static Texture2D DrawCross(int size, Color color)
    {
        Texture2D tex = NewTex(size);
        Color[] px = new Color[size * size];
        float cx = size * 0.5f, cy = size * 0.5f;
        float outerR = size * 0.5f - 2f;
        float innerR = outerR - Thick;

        DrawRingInto(px, size, cx, cy, outerR, innerR, 0f, 360f, color);

        // Symmetric diagonal lines — unaffected by Y-axis direction
        float off = 0.20f * size;
        DrawLine(px, size,
            new Vector2(cx - off, cy - off),
            new Vector2(cx + off, cy + off), color, 5);
        DrawLine(px, size,
            new Vector2(cx + off, cy - off),
            new Vector2(cx - off, cy + off), color, 5);

        tex.SetPixels(px);
        tex.Apply();
        return tex;
    }

    // ── Primitive drawing helpers ─────────────────────────────────────────

    static void DrawRingInto(Color[] px, int size,
        float cx, float cy, float outerR, float innerR,
        float startDeg, float arcDeg, Color color)
    {
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = x - cx, dy = y - cy;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                if (dist < innerR - 1f || dist > outerR + 1f) continue;

                // Anti-aliased edge
                float alpha = 1f;
                alpha = Mathf.Min(alpha, Mathf.Clamp01(dist - (innerR - 1f)));
                alpha = Mathf.Min(alpha, Mathf.Clamp01((outerR + 1f) - dist));

                // Angular check
                float angle = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;
                // Normalise angle relative to startDeg
                float rel = ((angle - startDeg) % 360f + 360f) % 360f;
                if (rel > arcDeg)
                {
                    // Soft fade at arc end for nicer look
                    float fade = Mathf.Clamp01(1f - (rel - arcDeg) * 3f);
                    alpha *= fade;
                    if (alpha < 0.01f) continue;
                }

                int idx = y * size + x;
                Color src = new Color(color.r, color.g, color.b, color.a * alpha);
                px[idx] = AlphaBlend(px[idx], src);
            }
    }

    static void DrawLine(Color[] px, int size,
        Vector2 a, Vector2 b, Color color, int halfWidth)
    {
        float len = Vector2.Distance(a, b);
        int steps = Mathf.CeilToInt(len * 2f);
        for (int s = 0; s <= steps; s++)
        {
            float t = s / (float)steps;
            float fx = Mathf.Lerp(a.x, b.x, t);
            float fy = Mathf.Lerp(a.y, b.y, t);
            // Paint a small filled circle at each step point
            for (int dy = -halfWidth; dy <= halfWidth; dy++)
                for (int dx = -halfWidth; dx <= halfWidth; dx++)
                {
                    int px2 = Mathf.RoundToInt(fx) + dx;
                    int py2 = Mathf.RoundToInt(fy) + dy;
                    if (px2 < 0 || px2 >= size || py2 < 0 || py2 >= size) continue;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    float alpha = Mathf.Clamp01(halfWidth - dist + 0.5f);
                    if (alpha < 0.01f) continue;
                    int idx = py2 * size + px2;
                    px[idx] = AlphaBlend(px[idx], new Color(color.r, color.g, color.b, alpha));
                }
        }
    }

    static Color AlphaBlend(Color dst, Color src)
    {
        float a = src.a + dst.a * (1f - src.a);
        if (a < 0.001f) return Color.clear;
        return new Color(
            (src.r * src.a + dst.r * dst.a * (1f - src.a)) / a,
            (src.g * src.a + dst.g * dst.a * (1f - src.a)) / a,
            (src.b * src.a + dst.b * dst.a * (1f - src.a)) / a,
            a);
    }

    static Texture2D NewTex(int size)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;
        // Clear to transparent
        Color[] blank = new Color[size * size];
        tex.SetPixels(blank);
        return tex;
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    RawImage GetOrCreateRawImage(string goName)
    {
        GameObject go = GameObject.Find(goName);
        if (go == null)
        {
            Debug.LogWarning($"[LoadingIconRenderer] '{goName}' not found. Creating it.");
            go = new GameObject(goName);
            go.transform.SetParent(transform, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(88, 88);
            rt.anchoredPosition = Vector2.zero;
        }

        // The builder creates these GOs with no Image component.
        // We just add RawImage directly — no cleanup needed.
        RawImage raw = go.GetComponent<RawImage>();
        if (raw == null) raw = go.AddComponent<RawImage>();
        raw.color = Color.white; // tint must be white so texture colours show correctly
        return raw;
    }
}

public enum LoadingIconState { Connecting, Success, Failed }