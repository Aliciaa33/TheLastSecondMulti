using UnityEngine;

public interface IInteractable
{
    string GetInteractionText();
    void Interact();
}

public abstract class InteractableBase : MonoBehaviour, IInteractable
{
    [Header("Interaction")]
    public string interactionText = "Press F to interact";

    public virtual string GetInteractionText()
    {
        return interactionText;
    }

    public abstract void Interact();
}