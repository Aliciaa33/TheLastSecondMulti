using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
using System.Collections.Generic;
using TMPro;

public class RoomList : MonoBehaviourPunCallbacks
{
    [SerializeField] private Transform roomListContainer;
    [SerializeField] private GameObject roomListItemPrefab;
    [SerializeField] private TextMeshProUGUI statusText;
    // [SerializeField] private Button refreshButton;

    private Dictionary<string, RoomInfo> cachedRoomList = new Dictionary<string, RoomInfo>();

    void Start()
    {
        // Join lobby to see available rooms
        if (!PhotonNetwork.InLobby)
        {
            PhotonNetwork.JoinLobby();
        }

        statusText.text = "Fetching available rooms...";
    }

    public void OnCreateRoomButtonClicked()
    {
        // Get player name or generate random one
        string playerName = ConnectToServer.Instance.GetPlayerName();

        // Set the player name in Photon
        PhotonNetwork.LocalPlayer.NickName = playerName;
        ConnectToServer.Instance.SetPlayerName(playerName);

        Debug.Log($"Creating room as: {playerName}");

        // Set mode to multiplayer
        PhotonNetwork.OfflineMode = false;
        ConnectToServer.Instance.SetGameMode(GameMode.Multiplayer);

        // Go to create room scene
        if (PhotonNetwork.IsConnected)
        {
            SceneManager.LoadScene("CreateRoom");
        }
        else
        {
            Debug.Log("Waiting for connection...");
            StartCoroutine(WaitForConnectionThenLoadScene("CreateRoom"));
        }
    }

    public void OnRefreshButtonClicked()
    {
        // Rejoin lobby to refresh room list
        if (PhotonNetwork.InLobby)
        {
            PhotonNetwork.LeaveLobby();
        }
        PhotonNetwork.JoinLobby();
        statusText.text = "Refreshing room list...";
    }

    private void DisplayRoomList()
    {
        // Clear existing list
        foreach (Transform child in roomListContainer)
        {
            Destroy(child.gameObject);
        }

        if (cachedRoomList.Count == 0)
        {
            statusText.text = "No available rooms. Create a new one!";
            return;
        }

        statusText.text = $"Available Rooms: {cachedRoomList.Count}";

        // Display each room
        foreach (RoomInfo room in cachedRoomList.Values)
        {
            // Skip rooms that are full or closed
            if (room.RemovedFromList || room.PlayerCount >= room.MaxPlayers)
            {
                continue;
            }

            GameObject roomItem = Instantiate(roomListItemPrefab, roomListContainer);

            // Setup room item UI
            TextMeshProUGUI roomNameText = roomItem.transform.Find("RoomName").GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI playerCountText = roomItem.transform.Find("PlayerCount").GetComponent<TextMeshProUGUI>();
            Button joinButton = roomItem.transform.Find("Join").GetComponent<Button>();

            roomNameText.text = room.Name;
            playerCountText.text = $"{room.PlayerCount} / {room.MaxPlayers}";

            // Add listener for join button
            string roomNameForListener = room.Name;
            joinButton.onClick.AddListener(() => JoinRoom(roomNameForListener));
        }
    }

    private void JoinRoom(string roomName)
    {
        Debug.Log("Joining room: " + roomName);
        ConnectToServer.Instance.SetGameMode(GameMode.Multiplayer);
        ConnectToServer.Instance.SetIsRoomCreator(false);
        PhotonNetwork.JoinRoom(roomName);
    }

    public void OnBackButtonClicked()
    {
        if (PhotonNetwork.InLobby)
        {
            PhotonNetwork.LeaveLobby();
        }
        SceneManager.LoadScene("Menu");
    }

    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        Debug.Log("Room list updated. Total rooms: " + roomList.Count);

        foreach (RoomInfo room in roomList)
        {
            if (room.RemovedFromList)
            {
                cachedRoomList.Remove(room.Name);
            }
            else
            {
                cachedRoomList[room.Name] = room;
            }
        }

        DisplayRoomList();
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("✓ Joined room: " + PhotonNetwork.CurrentRoom.Name);
        SceneManager.LoadScene("CreateRoom");
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.LogError("✗ Failed to join room: " + message);
        statusText.text = "Failed to join room!";
    }

    public override void OnJoinedLobby()
    {
        Debug.Log("✓ Joined Lobby");
    }

    public override void OnLeftLobby()
    {
        Debug.Log("Left Lobby");
    }

    System.Collections.IEnumerator WaitForConnectionThenLoadScene(string sceneName)
    {
        float timeout = 10f;
        while (!PhotonNetwork.IsConnected && timeout > 0)
        {
            yield return new WaitForSeconds(0.5f);
            timeout -= 0.5f;
        }

        if (PhotonNetwork.IsConnected)
        {
            SceneManager.LoadScene(sceneName);
        }
        else
        {
            Debug.LogError("Failed to connect to Photon");
        }
    }
}