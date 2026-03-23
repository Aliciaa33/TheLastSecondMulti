using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using Photon.Pun;
using Photon.Realtime;

public class NPCWander : MonoBehaviourPunCallbacks
{
    [Header("Wander Settings")]
    public float wanderRadius = 8f;
    public float arrivalThreshold = 0.5f;
    public float waitTimeMin = 1f;
    public float waitTimeMax = 3f;

    [Header("Network & Smoothing")]
    public float positionLerpSpeed = 8f;
    public float rotationLerpSpeed = 8f;
    public float sendRate = 10f;

    [Header("Interaction")]
    public float interactRadius = 3f;
    public float requiredStaySeconds = 2f;
    public string chatMessage = "want to play a game? give me some gold coins!";
    public float charDelay = 0.04f;
    public float scanInterval = 0.3f;

    [Header("References")]
    public ChatBubbleController chatBubble;

    private NavMeshAgent agent;
    private Vector3 networkPosition;
    private Quaternion networkRotation;
    private Coroutine wanderRoutine;
    private Coroutine syncRoutine;
    private Coroutine scanRoutine;

    private enum NPCState { Idle, Reserved, Chatting }
    private NPCState state = NPCState.Idle;
    private int reservedActor = -999; // 用 -999 表示"无人"，-1 留给单人模式玩家

    // ★ 判断是否为真正的多人模式
    private bool IsMultiplayer
    {
        get { return PhotonNetwork.InRoom && PhotonNetwork.CurrentRoom.PlayerCount > 1; }
    }

    // ★ 判断当前客户端是否为控制端（Master 或单人）
    private bool IsController
    {
        get { return PhotonNetwork.IsMasterClient || !PhotonNetwork.InRoom; }
    }

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        networkPosition = transform.position;
        networkRotation = transform.rotation;

        if (chatBubble == null)
            chatBubble = GetComponentInChildren<ChatBubbleController>(true);
    }

    void Start()
    {
        if (IsController)
        {
            EnsureAgentOnNavMesh();
            StartControl();
        }
        else
        {
            StopControl();
        }
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            state = NPCState.Idle;
            reservedActor = -999;
            EnsureAgentOnNavMesh();
            StartControl();
        }
        else
        {
            StopControl();
        }
    }

    private void EnsureAgentOnNavMesh()
    {
        if (agent == null) return;
        if (agent.isOnNavMesh) return;

        NavMeshHit hit;
        if (NavMesh.SamplePosition(transform.position, out hit, 20f, NavMesh.AllAreas))
        {
            transform.position = hit.position;
            agent.enabled = false;
            agent.enabled = true;
            if (!agent.isOnNavMesh) agent.Warp(hit.position);
            Debug.Log($"NPCWander: Warped agent to NavMesh at {hit.position}");
        }
        else
        {
            Debug.LogError("NPCWander: No NavMesh found near NPC!");
        }
    }

    void Update()
    {
        if (!IsController)
        {
            transform.position = Vector3.Lerp(transform.position, networkPosition, Time.deltaTime * positionLerpSpeed);
            transform.rotation = Quaternion.Slerp(transform.rotation, networkRotation, Time.deltaTime * rotationLerpSpeed);
        }
    }

    // -------------------- Control --------------------
    private void StartControl()
    {
        if (agent != null)
        {
            agent.updatePosition = true;
            agent.updateRotation = true;
        }
        if (wanderRoutine == null) wanderRoutine = StartCoroutine(WanderRoutine());
        if (syncRoutine == null) syncRoutine = StartCoroutine(SyncRoutine());
        if (scanRoutine == null) scanRoutine = StartCoroutine(PlayerScanRoutine());
    }

    private void StopControl()
    {
        if (wanderRoutine != null) { StopCoroutine(wanderRoutine); wanderRoutine = null; }
        if (syncRoutine != null) { StopCoroutine(syncRoutine); syncRoutine = null; }
        if (scanRoutine != null) { StopCoroutine(scanRoutine); scanRoutine = null; }
        if (agent != null)
        {
            if (agent.isOnNavMesh) agent.ResetPath();
            agent.updatePosition = false;
            agent.updateRotation = false;
        }
    }

    private void StopWanderImmediate()
    {
        if (wanderRoutine != null) { StopCoroutine(wanderRoutine); wanderRoutine = null; }
        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }
    }

    private IEnumerator WanderRoutine()
    {
        while (true)
        {
            if (agent != null && agent.isOnNavMesh)
            {
                Vector3 dest = RandomNavSphere(transform.position, wanderRadius);
                agent.SetDestination(dest);
                while (agent.pathPending || agent.remainingDistance > arrivalThreshold)
                {
                    yield return null;
                }
            }
            else
            {
                yield return null;
                continue;
            }
            float wait = Random.Range(waitTimeMin, waitTimeMax);
            yield return new WaitForSeconds(wait);
        }
    }

    private IEnumerator SyncRoutine()
    {
        var wait = new WaitForSeconds(1f / sendRate);
        while (true)
        {
            // ★ 只在多人模式下广播
            if (PhotonNetwork.InRoom && PhotonNetwork.IsMasterClient)
            {
                photonView.RPC("RPC_ReceiveTransform", RpcTarget.OthersBuffered,
                    transform.position, transform.rotation);
            }
            yield return wait;
        }
    }

    [PunRPC]
    private void RPC_ReceiveTransform(Vector3 pos, Quaternion rot)
    {
        networkPosition = pos;
        networkRotation = rot;
    }

    // -------------------- Scan --------------------
    private IEnumerator PlayerScanRoutine()
    {
        yield return new WaitForSeconds(5f);

        var wait = new WaitForSeconds(scanInterval);

        while (true)
        {
            if (state == NPCState.Idle)
            {
                // ★ 查找最近玩家（支持 actorNumber = -1 的单人模式）
                int foundActor;
                Transform nearest = FindNearestPlayerWithin(interactRadius, out foundActor);

                if (nearest != null && foundActor != -999)
                {
                    float dist = Vector3.Distance(transform.position, nearest.position);
                    Debug.Log($"[NPC] 检测到玩家 Actor={foundActor} 距离={dist:F1}m");

                    yield return StartCoroutine(HandleApproach(foundActor));
                }
            }
            yield return wait;
        }
    }

    // ★ 修改：返回 actorNumber（包括单人模式的 -1）
    private Transform FindNearestPlayerWithin(float radius, out int actorNumber)
    {
        Transform best = null;
        float bestDist = float.MaxValue;
        actorNumber = -999; // -999 表示"没找到"

        foreach (var kv in PlayerRegistration.PlayerMap)
        {
            Transform t = kv.Value;
            if (t == null) continue;
            float d = Vector3.Distance(transform.position, t.position);
            if (d <= radius && d < bestDist)
            {
                best = t;
                bestDist = d;
                actorNumber = kv.Key; // ★ 直接用 PlayerMap 的 key（多人是 ActorNumber，单人是 -1）
            }
        }
        return best;
    }

    // -------------------- Interaction --------------------
    private IEnumerator HandleApproach(int actorNumber)
    {
        if (state != NPCState.Idle) yield break;
        if (!PlayerRegistration.PlayerMap.ContainsKey(actorNumber)) yield break;

        // 二次确认距离
        Transform check = PlayerRegistration.PlayerMap[actorNumber];
        if (check == null) yield break;
        float checkDist = Vector3.Distance(transform.position, check.position);
        if (checkDist > interactRadius) yield break;

        state = NPCState.Reserved;
        reservedActor = actorNumber;
        StopWanderImmediate();

        Debug.Log($"[NPC] 状态 -> Reserved，等待 {requiredStaySeconds}s");

        // 等待玩家持续停留
        float elapsed = 0f;
        while (elapsed < requiredStaySeconds)
        {
            if (!PlayerRegistration.PlayerMap.ContainsKey(actorNumber))
            { CancelReserved(); yield break; }

            Transform t = PlayerRegistration.PlayerMap[actorNumber];
            if (t == null)
            { CancelReserved(); yield break; }

            float d = Vector3.Distance(transform.position, t.position);
            if (d > interactRadius)
            {
                Debug.Log($"[NPC] 玩家在等待期间离开（距离={d:F1}），取消");
                CancelReserved();
                yield break;
            }
            elapsed += Time.deltaTime;
            yield return null;
        }

        // 成功 -> Chatting
        state = NPCState.Chatting;
        Debug.Log($"[NPC] 状态 -> Chatting，显示聊天");

        // ★★★ 核心区分：多人用 RPC，单人直接本地调用 ★★★
        ShowChatToPlayer(actorNumber);

        // 持续监测玩家是否离开
        while (true)
        {
            if (!PlayerRegistration.PlayerMap.ContainsKey(actorNumber))
            {
                HideChatFromPlayer(actorNumber);
                ResetAfterInteraction();
                yield break;
            }
            Transform t = PlayerRegistration.PlayerMap[actorNumber];
            if (t == null)
            {
                HideChatFromPlayer(actorNumber);
                ResetAfterInteraction();
                yield break;
            }
            float d = Vector3.Distance(transform.position, t.position);
            if (d > interactRadius)
            {
                Debug.Log($"[NPC] 玩家离开（距离={d:F1}），隐藏聊天并恢复漫游");
                HideChatFromPlayer(actorNumber);
                ResetAfterInteraction();
                yield break;
            }
            yield return null;
        }
    }

    // ★★★ 显示 chat：多人走 RPC，单人直接本地调用 ★★★
    private void ShowChatToPlayer(int actorNumber)
    {
        if (IsMultiplayer)
        {
            // 多人模式：通过 RPC 只发给目标玩家
            Player targetPlayer = PhotonNetwork.CurrentRoom.GetPlayer(actorNumber);
            if (targetPlayer != null)
            {
                photonView.RPC("RPC_ShowChat_LocalOnly", targetPlayer, chatMessage, charDelay);
            }
        }
        else
        {
            // 单人模式：直接本地显示
            LocalShowChat(chatMessage, charDelay);
        }
    }

    // ★★★ 隐藏 chat：多人走 RPC，单人直接本地调用 ★★★
    private void HideChatFromPlayer(int actorNumber)
    {
        if (IsMultiplayer)
        {
            Player targetPlayer = PhotonNetwork.CurrentRoom.GetPlayer(actorNumber);
            if (targetPlayer != null)
            {
                photonView.RPC("RPC_HideChat_LocalOnly", targetPlayer);
            }
        }
        else
        {
            LocalHideChat();
        }
    }

    // ★ 本地显示/隐藏（单人模式 & RPC 内部共用）
    private void LocalShowChat(string msg, float perCharDelay)
    {
        if (chatBubble == null)
            chatBubble = GetComponentInChildren<ChatBubbleController>(true);
        if (chatBubble == null)
        {
            Debug.LogWarning("ChatBubbleController 未绑定");
            return;
        }
        chatBubble.ShowMessage(msg, perCharDelay);
    }

    private void LocalHideChat()
    {
        if (chatBubble == null) return;
        chatBubble.HideWithStop();
    }

    private void CancelReserved()
    {
        Debug.Log("[NPC] CancelReserved -> 恢复漫游");
        HideChatFromPlayer(reservedActor);
        ResetAfterInteraction();
    }

    private void ResetAfterInteraction()
    {
        reservedActor = -999;
        state = NPCState.Idle;
        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = false;
        }
        if (wanderRoutine == null) wanderRoutine = StartCoroutine(WanderRoutine());
        Debug.Log("[NPC] 状态 -> Idle，恢复漫游");
    }

    // -------------------- RPC --------------------
    [PunRPC]
    private void RPC_ShowChat_LocalOnly(string msg, float perCharDelay, PhotonMessageInfo info)
    {
        LocalShowChat(msg, perCharDelay);
    }

    [PunRPC]
    private void RPC_HideChat_LocalOnly(PhotonMessageInfo info)
    {
        LocalHideChat();
    }

    // -------------------- Utility --------------------
    public static Vector3 RandomNavSphere(Vector3 origin, float dist)
    {
        Vector3 randDirection = Random.insideUnitSphere * dist;
        randDirection += origin;
        NavMeshHit navHit;
        if (NavMesh.SamplePosition(randDirection, out navHit, dist, NavMesh.AllAreas))
        {
            return navHit.position;
        }
        return origin;
    }
}


/*
using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using Photon.Pun;
using Photon.Realtime;

public class NPCWander : MonoBehaviourPunCallbacks
{
    [Header("Wander Settings")]
    public float wanderRadius = 8f;
    public float arrivalThreshold = 0.5f;
    public float waitTimeMin = 1f;
    public float waitTimeMax = 3f;

    [Header("Network & Smoothing")]
    public float positionLerpSpeed = 8f;
    public float rotationLerpSpeed = 8f;
    public float sendRate = 10f;

    [Header("Interaction")]
    public float interactRadius = 3f;
    public float requiredStaySeconds = 2f;
    public string chatMessage = "want to play a game? give me some gold coins!";
    public float charDelay = 0.04f;
    public float scanInterval = 0.3f;

    [Header("References")]
    public ChatBubbleController chatBubble;

    private NavMeshAgent agent;
    private Vector3 networkPosition;
    private Quaternion networkRotation;
    private Coroutine wanderRoutine;
    private Coroutine syncRoutine;
    private Coroutine scanRoutine;

    private enum NPCState { Idle, Reserved, Chatting }
    private NPCState state = NPCState.Idle;
    private int reservedActor = -1;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        networkPosition = transform.position;
        networkRotation = transform.rotation;

        if (chatBubble == null)
            chatBubble = GetComponentInChildren<ChatBubbleController>(true);
    }

    void Start()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            EnsureAgentOnNavMesh();
            StartControl();
        }
        else
        {
            StopControl();
        }
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            state = NPCState.Idle;
            reservedActor = -1;
            EnsureAgentOnNavMesh();
            StartControl();
        }
        else
        {
            StopControl();
        }
    }

    private void EnsureAgentOnNavMesh()
    {
        if (agent == null) return;
        if (agent.isOnNavMesh) return;

        NavMeshHit hit;
        if (NavMesh.SamplePosition(transform.position, out hit, 20f, NavMesh.AllAreas))
        {
            transform.position = hit.position;
            agent.enabled = false;
            agent.enabled = true;
            if (!agent.isOnNavMesh) agent.Warp(hit.position);
            Debug.Log($"NPCWander: Warped agent to NavMesh at {hit.position}");
        }
        else
        {
            Debug.LogError("NPCWander: No NavMesh found near NPC!");
        }
    }

    // ★★★ Update 只做插值，不做玩家扫描 ★★★
    void Update()
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            transform.position = Vector3.Lerp(transform.position, networkPosition, Time.deltaTime * positionLerpSpeed);
            transform.rotation = Quaternion.Slerp(transform.rotation, networkRotation, Time.deltaTime * rotationLerpSpeed);
        }
    }

    // -------------------- Control --------------------
    private void StartControl()
    {
        if (agent != null)
        {
            agent.updatePosition = true;
            agent.updateRotation = true;
        }
        if (wanderRoutine == null) wanderRoutine = StartCoroutine(WanderRoutine());
        if (syncRoutine == null) syncRoutine = StartCoroutine(SyncRoutine());
        if (scanRoutine == null) scanRoutine = StartCoroutine(PlayerScanRoutine());
    }

    private void StopControl()
    {
        if (wanderRoutine != null) { StopCoroutine(wanderRoutine); wanderRoutine = null; }
        if (syncRoutine != null) { StopCoroutine(syncRoutine); syncRoutine = null; }
        if (scanRoutine != null) { StopCoroutine(scanRoutine); scanRoutine = null; }
        if (agent != null)
        {
            if (agent.isOnNavMesh) agent.ResetPath();
            agent.updatePosition = false;
            agent.updateRotation = false;
        }
    }

    private void StopWanderImmediate()
    {
        if (wanderRoutine != null) { StopCoroutine(wanderRoutine); wanderRoutine = null; }
        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }
    }

    private IEnumerator WanderRoutine()
    {
        while (true)
        {
            if (agent != null && agent.isOnNavMesh)
            {
                Vector3 dest = RandomNavSphere(transform.position, wanderRadius);
                agent.SetDestination(dest);
                while (agent.pathPending || agent.remainingDistance > arrivalThreshold)
                {
                    yield return null;
                }
            }
            else
            {
                yield return null;
                continue;
            }
            float wait = Random.Range(waitTimeMin, waitTimeMax);
            yield return new WaitForSeconds(wait);
        }
    }

    private IEnumerator SyncRoutine()
    {
        var wait = new WaitForSeconds(1f / sendRate);
        while (true)
        {
            if (PhotonNetwork.IsMasterClient)
            {
                photonView.RPC("RPC_ReceiveTransform", RpcTarget.OthersBuffered,
                    transform.position, transform.rotation);
            }
            yield return wait;
        }
    }

    [PunRPC]
    private void RPC_ReceiveTransform(Vector3 pos, Quaternion rot)
    {
        networkPosition = pos;
        networkRotation = rot;
    }

    // ★★★ 核心修复：用协程代替 Update，避免每帧重复启动 HandleApproach ★★★
    private IEnumerator PlayerScanRoutine()
    {
        // ★ 先让 NPC 走 5 秒再开始检测玩家（避免刚生成就误触发）
        yield return new WaitForSeconds(5f);

        var wait = new WaitForSeconds(scanInterval);

        while (true)
        {
            if (state == NPCState.Idle)
            {
                Transform nearest = FindNearestPlayerWithin(interactRadius);
                if (nearest != null)
                {
                    PhotonView pv = nearest.GetComponent<PhotonView>();
                    int actor = (pv != null && pv.Owner != null) ? pv.Owner.ActorNumber : -1;

                    if (actor != -1)
                    {
                        float dist = Vector3.Distance(transform.position, nearest.position);
                        Debug.Log($"[NPC] 检测到玩家 Actor={actor} 距离={dist:F1}m，开始 HandleApproach");

                        // ★ yield return 等待 HandleApproach 完成后再继续扫描
                        //   这样不会重复启动多个协程
                        yield return StartCoroutine(HandleApproach(actor));
                    }
                }
            }
            yield return wait;
        }
    }

    // -------------------- Interaction --------------------
    private Transform FindNearestPlayerWithin(float radius)
    {
        Transform best = null;
        float bestDist = float.MaxValue;
        foreach (var kv in PlayerRegistration.PlayerMap)
        {
            Transform t = kv.Value;
            if (t == null) continue;
            float d = Vector3.Distance(transform.position, t.position);

            // ★ 加 debug 帮助排查
            // Debug.Log($"[NPC] 检查玩家 Actor={kv.Key} 距离={d:F1} 半径={radius}");

            if (d <= radius && d < bestDist)
            {
                best = t;
                bestDist = d;
            }
        }
        return best;
    }

    private IEnumerator HandleApproach(int actorNumber)
    {
        if (state != NPCState.Idle) yield break;
        if (!PlayerRegistration.PlayerMap.ContainsKey(actorNumber)) yield break;

        // ★ 再次确认距离（防止扫描间隔内玩家已离开）
        Transform check = PlayerRegistration.PlayerMap[actorNumber];
        if (check == null) yield break;
        float checkDist = Vector3.Distance(transform.position, check.position);
        if (checkDist > interactRadius)
        {
            Debug.Log($"[NPC] HandleApproach 取消：玩家已离开（距离={checkDist:F1}）");
            yield break;
        }

        state = NPCState.Reserved;
        reservedActor = actorNumber;
        StopWanderImmediate();

        Debug.Log($"[NPC] 状态 -> Reserved，等待 {requiredStaySeconds}s");

        float elapsed = 0f;
        while (elapsed < requiredStaySeconds)
        {
            if (!PlayerRegistration.PlayerMap.ContainsKey(actorNumber))
            { CancelReserved(); yield break; }

            Transform t = PlayerRegistration.PlayerMap[actorNumber];
            if (t == null)
            { CancelReserved(); yield break; }

            float d = Vector3.Distance(transform.position, t.position);
            if (d > interactRadius)
            {
                Debug.Log($"[NPC] 玩家在等待期间离开（距离={d:F1}），取消");
                CancelReserved();
                yield break;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        // 成功
        state = NPCState.Chatting;
        Player targetPlayer = PhotonNetwork.CurrentRoom.GetPlayer(actorNumber);
        if (targetPlayer == null)
        { ResetAfterInteraction(); yield break; }

        Debug.Log($"[NPC] 状态 -> Chatting，向 Actor={actorNumber} 发送 ShowChat RPC");
        photonView.RPC("RPC_ShowChat_LocalOnly", targetPlayer, chatMessage, charDelay);

        // 持续监测
        while (true)
        {
            if (!PlayerRegistration.PlayerMap.ContainsKey(actorNumber))
            {
                photonView.RPC("RPC_HideChat_LocalOnly", targetPlayer);
                ResetAfterInteraction();
                yield break;
            }
            Transform t = PlayerRegistration.PlayerMap[actorNumber];
            if (t == null)
            {
                photonView.RPC("RPC_HideChat_LocalOnly", targetPlayer);
                ResetAfterInteraction();
                yield break;
            }
            float d = Vector3.Distance(transform.position, t.position);
            if (d > interactRadius)
            {
                Debug.Log($"[NPC] 玩家离开（距离={d:F1}），隐藏 chat 并恢复漫游");
                photonView.RPC("RPC_HideChat_LocalOnly", targetPlayer);
                ResetAfterInteraction();
                yield break;
            }
            yield return null;
        }
    }

    private void CancelReserved()
    {
        Debug.Log("[NPC] CancelReserved -> 恢复漫游");
        Player p = PhotonNetwork.CurrentRoom.GetPlayer(reservedActor);
        if (p != null)
        {
            photonView.RPC("RPC_HideChat_LocalOnly", p);
        }
        ResetAfterInteraction();
    }

    private void ResetAfterInteraction()
    {
        reservedActor = -1;
        state = NPCState.Idle;
        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = false;
        }
        if (wanderRoutine == null) wanderRoutine = StartCoroutine(WanderRoutine());
        Debug.Log("[NPC] 状态 -> Idle，恢复漫游");
    }

    // -------------------- RPC --------------------
    [PunRPC]
    private void RPC_ShowChat_LocalOnly(string msg, float perCharDelay, PhotonMessageInfo info)
    {
        if (chatBubble == null)
            chatBubble = GetComponentInChildren<ChatBubbleController>(true);

        if (chatBubble == null)
        {
            Debug.LogWarning("ChatBubbleController 未绑定 (RPC_ShowChat_LocalOnly)");
            return;
        }
        chatBubble.ShowMessage(msg, perCharDelay);
    }

    [PunRPC]
    private void RPC_HideChat_LocalOnly(PhotonMessageInfo info)
    {
        if (chatBubble == null) return;
        chatBubble.HideWithStop();
    }

    // -------------------- Utility --------------------
    public static Vector3 RandomNavSphere(Vector3 origin, float dist)
    {
        Vector3 randDirection = Random.insideUnitSphere * dist;
        randDirection += origin;
        NavMeshHit navHit;
        if (NavMesh.SamplePosition(randDirection, out navHit, dist, NavMesh.AllAreas))
        {
            return navHit.position;
        }
        return origin;
    }
}
*/

/*
using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using Photon.Pun;
using Photon.Realtime;

public class NPCWander : MonoBehaviourPunCallbacks
{
    [Header("Wander Settings")]
    public float wanderRadius = 8f;
    public float arrivalThreshold = 0.5f;
    public float waitTimeMin = 1f;
    public float waitTimeMax = 3f;

    [Header("Network & Smoothing")]
    public float positionLerpSpeed = 8f;
    public float rotationLerpSpeed = 8f;
    public float sendRate = 10f;

    [Header("Interaction")]
    public float interactRadius = 3f;
    public float requiredStaySeconds = 2f;
    public string chatMessage = "want to play a game? give me some gold coins!";
    public float charDelay = 0.04f;
    public float scanInterval = 0.2f; // ★ 扫描间隔（不再每帧扫描）

    [Header("References")]
    public ChatBubbleController chatBubble;

    private NavMeshAgent agent;
    private Vector3 networkPosition;
    private Quaternion networkRotation;
    private Coroutine wanderRoutine;
    private Coroutine syncRoutine;
    private Coroutine scanRoutine; // ★ 新增：玩家扫描协程

    private enum NPCState { Idle, Reserved, Chatting }
    private NPCState state = NPCState.Idle;
    private int reservedActor = -1;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        networkPosition = transform.position;
        networkRotation = transform.rotation;

        // ★ 自动查找 chatBubble
        if (chatBubble == null)
            chatBubble = GetComponentInChildren<ChatBubbleController>(true);
    }

    void Start()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            EnsureAgentOnNavMesh(); // ★ 先确保在 NavMesh 上
            StartControl();
        }
        else
        {
            StopControl();
        }
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            state = NPCState.Idle;
            reservedActor = -1;
            EnsureAgentOnNavMesh();
            StartControl();
        }
        else
        {
            StopControl();
        }
    }

    // ★ 安全地把 agent 放到 NavMesh
    private void EnsureAgentOnNavMesh()
    {
        if (agent == null) return;
        if (agent.isOnNavMesh) return;

        NavMeshHit hit;
        if (NavMesh.SamplePosition(transform.position, out hit, 20f, NavMesh.AllAreas))
        {
            transform.position = hit.position;
            agent.enabled = false;
            agent.enabled = true;
            if (!agent.isOnNavMesh) agent.Warp(hit.position);
            Debug.Log($"NPCWander: Warped agent to NavMesh at {hit.position}");
        }
        else
        {
            Debug.LogError("NPCWander: No NavMesh found near NPC!");
        }
    }

    void Update()
    {
        // 非 Master 客户端仅做插值
        if (!PhotonNetwork.IsMasterClient)
        {
            transform.position = Vector3.Lerp(transform.position, networkPosition, Time.deltaTime * positionLerpSpeed);
            transform.rotation = Quaternion.Slerp(transform.rotation, networkRotation, Time.deltaTime * rotationLerpSpeed);
        }
        // ★ Master 的玩家检测移到 ScanRoutine 协程里，不在 Update 里做
    }

    // -------------------- Control --------------------
    private void StartControl()
    {
        if (agent != null)
        {
            agent.updatePosition = true;
            agent.updateRotation = true;
        }
        if (wanderRoutine == null) wanderRoutine = StartCoroutine(WanderRoutine());
        if (syncRoutine == null) syncRoutine = StartCoroutine(SyncRoutine());
        if (scanRoutine == null) scanRoutine = StartCoroutine(PlayerScanRoutine()); // ★ 启动扫描
    }

    private void StopControl()
    {
        if (wanderRoutine != null) { StopCoroutine(wanderRoutine); wanderRoutine = null; }
        if (syncRoutine != null) { StopCoroutine(syncRoutine); syncRoutine = null; }
        if (scanRoutine != null) { StopCoroutine(scanRoutine); scanRoutine = null; }
        if (agent != null)
        {
            if (agent.isOnNavMesh) agent.ResetPath();
            agent.updatePosition = false;
            agent.updateRotation = false;
        }
    }

    // ★ 安全停止 wander
    private void StopWanderImmediate()
    {
        if (wanderRoutine != null) { StopCoroutine(wanderRoutine); wanderRoutine = null; }
        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }
    }

    private IEnumerator WanderRoutine()
    {
        while (true)
        {
            if (agent != null && agent.isOnNavMesh)
            {
                Vector3 dest = RandomNavSphere(transform.position, wanderRadius);
                agent.SetDestination(dest);

                // 等待到达
                while (agent.pathPending || agent.remainingDistance > arrivalThreshold)
                {
                    yield return null;
                }
            }
            else
            {
                yield return null;
                continue;
            }

            float wait = Random.Range(waitTimeMin, waitTimeMax);
            yield return new WaitForSeconds(wait);
        }
    }

    private IEnumerator SyncRoutine()
    {
        var wait = new WaitForSeconds(1f / sendRate);
        while (true)
        {
            if (PhotonNetwork.IsMasterClient)
            {
                photonView.RPC("RPC_ReceiveTransform", RpcTarget.OthersBuffered, transform.position, transform.rotation);
            }
            yield return wait;
        }
    }

    [PunRPC]
    private void RPC_ReceiveTransform(Vector3 pos, Quaternion rot)
    {
        networkPosition = pos;
        networkRotation = rot;
    }

    // ★ ==================== 核心修复：用协程定期扫描，而不是 Update 每帧扫描 ====================
    private IEnumerator PlayerScanRoutine()
    {
        // ★ 启动后先等 3 秒，让 NPC 先走一会儿再开始检测玩家
        yield return new WaitForSeconds(3f);

        var wait = new WaitForSeconds(scanInterval);

        while (true)
        {
            // 只在 Idle 时才扫描
            if (state == NPCState.Idle)
            {
                Transform nearest = FindNearestPlayerWithin(interactRadius);
                if (nearest != null)
                {
                    PhotonView pv = nearest.GetComponent<PhotonView>();
                    int actor = (pv != null && pv.Owner != null) ? pv.Owner.ActorNumber : -1;
                    if (actor != -1)
                    {
                        // ★ 直接在这里 yield 等待 HandleApproach 完成
                        // 这样不会重复启动多个协程
                        yield return StartCoroutine(HandleApproach(actor));
                    }
                }
            }
            yield return wait;
        }
    }

    // -------------------- Interaction --------------------
    private Transform FindNearestPlayerWithin(float radius)
    {
        Transform best = null;
        float bestDist = float.MaxValue;
        foreach (var kv in PlayerRegistration.PlayerMap)
        {
            Transform t = kv.Value;
            if (t == null) continue;
            float d = Vector3.Distance(transform.position, t.position);
            if (d <= radius && d < bestDist)
            {
                best = t;
                bestDist = d;
            }
        }
        return best;
    }

    private IEnumerator HandleApproach(int actorNumber)
    {
        if (state != NPCState.Idle) yield break;
        if (!PlayerRegistration.PlayerMap.ContainsKey(actorNumber)) yield break;

        state = NPCState.Reserved;
        reservedActor = actorNumber;

        // ★ 安全停止
        StopWanderImmediate();

        // 等待 requiredStaySeconds，期间持续检查玩家是否还在范围内
        float elapsed = 0f;
        while (elapsed < requiredStaySeconds)
        {
            if (!PlayerRegistration.PlayerMap.ContainsKey(actorNumber))
            { CancelReserved(); yield break; }

            Transform t = PlayerRegistration.PlayerMap[actorNumber];
            if (t == null)
            { CancelReserved(); yield break; }

            float d = Vector3.Distance(transform.position, t.position);
            if (d > interactRadius)
            { CancelReserved(); yield break; }

            elapsed += Time.deltaTime;
            yield return null;
        }

        // 成功：只通知目标玩家显示 chat
        state = NPCState.Chatting;
        Player targetPlayer = PhotonNetwork.CurrentRoom.GetPlayer(actorNumber);
        if (targetPlayer == null)
        { ResetAfterInteraction(); yield break; }

        photonView.RPC("RPC_ShowChat_LocalOnly", targetPlayer, chatMessage, charDelay);

        // 持续监测，玩家离开则取消
        while (true)
        {
            if (!PlayerRegistration.PlayerMap.ContainsKey(actorNumber))
            {
                photonView.RPC("RPC_HideChat_LocalOnly", targetPlayer);
                ResetAfterInteraction();
                yield break;
            }
            Transform t = PlayerRegistration.PlayerMap[actorNumber];
            if (t == null)
            {
                photonView.RPC("RPC_HideChat_LocalOnly", targetPlayer);
                ResetAfterInteraction();
                yield break;
            }
            float d = Vector3.Distance(transform.position, t.position);
            if (d > interactRadius)
            {
                photonView.RPC("RPC_HideChat_LocalOnly", targetPlayer);
                ResetAfterInteraction();
                yield break;
            }
            yield return null;
        }
    }

    private void CancelReserved()
    {
        Player p = PhotonNetwork.CurrentRoom.GetPlayer(reservedActor);
        if (p != null)
        {
            photonView.RPC("RPC_HideChat_LocalOnly", p);
        }
        ResetAfterInteraction();
    }

    private void ResetAfterInteraction()
    {
        reservedActor = -1;
        state = NPCState.Idle;
        // ★ 安全恢复
        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = false;
        }
        if (wanderRoutine == null) wanderRoutine = StartCoroutine(WanderRoutine());
    }

    // -------------------- RPC --------------------
    [PunRPC]
    private void RPC_ShowChat_LocalOnly(string msg, float perCharDelay, PhotonMessageInfo info)
    {
        if (chatBubble == null)
            chatBubble = GetComponentInChildren<ChatBubbleController>(true);

        if (chatBubble == null)
        {
            Debug.LogWarning("ChatBubbleController 未绑定 (RPC_ShowChat_LocalOnly)");
            return;
        }
        chatBubble.ShowMessage(msg, perCharDelay);
    }

    [PunRPC]
    private void RPC_HideChat_LocalOnly(PhotonMessageInfo info)
    {
        if (chatBubble == null) return;
        chatBubble.HideWithStop();
    }

    // -------------------- Utility --------------------
    public static Vector3 RandomNavSphere(Vector3 origin, float dist)
    {
        Vector3 randDirection = Random.insideUnitSphere * dist;
        randDirection += origin;
        NavMeshHit navHit;
        if (NavMesh.SamplePosition(randDirection, out navHit, dist, NavMesh.AllAreas))
        {
            return navHit.position;
        }
        return origin;
    }
}
*/

/*
using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using Photon.Pun;
using Photon.Realtime;

public class NPCWander : MonoBehaviourPunCallbacks
{
    [Header("Wander Settings")]
    public float wanderRadius = 8f;
    public float arrivalThreshold = 0.5f;
    public float waitTimeMin = 1f;
    public float waitTimeMax = 3f;

    [Header("Network & Smoothing")]
    public float positionLerpSpeed = 8f;
    public float rotationLerpSpeed = 8f;
    public float sendRate = 10f;

    [Header("Interaction")]
    public float interactRadius = 3f;
    public float requiredStaySeconds = 2f;
    public string chatMessage = "want to play a game? give me some gold coins!";
    public float charDelay = 0.04f;

    [Header("References")]
    public ChatBubbleController chatBubble;

    private NavMeshAgent agent;
    private Vector3 networkPosition;
    private Quaternion networkRotation;
    private Coroutine wanderRoutine;
    private Coroutine syncRoutine;

    private enum NPCState { Idle, Reserved, Chatting }
    private NPCState state = NPCState.Idle;
    private int reservedActor = -1;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        networkPosition = transform.position;
        networkRotation = transform.rotation;

        // ★ 自动查找 chatBubble（包括 inactive 子对象）
        if (chatBubble == null)
        {
            chatBubble = GetComponentInChildren<ChatBubbleController>(true);
        }
    }

    void Start()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            // ★ 先确保 agent 在 NavMesh 上
            EnsureAgentOnNavMesh();
            StartControl();
        }
        else
        {
            StopControl();
        }
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            state = NPCState.Idle;
            reservedActor = -1;
            EnsureAgentOnNavMesh();
            StartControl();
        }
        else
        {
            StopControl();
        }
    }

    // ★ 新增：安全地把 agent 放到 NavMesh 上
    private void EnsureAgentOnNavMesh()
    {
        if (agent == null) return;
        if (agent.isOnNavMesh) return;

        NavMeshHit hit;
        if (NavMesh.SamplePosition(transform.position, out hit, 10f, NavMesh.AllAreas))
        {
            transform.position = hit.position;
            agent.enabled = false;
            agent.enabled = true; // 重新启用让它重新定位
            if (!agent.isOnNavMesh)
            {
                agent.Warp(hit.position);
            }
            Debug.Log($"NPCWander: Warped agent to NavMesh at {hit.position}");
        }
        else
        {
            Debug.LogError("NPCWander: No NavMesh found near NPC! Please bake NavMesh.");
        }
    }

    void Update()
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            transform.position = Vector3.Lerp(transform.position, networkPosition, Time.deltaTime * positionLerpSpeed);
            transform.rotation = Quaternion.Slerp(transform.rotation, networkRotation, Time.deltaTime * rotationLerpSpeed);
            return;
        }

        if (state == NPCState.Idle)
        {
            Transform t = FindNearestPlayerWithin(interactRadius);
            if (t != null)
            {
                PhotonView pv = t.GetComponent<PhotonView>();
                int actor = (pv != null && pv.Owner != null) ? pv.Owner.ActorNumber : -1;
                if (actor != -1)
                {
                    StartCoroutine(HandleApproach(actor));
                }
            }
        }
    }

    // -------------------- Control --------------------
    private void StartControl()
    {
        if (agent != null)
        {
            agent.updatePosition = true;
            agent.updateRotation = true;
        }
        if (wanderRoutine == null) wanderRoutine = StartCoroutine(WanderRoutine());
        if (syncRoutine == null) syncRoutine = StartCoroutine(SyncRoutine());
    }

    private void StopControl()
    {
        if (wanderRoutine != null) { StopCoroutine(wanderRoutine); wanderRoutine = null; }
        if (syncRoutine != null) { StopCoroutine(syncRoutine); syncRoutine = null; }
        if (agent != null)
        {
            // ★ 加 isOnNavMesh 检查
            if (agent.isOnNavMesh) agent.ResetPath();
            agent.updatePosition = false;
            agent.updateRotation = false;
        }
    }

    // ★ 修复：所有 agent 操作前检查 isOnNavMesh
    private void StopWanderImmediate()
    {
        if (wanderRoutine != null) { StopCoroutine(wanderRoutine); wanderRoutine = null; }
        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }
    }

    private IEnumerator WanderRoutine()
    {
        while (true)
        {
            // ★ 加 isOnNavMesh 检查
            if (agent != null && agent.isOnNavMesh)
            {
                Vector3 dest = RandomNavSphere(transform.position, wanderRadius);
                agent.SetDestination(dest);
                while (agent.pathPending || agent.remainingDistance > arrivalThreshold)
                {
                    yield return null;
                }
            }
            else
            {
                // agent 不在 NavMesh，等一帧再试
                yield return null;
                continue;
            }
            float wait = Random.Range(waitTimeMin, waitTimeMax);
            yield return new WaitForSeconds(wait);
        }
    }

    private IEnumerator SyncRoutine()
    {
        var wait = new WaitForSeconds(1f / sendRate);
        while (true)
        {
            if (PhotonNetwork.IsMasterClient)
            {
                photonView.RPC("RPC_ReceiveTransform", RpcTarget.OthersBuffered, transform.position, transform.rotation);
            }
            yield return wait;
        }
    }

    [PunRPC]
    private void RPC_ReceiveTransform(Vector3 pos, Quaternion rot)
    {
        networkPosition = pos;
        networkRotation = rot;
    }

    // -------------------- Interaction --------------------
    private Transform FindNearestPlayerWithin(float radius)
    {
        Transform best = null;
        float bestDist = float.MaxValue;
        foreach (var kv in PlayerRegistration.PlayerMap)
        {
            Transform t = kv.Value;
            if (t == null) continue;
            float d = Vector3.Distance(transform.position, t.position);
            if (d <= radius && d < bestDist)
            {
                best = t;
                bestDist = d;
            }
        }
        return best;
    }

    private IEnumerator HandleApproach(int actorNumber)
    {
        if (state != NPCState.Idle) yield break;
        if (!PlayerRegistration.PlayerMap.ContainsKey(actorNumber)) yield break;

        state = NPCState.Reserved;
        reservedActor = actorNumber;

        // ★ 使用安全版本
        StopWanderImmediate();

        float elapsed = 0f;
        while (elapsed < requiredStaySeconds)
        {
            if (!PlayerRegistration.PlayerMap.ContainsKey(actorNumber))
            { CancelReserved(); yield break; }

            Transform t = PlayerRegistration.PlayerMap[actorNumber];
            if (t == null)
            { CancelReserved(); yield break; }

            float d = Vector3.Distance(transform.position, t.position);
            if (d > interactRadius)
            { CancelReserved(); yield break; }

            elapsed += Time.deltaTime;
            yield return null;
        }

        state = NPCState.Chatting;
        Player targetPlayer = PhotonNetwork.CurrentRoom.GetPlayer(actorNumber);
        if (targetPlayer != null)
        {
            photonView.RPC("RPC_ShowChat_LocalOnly", targetPlayer, chatMessage, charDelay);
        }
        else
        { ResetAfterInteraction(); yield break; }

        while (true)
        {
            if (!PlayerRegistration.PlayerMap.ContainsKey(actorNumber))
            {
                photonView.RPC("RPC_HideChat_LocalOnly", targetPlayer);
                ResetAfterInteraction();
                yield break;
            }
            Transform t = PlayerRegistration.PlayerMap[actorNumber];
            if (t == null)
            {
                photonView.RPC("RPC_HideChat_LocalOnly", targetPlayer);
                ResetAfterInteraction();
                yield break;
            }
            float d = Vector3.Distance(transform.position, t.position);
            if (d > interactRadius)
            {
                photonView.RPC("RPC_HideChat_LocalOnly", targetPlayer);
                ResetAfterInteraction();
                yield break;
            }
            yield return null;
        }
    }

    private void CancelReserved()
    {
        Player p = PhotonNetwork.CurrentRoom.GetPlayer(reservedActor);
        if (p != null)
        {
            photonView.RPC("RPC_HideChat_LocalOnly", p);
        }
        ResetAfterInteraction();
    }

    private void ResetAfterInteraction()
    {
        reservedActor = -1;
        state = NPCState.Idle;
        // ★ 安全恢复 agent
        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = false;
        }
        if (wanderRoutine == null) wanderRoutine = StartCoroutine(WanderRoutine());
    }

    // -------------------- RPC --------------------
    [PunRPC]
    private void RPC_ShowChat_LocalOnly(string msg, float perCharDelay, PhotonMessageInfo info)
    {
        // ★ 自动查找 fallback
        if (chatBubble == null)
            chatBubble = GetComponentInChildren<ChatBubbleController>(true);

        if (chatBubble == null)
        {
            Debug.LogWarning("ChatBubbleController 未绑定 (RPC_ShowChat_LocalOnly)");
            return;
        }
        chatBubble.ShowMessage(msg, perCharDelay);
    }

    [PunRPC]
    private void RPC_HideChat_LocalOnly(PhotonMessageInfo info)
    {
        if (chatBubble == null) return;
        chatBubble.HideWithStop();
    }

    // -------------------- Utility --------------------
    public static Vector3 RandomNavSphere(Vector3 origin, float dist)
    {
        Vector3 randDirection = Random.insideUnitSphere * dist;
        randDirection += origin;
        NavMeshHit navHit;
        if (NavMesh.SamplePosition(randDirection, out navHit, dist, NavMesh.AllAreas))
        {
            return navHit.position;
        }
        return origin;
    }
}

*/

/*
using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using Photon.Pun;
using Photon.Realtime;

public class NPCWander : MonoBehaviourPunCallbacks
{
    [Header("Wander Settings")]
    public float wanderRadius = 8f;
    public float arrivalThreshold = 0.5f;
    public float waitTimeMin = 1f;
    public float waitTimeMax = 3f;

    [Header("Network & Smoothing")]
    public float positionLerpSpeed = 8f;
    public float rotationLerpSpeed = 8f;
    public float sendRate = 10f; // master broadcast n times per sec

    [Header("Interaction")]
    public float interactRadius = 3f;
    public float requiredStaySeconds = 2f;
    public string chatMessage = "want to play a game? give me some gold coins!";
    public float charDelay = 0.04f;

    [Header("References")]
    public ChatBubbleController chatBubble; // 在 inspector 绑定（用于目标玩家本地显示）

    private NavMeshAgent agent;

    // network smoothing targets
    private Vector3 networkPosition;
    private Quaternion networkRotation;

    // coroutines
    private Coroutine wanderRoutine;
    private Coroutine syncRoutine;

    // interaction state
    private enum NPCState { Idle, Reserved, Chatting }
    private NPCState state = NPCState.Idle;
    private int reservedActor = -1;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        networkPosition = transform.position;
        networkRotation = transform.rotation;
    }

    void Start()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            StartControl(); // 启动 wander 与 SyncRoutine（保留原行为）[2]
        }
        else
        {
            StopControl();
        }
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            // 简单重置并接管
            state = NPCState.Idle;
            reservedActor = -1;
            StartControl();
        }
        else
        {
            StopControl();
        }
    }

    void Update()
    {
        // 非 Master 客户端仅做插值
        if (!PhotonNetwork.IsMasterClient)
        {
            transform.position = Vector3.Lerp(transform.position, networkPosition, Time.deltaTime * positionLerpSpeed);
            transform.rotation = Quaternion.Slerp(transform.rotation, networkRotation, Time.deltaTime * rotationLerpSpeed);
            return;
        }

        // Master: 当处于 Idle 时搜索玩家进入 interactRadius 并开始锁定流程
        if (state == NPCState.Idle)
        {
            Transform t = FindNearestPlayerWithin(interactRadius);
            if (t != null)
            {
                PhotonView pv = t.GetComponent<PhotonView>();
                int actor = (pv != null && pv.Owner != null) ? pv.Owner.ActorNumber : -1;
                if (actor != -1)
                {
                    StartCoroutine(HandleApproach(actor));
                }
            }
        }
        // 其它状态（Reserved/Chatting）由协程内部处理
    }

    // -------------------- 复用已有 StartControl/StopControl/SyncRoutine/WanderRoutine --------------------
    private void StartControl()
    {
        if (agent != null) { agent.updatePosition = true; agent.updateRotation = true; }
        if (wanderRoutine == null) wanderRoutine = StartCoroutine(WanderRoutine());
        if (syncRoutine == null) syncRoutine = StartCoroutine(SyncRoutine());
    }

    private void StopControl()
    {
        if (wanderRoutine != null) { StopCoroutine(wanderRoutine); wanderRoutine = null; }
        if (syncRoutine != null) { StopCoroutine(syncRoutine); syncRoutine = null; }
        if (agent != null) { agent.ResetPath(); agent.updatePosition = false; agent.updateRotation = false; }
    }

    private IEnumerator WanderRoutine()
    {
        while (true)
        {
            Vector3 dest = RandomNavSphere(transform.position, wanderRadius);
            if (agent != null && agent.isOnNavMesh)
            {
                agent.SetDestination(dest);
                while (agent.pathPending || agent.remainingDistance > arrivalThreshold)
                {
                    yield return null;
                }
            }
            float wait = Random.Range(waitTimeMin, waitTimeMax);
            yield return new WaitForSeconds(wait);
        }
    }

    private IEnumerator SyncRoutine()
    {
        var wait = new WaitForSeconds(1f / sendRate);
        while (true)
        {
            if (PhotonNetwork.IsMasterClient)
            {
                photonView.RPC("RPC_ReceiveTransform", RpcTarget.OthersBuffered, transform.position, transform.rotation);
            }
            yield return wait;
        }
    }

    [PunRPC]
    private void RPC_ReceiveTransform(Vector3 pos, Quaternion rot)
    {
        networkPosition = pos;
        networkRotation = rot;
    }

    // -------------------- Interaction: Master-only 协程处理 --------------------
    private Transform FindNearestPlayerWithin(float radius)
    {
        Transform best = null;
        float bestDist = float.MaxValue;
        foreach (var kv in PlayerRegistration.PlayerMap)
        {
            Transform t = kv.Value;
            if (t == null) continue;
            float d = Vector3.Distance(transform.position, t.position);
            if (d <= radius && d < bestDist)
            {
                best = t;
                bestDist = d;
            }
        }
        return best;
    }

    private IEnumerator HandleApproach(int actorNumber)
    {
        if (state != NPCState.Idle) yield break;
        if (!PlayerRegistration.PlayerMap.ContainsKey(actorNumber)) yield break;

        state = NPCState.Reserved;
        reservedActor = actorNumber;

        // 停止 wander（但维持 syncRoutine 运作以广播停下的位置）
        if (wanderRoutine != null) { StopCoroutine(wanderRoutine); wanderRoutine = null; }
        if (agent != null)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }

        float elapsed = 0f;
        while (elapsed < requiredStaySeconds)
        {
            if (!PlayerRegistration.PlayerMap.ContainsKey(actorNumber))
            {
                CancelReserved();
                yield break;
            }
            Transform t = PlayerRegistration.PlayerMap[actorNumber];
            if (t == null)
            {
                CancelReserved();
                yield break;
            }
            float d = Vector3.Distance(transform.position, t.position);
            if (d > interactRadius)
            {
                CancelReserved();
                yield break;
            }
            elapsed += Time.deltaTime;
            yield return null;
        }

        // 成功：通知目标玩家本地显示 chat（仅目标玩家收到）
        state = NPCState.Chatting;
        Player targetPlayer = PhotonNetwork.CurrentRoom.GetPlayer(actorNumber);
        if (targetPlayer != null)
        {
            photonView.RPC("RPC_ShowChat_LocalOnly", targetPlayer, chatMessage, charDelay);
        }
        else
        {
            ResetAfterInteraction();
            yield break;
        }

        // 持续监测目标玩家是否离开，离开则取消并隐藏
        while (true)
        {
            if (!PlayerRegistration.PlayerMap.ContainsKey(actorNumber))
            {
                // 玩家断线或移除
                photonView.RPC("RPC_HideChat_LocalOnly", targetPlayer);
                ResetAfterInteraction();
                yield break;
            }
            Transform t = PlayerRegistration.PlayerMap[actorNumber];
            if (t == null)
            {
                photonView.RPC("RPC_HideChat_LocalOnly", targetPlayer);
                ResetAfterInteraction();
                yield break;
            }
            float d = Vector3.Distance(transform.position, t.position);
            if (d > interactRadius)
            {
                photonView.RPC("RPC_HideChat_LocalOnly", targetPlayer);
                ResetAfterInteraction();
                yield break;
            }
            yield return null;
        }
    }

    private void CancelReserved()
    {
        Player p = PhotonNetwork.CurrentRoom.GetPlayer(reservedActor);
        if (p != null)
        {
            photonView.RPC("RPC_HideChat_LocalOnly", p);
        }
        ResetAfterInteraction();
    }

    private void ResetAfterInteraction()
    {
        reservedActor = -1;
        state = NPCState.Idle;
        if (agent != null)
        {
            agent.isStopped = false;
        }
        if (wanderRoutine == null) wanderRoutine = StartCoroutine(WanderRoutine());
    }

    // -------------------- RPC: 仅目标玩家本地显示/隐藏 chat --------------------
    [PunRPC]
    private void RPC_ShowChat_LocalOnly(string msg, float perCharDelay, PhotonMessageInfo info)
    {
        if (chatBubble == null)
        {
            Debug.LogWarning("ChatBubbleController 未绑定 (RPC_ShowChat_LocalOnly)");
            return;
        }
        chatBubble.ShowMessage(msg, perCharDelay);
    }

    [PunRPC]
    private void RPC_HideChat_LocalOnly(PhotonMessageInfo info)
    {
        if (chatBubble == null) return;
        chatBubble.HideWithStop();
    }

    // -------------------- 工具: RandomNavSphere --------------------
    public static Vector3 RandomNavSphere(Vector3 origin, float dist)
    {
        Vector3 randDirection = Random.insideUnitSphere * dist;
        randDirection += origin;
        NavMeshHit navHit;
        if (NavMesh.SamplePosition(randDirection, out navHit, dist, NavMesh.AllAreas))
        {
            return navHit.position;
        }
        return origin;
    }
}
*/
/*
using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using Photon.Pun;

public class NPCWander : MonoBehaviourPunCallbacks
{
    [Header("Wander Settings")]
    public float wanderRadius = 8f;
    public float arrivalThreshold = 0.5f;
    public float waitTimeMin = 1f;
    public float waitTimeMax = 3f;

    [Header("Network & Smoothing")]
    public float positionLerpSpeed = 8f;
    public float rotationLerpSpeed = 8f;
    public float sendRate = 10f; // master broadcast n times per sec

    private NavMeshAgent agent;

    // 接收端平滑目标
    private Vector3 networkPosition;
    private Quaternion networkRotation;

    // 用于控制 Master 的协程
    private Coroutine wanderRoutine;
    private Coroutine syncRoutine;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

        networkPosition = transform.position;
        networkRotation = transform.rotation;
    }

    void Start()
    {
        // if is masterclient, then start control and broadcast
        if (PhotonNetwork.IsMasterClient)
        {
            StartControl();
        }
        else
        {
            StopControl();
        }
    }

    public override void OnMasterClientSwitched(Photon.Realtime.Player newMasterClient)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            StartControl();
        }
        else
        {
            StopControl();
        }
    }

    void Update()
    {
        // 非 Master 客户端做插值平滑
        if (!PhotonNetwork.IsMasterClient)
        {
            transform.position = Vector3.Lerp(transform.position, networkPosition, Time.deltaTime * positionLerpSpeed);
            transform.rotation = Quaternion.Slerp(transform.rotation, networkRotation, Time.deltaTime * rotationLerpSpeed);
        }
        else
        {
            // MasterClient 由 NavMeshAgent 控制 transform，通常不需要额外处理
        }
    }

    private void StartControl()
    {
        // 启用 NavMeshAgent 移动并启动 wander 与同步协程
        if (agent != null)
        {
            agent.updatePosition = true;
            agent.updateRotation = true;
        }

        if (wanderRoutine == null) wanderRoutine = StartCoroutine(WanderRoutine());
        if (syncRoutine == null) syncRoutine = StartCoroutine(SyncRoutine());
    }

    private void StopControl()
    {
        // 停止 Master 专有协程，禁用 agent 的更新以避免和插值冲突
        if (wanderRoutine != null) { StopCoroutine(wanderRoutine); wanderRoutine = null; }
        if (syncRoutine != null) { StopCoroutine(syncRoutine); syncRoutine = null; }

        if (agent != null)
        {
            agent.ResetPath();
            agent.updatePosition = false;
            agent.updateRotation = false;
        }
    }

    private IEnumerator WanderRoutine()
    {
        while (true)
        {
            Vector3 dest = RandomNavSphere(transform.position, wanderRadius);
            if (agent != null && agent.isOnNavMesh)
            {
                agent.SetDestination(dest);

                // 等待移动到达
                while (agent.pathPending || agent.remainingDistance > arrivalThreshold)
                {
                    yield return null;
                }
            }

            // 停留一段随机时间
            float wait = Random.Range(waitTimeMin, waitTimeMax);
            yield return new WaitForSeconds(wait);
        }
    }

    private IEnumerator SyncRoutine()
    {
        var wait = new WaitForSeconds(1f / sendRate);
        while (true)
        {
            // 仅 MasterClient 广播 transform 给其他客户端
            if (PhotonNetwork.IsMasterClient)
            {
                // 使用 Buffered RPC，这样后加入的玩家也能马上收到最近状态
                photonView.RPC("RPC_ReceiveTransform", RpcTarget.OthersBuffered, transform.position, transform.rotation);
            }
            yield return wait;
        }
    }

    [PunRPC]
    private void RPC_ReceiveTransform(Vector3 pos, Quaternion rot)
    {
        // 接收端（非 Master）保存网络目标数据用于插值
        networkPosition = pos;
        networkRotation = rot;
    }

    // 工具：在 NavMesh 上随机点
    public static Vector3 RandomNavSphere(Vector3 origin, float dist)
    {
        Vector3 randDirection = Random.insideUnitSphere * dist;
        randDirection += origin;
        NavMeshHit navHit;
        if (NavMesh.SamplePosition(randDirection, out navHit, dist, NavMesh.AllAreas))
        {
            return navHit.position;
        }
        return origin;
    }
}
*/
