using UnityEngine;
using Photon.Pun;

public class NPCSpawner : MonoBehaviour
{
    public string npcPrefabResourcePath = "WanderingNPC"; // Resources/WanderingNPC.prefab
    public Vector3 spawnPosition = Vector3.zero;
    public Quaternion spawnRotation = Quaternion.identity;

    void Start()
    {
        // only the master client spawns the npc
        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.Instantiate(npcPrefabResourcePath, spawnPosition, spawnRotation);
        }
    }
}