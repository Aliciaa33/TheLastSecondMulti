using UnityEngine;
using System.Collections.Generic;
using System;
using UnityEngine.SceneManagement;
using StarterAssets;
using Photon.Pun;
using Photon.Realtime;

[DefaultExecutionOrder(-5)]
public class GameManager : MonoBehaviourPunCallbacks
{
    public static GameManager Instance { get; private set; }

    [Header("Game Settings")]
    public int maxHP = 5;
    public int currentHP = 5;

    [Header("Hint Settings")]
    public GameObject hintPrefab;
    public static int hintCount = 5;
    private int bombPassword;
    public Transform[] hintSpawnPoints = new Transform[hintCount];
    private List<int> hintIndices = new List<int>();

    [Header("Potion Settings")]
    public GameObject potionPrefab;
    public int potionCount = 0;
    public List<Transform> potionSpawnPoints = new List<Transform>();

    public int defusedBombs = 0;
    public int goal = 3;

    [SerializeField] private LayerMask obstacleLayerMask = 8;
    private StarterAssets.StarterAssetsInputs playerInputs;

    public bool gameActive = true; // the game is running
    // 添加gameover事件
    public static event Action OnGameOver;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        if (ConnectToServer.Instance != null)
        {
            GameMode mode = ConnectToServer.Instance.GetGameMode();
            Debug.Log("Game running in mode: " + mode);
        }
        InitializeGame();
        // 订阅炸弹爆炸事件
        BombFuse.OnBombExploded += OnBombExploded; // zhq
                                                   // 订阅炸弹拆除事件
        BombFuse.OnBombDefused += OnBombDefused; // zhq
        // FindPlayerInputs();
    }

    public void InitializeGame()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        gameActive = true;
        currentHP = maxHP;
        hintPrefab.SetActive(false);
        potionPrefab.SetActive(false);
        // Create hint spawn point indices
        for (int i = 0; i < hintSpawnPoints.Length; i++)
            hintIndices.Add(i);

        ClearExistingHints();
        ClearExistingPotions();

        // clear up the inventory
        if (InventoryManager.Instance != null)
            InventoryManager.Instance.ClearInventory();

        SpawnHint();

        // Initialize 3 potions at the start of each round
        for (int i = 0; i < 3; i++)
            SpawnPotion();

        // Update UI
        if (UIManager.Instance != null)
            UIManager.Instance.UpdateHP(currentHP, maxHP);
    }

    void StartNewRound()
    {
        if (!gameActive) return; // test whether the game has ended
        //currentTestCountdown = testCountdownTime; // 重置倒计时
        InventoryManager.Instance.ClearInventory(); // clear up the inventory
        ClearExistingHints();
        SpawnHint();
        SpawnPotion();
    }

    void SpawnHint()
    {
        // Only MasterClient generates and syncs hints
        if (!PhotonNetwork.IsConnected || PhotonNetwork.IsMasterClient)
        {
            GenerateAndSyncHints();
        }
    }

    private void GenerateAndSyncHints()
    {
        GenerateHintPos();
        ShuffleList(hintIndices);

        (int password, List<(string, string)> questionHints) = QuestionManager.Instance.GetRandomQuestion();
        this.bombPassword = password;
        Debug.Log($"New bomb password is {bombPassword}");

        int count = questionHints.Count;
        string[] numbers = new string[count];
        string[] hints = new string[count];
        int[] indices = new int[count];
        Vector3[] positions = new Vector3[count];
        Quaternion[] rotations = new Quaternion[count];

        for (int i = 0; i < count; i++)
        {
            numbers[i] = questionHints[i].Item1;
            hints[i] = questionHints[i].Item2;
            indices[i] = hintIndices[i];
            positions[i] = hintSpawnPoints[hintIndices[i]].position;
            rotations[i] = hintSpawnPoints[hintIndices[i]].rotation;
        }

        if (PhotonNetwork.IsConnected && PhotonNetwork.CurrentRoom.PlayerCount > 1)
        {
            // Sync to all clients including self
            photonView.RPC("RPC_SyncHints", RpcTarget.All,
                password, numbers, hints, positions, rotations);
        }
        else
        {
            // Single player — spawn directly
            SpawnHintsLocally(password, numbers, hints, positions, rotations);
        }
    }

    [PunRPC]
    private void RPC_SyncHints(int password, string[] numbers, string[] hints,
        Vector3[] positions, Quaternion[] rotations)
    {
        this.bombPassword = password;
        Debug.Log($"Bomb password synced: {password}");
        SpawnHintsLocally(password, numbers, hints, positions, rotations);
    }

    private void SpawnHintsLocally(int password, string[] numbers, string[] hints,
    Vector3[] positions, Quaternion[] rotations)
    {
        this.bombPassword = password;

        if (!PhotonNetwork.IsConnected || PhotonNetwork.IsMasterClient)
        {
            for (int i = 0; i < numbers.Length; i++)
            {
                GameObject hintObj;

                if (PhotonNetwork.IsConnected)
                {
                    // Pass number and hint as instantiation data
                    object[] initData = new object[] { numbers[i], hints[i] };
                    hintObj = PhotonNetwork.Instantiate("Hint", positions[i], rotations[i], 0, initData);
                }
                else
                    hintObj = Instantiate(hintPrefab, positions[i], rotations[i]);

                hintObj.SetActive(true);
                hintObj.tag = "Hint";

                HintPickUp hintPickup = hintObj.GetComponent<HintPickUp>();
                if (hintPickup != null)
                {
                    hintPickup.number = numbers[i];
                    hintPickup.hint = hints[i];
                }
            }
        }
        // Non-master clients do nothing — Photon replicates objects automatically
    }

    // public override void OnPlayerEnteredRoom(Photon.Realtime.Player newPlayer)
    // {
    //     if (PhotonNetwork.IsMasterClient && bombPassword != 0)
    //     {
    //         photonView.RPC("RPC_SyncPassword", newPlayer, bombPassword);
    //     }
    // }

    // [PunRPC]
    // void RPC_SyncPassword(int password)
    // {
    //     bombPassword = password;
    //     Debug.Log($"Password synced: {password} (Player: {PhotonNetwork.LocalPlayer.NickName})");
    // }

    public int GetCurrentPassword()
    {
        return bombPassword;
    }

    void SpawnPotion()
    {
        // Non-master clients do nothing — Photon replicates from MasterClient
        if (PhotonNetwork.IsConnected && !PhotonNetwork.IsMasterClient) return;

        Vector3 potionPos = FindValidPos(2);
        if (potionPos == Vector3.zero) return;

        potionCount++;
        GameObject potionObj;

        if (PhotonNetwork.IsConnected)
            potionObj = PhotonNetwork.Instantiate("Potion", potionPos, Quaternion.identity);
        else
            potionObj = Instantiate(potionPrefab, potionPos, Quaternion.identity);

        potionObj.name = $"Potion_{potionCount}";
        potionObj.transform.rotation = Quaternion.Euler(0f, 0f, 50f);
        potionObj.SetActive(true);
        potionSpawnPoints.Add(potionObj.transform);
    }

    void ClearExistingHints()
    {
        GameObject[] existingHints = GameObject.FindGameObjectsWithTag("Hint");
        foreach (GameObject hint in existingHints)
        {
            if (PhotonNetwork.IsConnected && hint.GetComponent<PhotonView>() != null)
                PhotonNetwork.Destroy(hint);
            else
                Destroy(hint);
        }
    }

    void ClearExistingPotions()
    {
        potionSpawnPoints.Clear();
        potionCount = 0;

        GameObject[] existingPotions = GameObject.FindGameObjectsWithTag("Potion");
        foreach (GameObject potion in existingPotions)
        {
            if (PhotonNetwork.IsConnected && potion.GetComponent<PhotonView>() != null)
                PhotonNetwork.Destroy(potion);
            else
                Destroy(potion);
        }
    }

    float GetGroundHeight(Vector3 pos)
    {
        RaycastHit hit;
        float raycastHeight = 10f; // 从足够高的地方发射射线
        Vector3 rayStart = new Vector3(pos.x, raycastHeight, pos.z);
        int groundLayerMask = LayerMask.GetMask("Ground"); // 建议为地形设置一个 Layer

        if (Physics.Raycast(rayStart, Vector3.down, out hit, Mathf.Infinity, groundLayerMask))
        {
            return hit.point.y;
        }

        Debug.LogWarning($"No ground found at position: {pos}");
        return pos.y; // 如果没找到地面，返回原 Y 值
    }

    // generate different heights for hint, potion
    // 1 = hint, 2 = potion
    Vector3 FindValidPos(int type)
    {
        Vector3 randomPos;
        int attempts = 0;
        do
        {
            float x = UnityEngine.Random.Range(30f, 70f);
            float z = UnityEngine.Random.Range(30f, 70f);
            float y = GetGroundHeight(new Vector3(x, 0, z));
            if (type == 1)
                randomPos = new Vector3(x, y + 0.1f, z);
            else if (type == 2)
                randomPos = new Vector3(x, y + 0.29f, z);
            else
                randomPos = new Vector3(x, y, z);

            // Check whether the position will overlap with objects in obstacle layer
            Collider[] colliders = Physics.OverlapSphere(randomPos, 3f, obstacleLayerMask);
            bool isInsideObstacle = colliders.Length > 0;

            if (!isInsideObstacle)
                return randomPos;

            attempts++;
        } while (attempts < 100); // Limit attempts to prevent infinite loops

        Debug.LogWarning("Failed to find valid hint position after 100 attempts.");
        return Vector3.zero; // Return zero vector to indicate failure
    }

    void GenerateHintPos()
    {
        // Generate random hint spawn points
        GameObject spawnedHint;
        for (int i = 0; i < hintSpawnPoints.Length; i++)
        {
            Vector3 pos = FindValidPos(1);
            if (pos != Vector3.zero)
            {
                spawnedHint = new GameObject($"HintSpawnPoint_{i}");
                spawnedHint.transform.position = pos;
                spawnedHint.transform.SetParent(this.transform);
                hintSpawnPoints[i] = spawnedHint.transform;
            }
        }
    }

    void ShuffleList(List<int> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int temp = list[i];
            int randomIndex = UnityEngine.Random.Range(i, list.Count);
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }

    void SpawnHintAtPosition(int index, string number, string hint)
    {
        GameObject hintObj = Instantiate(hintPrefab, hintSpawnPoints[index].position, hintSpawnPoints[index].rotation);
        hintObj.SetActive(true);
        hintObj.tag = "Hint";
        HintPickUp hintPickup = hintObj.GetComponent<HintPickUp>();
        if (hintPickup != null)
        {
            hintPickup.number = number;
            hintPickup.hint = hint;
        }
    }

    /*//zhq
    public void BombDefused()
    {
        if (!gameActive) return; // test whether the game has ended
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowToast("Bomb defused! Round won!");
        }
        // Start new round after delay
        Invoke("StartNewRound", 3f);
    }
    */

    /*
    public void BombExploded()
    {
        Debug.Log("收到炸弹爆炸事件，减少生命值"); // zhq
        TakeDamage();
        if (currentHP > 0)
        {
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowToast("Bomb exploded! Starting new round...");
            }
            Invoke("StartNewRound", 3f);
        }
    }
    */

    public void OnBombExploded()
    {
        if (!gameActive) return; // test whether the game has ended
        Debug.Log("收到炸弹爆炸事件，减少生命值"); // zhq
        TakeDamage();
        if (currentHP > 0)
        {
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowToast("Bomb exploded! Starting new round...");
            }
            Invoke("StartNewRound", 3f);
        }
    }

    public void OnBombDefused()
    {
        Debug.Log("Enter onbombdefused func in game manager");
        if (!PhotonNetwork.IsMasterClient)
        {
            Debug.Log("非MasterClient，跳过defuseBomb");
            return;
        }
        defuseBomb();
        if (!gameActive) return; // test whether the game has ended
        Invoke("StartNewRound", 3f); // zhq
    }

    public void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Debug.Log($"场景加载完成: {scene.name}, 重新初始化游戏");
        InitializeGame();
    }

    public void TakeDamage()
    {
        currentHP--;

        // Update health UI
        if (UIManager.Instance != null)
            UIManager.Instance.UpdateHP(currentHP, maxHP);

        // Show Game Over screen if health is zero
        if (currentHP <= 0) GameOver(false);
    }

    public void Restore()
    {
        if (!gameActive) return;
        if (PhotonNetwork.IsConnected)
            photonView.RPC("RPC_Restore", RpcTarget.All);
        else
            RestoreLocally();
    }

    [PunRPC]
    void RPC_Restore()
    {
        RestoreLocally();
    }
    public void RestoreLocally()
    {
        if (!gameActive) return;
        currentHP++;
        if (currentHP > maxHP) currentHP = maxHP;

        // Update health UI
        if (UIManager.Instance != null)
            UIManager.Instance.UpdateHP(currentHP, maxHP);
    }

    public void defuseBomb()
    {
        // Only the MasterClient increment the counter
        if (PhotonNetwork.IsMasterClient)
        {
            defusedBombs++;
            Debug.Log($"defused bomb number: {defusedBombs}/{goal}");

            if (UIManager.Instance != null)
                UIManager.Instance.UpdateDefusedBombs();

            if (defusedBombs >= goal)
                GameOver(true);
        }

        // sync the count to all clients
        if (photonView != null && PhotonNetwork.IsConnected)
        {
            Debug.Log("Syncing defused bomb count");
            photonView.RPC("RPC_SyncDefusedBombs", RpcTarget.Others, defusedBombs);
        }
    }

    [PunRPC]
    void RPC_SyncDefusedBombs(int newCount)
    {
        defusedBombs = newCount;
        Debug.Log($"Synced defused bomb count: {defusedBombs}/{goal}");

        if (UIManager.Instance != null)
            UIManager.Instance.UpdateDefusedBombs();

        if (defusedBombs >= goal)
            GameOver(true);
    }

    void GameOver(bool win)
    {
        gameActive = false;
        OnGameOver?.Invoke();

        Debug.Log("Game Over");
        // 停止所有正在执行的 Invoke 调用
        CancelInvoke("StartNewRound");

        // 禁用玩家移动
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        foreach (GameObject player in players)
        {
            ThirdPersonController controller = player.GetComponent<ThirdPersonController>();
            if (controller != null)
                controller.enabled = false;
        }

        // 设置鼠标状态
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowGameOver(win);
        }
    }

    void OnDestroy()
    {
        // 取消订阅事件，防止内存泄漏
        BombFuse.OnBombExploded -= OnBombExploded;
        BombFuse.OnBombDefused -= OnBombDefused;
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}
