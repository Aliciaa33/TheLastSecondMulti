using UnityEngine;
using UnityEngine.SceneManagement;
using Photon.Pun;
using Photon.Realtime;

[DefaultExecutionOrder(-6)]
public class ConnectToServer : MonoBehaviourPunCallbacks
{
    public static ConnectToServer Instance { get; private set; }

    private GameMode currentGameMode = GameMode.SinglePlayer;
    private string playerName = "Player";
    private bool isRoomCreator = false;

    // Only true while player is actively in a multiplayer game.
    // PauseManager sets this to false before disconnecting intentionally,
    // so OnDisconnected knows not to treat it as an unexpected dropout.
    private bool watchForDisconnect = false;

    // Causes that are expected and should never trigger the dropout flow
    private static readonly DisconnectCause[] IntentionalCauses = {
        DisconnectCause.DisconnectByClientLogic,    // we called Disconnect()
        DisconnectCause.DisconnectByServerLogic,    // server-side clean close
        DisconnectCause.ApplicationQuit,            // player quit the app
    };

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            // Unlock cursor by default — individual scenes lock it as needed
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // Restore cursor whenever a new scene loads so no scene ever
    // inherits a locked/invisible cursor from the previous one.
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // The game scene locks the cursor itself via StarterAssetsInputs.
        // Menu and Loading scenes need it unlocked and visible.
        bool isGameScene = scene.name != "Menu" && scene.name != "Loading";
        if (!isGameScene)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    public void BeginWatchingConnection() => watchForDisconnect = true;
    public void StopWatchingConnection() => watchForDisconnect = false;

    public void SetGameMode(GameMode mode)
    {
        currentGameMode = mode;
        watchForDisconnect = (mode == GameMode.Multiplayer);
    }

    public void SetPlayerName(string name) => playerName = name;
    public void SetIsRoomCreator(bool v) => isRoomCreator = v;
    public GameMode GetGameMode() => currentGameMode;
    public string GetPlayerName() => playerName;
    public bool IsRoomCreator() => isRoomCreator;
    public bool IsMultiplayer() => currentGameMode == GameMode.Multiplayer;
    public bool IsSinglePlayer() => currentGameMode == GameMode.SinglePlayer;

    public override void OnConnectedToMaster()
    {
        // Re-enable scene sync on every fresh connection.
        // It gets disabled before leaving a room to prevent the
        // "SetProperties / client state: Leaving" error.
        PhotonNetwork.AutomaticallySyncScene = true;
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("[ConnectToServer] Joined room: " + PhotonNetwork.CurrentRoom.Name);
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        // Check if this is an intentional disconnect (e.g. from PauseManager.QuitToMenu)
        bool isIntentional = System.Array.IndexOf(IntentionalCauses, cause) >= 0;

        if (isIntentional)
        {
            // Expected — log at Info level only, never trigger dropout flow
            Debug.Log($"[ConnectToServer] Disconnected intentionally: {cause}");
            watchForDisconnect = false;
            return;
        }

        // Unexpected dropout (network failure, timeout, etc.)
        Debug.LogWarning($"[ConnectToServer] Unexpected disconnect: {cause}");

        if (watchForDisconnect)
        {
            watchForDisconnect = false;
            LoadingSceneManager.RedirectToLoading(
                reason: "You were disconnected: " + cause,
                returnTo: "Menu"
            );
        }
    }
}

public enum GameMode
{
    SinglePlayer,
    Multiplayer
}