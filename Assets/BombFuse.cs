using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Photon.Pun;

public class BombFuse : MonoBehaviourPun
{
    public float FuseTime = 300f;
    public GameObject explosionEffect;
    public float spawnDelay = 2f;

    private Coroutine fuseCoroutine;
    private bool hasExploded = false;
    private bool isDefused = false;
    private bool isGameOver = false;

    public static event Action OnBombExploded;
    public static event Action OnBombDefused;

    void Start()
    {
        Debug.Log("炸弹爆炸时间: " + FuseTime + " 秒");
        GameManager.OnGameOver += OnGameOver;

        if (PhotonNetwork.IsMasterClient)
        {
            StartCoroutine(StartFuseAfterDelay());
        }
    }

    void OnDestroy()
    {
        GameManager.OnGameOver -= OnGameOver;
    }

    void OnGameOver()
    {
        Debug.Log("🛑 游戏结束，炸弹停止倒计时");
        isGameOver = true;
        if (fuseCoroutine != null) StopCoroutine(fuseCoroutine);
    }

    private IEnumerator StartFuseAfterDelay()
    {
        yield return new WaitForSeconds(spawnDelay);
        fuseCoroutine = StartCoroutine(FuseCountdown());
    }

    private IEnumerator FuseCountdown()
    {
        yield return new WaitForSeconds(FuseTime);

        if (!hasExploded && !isDefused && !isGameOver)
        {
            // MasterClient 本地先炸，再通知其他人
            ExecuteExplosion();
            photonView.RPC("RPC_Explode", RpcTarget.Others);
        }
    }

    public void TriggerExplosion()
    {
        if (hasExploded || isDefused || isGameOver) return;

        if (fuseCoroutine != null) StopCoroutine(fuseCoroutine);

        // 本地立刻执行，然后只通知其他客户端
        ExecuteExplosion();
        photonView.RPC("RPC_Explode", RpcTarget.Others);
    }


    public void DefuseBomb()
    {
        if (hasExploded || isDefused || isGameOver) return;

        // 本地先执行拆弹，再通知其他人
        ExecuteDefuse();
        photonView.RPC("RPC_Defuse", RpcTarget.Others);
    }

    // 其他人收到的爆炸通知
    [PunRPC]
    void RPC_Explode()
    {
        ExecuteExplosion();
    }

    // 其他人收到的拆弹通知
    [PunRPC]
    void RPC_Defuse()
    {
        ExecuteDefuse();
    }


    [PunRPC]
    void RPC_TriggerExplosion()
    {
        if (hasExploded) return;
        TriggerExplosion();
    }

    // 执行爆炸逻辑
    private void ExecuteExplosion()
    {
        if (hasExploded) return;
        hasExploded = true;

        Vector3 explosionPos = transform.position;

        // 立刻隐藏炸弹
        HideBomb();

        // 触发爆炸事件（血条-1 。。。）
        if (!isGameOver)
        {
            OnBombExploded?.Invoke();
        }

        Debug.Log("💥炸弹爆炸！位置: " + explosionPos);

        // 本地生成爆炸特效
        if (explosionEffect != null)
        {
            GameObject vfx = Instantiate(explosionEffect, explosionPos, Quaternion.identity);
            Destroy(vfx, 3f);
            Debug.Log("💥 爆炸特效已生成");
        }
        else
        {
            Debug.LogError("explosionEffect 未分配！");
        }

        // MasterClient 延迟销毁网络对象
        if (PhotonNetwork.IsMasterClient)
        {
            StartCoroutine(DelayedNetworkDestroy());
        }
    }

    // 执行拆弹逻辑
    private void ExecuteDefuse()
    {
        if (hasExploded || isDefused) return;
        isDefused = true;

        if (fuseCoroutine != null)
        {
            StopCoroutine(fuseCoroutine);
        }

        HideBomb();


        Debug.Log("✅ 炸弹已被拆除");

        if (!isGameOver)
        {
            OnBombDefused?.Invoke();
        }


        if (PhotonNetwork.IsMasterClient)
        {
            StartCoroutine(DelayedNetworkDestroy());
        }
    }


    private void HideBomb()
    {
        foreach (var r in GetComponentsInChildren<Renderer>())
            r.enabled = false;
        foreach (var c in GetComponentsInChildren<Collider>())
            c.enabled = false;
    }


    private IEnumerator DelayedNetworkDestroy()
    {
        yield return new WaitForSeconds(0.5f);

        if (gameObject != null && photonView != null)
        {
            PhotonNetwork.Destroy(gameObject);
            Debug.Log("MasterClient 延迟销毁炸弹完成");
        }
    }

    void Update() { }
}