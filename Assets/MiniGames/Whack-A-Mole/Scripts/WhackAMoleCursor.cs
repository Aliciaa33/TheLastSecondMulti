using UnityEngine;

public class WhackAMoleCursor : MonoBehaviour
{
    [Header("Cursor Sprites")]
    public Texture2D hammer;   // hammer at rest

    [Header("Settings")]
    // Hotspot is where the click registers on the cursor image
    // (0,0) = top left, (width/2, height/2) = center
    public Vector2 hotspot = new Vector2(0f, 0f);

    void Start()
    {
        SetHammerCursor();

        // Register with MiniGameManager so it can reapply every frame
        if (MiniGameManager.Instance != null)
            MiniGameManager.Instance.RegisterCursor(hammer, hotspot);
    }

    public void SetHammerCursor()
    {
        if (hammer != null)
            Cursor.SetCursor(hammer, hotspot, CursorMode.Auto);
    }

    void OnDestroy()
    {
        // Unregister cursor when scene unloads
        if (MiniGameManager.Instance != null)
            MiniGameManager.Instance.RegisterCursor(null, Vector2.zero);
            
        // Reset to default cursor when leaving mini game
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
    }
}