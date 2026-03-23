/*using UnityEngine;
using UnityEngine.AI;
using Photon.Pun;

public class NPCSpawner : MonoBehaviour
{
    public string npcPrefabResourcePath = "WanderingNPC";
    public Vector3 spawnPosition = new Vector3(110f, 9f, 75f);
    public Quaternion spawnRotation = Quaternion.identity;
    public float navSampleMaxDistance = 20f;

    void Start()
    {
        // ★ 多人模式：只有 Master 生成
        // ★ 单人模式：也需要生成（用本地 Instantiate）
        bool isMultiplayer = PhotonNetwork.InRoom && PhotonNetwork.CurrentRoom.PlayerCount > 1;

        if (isMultiplayer)
        {
            if (!PhotonNetwork.IsMasterClient) return;
            Vector3 pos = GetNavMeshPosition();
            PhotonNetwork.Instantiate(npcPrefabResourcePath, pos, spawnRotation);
        }
        else
        {
            // ★ 单人模式：从 Resources 加载并本地实例化
            Vector3 pos = GetNavMeshPosition();
            GameObject prefab = Resources.Load<GameObject>(npcPrefabResourcePath);
            if (prefab != null)
            {
                Instantiate(prefab, pos, spawnRotation);
                Debug.Log($"NPCSpawner: 单人模式生成 NPC at {pos}");
            }
            else
            {
                Debug.LogError($"NPCSpawner: 找不到 prefab: Resources/{npcPrefabResourcePath}");
            }
        }
    }

    private Vector3 GetNavMeshPosition()
    {
        NavMeshHit hit;
        if (NavMesh.SamplePosition(spawnPosition, out hit, navSampleMaxDistance, NavMesh.AllAreas))
        {
            Debug.Log($"NPCSpawner: Found NavMesh point at {hit.position}");
            return hit.position;
        }
        Debug.LogWarning($"NPCSpawner: No NavMesh near {spawnPosition}");
        return spawnPosition;
    }
}
*/

using UnityEngine;
using UnityEngine.AI;
using Photon.Pun;

public class NPCSpawner : MonoBehaviour
{
    public string npcPrefabResourcePath = "WanderingNPC"; // Resources/WanderingNPC.prefab
    public Vector3 spawnPosition = Vector3.zero;
    public Quaternion spawnRotation = Quaternion.identity;
    public float navSampleMaxDistance = 5f; // 搜索 NavMesh 的最大距离

    void Start()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        Vector3 spawnPos = spawnPosition;
        NavMeshHit hit;
        if (NavMesh.SamplePosition(spawnPosition, out hit, navSampleMaxDistance, NavMesh.AllAreas))
        {
            spawnPos = hit.position; // 找到合适 NavMesh 点
        }
        else
        {
            Debug.LogWarning($"NPCSpawner: spawnPosition {spawnPosition} not on NavMesh. Will instantiate at original position (agent may error).");
        }

        PhotonNetwork.Instantiate(npcPrefabResourcePath, spawnPos, spawnRotation);
    }
}


/*
using UnityEngine;
using Photon.Pun;

public class NPCSpawner : MonoBehaviour
{
    public string npcPrefabResourcePath = "WanderingNPC"; // Resources/WanderingNPC.prefab
    public Vector3 spawnPosition = Vector3.zero;
    public Quaternion spawnRotation = Quaternion.identity;

    void Start()
    {
        // only the master client spawns the npc
        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.Instantiate(npcPrefabResourcePath, spawnPosition, spawnRotation);
        }
    }
}
*/
