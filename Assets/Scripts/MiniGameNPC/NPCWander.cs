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