using UnityEngine;
using UnityEngine.SceneManagement;
using Photon.Pun;
using System.Collections;
using Microsoft.VisualBasic;

public class MiniGameManager : MonoBehaviour
{
    public static MiniGameManager Instance { get; private set; }

    [Header("UICanvas")]
    public GameObject uiCanvas;

    private bool _inMiniGame = false;
    private bool _pendingPotionReward = false;
    private string _currentMiniGameScene;

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
            return;
        }
    }

    public void EnterMiniGame(string miniGameScene)
    {
        if (_inMiniGame) return;

        _inMiniGame = true;
        _currentMiniGameScene = miniGameScene;

        // Pause the 3D game
        PauseGame();

        // Load mini game ON TOP of existing scene
        SceneManager.LoadSceneAsync(miniGameScene, LoadSceneMode.Additive);

        Debug.Log($"Loading mini game additively: {miniGameScene}");
    }

    public void ExitMiniGame(bool wonGame = false)
    {
        if (!_inMiniGame) return;

        _inMiniGame = false;
        _pendingPotionReward = wonGame;

        // Unload mini game scene — Game scene stays untouched
        StartCoroutine(UnloadMiniGame());
    }

    private IEnumerator UnloadMiniGame()
    {
        // Unload the mini game scene
        AsyncOperation unload = SceneManager.UnloadSceneAsync(_currentMiniGameScene);
        yield return unload;

        // Resume the 3D game
        ResumeGame();

        // Give reward if won
        if (_pendingPotionReward)
        {
            _pendingPotionReward = false;

            if (InventoryManager.Instance != null)
                InventoryManager.Instance.AddPotion();

            if (UIManager.Instance != null)
                UIManager.Instance.ShowToast("🎉 Mini Game Won! You received a Potion!");
        }

        Debug.Log("Mini game unloaded, game resumed");
    }

    private void PauseGame()
    {
        // Freeze the 3D game world
        Time.timeScale = 0f;

        // Unlock cursor for mini game
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Pause Photon to prevent network events during mini game
        PhotonNetwork.IsMessageQueueRunning = false;

        // Disable player input
        DisablePlayer();

        // Hide any 3D game UI elements if needed
        uiCanvas.SetActive(false);
    }

    private void ResumeGame()
    {
        // Unfreeze the 3D game world
        Time.timeScale = 1f;

        // Restore cursor lock
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Resume Photon
        PhotonNetwork.IsMessageQueueRunning = true;

        // Re-enable player input
        EnablePlayer();

        // Show 3D game UI elements if needed
        uiCanvas.SetActive(true);
    }

    private void DisablePlayer()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        foreach (GameObject player in players)
        {
            var controller = player.GetComponent<StarterAssets.ThirdPersonController>();
            if (controller != null) controller.enabled = false;

            // Also disable interaction so prompt disappears
            var interaction = player.GetComponent<PlayerInteraction>();
            if (interaction != null)
            {
                interaction.enabled = false;
            }
        }
    }

    private void EnablePlayer()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        foreach (GameObject player in players)
        {
            var controller = player.GetComponent<StarterAssets.ThirdPersonController>();
            if (controller != null) controller.enabled = true;

            // Also disable interaction so prompt disappears
            var interaction = player.GetComponent<PlayerInteraction>();
            if (interaction != null)
            {
                interaction.enabled = true;
                // hide interaction prompt
                interaction.HideInteractionPrompt();
            }
        }
    }

    public bool IsInMiniGame() => _inMiniGame;
}