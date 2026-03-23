// MiniGameTriggerZone.cs
using UnityEngine;

public class MiniGameTrigger : MonoBehaviour, IInteractable
{
    public string miniGameSceneName = "WhackAMole";

    public string GetInteractionText()
    {
        if (MiniGameCooldownManager.Instance != null &&
            MiniGameCooldownManager.Instance.IsOnCooldown())
        {
            float remaining = MiniGameCooldownManager.Instance.GetRemainingCooldown();
            return $"Available in {Mathf.CeilToInt(remaining)}s";
        }
        return $"Play Whack-A-Mole";
    }

    public void Interact()
    {
        // Check cooldown
        if (MiniGameCooldownManager.Instance != null &&
            MiniGameCooldownManager.Instance.IsOnCooldown())
        {
            float remaining = MiniGameCooldownManager.Instance.GetRemainingCooldown();
            UIManager.Instance?.ShowToast(
                $"Please wait {Mathf.CeilToInt(remaining)}s before playing again!");
            return;
        }

        // Enter mini game
        MiniGameManager.Instance.EnterMiniGame(miniGameSceneName);
    }
}