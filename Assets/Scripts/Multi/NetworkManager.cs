using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using Cinemachine;

public class NetworkGameManager : MonoBehaviourPunCallbacks
{
    [Header("Spawning")]
    [SerializeField] private Transform[] spawnPoints;       // 4 empty GameObjects in scene
    [SerializeField] private string playerPrefabName = "PlayerCapsule"; // Must be in Assets/Resources/

    [Header("Single Player Fallback")]
    [SerializeField] private GameObject singlePlayerPrefab; // Drag your existing player here

    void Start()
    {
        if (ConnectToServer.Instance != null && ConnectToServer.Instance.IsMultiplayer())
        {
            // Multiplayer: must be in a Photon room
            if (PhotonNetwork.InRoom)
                SpawnNetworkPlayer();
            else
                StartCoroutine(WaitForRoomThenSpawn());
        }
        else
        {
            // Single player: just instantiate locally, no Photon needed
            SpawnSinglePlayer();
        }
    }

    private void SpawnSinglePlayer()
    {
        int spawnIndex = 0;
        Transform spawnPoint = spawnPoints[spawnIndex];

        // Regular Unity instantiate — no Photon involved
        GameObject player = Instantiate(singlePlayerPrefab, spawnPoint.position, spawnPoint.rotation);

        // Retarget camera to this player
        CinemachineVirtualCamera vCam = FindObjectOfType<CinemachineVirtualCamera>();
        if (vCam != null)
        {
            Transform camTarget = player.transform.Find("PlayerCameraRoot");
            vCam.Follow = camTarget;
            vCam.LookAt = camTarget;
        }

        Debug.Log("Single player spawned");
    }

    private void SpawnNetworkPlayer()
    {
        // Set the player's nickname to display it above the character
        PhotonNetwork.LocalPlayer.NickName = ConnectToServer.Instance.GetPlayerName();

        // ActorNumber is 1-based and unique per session
        int spawnIndex = (PhotonNetwork.LocalPlayer.ActorNumber - 1) % spawnPoints.Length;
        Transform spawnPoint = spawnPoints[spawnIndex];

        // Instantiate across network — all clients see this
        PhotonNetwork.Instantiate(
            playerPrefabName,
            spawnPoint.position,
            spawnPoint.rotation
        );

        Debug.Log($"Spawned player at index {spawnIndex} as ActorNumber {PhotonNetwork.LocalPlayer.ActorNumber}");
    }

    // If a player disconnects mid-game
    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        Debug.Log($"{otherPlayer.NickName} left the game");
        // Optional: notify UIManager
    }

    IEnumerator WaitForRoomThenSpawn()
    {
        while (!PhotonNetwork.InRoom)
            yield return new WaitForSeconds(0.5f);
        SpawnNetworkPlayer();
    }
}