using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Handles P/Escape key toggling, Time.timeScale, cursor unlock, and all pause button actions.
/// Mirrors the same SetUIMode pattern used by InventoryUI.
/// </summary>
public class PauseManager : MonoBehaviour
{
    [Header("Scene Names")]
    [SerializeField] private string menuSceneName = "Menu";

    [Header("Pause Keys")]
    [SerializeField] private KeyCode pauseKey = KeyCode.P;
    [SerializeField] private bool escapeAlsoPauses = true;

    private GameObject pauseCanvas;
    private GameObject settingsSection;
    private Slider musicSlider;
    private Slider sfxSlider;

    private bool isPaused = false;
    private bool settingsOpen = false;

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
            Debug.LogWarning("[PauseManager] 'PauseCanvas' not found. Run Tools > Create Pause Panel.");

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
        // Same lazy-find pattern as InventoryUI — safe even if player loads late
        if (playerInputs == null)
        {
            FindPlayerInputs();
            return;
        }

        // Block pause input while inventory (or any other UI) is already open
        if (playerInputs.IsUIMode && !isPaused)
            return;

        bool togglePressed = Input.GetKeyDown(pauseKey)
            || (escapeAlsoPauses && Input.GetKeyDown(KeyCode.Escape));

        if (togglePressed)
            TogglePause();
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

        // Unlock cursor exactly like InventoryUI does
        if (playerInputs != null)
            playerInputs.SetUIMode(true);

        SetPanelVisible(true);
    }

    public void Resume()
    {
        isPaused = false;
        Time.timeScale = 1f;

        // Restore game input mode exactly like InventoryUI.CloseInventory() does
        if (playerInputs != null)
            playerInputs.SetUIMode(false);

        SetPanelVisible(false);

        if (settingsOpen)
            ToggleSettings();
    }

    public void Restart()
    {
        Time.timeScale = 1f;
        if (playerInputs != null) playerInputs.SetUIMode(false);
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void QuitToMenu()
    {
        Time.timeScale = 1f;
        if (playerInputs != null) playerInputs.SetUIMode(true);
        SceneManager.LoadScene(menuSceneName);
    }

    public void ToggleSettings()
    {
        settingsOpen = !settingsOpen;
        if (settingsSection != null)
            settingsSection.SetActive(settingsOpen);
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
        if (pauseCanvas != null)
            pauseCanvas.SetActive(visible);
    }

    void WireButton(string goName, UnityEngine.Events.UnityAction action)
    {
        GameObject go = GameObject.Find(goName);
        if (go == null) { Debug.LogWarning($"[PauseManager] Button '{goName}' not found."); return; }
        Button btn = go.GetComponent<Button>();
        if (btn != null) btn.onClick.AddListener(action);
    }
}