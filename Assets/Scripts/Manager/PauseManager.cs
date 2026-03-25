using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Handles P/Escape key toggling, Time.timeScale, cursor unlock, and all pause button actions.
/// Leaves the Photon room cleanly before returning to the main menu.
/// </summary>
public class PauseManager : MonoBehaviour
{
    public static PauseManager Instance { get; private set; }

    [Header("Scene Names")]
    [SerializeField] private string mainMenuSceneName = "Menu";

    [Header("Pause Keys")]
    [SerializeField] private KeyCode pauseKey = KeyCode.P;
    [SerializeField] private bool escapeAlsoPauses = true;

    private GameObject pauseCanvas;
    private GameObject settingsSection;
    private Slider musicSlider;
    private Slider sfxSlider;

    private bool isPaused = false;
    private bool settingsOpen = false;
    private bool isQuitting = false; // guard against double-quit

    private StarterAssets.StarterAssetsInputs playerInputs;

    private const string MusicVolumeKey = "MusicVolume";
    private const string SFXVolumeKey = "SFXVolume";

    void Awake()
    {
        pauseCanvas = GameObject.Find("PauseCanvas");
        settingsSection = GameObject.Find("SettingsSection");
        musicSlider = GameObject.Find("MusicSlider")?.GetComponent<Slider>();
        sfxSlider = GameObject.Find("SFXSlider")?.GetComponent<Slider>();

        if (pauseCanvas == null)
            Debug.LogWarning("[PauseManager] 'PauseCanvas' not found.");

        WireButton("ResumeButton", Resume);
        WireButton("RestartButton", Restart);
        WireButton("SettingsButton", ToggleSettings);
        WireButton("QuitButton", QuitToMenu);

        if (musicSlider != null)
        {
            musicSlider.value = PlayerPrefs.GetFloat(MusicVolumeKey, 0.7f);
            musicSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
        }
        if (sfxSlider != null)
        {
            sfxSlider.value = PlayerPrefs.GetFloat(SFXVolumeKey, 0.85f);
            sfxSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
        }

        SetPanelVisible(false);
    }

    void Update()
    {
        // Don't allow pausing during mini game
        if (MiniGameManager.Instance != null && MiniGameManager.Instance.IsInMiniGame())
            return;

        if (isQuitting) return;

        if (playerInputs == null)
        {
            FindPlayerInputs();
            return;
        }

        if (playerInputs.IsUIMode && !isPaused) return;

        bool togglePressed = Input.GetKeyDown(pauseKey)
            || (escapeAlsoPauses && Input.GetKeyDown(KeyCode.Escape));

        if (togglePressed) TogglePause();
    }

    void FindPlayerInputs()
    {
        foreach (GameObject player in GameObject.FindGameObjectsWithTag("Player"))
        {
            if (player == null) continue;
            playerInputs = player.GetComponent<StarterAssets.StarterAssetsInputs>();
            if (playerInputs != null) break;
        }
    }

    // ── Pause / Resume ────────────────────────────────────────────────────

    public void TogglePause()
    {
        if (isPaused) Resume(); else Pause();
    }

    public void Pause()
    {
        isPaused = true;
        Time.timeScale = 0f;
        if (playerInputs != null) playerInputs.SetUIMode(true);
        SetPanelVisible(true);
    }

    public void Resume()
    {
        isPaused = false;
        Time.timeScale = 1f;
        if (playerInputs != null) playerInputs.SetUIMode(false);
        SetPanelVisible(false);
        if (settingsOpen) ToggleSettings();
    }

    public void Restart()
    {
        Time.timeScale = 1f;
        if (playerInputs != null) playerInputs.SetUIMode(false);
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    /// <summary>
    /// Cleanly leaves the Photon room before loading the main menu.
    /// Prevents the "SendError / NetworkUnreachable" bug caused by
    /// abandoning an active Photon connection mid-game.
    /// </summary>
    public void QuitToMenu()
    {
        if (isQuitting) return;
        isQuitting = true;

        // Restore time and cursor immediately so UI stays responsive
        Time.timeScale = 1f;
        if (playerInputs != null) playerInputs.SetUIMode(false);

        StartCoroutine(QuitToMenuCoroutine());
    }

    private IEnumerator QuitToMenuCoroutine()
    {
        // ── Step 0: Tell ConnectToServer this is intentional ──────────────
        // Must be done BEFORE Disconnect() fires OnDisconnected
        if (ConnectToServer.Instance != null)
            ConnectToServer.Instance.StopWatchingConnection();

        // ── Step 1: Leave the current room if we are in one ───────────────
        if (Photon.Pun.PhotonNetwork.InRoom)
        {
            // Disable scene sync BEFORE leaving — prevents Photon from trying
            // to write room properties when the Loading scene loads mid-leave,
            // which causes the "SetProperties / client state: Leaving" error.
            Photon.Pun.PhotonNetwork.AutomaticallySyncScene = false;

            Photon.Pun.PhotonNetwork.LeaveRoom();

            // Wait until Photon confirms we have left the room
            float timeout = 5f;
            while (Photon.Pun.PhotonNetwork.InRoom && timeout > 0f)
            {
                timeout -= Time.unscaledDeltaTime;
                yield return null;
            }

            if (Photon.Pun.PhotonNetwork.InRoom)
                Debug.LogWarning("[PauseManager] LeaveRoom timed out — forcing disconnect.");
        }

        // ── Step 2: Disconnect from Photon entirely ───────────────────────
        // This ensures the next scene starts with a clean connection state.
        // LoadingSceneManager will reconnect when the Loading scene runs.
        if (Photon.Pun.PhotonNetwork.IsConnected)
        {
            Photon.Pun.PhotonNetwork.Disconnect();

            float timeout = 5f;
            while (Photon.Pun.PhotonNetwork.IsConnected && timeout > 0f)
            {
                timeout -= Time.unscaledDeltaTime;
                yield return null;
            }
        }

        // ── Step 3: Load the Loading scene (which reconnects) ────────────
        // We go through the Loading scene rather than jumping straight to
        // Menu so that Photon is fully reconnected before the player
        // tries to create/join a room again.
        LoadingSceneManager.RedirectToLoading(returnTo: mainMenuSceneName);
    }

    public void ToggleSettings()
    {
        settingsOpen = !settingsOpen;
        if (settingsSection != null) settingsSection.SetActive(settingsOpen);
    }

    public bool IsPaused => isPaused;

    // ── Audio ─────────────────────────────────────────────────────────────

    void OnMusicVolumeChanged(float value)
    {
        PlayerPrefs.SetFloat(MusicVolumeKey, value);
        // AudioManager.Instance?.SetMusicVolume(value);
    }

    void OnSFXVolumeChanged(float value)
    {
        PlayerPrefs.SetFloat(SFXVolumeKey, value);
        // AudioManager.Instance?.SetSFXVolume(value);
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    void SetPanelVisible(bool visible)
    {
        if (pauseCanvas != null) pauseCanvas.SetActive(visible);
    }

    void WireButton(string goName, UnityEngine.Events.UnityAction action)
    {
        GameObject go = GameObject.Find(goName);
        if (go == null) { Debug.LogWarning($"[PauseManager] Button '{goName}' not found."); return; }
        Button btn = go.GetComponent<Button>();
        if (btn != null) btn.onClick.AddListener(action);
    }
}