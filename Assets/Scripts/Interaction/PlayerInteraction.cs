using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using Photon.Realtime;

public class PlayerInteraction : MonoBehaviourPun
{
    [Header("Interaction Settings")]
    public float interactionRange = 3f;
    public LayerMask interactableLayer = -1;
    public KeyCode interactKey = KeyCode.F;

    [Header("UI References")]
    // UI element to show "Press F to interact"
    public GameObject interactionPrompt;
    // Text component to show what can be interacted with
    public TextMeshProUGUI promptText;

    private Camera playerCamera;
    private IInteractable currentInteractable;

    void Start()
    {
        PhotonView pv = GetComponent<PhotonView>();
        bool isLocalPlayer = pv == null || pv.ViewID == 0 || pv.IsMine;

        if (!isLocalPlayer)
        {
            enabled = false;
            return;
        }

        GameObject canvas = GameObject.Find("Canvas");
        if (canvas != null)
        {
            Transform promptTransform = canvas.transform.Find("InteractionPrompt");
            if (promptTransform != null)
            {
                interactionPrompt = promptTransform.gameObject;
                promptText = interactionPrompt.GetComponentInChildren<TextMeshProUGUI>(true);
                // true = include inactive children
            }
            else
                Debug.LogError("InteractionPrompt not found under Canvas!");
        }
        else
            Debug.LogError("Canvas not found!");


        playerCamera = Camera.main;
        if (playerCamera == null)
            playerCamera = FindObjectOfType<Camera>();

        if (interactionPrompt != null)
            interactionPrompt.SetActive(false);
    }

    void Update()
    {
        CheckForInteractable();
        HandleInteractionInput();
    }

    void CheckForInteractable()
    {
        RaycastHit hit;
        Vector3 rayOrigin = transform.position + Vector3.up * 0.3f;
        Vector3 rayDirection = playerCamera.transform.forward;

        // 用球形射线检测以增加交互的容错率
        float sphereRadius = 0.7f;
        Debug.DrawRay(rayOrigin, rayDirection * interactionRange, Color.red, 0.1f);

        if (Physics.SphereCast(rayOrigin, sphereRadius, rayDirection, out hit, interactionRange, interactableLayer))
        {
            // Debug.Log($"Hit: {hit.collider.gameObject.name} on layer: {LayerMask.LayerToName(hit.collider.gameObject.layer)}");
            IInteractable interactable = hit.collider.GetComponent<IInteractable>();
            // Debug.Log($"IInteractable found: {interactable != null}");

            if (interactable != null)
            {
                if (currentInteractable != interactable)
                {
                    currentInteractable = interactable;
                    ShowInteractionPrompt(interactable.GetInteractionText());
                }
            }
            else
            {
                HideInteractionPrompt();
            }
        }
        else
        {
            HideInteractionPrompt();
        }
    }

    void HandleInteractionInput()
    {
        if (Input.GetKeyDown(interactKey) && currentInteractable != null)
        {
            currentInteractable.Interact();
        }
    }

    void ShowInteractionPrompt(string text)
    {
        if (interactionPrompt != null)
        {
            interactionPrompt.SetActive(true);
            if (promptText != null)
                promptText.text = text;
        }
    }

    public void HideInteractionPrompt()
    {
        currentInteractable = null;
        if (interactionPrompt != null)
            interactionPrompt.SetActive(false);
    }

    void OnDrawGizmosSelected()
    {
        if (playerCamera != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(playerCamera.transform.position, playerCamera.transform.forward * interactionRange);
        }
    }
}