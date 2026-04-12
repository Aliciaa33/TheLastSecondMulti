using System.Collections;
using UnityEngine;
using TMPro;

public class WhackAMoleChatBubbleController : MonoBehaviour
{
    public TextMeshProUGUI messageText;
    public float charDelay = 0.04f;
    public float fadeDuration = 0.12f;

    private Coroutine typeRoutine;
    private CanvasGroup canvasGroup;
    private bool hasInitialized = false;

    private bool isShowing = false;
    private bool canOpenMiniGame = false;

    public string miniGameSceneName = "WhackAMole";

    void Awake()
    {
        InitIfNeeded();
    }

    private void InitIfNeeded()
    {
        if (hasInitialized) return;
        hasInitialized = true;

        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        if (messageText == null)
            messageText = GetComponentInChildren<TextMeshProUGUI>(true);

        if (canvasGroup != null) canvasGroup.alpha = 0f;
        if (messageText != null) messageText.text = "";
    }

    void Update()
    {
        if (!isShowing || !canOpenMiniGame) return;

        if (Input.GetKeyDown(KeyCode.F))
        {
            if (MiniGameManager.Instance == null) return;
            if (MiniGameManager.Instance.IsInMiniGame()) return;

            // 冷却中不允许打开
            if (MiniGameCooldownManager.Instance != null && MiniGameCooldownManager.Instance.IsOnCooldown())
                return;

            MiniGameManager.Instance.EnterMiniGame(miniGameSceneName);
        }
    }

    // 可交互提示：会监听 E
    public void ShowMessage(string msg, float perCharDelay)
    {
        charDelay = perCharDelay;

        gameObject.SetActive(true);
        InitIfNeeded();

        isShowing = true;
        canOpenMiniGame = true;

        if (typeRoutine != null) StopCoroutine(typeRoutine);
        typeRoutine = StartCoroutine(TypeRoutine(msg));
    }

    // 非交互提示：只显示文本，不监听 E
    public void SetDirectText(string text)
    {
        if (typeRoutine != null)
        {
            StopCoroutine(typeRoutine);
            typeRoutine = null;
        }

        gameObject.SetActive(true);
        InitIfNeeded();

        isShowing = true;
        canOpenMiniGame = false;

        if (canvasGroup != null) canvasGroup.alpha = 1f;
        if (messageText != null) messageText.text = text;
    }

    public void HideImmediate()
    {
        isShowing = false;
        canOpenMiniGame = false;

        if (typeRoutine != null)
        {
            StopCoroutine(typeRoutine);
            typeRoutine = null;
        }

        if (messageText != null) messageText.text = "";
        if (canvasGroup != null) canvasGroup.alpha = 0f;
        gameObject.SetActive(false);
    }

    public void HideWithStop()
    {
        isShowing = false;
        canOpenMiniGame = false;

        if (typeRoutine != null)
        {
            StopCoroutine(typeRoutine);
            typeRoutine = null;
        }

        if (messageText != null) messageText.text = "";

        if (!gameObject.activeInHierarchy)
        {
            if (canvasGroup != null) canvasGroup.alpha = 0f;
            gameObject.SetActive(false);
            return;
        }

        StartCoroutine(FadeOutAndDisable());
    }

    private IEnumerator TypeRoutine(string msg)
    {
        float t = 0f;
        if (canvasGroup != null) canvasGroup.alpha = 0f;

        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            if (canvasGroup != null)
                canvasGroup.alpha = Mathf.Clamp01(t / fadeDuration);
            yield return null;
        }

        if (canvasGroup != null) canvasGroup.alpha = 1f;
        if (messageText != null) messageText.text = "";

        foreach (char c in msg)
        {
            if (messageText != null) messageText.text += c;
            yield return new WaitForSeconds(charDelay);
        }

        typeRoutine = null;
    }

    private IEnumerator FadeOutAndDisable()
    {
        float t = 0f;
        float start = (canvasGroup != null) ? canvasGroup.alpha : 1f;

        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            if (canvasGroup != null)
                canvasGroup.alpha = Mathf.Lerp(start, 0f, t / fadeDuration);
            yield return null;
        }

        if (canvasGroup != null) canvasGroup.alpha = 0f;
        gameObject.SetActive(false);
    }
}