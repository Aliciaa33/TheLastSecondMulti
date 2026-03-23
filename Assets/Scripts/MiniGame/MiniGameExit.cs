using UnityEngine;
using UnityEngine.UI;

public class WhackAMoleExit : MonoBehaviour
{
    void Start()
    {
        // Add to existing game over button, or create new one
        GetComponent<Button>().onClick.AddListener(() =>
        {
            MiniGameManager.Instance.ExitMiniGame(WhackAMoleGameManager.Instance.win);

            // Start cooldown for all players
            MiniGameCooldownManager.Instance?.StartCooldown();
        });
    }
}