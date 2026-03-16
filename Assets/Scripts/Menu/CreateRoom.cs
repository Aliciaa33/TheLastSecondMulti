using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using TMPro;

public class CreateRoom : MonoBehaviourPunCallbacks
{
    [SerializeField] private TextMeshProUGUI playerListText;
    [SerializeField] private TextMeshProUGUI roomInfoText;
    [SerializeField] private Button startButton;
    [SerializeField] private int maxPlayersPerRoom = 4;

    private bool isRoomCreated = false;
    private string createdRoomName;
    private bool isLeaving = false; // guard against double-back

    void Start()
    {
        bool isRoomCreator = ConnectToServer.Instance.IsRoomCreator();
        // Debug.Log($"[CreateRoom] isRoomCreator={isRoomCreator}, InRoom={PhotonNetwork.InRoom}, IsMasterClient={PhotonNetwork.IsMasterClient}");

        if (startButton != null)
        {
            startButton.gameObject.SetActive(PhotonNetwork.IsMasterClient);
            Debug.Log("IsMasterClient: " + PhotonNetwork.IsMasterClient);
        }

        if (isRoomCreator)
        {
            StartCoroutine(WaitAndCreateRoom());
        }
        else if (PhotonNetwork.InRoom)
        {
            isRoomCreated = true;
            RefreshPlayerList();
        }
        else
        {
            Debug.LogWarning("[CreateRoom] Not room creator and not in room. Skipping room creation.");
        }
    }

    private IEnumerator WaitAndCreateRoom()
    {
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
        if (isRoomCreated)
            RefreshPlayerList();
    }

    public void OnStartGameButtonClicked()
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            Debug.LogWarning("Only room creator can start the game!");
            return;
        }

        Debug.Log("Starting game with " + PhotonNetwork.CurrentRoom.PlayerCount + " players");

        if (PhotonNetwork.CurrentRoom.PlayerCount == 1)
            ConnectToServer.Instance.SetGameMode(GameMode.SinglePlayer);
        else
            ConnectToServer.Instance.SetGameMode(GameMode.Multiplayer);

        PhotonNetwork.LoadLevel("Game");
    }

    private void RefreshPlayerList()
    {
        if (PhotonNetwork.CurrentRoom == null) return;

        if (roomInfoText != null)
        {
            roomInfoText.text = $"Room No.: {PhotonNetwork.CurrentRoom.Name}     " +
                                $"Players: {PhotonNetwork.CurrentRoom.PlayerCount} / {PhotonNetwork.CurrentRoom.MaxPlayers}" +
                                $"\nCreator: {PhotonNetwork.MasterClient.NickName}";
        }

        if (playerListText != null)
        {
            string playerListStr = "";
            int index = 1;
            foreach (Player player in PhotonNetwork.CurrentRoom.Players.Values)
            {
                string tag = (player == PhotonNetwork.LocalPlayer) ? " (You)" : "";
                playerListStr += $"Player {index}: {player.NickName}{tag}\n\n";
                index++;
            }
            playerListText.text = playerListStr;
        }
    }

    /// <summary>
    /// Waits for LeaveRoom to complete before loading the Menu scene.
    /// Going straight to SceneManager.LoadScene while Photon is still in
    /// "Leaving" state causes the SetProperties error.
    /// </summary>
    public void OnBackButtonClicked()
    {
        if (isLeaving) return;
        isLeaving = true;
        StartCoroutine(LeaveAndGoBack());
    }

    private IEnumerator LeaveAndGoBack()
    {
        if (PhotonNetwork.InRoom)
        {
            // Disable scene sync BEFORE leaving so Photon does not try to
            // write room properties when the Menu scene loads mid-leave.
            // This is the root cause of the "SetProperties / client state: Leaving" error.
            PhotonNetwork.AutomaticallySyncScene = false;

            PhotonNetwork.LeaveRoom();

            // Wait until Photon confirms the room has been left
            float timeout = 5f;
            while (PhotonNetwork.InRoom && timeout > 0f)
            {
                timeout -= Time.deltaTime;
                yield return null;
            }

            if (PhotonNetwork.InRoom)
                Debug.LogWarning("[CreateRoom] LeaveRoom timed out — loading anyway.");
        }

        SceneManager.LoadScene("Menu");
    }

    // ── Photon callbacks ──────────────────────────────────────────────────

    public override void OnCreatedRoom()
    {
        Debug.Log("Room created successfully: " + PhotonNetwork.CurrentRoom.Name);
        isRoomCreated = true;
        RefreshPlayerList();
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.LogError("Failed to create room: " + message);
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("Joined room callback in CreateRoom: " + PhotonNetwork.CurrentRoom.Name);
        isRoomCreated = true;
        RefreshPlayerList();
        if (startButton != null)
            startButton.gameObject.SetActive(PhotonNetwork.IsMasterClient);
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
        if (startButton != null)
            startButton.gameObject.SetActive(PhotonNetwork.IsMasterClient);
    }
}