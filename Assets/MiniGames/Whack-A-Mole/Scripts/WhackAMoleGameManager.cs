using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class WhackAMoleGameManager : MonoBehaviour
{
  public static WhackAMoleGameManager Instance { get; private set; }

  [SerializeField] private List<Mole> moles;

  [Header("UI objects")]
  [SerializeField] private GameObject gameUI;
  [SerializeField] private TextMeshProUGUI timeText;
  [SerializeField] private TextMeshProUGUI scoreText;
  [SerializeField] private TextMeshProUGUI goalText;
  [SerializeField] public GameObject countdownPanel;
  [SerializeField] public TextMeshProUGUI countdownText;

  [Header("Game Over UI")]
  [SerializeField] public GameObject gameOverPanel;
  [SerializeField] public TextMeshProUGUI gameOverTitle;
  [SerializeField] public TextMeshProUGUI resultMessage;
  [SerializeField] public TextMeshProUGUI resultScore;
  [SerializeField] public GameObject winStars;
  [SerializeField] public GameObject loseStar;
  [SerializeField] public GameObject winIcon;
  [SerializeField] public GameObject loseIcon;


  // Hardcoded variables you may want to tune.
  private float startingTime = 2f;
  private int goal = 5;
  public bool win = false;

  // Global variables
  private float timeRemaining;
  private HashSet<Mole> currentMoles = new HashSet<Mole>();
  private int score;
  private bool playing = false;

  void Awake()
  {
    if (Instance == null) Instance = this;
    else Destroy(gameObject);
  }

  void Start()
  {
    // restartButton.onClick.AddListener(RestartGame);
    // backTo3DButton.onClick.AddListener(BackTo3DGame);
    StartCoroutine(CountdownAndStart());
  }

  IEnumerator CountdownAndStart()
  {
    countdownPanel.SetActive(true);
    string[] counts = { "3", "2", "1", "GO!" };
    foreach (string count in counts)
    {
      countdownText.text = count;
      yield return new WaitForSecondsRealtime(0.8f);
    }
    countdownPanel.SetActive(false);
    StartGame();
  }

  public void StartGame()
  {
    // Hide/show the UI elements we don't/do want to see.
    gameUI.SetActive(true);
    // Hide all the visible moles.
    for (int i = 0; i < moles.Count; i++)
    {
      moles[i].Hide();
      moles[i].SetIndex(i);
    }
    // Remove any old game state.
    currentMoles.Clear();
    // Start with 30 seconds.
    timeRemaining = startingTime;
    score = 0;
    scoreText.text = "0";
    goalText.text = $"{goal}";
    playing = true;
  }

  public void GameOver()
  {
    gameOverPanel.SetActive(true);
    if (score >= goal)
    {
      gameOverTitle.text = "You Win!";
      resultMessage.text = $"A Potion added to Inventory!";
      loseIcon.SetActive(false);
      loseStar.SetActive(false);
      win = true;
    }
    else
    {
      gameOverTitle.text = "Game Over!";
      resultMessage.text = $"Try Again Next Time!";
      winIcon.SetActive(false);
      winStars.SetActive(false);
    }

    resultScore.text = $"Score: {score} / {goal}";
    StartCoroutine(SlideInPanel(gameOverPanel.transform));

    // Hide the game UI.
    // Hide all moles.
    foreach (Mole mole in moles)
    {
      mole.StopGame();
    }
    // Stop the game and show the start UI.
    playing = false;
  }

  // Animation for sliding in the game over panel.
  IEnumerator SlideInPanel(Transform panel)
  {
    Vector3 offScreen = new Vector3(0, -Screen.height, 0);
    Vector3 onScreen = Vector3.zero;
    float elapsed = 0f;
    float duration = 0.4f;

    panel.localPosition = offScreen;

    while (elapsed < duration)
    {
      elapsed += Time.unscaledDeltaTime;
      float t = elapsed / duration;
      // Ease out
      t = 1f - Mathf.Pow(1f - t, 3f);
      panel.localPosition = Vector3.Lerp(offScreen, onScreen, t);
      yield return null;
    }

    panel.localPosition = onScreen;
  }

  // Update is called once per frame
  void Update()
  {
    if (playing)
    {
      // Update time.
      timeRemaining -= Time.unscaledDeltaTime;
      if (timeRemaining <= 0)
      {
        timeRemaining = 0;
        GameOver();
      }
      timeText.text = $"{(int)timeRemaining / 60}:{(int)timeRemaining % 60:D2}";
      // Check if we need to start any more moles.
      if (currentMoles.Count <= (score / 10))
      {
        // Choose a random mole.
        int index = Random.Range(0, moles.Count);
        // Doesn't matter if it's already doing something, we'll just try again next frame.
        if (!currentMoles.Contains(moles[index]))
        {
          currentMoles.Add(moles[index]);
          moles[index].Activate(score / 10);
        }
      }
    }
  }

  public void AddScore(int moleIndex)
  {
    // Add and update score.
    score += 1;
    scoreText.text = $"{score}";
    // Change the color of the score text to give feedback.
    if (score >= goal)
      scoreText.color = Color.green;
    // Increase time by a little bit.
    timeRemaining += 1;
    // Remove from active moles.
    currentMoles.Remove(moles[moleIndex]);
  }

  public void Missed(int moleIndex, bool isMole)
  {
    if (isMole)
    {
      // Decrease time by a little bit.
      timeRemaining -= 2;
    }
    // Remove from active moles.
    currentMoles.Remove(moles[moleIndex]);
  }
}