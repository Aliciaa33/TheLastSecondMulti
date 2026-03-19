using UnityEngine;
using UnityEngine.UI;
using TMPro;
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

    private bool isInventoryOpen = false;
    private StarterAssets.StarterAssetsInputs playerInputs;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // 初始化UI状态
        if (inventoryPanel != null)
            inventoryPanel.SetActive(false);

        // 设置关闭按钮事件
        if (inventoryCloseButton != null)
        {
            inventoryCloseButton.onClick.AddListener(CloseInventory);
        }
    }

    void Update()
    {
        // 因为InventoryUI在场景中可能比玩家对象先加载，所以在Update中尝试获取玩家输入组件
        if (playerInputs == null)
        {
            FindPlayerInputs();
            return;
        }

        if (playerInputs.GetInventoryInput())
            ToggleInventory();

        // // 使用 Input System 检测库存按键
        // if (playerInputs != null && playerInputs.GetInventoryInput())
        //     ToggleInventory();

        // // ESC键关闭库存
        // if (isInventoryOpen && playerInputs.GetInventoryInput())
        // {
        //     CloseInventory();
        // }
    }

    void FindPlayerInputs()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        foreach (GameObject player in players)
        {
            if (player != null)
            {
                playerInputs = player.GetComponent<StarterAssets.StarterAssetsInputs>();
                if (playerInputs != null)
                {
                    break;
                }
            }
            else
            {
                Debug.LogWarning("Player object with 'Player' tag not found!");
            }
        }
    }

    public void ToggleInventory()
    {
        isInventoryOpen = !isInventoryOpen;
        inventoryPanel.SetActive(isInventoryOpen);

        // 切换输入模式
        if (playerInputs != null)
        {
            playerInputs.SetUIMode(isInventoryOpen);
        }

        if (isInventoryOpen)
        {
            ShowInventory();
        }
    }

    public void CloseInventory()
    {
        isInventoryOpen = false;
        if (inventoryPanel != null)
            inventoryPanel.SetActive(false);

        // 恢复游戏输入模式
        if (playerInputs != null)
        {
            playerInputs.SetUIMode(false);
        }
    }

    public void ShowInventory()
    {
        // 清空当前网格
        foreach (Transform child in inventoryGrid)
            Destroy(child.gameObject);

        // 创建物品UI
        if (InventoryManager.Instance != null)
        {
            List<InventoryManager.HintItem> hints = InventoryManager.Instance.GetCollectedHints();
            foreach (InventoryManager.HintItem hint in hints)
                CreateHintItemUI(hint);

            // 更新数量
            UpdateInventoryCount();
        }
    }

    public void UpdateInventoryCount()
    {
        if (inventoryCountText != null && InventoryManager.Instance != null)
        {
            int count = InventoryManager.Instance.GetCollectedHints().Count;
            inventoryCountText.text = $"Total: {count}";
        }
    }

    void CreateHintItemUI(InventoryManager.HintItem hintItem)
    {
        GameObject itemUI = Instantiate(inventoryItem, inventoryGrid);
        InventoryItemUI itemUIComponent = itemUI.GetComponent<InventoryItemUI>();

        if (itemUIComponent != null)
            itemUIComponent.SetupHintItem(hintItem.number, hintItem.hint);
        else
            Debug.LogError("InventoryItemUI component not found on card prefab!");
    }
}