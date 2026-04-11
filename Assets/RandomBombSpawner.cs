using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class RandomBombPositioner : MonoBehaviourPun
{
    [Header("Spawn Settings")]
    public GameObject bombPrefab;
    public float initialSpawnDelay = 2f;
    public float respawnDelay = 3f;
    public int maxAttempt = 30;

    [Header("Spawn Area")]
    public BoxCollider spawnArea;

    [Header("Environment Checks")]
    public LayerMask groundMask;
    public LayerMask obstacleMask;
    public float checkRadius = 1.5f;
    public float PlayerHeight = 2f;

    // add Table Mask:
    public LayerMask tableMask;

    private bool isWaitingToSpawn = false;
    private bool isGameOver = false;

    void Start()
    {
        if (spawnArea == null)
            Debug.LogWarning("Spawn area (BoxCollider) is not assigned on " + gameObject.name);

        BombFuse.OnBombExploded += OnBombExploded;
        BombFuse.OnBombDefused += OnBombDefused;
        GameManager.OnGameOver += OnGameOver;

        // ★ 只有 MasterClient 负责生成炸弹
        if (PhotonNetwork.IsMasterClient)
        {
            StartCoroutine(SpawnFirstBomb());
        }
    }

    void OnDestroy()
    {
        BombFuse.OnBombExploded -= OnBombExploded;
        BombFuse.OnBombDefused -= OnBombDefused;
        GameManager.OnGameOver -= OnGameOver;
    }

    IEnumerator SpawnFirstBomb()
    {
        yield return new WaitForSeconds(initialSpawnDelay);
        if (!isGameOver)
        {
            SpawnRandomBomb();
        }
    }

    public void OnGameOver()
    {
        Debug.Log("🛑 游戏结束，停止生成炸弹");
        isGameOver = true;
        StopAllCoroutines();
        isWaitingToSpawn = false;
    }

    void OnBombExploded()
    {
        if (isGameOver) return;

        // ★ 只有 MasterClient 生成新炸弹
        if (PhotonNetwork.IsMasterClient)
        {
            Debug.Log("Master Client: 炸弹爆炸了，准备生成新炸弹...");
            if (!isWaitingToSpawn)
            {
                StartCoroutine(SpawnNewBombAfterDelay());
            }
        }
    }

    void OnBombDefused()
    {
        if (isGameOver) return;

        // ★ 只有 MasterClient 生成新炸弹
        if (PhotonNetwork.IsMasterClient)
        {
            Debug.Log("🧑‍💻 MasterClient: 炸弹拆除，准备生成新炸弹...");
            if (!isWaitingToSpawn)
            {
                StartCoroutine(SpawnNewBombAfterDelay());
            }
        }
    }

    IEnumerator SpawnNewBombAfterDelay()
    {
        isWaitingToSpawn = true;
        yield return new WaitForSeconds(respawnDelay);
        SpawnRandomBomb();
        isWaitingToSpawn = false;
    }

    bool SpawnRandomBomb()
    {
        if (isGameOver) return false;
        if (!PhotonNetwork.IsMasterClient) return false;

        if (bombPrefab == null)
        {
            Debug.LogError("bombPrefab未分配！");
            return false;
        }

        if (spawnArea == null)
        {
            Debug.LogError("spawnArea未分配！");
            return false;
        }

        Debug.Log("开始尝试生成新炸弹...");

        int surfaceMask = groundMask | tableMask;
        int obstacles = obstacleMask;
        int table = tableMask;

        Vector3 spawnPosition;
        bool found = SpawnValidator.TryFindSafeSpawnPointInBox(
            spawnArea,
            out spawnPosition,
            maxAttempts: maxAttempt,
            spawnSurfaceMask: surfaceMask,
            obstacleMask: obstacles,
            tableMask: table,
            clearRadius: checkRadius,
            playerHeight: PlayerHeight,
            spawnHeightOffset: 0.05f
        );

        if (found)
        {
            PhotonNetwork.Instantiate("BombModel", spawnPosition, Quaternion.identity);
            Debug.Log("✅ 炸弹生成成功! 位置: " + spawnPosition);
            return true;
        }

        /*
        int surfaceMask = groundMask | obstacleMask;


        for (int i = 0; i < maxAttempt; i++)
        {
            Vector3 randomPoint = GetRandomPointInBox(spawnArea);
            Vector3 rayOrigin = new Vector3(randomPoint.x, spawnArea.bounds.max.y + 5f, randomPoint.z);

            if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, 100f, surfaceMask))
            {
                Vector3 hitPoint = hit.point;
                Vector3 hitNormal = hit.normal;
                Collider hitCollider = hit.collider;
                Vector3 spawnPosition = hitPoint + hitNormal * 0.01f;

                Collider[] nearbyObstacles = Physics.OverlapSphere(hitPoint, checkRadius, obstacleMask);
                bool blocked = false;
                foreach (var c in nearbyObstacles)
                {
                    if (c != hitCollider)
                    {
                        blocked = true;
                        break;
                    }
                }

                if (!blocked)
                {
                    PhotonNetwork.Instantiate("BombModel", spawnPosition, Quaternion.identity);
                    Debug.Log($"✅ 炸弹生成成功！位置: {spawnPosition}");
                    return true;
                }
            }
        }
        */

        Debug.LogWarning("❌ 找不到合适的生成位置，稍后重试...");
        if (!isGameOver)
        {
            StartCoroutine(SpawnNewBombAfterDelay());
        }
        return false;
    }

    Vector3 GetRandomPointInBox(BoxCollider box)
    {
        Vector3 localMin = box.center - box.size * 0.5f;
        Vector3 localMax = box.center + box.size * 0.5f;
        Vector3 localPoint = new Vector3(
            Random.Range(localMin.x, localMax.x),
            Random.Range(localMin.y, localMax.y),
            Random.Range(localMin.z, localMax.z)
        );
        return box.transform.TransformPoint(localPoint);
    }

    void OnDrawGizmosSelected()
    {
        if (spawnArea != null)
        {
            Gizmos.color = Color.black;
            Gizmos.matrix = Matrix4x4.TRS(
                spawnArea.transform.position,
                spawnArea.transform.rotation,
                spawnArea.transform.lossyScale
            );
            Gizmos.DrawWireCube(spawnArea.center, spawnArea.size);
        }
    }
}