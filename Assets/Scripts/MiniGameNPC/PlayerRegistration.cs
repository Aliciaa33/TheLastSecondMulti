using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class PlayerRegistration : MonoBehaviour
{
    public static Dictionary<int, Transform> PlayerMap = new Dictionary<int, Transform>();
    private PhotonView pv;

    void Awake()
    {
        pv = GetComponent<PhotonView>();
    }

    void OnEnable()
    {
        if (pv != null && pv.Owner != null)
        {
            int a = pv.Owner.ActorNumber;
            PlayerMap[a] = this.transform;
        }
    }

    void OnDisable()
    {
        if (pv != null && pv.Owner != null)
        {
            int a = pv.Owner.ActorNumber;
            if (PlayerMap.ContainsKey(a)) PlayerMap.Remove(a);
        }
    }

    void OnDestroy()
    {
        OnDisable();
    }
}