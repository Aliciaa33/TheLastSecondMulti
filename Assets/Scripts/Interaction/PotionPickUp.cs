using UnityEngine;
using Photon.Pun;

public class PotionPickUp : InteractableBase
{
    void Start()
    {
        interactionText = "Press F to pick up potion";
    }

    public override void Interact()
    {
        // Apply potion effect locally
        GameManager.Instance?.Restore();

        PhotonView pv = GetComponent<PhotonView>();
        if (pv != null && PhotonNetwork.IsConnected)
        {
            pv.RPC("RPC_DestroyPotion", RpcTarget.MasterClient);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    [PunRPC]
    void RPC_DestroyPotion()
    {
        if (PhotonNetwork.IsMasterClient)
            PhotonNetwork.Destroy(gameObject);
    }
}