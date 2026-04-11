// MiniGameTriggerZone.cs
using UnityEngine;

public class WhackAMoleTrigger : MonoBehaviour, IInteractable
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
        return $"Press F\nplay Whack A Mole";
    }

    public void Interact()
    {
        // Check cooldown
        if (MiniGameCooldownManager.Instance != null &&
            MiniGameCooldownManager.Instance.IsOnCooldown())
        {
            float remaining = MiniGameCooldownManager.Instance.GetRemainingCooldown();
            UIManager.Instance?.ShowToast(
                $"Please wait {Mathf.CeilToInt(remaining)}s before playing again!", 4);
            return;
        }

        // Start cooldown IMMEDIATELY when anyone enters
        // This prevents other players from entering simultaneously
        MiniGameCooldownManager.Instance?.StartCooldown();

        // Enter mini game
        MiniGameManager.Instance.EnterMiniGame(miniGameSceneName);
    }
}