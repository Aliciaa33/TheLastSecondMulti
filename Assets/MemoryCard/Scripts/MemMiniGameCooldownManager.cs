using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class MemMiniGameCooldownManager : MonoBehaviourPunCallbacks
{
    public static MemMiniGameCooldownManager Instance { get; private set; }

    [Header("Cooldown Settings")]
    public float cooldownDuration = 60f; // 秒

    private float _remaining = 0f;
    private bool _onCooldown = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    void Update()
    {
        if (!_onCooldown) return;
        _remaining -= Time.deltaTime;
        if (_remaining <= 0f)
        {
            _remaining = 0f;
            _onCooldown = false;
        }
    }

    // ── 启动 Cooldown（所有客户端同步）──
    public void StartCooldown()
    {
        if (PhotonNetwork.IsConnected)
            photonView.RPC("RPC_StartMemCooldown", RpcTarget.All, cooldownDuration);
        else
            ApplyCooldown(cooldownDuration);
    }

    [PunRPC]
    void RPC_StartMemCooldown(float dur)
    {
        ApplyCooldown(dur);
    }

    private void ApplyCooldown(float dur)
    {
        _onCooldown = true;
        _remaining = dur;
        Debug.Log($"[MemCooldown] Started: {dur}s");
    }

    // ── 新玩家加入时同步剩余 cooldown ──
    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        if (_onCooldown && _remaining > 0f)
            photonView.RPC("RPC_StartMemCooldown", newPlayer, _remaining);
    }

    // ── 公共查询 ──
    public bool IsOnCooldown() => _onCooldown;
    public float GetRemainingCooldown() => _remaining;
}