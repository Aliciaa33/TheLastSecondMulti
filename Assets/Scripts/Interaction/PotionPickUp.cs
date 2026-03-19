using UnityEngine;
using Photon.Pun;

/// <summary>
/// Picking up a potion now adds it to InventoryManager instead of
/// immediately restoring HP. The player uses it from the Inventory UI.
/// </summary>
public class PotionPickUp : InteractableBase
{
    AudioManager audioManager;

    void Start()
    {
        interactionText = "Press F to pick up potion";
        audioManager = GameObject.FindGameObjectWithTag("Audio").GetComponent<AudioManager>();
    }

    public override void Interact()
    {
        // Add to inventory — does NOT restore HP yet
        if (InventoryManager.Instance != null)
            InventoryManager.Instance.AddPotion();
        // Play sound effect for local player
        audioManager.PlaySFX(audioManager.interact);

        // Destroy the world object
        PhotonView pv = GetComponent<PhotonView>();
        if (pv != null && PhotonNetwork.IsConnected)
            pv.RPC("RPC_DestroyPotion", RpcTarget.MasterClient);
        else
            Destroy(gameObject);
    }

    [PunRPC]
    void RPC_DestroyPotion()
    {
        if (PhotonNetwork.IsMasterClient)
            PhotonNetwork.Destroy(gameObject);
    }
}