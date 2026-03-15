using UnityEngine;
using UnityEngine.SceneManagement;
using Photon.Pun;
using Photon.Realtime;

/// <summary>
/// Attach to a GameObject called "LoadingScene" in your Loading scene.
/// Finds LoadingSceneUI automatically - no Inspector wiring needed.
/// </summary>
public class LoadingSceneManager : MonoBehaviourPunCallbacks
{
    [Header("Scene Names")]
    [SerializeField] private string menuSceneName = "Menu";

    // Found automatically - no Inspector wiring needed
    private LoadingSceneUI loadingUI;

    public static string TargetSceneAfterConnect = null;
    public static string StatusMessage = null;

    private bool isConnecting = false;

    void Awake()
    {
        // Find the UI controller automatically
        loadingUI = FindObjectOfType<LoadingSceneUI>();
        if (loadingUI == null)
            Debug.LogWarning("[LoadingSceneManager] No LoadingSceneUI found in scene.");
    }

    void Start()
    {
        if (!string.IsNullOrEmpty(StatusMessage))
        {
            loadingUI?.SetStatusMessage(StatusMessage);
            StatusMessage = null;
        }

        if (PhotonNetwork.IsConnected)
        {
            GoToTargetScene();
            return;
        }

        BeginConnecting();
    }

    private void BeginConnecting()
    {
        isConnecting = true;
        loadingUI?.SetState(LoadingState.Connecting);
        PhotonNetwork.ConnectUsingSettings();
    }

    public override void OnConnectedToMaster()
    {
        PhotonNetwork.AutomaticallySyncScene = true;
        isConnecting = false;
        loadingUI?.SetState(LoadingState.Success);
        Invoke(nameof(GoToTargetScene), 0.8f);
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.LogWarning("[LoadingSceneManager] Disconnected: " + cause);
        if (isConnecting)
        {
            loadingUI?.SetState(LoadingState.Failed);
            loadingUI?.SetStatusMessage("Could not connect: " + cause);
        }
    }

    private void GoToTargetScene()
    {
        string target = string.IsNullOrEmpty(TargetSceneAfterConnect)
            ? menuSceneName
            : TargetSceneAfterConnect;
        TargetSceneAfterConnect = null;
        SceneManager.LoadScene(target);
    }

    public void RetryConnection()
    {
        loadingUI?.SetStatusMessage(null);
        BeginConnecting();
    }

    public static void RedirectToLoading(string reason = null, string returnTo = null)
    {
        StatusMessage = reason;
        TargetSceneAfterConnect = returnTo;
        SceneManager.LoadScene("Loading");
    }
}
