using System.Collections;
using UnityEngine;
using TMPro;

public class ChatBubbleController : MonoBehaviour
{
    public TextMeshProUGUI messageText;
    public float charDelay = 0.04f;
    public float fadeDuration = 0.12f;

    private Coroutine typeRoutine;
    private CanvasGroup canvasGroup;
    private bool hasInitialized = false; // ★ 防止 Awake 和 ShowMessage 冲突

    void Awake()
    {
        Initialize();
    }

    private void Initialize()
    {
        if (hasInitialized) return;
        hasInitialized = true;

        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();

        if (messageText == null)
            messageText = GetComponentInChildren<TextMeshProUGUI>(true);

        if (canvasGroup != null) canvasGroup.alpha = 0f;
        if (messageText != null) messageText.text = "";

        // ★★★ 不在这里 SetActive(false) ★★★
        // 初始隐藏由 Inspector 中预设 ChatBubble 为 inactive 即可
    }

    public void ShowMessage(string msg, float perCharDelay)
    {
        // ★ 先激活，再确保初始化完成
        gameObject.SetActive(true);
        Initialize(); // 如果 Awake 还没跑过，手动初始化

        charDelay = perCharDelay;

        if (typeRoutine != null) StopCoroutine(typeRoutine);
        typeRoutine = StartCoroutine(TypeRoutine(msg));
    }

    public void HideImmediate()
    {
        if (typeRoutine != null) { StopCoroutine(typeRoutine); typeRoutine = null; }
        if (messageText != null) messageText.text = "";
        if (canvasGroup != null) canvasGroup.alpha = 0f;
        gameObject.SetActive(false);
    }

    public void HideWithStop()
    {
        if (typeRoutine != null) { StopCoroutine(typeRoutine); typeRoutine = null; }
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

/*
using System.Collections;
using UnityEngine;
using TMPro;

public class ChatBubbleController : MonoBehaviour
{
    public TextMeshProUGUI messageText;
    public float charDelay = 0.04f;
    public float fadeDuration = 0.12f;

    private Coroutine typeRoutine;
    private CanvasGroup canvasGroup;

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();

        if (messageText == null)
            messageText = GetComponentInChildren<TextMeshProUGUI>(true);

        // ★ 安全初始化
        if (canvasGroup != null) canvasGroup.alpha = 0f;
        if (messageText != null) messageText.text = "";
        gameObject.SetActive(false);
    }

    public void ShowMessage(string msg, float perCharDelay)
    {
        charDelay = perCharDelay;

        // ★★★ 必须先 SetActive(true)，再 StartCoroutine ★★★
        gameObject.SetActive(true);

        if (typeRoutine != null) StopCoroutine(typeRoutine);
        typeRoutine = StartCoroutine(TypeRoutine(msg));
    }

    public void HideImmediate()
    {
        if (typeRoutine != null) { StopCoroutine(typeRoutine); typeRoutine = null; }
        if (messageText != null) messageText.text = "";
        if (canvasGroup != null) canvasGroup.alpha = 0f;
        gameObject.SetActive(false);
    }

    public void HideWithStop()
    {
        if (typeRoutine != null) { StopCoroutine(typeRoutine); typeRoutine = null; }
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
*/

/*
using System.Collections;
using UnityEngine;
using TMPro;

public class ChatBubbleController : MonoBehaviour
{
    public TextMeshProUGUI messageText;
    public float charDelay = 0.04f;
    public float fadeDuration = 0.12f;

    private Coroutine typeRoutine;
    private CanvasGroup canvasGroup;

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();

        if (messageText == null)
            messageText = GetComponentInChildren<TextMeshProUGUI>(true);

        // ★ 安全初始化（不调用 HideImmediate，避免在 Awake 时出错）
        if (canvasGroup != null) canvasGroup.alpha = 0f;
        if (messageText != null) messageText.text = "";
        gameObject.SetActive(false);
    }

    public void ShowMessage(string msg, float perCharDelay)
    {
        charDelay = perCharDelay;

        // ★★★ 关键：必须先激活 GameObject，再 StartCoroutine ★★★
        gameObject.SetActive(true);

        if (typeRoutine != null) StopCoroutine(typeRoutine);
        typeRoutine = StartCoroutine(TypeRoutine(msg));
    }

    public void HideImmediate()
    {
        if (typeRoutine != null) { StopCoroutine(typeRoutine); typeRoutine = null; }
        if (messageText != null) messageText.text = "";
        if (canvasGroup != null) canvasGroup.alpha = 0f;
        gameObject.SetActive(false);
    }

    public void HideWithStop()
    {
        if (typeRoutine != null) { StopCoroutine(typeRoutine); typeRoutine = null; }
        if (messageText != null) messageText.text = "";

        // ★ 如果已经 inactive，直接关，不启动协程
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
*/

/*
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ChatBubbleController : MonoBehaviour
{
    public TextMeshProUGUI messageText;
    public float charDelay = 0.04f;
    public float fadeDuration = 0.12f;

    private Coroutine typeRoutine;
    private CanvasGroup canvasGroup;

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();

        // ★ 自动查找 messageText（防止 Inspector 忘记绑定）
        if (messageText == null)
            messageText = GetComponentInChildren<TextMeshProUGUI>(true);

        // ★ 安全隐藏（加 null 检查）
        if (canvasGroup != null) canvasGroup.alpha = 0f;
        if (messageText != null) messageText.text = "";
        gameObject.SetActive(false); // ★ 确保初始隐藏
    }

    public void ShowMessage(string msg, float perCharDelay)
    {
        charDelay = perCharDelay;

        // ★ 先激活再启动协程（避免 inactive 对象无法 StartCoroutine）
        gameObject.SetActive(true);

        if (typeRoutine != null) StopCoroutine(typeRoutine);
        typeRoutine = StartCoroutine(TypeRoutine(msg));
    }

    public void HideImmediate()
    {
        if (typeRoutine != null) { StopCoroutine(typeRoutine); typeRoutine = null; }
        if (messageText != null) messageText.text = "";
        if (canvasGroup != null) canvasGroup.alpha = 0f;
        gameObject.SetActive(false);
    }

    public void HideWithStop()
    {
        if (typeRoutine != null) { StopCoroutine(typeRoutine); typeRoutine = null; }
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
*/

/*
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ChatBubbleController : MonoBehaviour
{
    public TextMeshProUGUI messageText;
    public float charDelay = 0.04f;
    public float fadeDuration = 0.12f;

    private Coroutine typeRoutine;
    private CanvasGroup canvasGroup;

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
        // 不在 Awake 里调用 HideImmediate()，因为 Awake 时 gameObject 可能正在被激活
        // 改为直接设置初始状态
        if (messageText != null) messageText.text = "";
        if (canvasGroup != null) canvasGroup.alpha = 0f;
    }

    public void ShowMessage(string msg, float perCharDelay)
    {
        charDelay = perCharDelay;

        // ★ 关键修复：先激活 GameObject，再启动协程
        gameObject.SetActive(true);

        if (typeRoutine != null) StopCoroutine(typeRoutine);
        typeRoutine = StartCoroutine(TypeRoutine(msg));
    }

    public void HideImmediate()
    {
        if (typeRoutine != null) { StopCoroutine(typeRoutine); typeRoutine = null; }
        if (messageText != null) messageText.text = "";
        if (canvasGroup != null) canvasGroup.alpha = 0f;
        gameObject.SetActive(false);
    }

    public void HideWithStop()
    {
        if (typeRoutine != null) { StopCoroutine(typeRoutine); typeRoutine = null; }
        if (messageText != null) messageText.text = "";

        // 如果已经 inactive 就直接关闭，不启动协程
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
        // gameObject 已在 ShowMessage 里被激活，无需再次 SetActive
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
*/


/*using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;  // 加到文件顶部

public class ChatBubbleController : MonoBehaviour
{
    // public Text messageText; // inspector 绑定 UI Text (或 TextMeshProUGUI)
    public TextMeshProUGUI messageText;  // 支持 TextMeshPro

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
*/
