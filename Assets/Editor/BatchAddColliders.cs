using UnityEngine;
using UnityEditor;

public class BatchAddColliders
{
    [MenuItem("Tools/Colliders/Add MeshColliders To Selected Root")]
    static void AddMeshCollidersToSelectedRoot()
    {
        foreach (GameObject root in Selection.gameObjects)
        {
            int count = 0;
            MeshFilter[] meshFilters = root.GetComponentsInChildren<MeshFilter>(true);

            foreach (MeshFilter mf in meshFilters)
            {
                if (mf.sharedMesh == null) continue;
                if (mf.GetComponent<Collider>() != null) continue;

                MeshCollider mc = Undo.AddComponent<MeshCollider>(mf.gameObject);
                mc.sharedMesh = mf.sharedMesh;
                count++;
            }

            Debug.Log($"Added {count} MeshColliders under {root.name}");
        }
    }

    [MenuItem("Tools/Colliders/Add BoxColliders To Selected Root")]
    static void AddBoxCollidersToSelectedRoot()
    {
        foreach (GameObject root in Selection.gameObjects)
        {
            int count = 0;
            MeshFilter[] meshFilters = root.GetComponentsInChildren<MeshFilter>(true);

            foreach (MeshFilter mf in meshFilters)
            {
                if (mf.sharedMesh == null) continue;
                if (mf.GetComponent<Collider>() != null) continue;

                BoxCollider bc = Undo.AddComponent<BoxCollider>(mf.gameObject);
                Bounds bounds = mf.sharedMesh.bounds;
                bc.center = bounds.center;
                bc.size = bounds.size;
                count++;
            }

            Debug.Log($"Added {count} BoxColliders under {root.name}");
        }
    }

    [MenuItem("Tools/Colliders/Remove All Colliders From Selected Root")]
    static void RemoveAllCollidersFromSelectedRoot()
    {
        foreach (GameObject root in Selection.gameObjects)
        {
            int count = 0;
            Collider[] colliders = root.GetComponentsInChildren<Collider>(true);

            foreach (Collider col in colliders)
            {
                Undo.DestroyObjectImmediate(col);
                count++;
            }

            Debug.Log($"Removed {count} Colliders under {root.name}");
        }
    }
}