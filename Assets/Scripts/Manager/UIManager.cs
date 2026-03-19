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

    [Header("Game Over")]
    public GameObject gameOverPanel;
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

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

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
        if (gameOverPanel != null)
        {
            if (win)
                gameOverPanel.GetComponentInChildren<TextMeshProUGUI>().text = "You Win!";
            else
                gameOverPanel.GetComponentInChildren<TextMeshProUGUI>().text = "Game Over!";
            gameOverPanel.SetActive(true);
        }

        if (resultText != null)
        {
            int defused = GameManager.Instance.defusedBombs;
            int goal = GameManager.Instance.goal;
            resultText.text = $"Defused Bombs: {defused}/{goal}";
        }
    }

    public void RestartButton()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}