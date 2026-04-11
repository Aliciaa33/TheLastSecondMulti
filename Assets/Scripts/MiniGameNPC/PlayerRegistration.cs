using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class PlayerRegistration : MonoBehaviour
{
    public static Dictionary<int, Transform> PlayerMap = new Dictionary<int, Transform>();
    private PhotonView pv;
    private int registeredActor = int.MinValue; // 用 MinValue 表示"未注册"

    void Awake()
    {
        pv = GetComponent<PhotonView>();
    }

    void OnEnable()
    {
        StartCoroutine(RegisterWhenReady());
    }

    private IEnumerator RegisterWhenReady()
    {
        // ★ 多人模式：等待 Photon 分配 Owner（最多等 5 秒）
        if (pv != null)
        {
            float timeout = 5f;
            while (pv.Owner == null && timeout > 0f)
            {
                timeout -= Time.deltaTime;
                yield return null;
            }

            if (pv.Owner != null)
            {
                registeredActor = pv.Owner.ActorNumber;
                PlayerMap[registeredActor] = transform;
                Debug.Log($"PlayerRegistration: 注册 Actor={registeredActor}");
                yield break;
            }
        }

        // ★ 单人模式 fallback
        registeredActor = -1;
        PlayerMap[-1] = transform;
        Debug.Log("PlayerRegistration: 单人模式注册 Actor=-1");
    }

    void OnDisable()
    {
        if (registeredActor != int.MinValue && PlayerMap.ContainsKey(registeredActor))
            PlayerMap.Remove(registeredActor);
    }

    void OnDestroy() { OnDisable(); }
}



/*
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class PlayerRegistration : MonoBehaviour
{
    public static Dictionary<int, Transform> PlayerMap = new Dictionary<int, Transform>();
    private PhotonView pv;
    private int registeredActor = -1;

    void Awake()
    {
        pv = GetComponent<PhotonView>();
    }

    void OnEnable()
    {
        if (pv != null && pv.Owner != null)
        {
            registeredActor = pv.Owner.ActorNumber;
            PlayerMap[registeredActor] = transform;
        }
        else
        {
            // ★ 单人模式 fallback：用 -1 作为 key
            registeredActor = -1;
            PlayerMap[-1] = transform;
        }
    }

    void OnDisable()
    {
        if (registeredActor != -1 && PlayerMap.ContainsKey(registeredActor))
            PlayerMap.Remove(registeredActor);
        else if (PlayerMap.ContainsKey(-1))
            PlayerMap.Remove(-1);
    }

    void OnDestroy() { OnDisable(); }
}
*/

/*
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
*/
