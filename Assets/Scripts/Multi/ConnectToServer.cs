using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

[DefaultExecutionOrder(-6)]
public class ConnectToServer : MonoBehaviourPunCallbacks
{
    public static ConnectToServer Instance { get; private set; }

    private GameMode currentGameMode = GameMode.SinglePlayer;
    private string playerName = "Player";
    private bool isRoomCreator = false;
    private bool watchForDisconnect = false;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Connection is handled entirely by LoadingSceneManager now.
    // ConnectToServer only monitors for mid-game dropouts.

    public void BeginWatchingConnection()  => watchForDisconnect = true;
    public void StopWatchingConnection()   => watchForDisconnect = false;

    public void SetGameMode(GameMode mode)
    {
        currentGameMode = mode;
        watchForDisconnect = (mode == GameMode.Multiplayer);
    }

    public void SetPlayerName(string name) => playerName = name;
    public void SetIsRoomCreator(bool v)   => isRoomCreator = v;
    public GameMode GetGameMode()          => currentGameMode;
    public string GetPlayerName()          => playerName;
    public bool IsRoomCreator()            => isRoomCreator;
    public bool IsMultiplayer()            => currentGameMode == GameMode.Multiplayer;
    public bool IsSinglePlayer()           => currentGameMode == GameMode.SinglePlayer;

    public override void OnConnectedToMaster()
    {
        PhotonNetwork.AutomaticallySyncScene = true;
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("Joined room: " + PhotonNetwork.CurrentRoom.Name);
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.LogWarning("[ConnectToServer] Disconnected: " + cause);
        if (watchForDisconnect)
        {
            watchForDisconnect = false;
            LoadingSceneManager.RedirectToLoading(
                reason: "You were disconnected: " + cause,
                returnTo: "MainMenu"
            );
        }
    }
}

public enum GameMode
{
    SinglePlayer,
    Multiplayer
}
