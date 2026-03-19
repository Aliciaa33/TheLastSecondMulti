using UnityEngine;
using TMPro;

public class InventoryItemUI : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI numberText;
    public TextMeshProUGUI hintText;

    public void SetupHintItem(string number, string hint)
    {
        // 设置UI显示
        if (numberText != null)
        {
            numberText.text = number;
            numberText.alignment = TextAlignmentOptions.Center;
        }

        if (hintText != null)
        {
            hintText.text = hint;
            hintText.alignment = TextAlignmentOptions.Center;
            Debug.Log("Hint Text: " + hintText.text);
        }
    }
}