using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Photon.Pun;
using Photon.Realtime;
using TMPro;

public class MainMenu : MonoBehaviourPunCallbacks
{
    [SerializeField] private TMP_InputField playerNameInput;

    private string playerName;

    void Start()
    {
        // Create ConnectToServer if it doesn't exist
        if (ConnectToServer.Instance == null)
        {
            GameObject gm = new GameObject("ConnectToServer");
            gm.AddComponent<ConnectToServer>();
        }
    }

    public void OnPlayButtonClicked()
    {
        // Get player name or generate random one
        playerName = GetOrGeneratePlayerName();

        // Set the player name in Photon
        PhotonNetwork.LocalPlayer.NickName = playerName;
        ConnectToServer.Instance.SetPlayerName(playerName);

        Debug.Log($"Playing as: {playerName}");

        // Start single player game
        PhotonNetwork.OfflineMode = false;
        ConnectToServer.Instance.SetGameMode(GameMode.SinglePlayer);

        // Create private 1-player room
        CreateSinglePlayerRoom();
    }

    public void OnCreateRoomButtonClicked()
    {
        // Get player name or generate random one
        playerName = GetOrGeneratePlayerName();

        // Set the player name in Photon
        PhotonNetwork.LocalPlayer.NickName = playerName;
        ConnectToServer.Instance.SetPlayerName(playerName);

        Debug.Log($"Creating room as: {playerName}");

        // Set mode to multiplayer
        PhotonNetwork.OfflineMode = false;
        ConnectToServer.Instance.SetGameMode(GameMode.Multiplayer);
        ConnectToServer.Instance.SetIsRoomCreator(true);

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

    public void OnJoinRoomButtonClicked()
    {
        // Get player name or generate random one
        playerName = GetOrGeneratePlayerName();

        // Set the player name in Photon
        PhotonNetwork.LocalPlayer.NickName = playerName;
        ConnectToServer.Instance.SetPlayerName(playerName);

        Debug.Log($"Looking for rooms as: {playerName}");

        // Set mode to multiplayer
        PhotonNetwork.OfflineMode = false;
        ConnectToServer.Instance.SetGameMode(GameMode.Multiplayer);

        // Go to room list scene
        if (PhotonNetwork.IsConnected)
        {
            SceneManager.LoadScene("RoomList");
        }
        else
        {
            Debug.Log("Waiting for connection...");
            StartCoroutine(WaitForConnectionThenLoadScene("RoomList"));
        }
    }

    private string GetOrGeneratePlayerName()
    {
        if(playerNameInput == null)
        {
            
        }
        // If player entered a name, use it
        if (playerNameInput != null && !string.IsNullOrEmpty(playerNameInput.text))
        {
            return playerNameInput.text.Trim();
        }

        // Otherwise, generate random name
        return GenerateRandomName();
    }

    private string GenerateRandomName()
    {
        string[] names = new string[]
        {
            "Shadow", "Phoenix", "Dragon", "Tiger", "Wolf",
            "Storm", "Thunder", "Blaze", "Frost", "Void",
            "Alpha", "Beta", "Gamma", "Delta", "Echo",
            "Nova", "Zenith", "Apex", "Pixel", "Neon"
        };

        string randomName = names[Random.Range(0, names.Length)];
        int randomNumber = Random.Range(100, 999);
        return randomName + randomNumber;
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

    private void CreateSinglePlayerRoom()
    {
        string roomName = "SP_" + PhotonNetwork.LocalPlayer.UserId;
        RoomOptions roomOpts = new RoomOptions
        {
            MaxPlayers = 1,
            IsVisible = false,
            IsOpen = false,
            CleanupCacheOnLeave = false
        };

        PhotonNetwork.CreateRoom(roomName, roomOpts);
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("Joined room: " + PhotonNetwork.CurrentRoom.Name);
        SceneManager.LoadScene("Game");
    }

    public void QuitGame()
    {
        if (PhotonNetwork.IsConnected)
        {
            PhotonNetwork.Disconnect();
        }
        Application.Quit();
    }
}