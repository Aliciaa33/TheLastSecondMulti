using UnityEngine;
using UnityEngine.UI;

public class MiniGameCloseHandler : MonoBehaviour
{
    public Button closeButton;     // 右上角 "✕" 按钮
    public Button winCloseButton;  // 通关面板上的关闭按钮

    void Start()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(Close);
        if (winCloseButton != null)
            winCloseButton.onClick.AddListener(Close);
    }

    private void Close()
    {
        if (MiniGameManager.Instance != null)
            MiniGameManager.Instance.CloseMiniGame();
    }
}