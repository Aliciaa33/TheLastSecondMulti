using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ChatBubbleController : MonoBehaviour
{
    public Text messageText; // inspector 绑定 UI Text (或 TextMeshProUGUI)
    public float charDelay = 0.04f;
    public float fadeDuration = 0.12f;

    private Coroutine typeRoutine;
    private CanvasGroup canvasGroup;

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
        HideImmediate();
    }

    public void ShowMessage(string msg, float perCharDelay)
    {
        charDelay = perCharDelay;
        if (typeRoutine != null) StopCoroutine(typeRoutine);
        typeRoutine = StartCoroutine(TypeRoutine(msg));
    }

    public void HideImmediate()
    {
        if (typeRoutine != null) { StopCoroutine(typeRoutine); typeRoutine = null; }
        messageText.text = "";
        canvasGroup.alpha = 0f;
        gameObject.SetActive(false);
    }

    public void HideWithStop()
    {
        if (typeRoutine != null) { StopCoroutine(typeRoutine); typeRoutine = null; }
        messageText.text = "";
        StartCoroutine(FadeOutAndDisable());
    }

    private IEnumerator TypeRoutine(string msg)
    {
        gameObject.SetActive(true);
        float t = 0f;
        canvasGroup.alpha = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = Mathf.Clamp01(t / fadeDuration);
            yield return null;
        }
        canvasGroup.alpha = 1f;

        messageText.text = "";
        foreach (char c in msg)
        {
            messageText.text += c;
            yield return new WaitForSeconds(charDelay);
        }
        typeRoutine = null;
    }

    private IEnumerator FadeOutAndDisable()
    {
        float t = 0f;
        float start = canvasGroup.alpha;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(start, 0f, t / fadeDuration);
            yield return null;
        }
        canvasGroup.alpha = 0f;
        gameObject.SetActive(false);
    }
}