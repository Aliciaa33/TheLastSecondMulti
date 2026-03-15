using UnityEngine;
using System.Collections.Generic;
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
            UIManager.Instance.ShowToast($"Hint collected: {number}");

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
            UIManager.Instance.ShowToast("Potion collected! Open inventory to use.");
        if (InventoryUI.Instance != null)
            InventoryUI.Instance.RefreshIfOpen();
    }

    /// Called by the USE button in InventoryUI.
    public void UsePotion()
    {
        if (potionCount <= 0)
        {
            if (UIManager.Instance != null)
                UIManager.Instance.ShowToast("No potions left!");
            return;
        }

        if (PhotonNetwork.IsConnected)
            photonView.RPC("RPC_UsePotion", RpcTarget.All);
        else
            UsePotionLocally();
    }

    [PunRPC]
    void RPC_UsePotion() => UsePotionLocally();

    private void UsePotionLocally()
    {
        if (potionCount <= 0) return;
        potionCount--;

        // Actually restore HP
        GameManager.Instance?.Restore();

        if (UIManager.Instance != null)
            UIManager.Instance.ShowToast("Potion used! HP restored.");

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