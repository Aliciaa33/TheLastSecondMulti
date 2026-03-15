using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("HP UI")]
    public TextMeshProUGUI hpText;
    public Slider hpSlider;
    public Image[] hpHearts; // Alternative heart-based display

    [Header("Defused Bombs UI")]
    public TextMeshProUGUI defusedBombsText;

    [Header("Toast Notifications")]
    public GameObject toastPanel;
    public Text toastText;
    public float toastDuration = 3f;

    [Header("Game Over UI")]
    [SerializeField] private GameObject gameOverCanvas;

    public TextMeshProUGUI resultText;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        if (toastPanel != null)
            toastPanel.SetActive(false);

        if (gameOverCanvas != null)
            gameOverCanvas.SetActive(false);

        if (defusedBombsText != null)
            UpdateDefusedBombs();
    }

    public void UpdateHP(int current, int max)
    {
        // Update text
        if (hpText != null)
        {
            hpText.text = $"{current}/{max}";
        }

        // Update slider
        if (hpSlider != null)
        {
            hpSlider.value = current;
        }
    }

    public void UpdateDefusedBombs()
    {
        if (defusedBombsText != null)
        {
            int defused = GameManager.Instance.defusedBombs;
            int goal = GameManager.Instance.goal;
            defusedBombsText.text = $"{defused}/{goal}";
        }
    }

    public void ShowToast(string message)
    {
        if (toastPanel != null && toastText != null)
        {
            toastText.text = message;
            toastPanel.SetActive(true);
            StopAllCoroutines();
            StartCoroutine(HideToastAfterDelay());
        }
    }

    IEnumerator HideToastAfterDelay()
    {
        yield return new WaitForSeconds(toastDuration);
        if (toastPanel != null)
            toastPanel.SetActive(false);
    }

    public void ShowGameOver(bool win)
    {
        if (gameOverCanvas == null)
        {
            Debug.LogWarning("[UIManager] gameOverCanvas not assigned.");
            return;
        }

        gameOverCanvas.SetActive(true);

        // Pass live stats so the panel shows real numbers
        GameOverPanelStyler styler = gameOverCanvas.GetComponent<GameOverPanelStyler>();
        if (styler != null)
        {
            int hp = GameManager.Instance != null ? GameManager.Instance.currentHP : 0;
            int maxHP = GameManager.Instance != null ? GameManager.Instance.maxHP : 5;
            int defused = GameManager.Instance != null ? GameManager.Instance.defusedBombs : 0;
            int goal = GameManager.Instance != null ? GameManager.Instance.goal : 3;
            int hints = InventoryManager.Instance != null
                        ? InventoryManager.Instance.GetCollectedHints().Count : 0;

            styler.ApplyResultStyle(win, hp, maxHP, defused, goal, hints);
        }

        // Wire buttons if not yet wired
        WireGameOverButtons();
    }

    private bool gameOverButtonsWired = false;
    void WireGameOverButtons()
    {
        if (gameOverButtonsWired || gameOverCanvas == null) return;
        gameOverButtonsWired = true;

        UnityEngine.UI.Button restart =
            gameOverCanvas.GetComponentsInChildren<UnityEngine.UI.Button>(true)
            [0]; // RestartButton is first
        UnityEngine.UI.Button quit =
            gameOverCanvas.GetComponentsInChildren<UnityEngine.UI.Button>(true)
            .Length > 1
            ? gameOverCanvas.GetComponentsInChildren<UnityEngine.UI.Button>(true)[1]
            : null;

        // Better: find by name
        foreach (var btn in gameOverCanvas.GetComponentsInChildren<UnityEngine.UI.Button>(true))
        {
            if (btn.name == "RestartButton")
                btn.onClick.AddListener(() =>
                {
                    gameOverCanvas.SetActive(false);
                    UnityEngine.SceneManagement.SceneManager.LoadScene(
                        UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
                });

            if (btn.name == "QuitMenuButton")
                btn.onClick.AddListener(() =>
                {
                    gameOverCanvas.SetActive(false);
                    // Use the same clean-quit flow as PauseManager
                    Time.timeScale = 1f;
                    LoadingSceneManager.RedirectToLoading(returnTo: "Menu");
                });
        }
    }

    public void RestartButton()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}