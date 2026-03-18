using UnityEngine;

public class MinimapBounds : MonoBehaviour
{
    [Header("Optional: auto fill from terrain")]
    public Terrain terrain;

    [Header("World bounds on XZ plane")]
    public Vector2 worldMinXZ;
    public Vector2 worldMaxXZ = new Vector2(200f, 200f);

    [ContextMenu("Auto Fill From Terrain")]
    public void AutoFillFromTerrain()
    {
        if (terrain == null || terrain.terrainData == null)
            return;

        Vector3 pos = terrain.transform.position;
        Vector3 size = terrain.terrainData.size;

        worldMinXZ = new Vector2(pos.x, pos.z);
        worldMaxXZ = new Vector2(pos.x + size.x, pos.z + size.z);
    }

    public Vector2 WorldToNormalized(Vector3 worldPosition)
    {
        float u = Mathf.InverseLerp(worldMinXZ.x, worldMaxXZ.x, worldPosition.x);
        float v = Mathf.InverseLerp(worldMinXZ.y, worldMaxXZ.y, worldPosition.z);

        return new Vector2(Mathf.Clamp01(u), Mathf.Clamp01(v));
    }

    public Vector3 CenterWorld
    {
        get
        {
            return new Vector3(
                (worldMinXZ.x + worldMaxXZ.x) * 0.5f,
                0f,
                (worldMinXZ.y + worldMaxXZ.y) * 0.5f
            );
        }
    }

    public float Width
    {
        get { return Mathf.Abs(worldMaxXZ.x - worldMinXZ.x); }
    }

    public float Depth
    {
        get { return Mathf.Abs(worldMaxXZ.y - worldMinXZ.y); }
    }
}