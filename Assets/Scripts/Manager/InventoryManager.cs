using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using Photon.Pun;

public class InventoryManager : MonoBehaviourPunCallbacks
{
    public static InventoryManager Instance { get; private set; }

    [System.Serializable]
    public class HintItem
    {
        public string itemId;
        public string itemName;
        public string number;
        public string hint;
    }

    private List<HintItem> collectedHints = new List<HintItem>();
    private int potionCount = 0;  // potions held but not yet used
    private bool _potionBeingUsed = false; // lock flag to prevent multiple uses at once

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // ── Hints ─────────────────────────────────────────────────────────────

    public void AddHint(string number, string hint)
    {
        if (PhotonNetwork.IsConnected)
            photonView.RPC("RPC_AddHint", RpcTarget.All, number, hint);
        else
            AddHintLocally(number, hint);
    }

    [PunRPC]
    void RPC_AddHint(string number, string hint) => AddHintLocally(number, hint);

    private void AddHintLocally(string number, string hint)
    {
        collectedHints.Add(new HintItem
        {
            itemId = System.Guid.NewGuid().ToString(),
            itemName = "Password Hint",
            number = number,
            hint = hint
        });

        if (UIManager.Instance != null)
            UIManager.Instance.ShowToast($"Hint collected: {number}", 0);

        if (InventoryUI.Instance != null)
            InventoryUI.Instance.RefreshIfOpen();
    }

    public List<HintItem> GetCollectedHints() => new List<HintItem>(collectedHints);

    // ── Potions ───────────────────────────────────────────────────────────

    /// Called by PotionPickUp — adds a potion to inventory without using it.
    public void AddPotion()
    {
        if (PhotonNetwork.IsConnected)
            photonView.RPC("RPC_AddPotion", RpcTarget.All);
        else
            AddPotionLocally();
    }

    [PunRPC]
    void RPC_AddPotion() => AddPotionLocally();

    private void AddPotionLocally()
    {
        potionCount++;
        if (UIManager.Instance != null)
            UIManager.Instance.ShowToast("Potion collected! Open inventory to use.", 1);
        if (InventoryUI.Instance != null)
            InventoryUI.Instance.RefreshIfOpen();
    }

    /// Called by the USE button in InventoryUI.
    public void UsePotion()
    {
        if (potionCount <= 0)
        {
            if (UIManager.Instance != null)
                UIManager.Instance.ShowToast("No potions left!", 4);
            return;
        }

        // Prevent race condition — only one use at a time
        if (_potionBeingUsed) return;

        if (PhotonNetwork.IsConnected)
            // Route through MasterClient for authority
            photonView.RPC("RPC_RequestUsePotion", RpcTarget.MasterClient);
        else
            UsePotionLocally();
    }

    [PunRPC]
    void RPC_RequestUsePotion()
    {
        // Only MasterClient processes this
        if (!PhotonNetwork.IsMasterClient) return;

        // Check again on MasterClient side — prevents race condition
        if (potionCount <= 0 || _potionBeingUsed)
        {
            // Debug.Log("Potion use rejected — none left or already being used");
            return;
        }

        _potionBeingUsed = true;

        // Tell all clients to execute the potion use
        photonView.RPC("RPC_UsePotion", RpcTarget.All);

        // Restore HP once — GameManager.Restore() handles its own sync
        // prevent multiple restores
        GameManager.Instance?.Restore();

        // Unlock after brief delay
        StartCoroutine(UnlockPotionUse());
    }

    IEnumerator UnlockPotionUse()
    {
        yield return new WaitForSeconds(0.5f);
        _potionBeingUsed = false;
    }

    [PunRPC]
    void RPC_UsePotion() => UsePotionLocally();

    private void UsePotionLocally()
    {
        if (potionCount <= 0) return;
        potionCount--;

        if (UIManager.Instance != null)
            UIManager.Instance.ShowToast("Potion used! HP restored.", 3);

        if (InventoryUI.Instance != null)
            InventoryUI.Instance.RefreshIfOpen();
    }

    public int GetPotionCount() => potionCount;

    // ── Clear ─────────────────────────────────────────────────────────────

    public void ClearInventory()
    {
        if (PhotonNetwork.IsConnected)
            photonView.RPC("RPC_ClearInventory", RpcTarget.All);
        else
            ClearInventoryLocally();
    }

    [PunRPC]
    void RPC_ClearInventory() => ClearInventoryLocally();

    private void ClearInventoryLocally()
    {
        collectedHints.Clear();
        // potionCount = 0; don't clear potion count when a bomb explodes
        if (InventoryUI.Instance != null)
            InventoryUI.Instance.RefreshIfOpen();
    }

    // ── Sync new player ───────────────────────────────────────────────────

    public override void OnPlayerEnteredRoom(Photon.Realtime.Player newPlayer)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        foreach (HintItem hint in collectedHints)
            photonView.RPC("RPC_AddHint", newPlayer, hint.number, hint.hint);
        for (int i = 0; i < potionCount; i++)
            photonView.RPC("RPC_AddPotion", newPlayer);
    }
}