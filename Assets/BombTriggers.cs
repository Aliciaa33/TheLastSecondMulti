using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class BombTriggers : MonoBehaviourPun
{
    [Tooltip("Tag of the player")]
    public string playerTag = "Player";

    private BombFuse bombFuse;

    void Awake()
    {
        bombFuse = GetComponent<BombFuse>();
        if (bombFuse == null)
        {
            Debug.LogWarning("BombTrigger: 未在 Bomb 上找到 BombFuse 组件。");
        }
    }



    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"碰撞物体: {other.name}, Tag: '{other.tag}'");

        if (!other.CompareTag(playerTag)) return;
        if (PhotonNetwork.InRoom && PhotonNetwork.CurrentRoom.PlayerCount > 1)
        {
            // 只有本地玩家触发
            PhotonView playerPV = other.GetComponentInParent<PhotonView>();

            if (playerPV == null)
            {
                Debug.LogWarning("⚠️ 玩家缺少 PhotonView");
                return;
            }

            if (!playerPV.IsMine) return;

            Debug.Log($"本地玩家 {playerPV.Owner.NickName} 触发了炸弹！");

        }
        else
        {
            Debug.Log("单人模式下触发炸弹");
        }

        if (bombFuse != null)
        {
            // TriggerExplosion ：本地先炸 + RPC 通知其他人
            bombFuse.TriggerExplosion();
            Debug.Log("Bomb triggered and exploded.");
        }
    }
}