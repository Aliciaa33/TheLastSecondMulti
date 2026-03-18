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

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public void AddHint(string number, string hint)
    {
        if (PhotonNetwork.IsConnected)
        {
            // Send to all clients including self
            photonView.RPC("RPC_AddHint", RpcTarget.All, number, hint);
        }
        else
        {
            // Single player — add directly
            AddHintLocally(number, hint);
        }
    }

    [PunRPC]
    void RPC_AddHint(string number, string hint)
    {
        AddHintLocally(number, hint);
    }

    private void AddHintLocally(string number, string hint)
    {
        HintItem newHint = new HintItem
        {
            itemId = System.Guid.NewGuid().ToString(),
            itemName = "Password Hint",
            number = number,
            hint = hint
        };

        collectedHints.Add(newHint);

        // Update UI for this client
        if (UIManager.Instance != null)
            UIManager.Instance.ShowToast($"Hint collected: {number}");

        // Refresh inventory panel if it's open
        if (InventoryUI.Instance != null)
            InventoryUI.Instance.ShowInventory();
    }

    public void ClearInventory()
    {
        if (PhotonNetwork.IsConnected)
            photonView.RPC("RPC_ClearInventory", RpcTarget.All);
        else
            ClearInventoryLocally();
    }

    [PunRPC]
    void RPC_ClearInventory()
    {
        ClearInventoryLocally();
    }

    private void ClearInventoryLocally()
    {
        collectedHints.Clear();
        if (InventoryUI.Instance != null)
            InventoryUI.Instance.ShowInventory();
    }

    public List<HintItem> GetCollectedHints()
    {
        return new List<HintItem>(collectedHints);
    }

    // Called when a new player joins the room
    public override void OnPlayerEnteredRoom(Photon.Realtime.Player newPlayer)
    {
        // Only MasterClient syncs state to the new player
        if (!PhotonNetwork.IsMasterClient) return;

        // Send all current hints to the new player only
        foreach (HintItem hint in collectedHints)
        {
            photonView.RPC("RPC_AddHint", newPlayer, hint.number, hint.hint);
        }
    }
}