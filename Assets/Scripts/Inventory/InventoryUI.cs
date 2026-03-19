using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class InventoryUI : MonoBehaviour
{
    public static InventoryUI Instance { get; private set; }

    [Header("Inventory UI")]
    public GameObject inventoryPanel;
    public Transform inventoryGrid;
    public GameObject inventoryItem;
    public TextMeshProUGUI inventoryCountText;
    public Button inventoryCloseButton;

    [Header("Potion UI")]
    public TextMeshProUGUI potionCountText;
    public Button usePotionButton;

    private bool isInventoryOpen = false;
    private StarterAssets.StarterAssetsInputs playerInputs;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    void Start()
    {
        if (inventoryPanel != null)
            inventoryPanel.SetActive(false);

        if (inventoryCloseButton != null)
            inventoryCloseButton.onClick.AddListener(CloseInventory);

        if (usePotionButton != null)
            usePotionButton.onClick.AddListener(OnUsePotionClicked);
    }

    void Update()
    {
        if (playerInputs == null) { FindPlayerInputs(); return; }
        if (playerInputs.GetInventoryInput()) ToggleInventory();
    }

    void FindPlayerInputs()
    {
        foreach (GameObject p in GameObject.FindGameObjectsWithTag("Player"))
        {
            if (p == null) continue;
            playerInputs = p.GetComponent<StarterAssets.StarterAssetsInputs>();
            if (playerInputs != null) break;
        }
    }

    // ── Toggle / Open / Close ─────────────────────────────────────────────

    public void ToggleInventory()
    {
        if (isInventoryOpen) CloseInventory(); else OpenInventory();
    }

    public void OpenInventory()
    {
        isInventoryOpen = true;
        if (inventoryPanel != null) inventoryPanel.SetActive(true);
        if (playerInputs != null) playerInputs.SetUIMode(true);
        StartCoroutine(ShowInventoryNextFrame());
    }

    public void CloseInventory()
    {
        isInventoryOpen = false;
        if (inventoryPanel != null) inventoryPanel.SetActive(false);
        if (playerInputs != null) playerInputs.SetUIMode(false);
    }

    public void RefreshIfOpen()
    {
        if (isInventoryOpen)
            StartCoroutine(ShowInventoryNextFrame());
    }

    // ── Coroutine: clear → wait one frame → populate → rebuild ────────────
    // Waiting one frame lets Unity actually process the Destroy() calls
    // before we instantiate new cards and force the layout rebuild.
    IEnumerator ShowInventoryNextFrame()
    {
        // 1. Destroy all existing cards
        if (inventoryGrid != null)
            foreach (Transform child in inventoryGrid)
                Destroy(child.gameObject);

        // 2. Wait for Destroy to be processed
        yield return null;

        // 3. Populate hints
        if (InventoryManager.Instance != null)
        {
            List<InventoryManager.HintItem> hints =
                InventoryManager.Instance.GetCollectedHints();

            foreach (var hint in hints)
                CreateHintCard(hint);

            UpdateHintCount(hints.Count);
        }

        // 4. Populate potions
        ShowPotions();

        // 5. Force layout rebuild — now that cards actually exist and old
        //    ones are truly gone, this correctly resizes the content rect.
        if (inventoryGrid != null)
        {
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(
                inventoryGrid.GetComponent<RectTransform>());
            // Canvas.ForceUpdateCanvases();

            // RectTransform gridRect = inventoryGrid.GetComponent<RectTransform>();
            // LayoutRebuilder.ForceRebuildLayoutImmediate(gridRect);
            // LayoutRebuilder.ForceRebuildLayoutImmediate(
            //     gridRect.parent.GetComponent<RectTransform>());
        }

        // 6. Re-style cards
        InventoryPanelStyler styler =
            inventoryPanel != null
            ? inventoryPanel.GetComponent<InventoryPanelStyler>()
              ?? inventoryPanel.GetComponentInParent<InventoryPanelStyler>()
            : FindObjectOfType<InventoryPanelStyler>();
        if (styler != null) styler.RefreshItemCards();
    }

    public void ShowInventory() => StartCoroutine(ShowInventoryNextFrame());

    void ShowPotions()
    {
        if (InventoryManager.Instance == null) return;
        int count = InventoryManager.Instance.GetPotionCount();

        if (potionCountText != null)
            potionCountText.text = $"x {count}";

        if (usePotionButton != null)
        {
            usePotionButton.interactable = count > 0;
            Image btnImg = usePotionButton.GetComponent<Image>();
            if (btnImg != null)
                btnImg.color = count > 0
                    ? new Color(0.96f, 0.93f, 0.85f, 1f)
                    : new Color(0.5f, 0.5f, 0.5f, 0.5f);
        }
    }

    void CreateHintCard(InventoryManager.HintItem hintItem)
    {
        if (inventoryItem == null || inventoryGrid == null) return;
        GameObject card = Instantiate(inventoryItem, inventoryGrid);
        InventoryItemUI ui = card.GetComponent<InventoryItemUI>();
        if (ui != null)
            ui.SetupHintItem(hintItem.number, hintItem.hint);
        else
            Debug.LogError("[InventoryUI] InventoryItemUI not found on card prefab.");
    }

    void UpdateHintCount(int count)
    {
        if (inventoryCountText != null)
            inventoryCountText.text = $"HINTS COLLECTED: {count}";
    }

    void OnUsePotionClicked()
    {
        if (InventoryManager.Instance != null)
            InventoryManager.Instance.UsePotion();
    }
}