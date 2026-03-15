using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Attach to the GameOverPanel's IconContainer.
/// Draws a red X (loss) or green checkmark (win) into RawImage textures.
/// Same approach as LoadingIconRenderer — no sprites needed.
/// </summary>
public class GameOverIconRenderer : MonoBehaviour
{
    private RawImage winIcon;
    private RawImage lossIcon;

    private const int TexSize = 128;
    private const int Thick   = 10;

    static readonly Color NeonGreen = new Color(0.22f, 1.00f, 0.43f, 1f);
    static readonly Color NeonRed   = new Color(1.00f, 0.24f, 0.24f, 1f);

    void Awake()
    {
        winIcon  = GetOrCreateRawImage("GOWinIcon");
        lossIcon = GetOrCreateRawImage("GOLossIcon");

        winIcon.texture  = DrawCheckmark(TexSize, NeonGreen);
        lossIcon.texture = DrawCross(TexSize, NeonRed);

        winIcon.gameObject.SetActive(false);
        lossIcon.gameObject.SetActive(false);
    }

    public void ShowResult(bool win)
    {
        if (winIcon  != null) winIcon.gameObject.SetActive(win);
        if (lossIcon != null) lossIcon.gameObject.SetActive(!win);
    }

    // ── Drawing (same helpers as LoadingIconRenderer) ─────────────────────
    static Texture2D DrawCheckmark(int size, Color color)
    {
        Texture2D tex = NewTex(size);
        Color[] px = new Color[size * size];
        float cx = size * 0.5f, cy = size * 0.5f;
        float outerR = size * 0.5f - 2f, innerR = outerR - Thick;
        DrawRingInto(px, size, cx, cy, outerR, innerR, 0f, 360f, color);
        Vector2 p1 = new Vector2(cx - 0.20f*size, cy - 0.04f*size);
        Vector2 p2 = new Vector2(cx - 0.04f*size, cy - 0.18f*size);
        Vector2 p3 = new Vector2(cx + 0.24f*size, cy + 0.16f*size);
        DrawLine(px, size, p1, p2, color, 5);
        DrawLine(px, size, p2, p3, color, 5);
        tex.SetPixels(px); tex.Apply();
        return tex;
    }

    static Texture2D DrawCross(int size, Color color)
    {
        Texture2D tex = NewTex(size);
        Color[] px = new Color[size * size];
        float cx = size * 0.5f, cy = size * 0.5f;
        float outerR = size * 0.5f - 2f, innerR = outerR - Thick;
        DrawRingInto(px, size, cx, cy, outerR, innerR, 0f, 360f, color);
        float off = 0.20f * size;
        DrawLine(px, size, new Vector2(cx-off,cy-off), new Vector2(cx+off,cy+off), color, 5);
        DrawLine(px, size, new Vector2(cx+off,cy-off), new Vector2(cx-off,cy+off), color, 5);
        tex.SetPixels(px); tex.Apply();
        return tex;
    }

    static void DrawRingInto(Color[] px, int size,
        float cx, float cy, float outerR, float innerR,
        float startDeg, float arcDeg, Color color)
    {
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float dx = x - cx, dy = y - cy;
            float dist = Mathf.Sqrt(dx*dx + dy*dy);
            if (dist < innerR-1f || dist > outerR+1f) continue;
            float alpha = Mathf.Min(
                Mathf.Clamp01(dist-(innerR-1f)),
                Mathf.Clamp01((outerR+1f)-dist));
            float angle = Mathf.Atan2(dy,dx)*Mathf.Rad2Deg;
            float rel = ((angle-startDeg)%360f+360f)%360f;
            if (rel > arcDeg) { alpha *= Mathf.Clamp01(1f-(rel-arcDeg)*3f); if(alpha<.01f) continue; }
            int idx = y*size+x;
            Color src = new Color(color.r,color.g,color.b,color.a*alpha);
            px[idx] = AlphaBlend(px[idx], src);
        }
    }

    static void DrawLine(Color[] px, int size, Vector2 a, Vector2 b, Color color, int hw)
    {
        int steps = Mathf.CeilToInt(Vector2.Distance(a,b)*2f);
        for (int s = 0; s <= steps; s++)
        {
            float t = s/(float)steps;
            float fx = Mathf.Lerp(a.x,b.x,t), fy = Mathf.Lerp(a.y,b.y,t);
            for (int dy = -hw; dy <= hw; dy++)
            for (int dx = -hw; dx <= hw; dx++)
            {
                int px2 = Mathf.RoundToInt(fx)+dx, py2 = Mathf.RoundToInt(fy)+dy;
                if (px2<0||px2>=size||py2<0||py2>=size) continue;
                float dist = Mathf.Sqrt(dx*dx+dy*dy);
                float alpha = Mathf.Clamp01(hw-dist+0.5f);
                if (alpha<.01f) continue;
                int idx = py2*size+px2;
                px[idx] = AlphaBlend(px[idx], new Color(color.r,color.g,color.b,alpha));
            }
        }
    }

    static Color AlphaBlend(Color dst, Color src)
    {
        float a = src.a + dst.a*(1f-src.a);
        if (a < .001f) return Color.clear;
        return new Color(
            (src.r*src.a + dst.r*dst.a*(1f-src.a))/a,
            (src.g*src.a + dst.g*dst.a*(1f-src.a))/a,
            (src.b*src.a + dst.b*dst.a*(1f-src.a))/a, a);
    }

    static Texture2D NewTex(int size)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode   = TextureWrapMode.Clamp;
        tex.SetPixels(new Color[size*size]);
        return tex;
    }

    RawImage GetOrCreateRawImage(string goName)
    {
        GameObject go = GameObject.Find(goName);
        if (go == null)
        {
            go = new GameObject(goName);
            go.transform.SetParent(transform, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f,0.5f);
            rt.sizeDelta = new Vector2(88,88);
            rt.anchoredPosition = Vector2.zero;
        }
        RawImage raw = go.GetComponent<RawImage>() ?? go.AddComponent<RawImage>();
        raw.color = Color.white;
        return raw;
    }
}
