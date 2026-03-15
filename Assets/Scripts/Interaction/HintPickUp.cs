using UnityEngine;
using Photon.Pun;

public class HintPickUp : InteractableBase
{
    public string number;
    public string hint;

    void Start()
    {
        interactionText = "Press F to pick up hint";

        PhotonView pv = GetComponent<PhotonView>();
        if (pv != null && pv.InstantiationData != null)
        {
            object[] data = pv.InstantiationData;
            if (data.Length >= 2)
            {
                number = (string)data[0];
                hint = (string)data[1];
            }
        }
    }

    public override void Interact()
    {
        // Add to local inventory immediately
        InventoryManager.Instance?.AddHint(number, hint);
        UIManager.Instance?.ShowToast("Collected Number: " + number);

        PhotonView pv = GetComponent<PhotonView>();
        if (pv != null && PhotonNetwork.IsConnected)
        {
            // Ask MasterClient to destroy this object for everyone
            pv.RPC("RPC_DestroyHint", RpcTarget.MasterClient);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    [PunRPC]
    void RPC_DestroyHint()
    {
        // Only MasterClient executes this and destroys for all
        if (PhotonNetwork.IsMasterClient)
            PhotonNetwork.Destroy(gameObject);
    }
}