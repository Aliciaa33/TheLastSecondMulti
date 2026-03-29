using UnityEngine;
using Photon.Pun;

public class MiniGameCooldownManager : MonoBehaviour
{
    public static MiniGameCooldownManager Instance { get; private set; }

    [Header("Cooldown Settings")]
    public float cooldownDuration = 120f;

    private float _cooldownRemaining = 0f;
    private bool _onCooldown = false;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            enabled = false;
    }

    void Update()
    {
        if (!_onCooldown) return;

        _cooldownRemaining -= Time.deltaTime;
        if (_cooldownRemaining <= 0f)
        {
            _cooldownRemaining = 0f;
            _onCooldown = false;

            if (UIManager.Instance != null)
                UIManager.Instance.ShowToast("Whack-A-Mole is available again!");
        }
    }

    public void StartCooldown()
    {
        if (PhotonNetwork.IsConnected && PhotonNetwork.InRoom)
        {
            // Use ConnectToServer's PhotonView for RPC
            // since it's always available and properly registered
            GameManager.Instance.GetComponent<PhotonView>()
                .RPC("RPC_StartMiniGameCooldown", RpcTarget.All, cooldownDuration);
        }
        else
        {
            ApplyCooldown(cooldownDuration);
        }
    }

    public void ApplyCooldown(float duration)
    {
        _onCooldown = true;
        _cooldownRemaining = duration;
        Debug.Log($"Mini game cooldown started: {duration}s");
    }

    public bool IsOnCooldown() => _onCooldown;
    public float GetRemainingCooldown() => _cooldownRemaining;
}