using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Photon.Pun;
using Photon.Realtime;

public class WhackAMoleNPC : MonoBehaviourPunCallbacks
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
    public string chatMessage = "Press F to play";
    public float charDelay = 0.04f;
    public float scanInterval = 0.2f;
    public KeyCode miniGameKey = KeyCode.F;
    public string miniGameSceneName = "WhackAMole";

    [Header("Cooldown")]
    public string cooldownTextFormat = "Available in {0} seconds";

    [Header("References")]
    public WhackAMoleChatBubbleController chatBubble;

    private NavMeshAgent agent;
    private Vector3 networkPosition;
    private Quaternion networkRotation;

    private Coroutine wanderRoutine;
    private Coroutine syncRoutine;
    private Coroutine scanRoutine;

    private enum NPCState
    {
        Idle,
        Reserved,
        Chatting,
        Cooldown
    }

    private NPCState state = NPCState.Idle;

    private const int NoActor = -999;
    private int reservedActor = NoActor;

    private readonly HashSet<int> cooldownViewers = new HashSet<int>();
    private int lastCooldownSecondShown = -1;

    private bool IsMultiplayer
    {
        get { return PhotonNetwork.InRoom && PhotonNetwork.CurrentRoom != null && PhotonNetwork.CurrentRoom.PlayerCount > 1; }
    }

    private bool IsController
    {
        get { return !PhotonNetwork.InRoom || PhotonNetwork.IsMasterClient; }
    }

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        networkPosition = transform.position;
        networkRotation = transform.rotation;

        EnsureChatBubbleReference();
    }

    private void EnsureChatBubbleReference()
    {
        if (chatBubble != null) return;

        chatBubble = GetComponentInChildren<WhackAMoleChatBubbleController>(true);
        if (chatBubble == null)
        {
            var allBubbles = Resources.FindObjectsOfTypeAll<WhackAMoleChatBubbleController>();
            if (allBubbles.Length > 0)
                chatBubble = allBubbles[0];
        }

        if (chatBubble == null)
        {
            Debug.LogWarning("WhackAMoleNPC: WhackAMoleChatBubbleController not found. Please assign it in inspector or make it a child of the NPC.");
        }
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
            reservedActor = NoActor;
            EnsureAgentOnNavMesh();
            StartControl();
        }
        else
        {
            StopControl();
        }
    }

    void Update()
    {
        if (!IsController)
        {
            transform.position = Vector3.Lerp(
                transform.position,
                networkPosition,
                Time.deltaTime * positionLerpSpeed);

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                networkRotation,
                Time.deltaTime * rotationLerpSpeed);
        }
    }

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
        if (wanderRoutine != null)
        {
            StopCoroutine(wanderRoutine);
            wanderRoutine = null;
        }

        if (syncRoutine != null)
        {
            StopCoroutine(syncRoutine);
            syncRoutine = null;
        }

        if (scanRoutine != null)
        {
            StopCoroutine(scanRoutine);
            scanRoutine = null;
        }

        if (agent != null)
        {
            agent.ResetPath();
            agent.updatePosition = false;
            agent.updateRotation = false;
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

            if (!agent.isOnNavMesh)
                agent.Warp(hit.position);
        }
        else
        {
            Debug.LogError("WhackAMoleNPC: No NavMesh found near NPC!");
        }
    }

    private IEnumerator WanderRoutine()
    {
        while (true)
        {
            if (state != NPCState.Idle)
            {
                yield return null;
                continue;
            }

            Vector3 dest = RandomNavSphere(transform.position, wanderRadius);

            if (agent != null && agent.isOnNavMesh)
            {
                agent.isStopped = false;
                agent.SetDestination(dest);

                while (state == NPCState.Idle &&
                       (agent.pathPending || agent.remainingDistance > arrivalThreshold))
                {
                    yield return null;
                }
            }

            float wait = Random.Range(waitTimeMin, waitTimeMax);
            float t = 0f;

            while (state == NPCState.Idle && t < wait)
            {
                t += Time.deltaTime;
                yield return null;
            }
        }
    }

    private IEnumerator SyncRoutine()
    {
        float interval = 1f / Mathf.Max(1f, sendRate);
        WaitForSeconds wait = new WaitForSeconds(interval);

        while (true)
        {
            if (IsMultiplayer && photonView != null)
            {
                photonView.RPC(
                    "RPC_ReceiveTransform",
                    RpcTarget.Others,
                    transform.position,
                    transform.rotation);
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

    private IEnumerator PlayerScanRoutine()
    {
        yield return new WaitForSeconds(1f);
        WaitForSeconds wait = new WaitForSeconds(scanInterval);

        while (true)
        {
            bool onCooldown = MiniGameCooldownManager.Instance != null &&
                              MiniGameCooldownManager.Instance.IsOnCooldown();

            if (onCooldown)
            {
                EnterCooldownState();
                UpdateCooldownUIForNearbyPlayers();
                yield return wait;
                continue;
            }

            if (state == NPCState.Cooldown)
            {
                ExitCooldownState();
            }

            if (state == NPCState.Idle)
            {
                int actor = FindNearestPlayerActorWithin(interactRadius);
                if (actor != NoActor)
                {
                    yield return StartCoroutine(HandleApproach(actor));
                }
            }

            yield return wait;
        }
    }

    private int FindNearestPlayerActorWithin(float radius)
    {
        int bestActor = NoActor;
        float bestDist = float.MaxValue;

        foreach (var kv in PlayerRegistration.PlayerMap)
        {
            Transform t = kv.Value;
            if (t == null) continue;

            float d = Vector3.Distance(transform.position, t.position);
            if (d <= radius && d < bestDist)
            {
                bestDist = d;
                bestActor = kv.Key;
            }
        }

        return bestActor;
    }

    private IEnumerator HandleApproach(int actorNumber)
    {
        if (state != NPCState.Idle) yield break;
        if (!PlayerRegistration.PlayerMap.ContainsKey(actorNumber)) yield break;

        if (MiniGameCooldownManager.Instance != null &&
            MiniGameCooldownManager.Instance.IsOnCooldown())
        {
            EnterCooldownState();
            yield break;
        }

        Transform check = PlayerRegistration.PlayerMap[actorNumber];
        if (check == null) yield break;

        float checkDist = Vector3.Distance(transform.position, check.position);
        if (checkDist > interactRadius) yield break;

        state = NPCState.Reserved;
        reservedActor = actorNumber;

        StopWanderImmediate();

        float elapsed = 0f;
        while (elapsed < requiredStaySeconds)
        {
            if (MiniGameCooldownManager.Instance != null &&
                MiniGameCooldownManager.Instance.IsOnCooldown())
            {
                EnterCooldownState();
                yield break;
            }

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

        if (MiniGameCooldownManager.Instance != null &&
            MiniGameCooldownManager.Instance.IsOnCooldown())
        {
            EnterCooldownState();
            yield break;
        }

        state = NPCState.Chatting;
        ShowChatToPlayer(actorNumber);

        while (true)
        {
            if (MiniGameCooldownManager.Instance != null &&
                MiniGameCooldownManager.Instance.IsOnCooldown())
            {
                HideChatFromPlayer(actorNumber);
                EnterCooldownState();
                yield break;
            }

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
                HideChatFromPlayer(actorNumber);
                ResetAfterInteraction();
                yield break;
            }

            yield return null;
        }
    }

    private void EnterCooldownState()
    {
        if (state == NPCState.Cooldown) return;

        if (reservedActor != NoActor)
        {
            HideChatFromPlayer(reservedActor);
        }

        reservedActor = NoActor;
        state = NPCState.Cooldown;
        StopWanderImmediate();
    }

    private void ExitCooldownState()
    {
        HideCooldownFromAllViewers();

        reservedActor = NoActor;
        state = NPCState.Idle;
        lastCooldownSecondShown = -1;

        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = false;
        }

        if (wanderRoutine == null)
            wanderRoutine = StartCoroutine(WanderRoutine());
    }

    private void UpdateCooldownUIForNearbyPlayers()
    {
        if (MiniGameCooldownManager.Instance == null) return;

        int seconds = Mathf.CeilToInt(MiniGameCooldownManager.Instance.GetRemainingCooldown());
        string text = string.Format(cooldownTextFormat, seconds);

        bool secondChanged = (seconds != lastCooldownSecondShown);
        HashSet<int> inRangeNow = new HashSet<int>();

        foreach (var kv in PlayerRegistration.PlayerMap)
        {
            int actor = kv.Key;
            Transform t = kv.Value;
            if (t == null) continue;

            float d = Vector3.Distance(transform.position, t.position);
            if (d <= interactRadius)
            {
                inRangeNow.Add(actor);

                if (secondChanged || !cooldownViewers.Contains(actor))
                {
                    ShowDirectTextToPlayer(actor, text);
                }
            }
        }

        List<int> toHide = new List<int>();
        foreach (int actor in cooldownViewers)
        {
            if (!inRangeNow.Contains(actor))
            {
                toHide.Add(actor);
            }
        }

        foreach (int actor in toHide)
        {
            HideChatFromPlayer(actor);
            cooldownViewers.Remove(actor);
        }

        foreach (int actor in inRangeNow)
        {
            cooldownViewers.Add(actor);
        }

        lastCooldownSecondShown = seconds;
    }

    private void HideCooldownFromAllViewers()
    {
        foreach (int actor in cooldownViewers)
        {
            HideChatFromPlayer(actor);
        }

        cooldownViewers.Clear();
    }

    private void StopWanderImmediate()
    {
        if (wanderRoutine != null)
        {
            StopCoroutine(wanderRoutine);
            wanderRoutine = null;
        }

        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }
    }

    private void CancelReserved()
    {
        if (reservedActor != NoActor)
        {
            HideChatFromPlayer(reservedActor);
        }

        ResetAfterInteraction();
    }

    private void ResetAfterInteraction()
    {
        reservedActor = NoActor;

        bool onCooldown = MiniGameCooldownManager.Instance != null &&
                          MiniGameCooldownManager.Instance.IsOnCooldown();

        if (onCooldown)
        {
            state = NPCState.Cooldown;
            StopWanderImmediate();
            return;
        }

        state = NPCState.Idle;

        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = false;
        }

        if (wanderRoutine == null)
        {
            wanderRoutine = StartCoroutine(WanderRoutine());
        }
    }

    private void ShowChatToPlayer(int actorNumber)
    {
        if (IsMultiplayer)
        {
            Player targetPlayer = PhotonNetwork.CurrentRoom.GetPlayer(actorNumber);
            if (targetPlayer != null)
            {
                photonView.RPC("RPC_ShowChat_LocalOnly", targetPlayer, chatMessage, charDelay);
            }
        }
        else
        {
            LocalShowChat(chatMessage, charDelay);
        }
    }

    private void ShowDirectTextToPlayer(int actorNumber, string text)
    {
        if (IsMultiplayer)
        {
            Player targetPlayer = PhotonNetwork.CurrentRoom.GetPlayer(actorNumber);
            if (targetPlayer != null)
            {
                photonView.RPC("RPC_SetDirectText_LocalOnly", targetPlayer, text);
            }
        }
        else
        {
            LocalSetDirectText(text);
        }
    }

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

    private void LocalShowChat(string msg, float perCharDelay)
    {
        EnsureChatBubbleReference();
        if (chatBubble == null) return;
        chatBubble.ShowMessage(msg, perCharDelay);
    }

    private void LocalSetDirectText(string text)
    {
        EnsureChatBubbleReference();
        if (chatBubble == null) return;
        chatBubble.SetDirectText(text);
    }

    private void LocalHideChat()
    {
        EnsureChatBubbleReference();
        if (chatBubble == null) return;
        chatBubble.HideWithStop();
    }

    [PunRPC]
    private void RPC_ShowChat_LocalOnly(string msg, float perCharDelay, PhotonMessageInfo info)
    {
        if (chatBubble == null)
            chatBubble = GetComponentInChildren<WhackAMoleChatBubbleController>(true);

        if (chatBubble == null)
        {
            Debug.LogWarning("WhackAMoleChatBubbleController not found.");
            return;
        }

        chatBubble.ShowMessage(msg, perCharDelay);
    }

    [PunRPC]
    private void RPC_SetDirectText_LocalOnly(string text, PhotonMessageInfo info)
    {
        if (chatBubble == null)
            chatBubble = GetComponentInChildren<WhackAMoleChatBubbleController>(true);

        if (chatBubble == null)
        {
            Debug.LogWarning("WhackAMoleChatBubbleController not found.");
            return;
        }

        chatBubble.SetDirectText(text);
    }

    [PunRPC]
    private void RPC_HideChat_LocalOnly(PhotonMessageInfo info)
    {
        if (chatBubble == null)
            chatBubble = GetComponentInChildren<WhackAMoleChatBubbleController>(true);

        if (chatBubble == null) return;
        chatBubble.HideWithStop();
    }

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