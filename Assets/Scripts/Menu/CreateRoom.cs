using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using TMPro;

public class CreateRoom : MonoBehaviourPunCallbacks
{
    // [SerializeField] private InputField roomNameInput;
    [SerializeField] private TextMeshProUGUI playerListText;
    [SerializeField] private TextMeshProUGUI roomInfoText;
    [SerializeField] private Button startButton;
    [SerializeField] private int maxPlayersPerRoom = 4;

    private bool isRoomCreated = false;
    private string createdRoomName;

    void Start()
    {
        // Mark the player as room creator
        ConnectToServer.Instance.SetIsRoomCreator(true);

        // Only creator can see start button
        if (startButton != null)
        {
            startButton.gameObject.SetActive(PhotonNetwork.IsMasterClient);
            Debug.Log("IsMasterClient" + PhotonNetwork.IsMasterClient);
        }

        // Wait for Photon to be ready before creating room
        StartCoroutine(WaitAndCreateRoom());
    }

    private IEnumerator WaitAndCreateRoom()
    {
        // Wait until connected to Master Server and in lobby
        float timeout = 10f;
        while (!PhotonNetwork.IsConnectedAndReady && timeout > 0)
        {
            yield return new WaitForSeconds(0.5f);
            timeout -= 0.5f;
        }

        if (!PhotonNetwork.IsConnectedAndReady)
        {
            Debug.LogError("Timed out waiting for Photon connection");
            yield break;
        }

        // Now safe to create room
        string roomName = "" + Random.Range(1000, 9999);
        RoomOptions roomOptions = new RoomOptions
        {
            MaxPlayers = (byte)maxPlayersPerRoom,
            IsVisible = true,
            IsOpen = true,
            CleanupCacheOnLeave = false
        };

        PhotonNetwork.CreateRoom(roomName, roomOptions);
        createdRoomName = roomName;
        Debug.Log("Auto-creating room: " + roomName);

        RefreshPlayerList();
    }

    void Update()
    {
        // Continuously update player list
        if (isRoomCreated)
        {
            RefreshPlayerList();
        }
    }

    public void OnStartGameButtonClicked()
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            Debug.LogWarning("Only room creator can start the game!");
            return;
        }

        Debug.Log("Starting game with " + PhotonNetwork.CurrentRoom.PlayerCount + " players");

        // Set game mode based on player count
        if (PhotonNetwork.CurrentRoom.PlayerCount == 1)
        {
            ConnectToServer.Instance.SetGameMode(GameMode.SinglePlayer);
        }
        else
        {
            ConnectToServer.Instance.SetGameMode(GameMode.Multiplayer);
        }

        // Load game scene
        PhotonNetwork.LoadLevel("Game");
    }

    private void RefreshPlayerList()
    {
        if (PhotonNetwork.CurrentRoom == null)
        {
            return;
        }

        // Update room info
        if (roomInfoText != null)
        {
            roomInfoText.text = $"Room No.: {PhotonNetwork.CurrentRoom.Name}     " +
                               $"Players: {PhotonNetwork.CurrentRoom.PlayerCount} / {PhotonNetwork.CurrentRoom.MaxPlayers}" +
                               $"\nCreator: {PhotonNetwork.MasterClient.NickName}";
        }

        // Update player list
        if (playerListText != null)
        {
            string playerListStr = "";
            int index = 1;
            foreach (Player player in PhotonNetwork.CurrentRoom.Players.Values)
            {
                string creatorTag = (player == PhotonNetwork.LocalPlayer) ? " (You)" : "";
                playerListStr += $"Player {index}: {player.NickName}{creatorTag}\n\n";
                index++;
            }

            playerListText.text = playerListStr;
        }

        // Check if room is full
        // if (PhotonNetwork.CurrentRoom.PlayerCount >= PhotonNetwork.CurrentRoom.MaxPlayers)
        // {
        //     if (roomInfoText != null)
        //     {
        //         roomInfoText.text += "\n[FULL - No more players can join]";
        //     }
        // }
    }

    public void OnBackButtonClicked()
    {
        if (PhotonNetwork.InRoom)
        {
            PhotonNetwork.LeaveRoom();
        }
        SceneManager.LoadScene("Menu");
    }

    public override void OnCreatedRoom()
    {
        Debug.Log("✓ Room created successfully!");
        isRoomCreated = true;
        RefreshPlayerList();
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.LogError("✗ Failed to create room: " + message);
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log("Player joined: " + newPlayer.NickName);
        RefreshPlayerList();
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        Debug.Log("Player left: " + otherPlayer.NickName);
        RefreshPlayerList();
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        Debug.Log("Master client switched to: " + newMasterClient.NickName);

        // Update start button visibility when master changes
        if (startButton != null)
        {
            startButton.gameObject.SetActive(PhotonNetwork.IsMasterClient);
        }
    }
}