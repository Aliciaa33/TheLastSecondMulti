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
        SetNormalCursor();
    }

    void SetNormalCursor()
    {
        if (hammer != null)
            Cursor.SetCursor(hammer, hotspot, CursorMode.Auto);
    }

    void OnDestroy()
    {
        // Reset to default cursor when leaving mini game
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
    }

    void OnApplicationFocus(bool hasFocus)
{
    if (hasFocus)
    {
        // Reapply hammer cursor when window regains focus
        Cursor.lockState = CursorLockMode.Confined;
        Cursor.visible = true;
        if (hammer != null)
            Cursor.SetCursor(hammer, hotspot, CursorMode.Auto);
    }
}
}