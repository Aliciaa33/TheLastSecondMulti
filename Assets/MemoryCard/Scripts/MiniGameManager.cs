using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MiniGameManager : MonoBehaviour
{
    public static MiniGameManager Instance { get; private set; }
    public string miniGameSceneName = "lvl_03";
    private bool isMiniGameActive = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    void Update()
    {
        // ★ 小游戏期间每帧强制光标解锁（防止主场景脚本抢走光标）
        if (isMiniGameActive)
        {
            if (Cursor.lockState != CursorLockMode.None)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }
    }

    public void OpenMiniGame()
    {
        if (isMiniGameActive) return;
        StartCoroutine(LoadRoutine());
    }

    public void CloseMiniGame()
    {
        if (!isMiniGameActive) return;
        StartCoroutine(UnloadRoutine());
    }

    private IEnumerator LoadRoutine()
    {
        isMiniGameActive = true;
        FreezePlayer(true);

        AsyncOperation op = SceneManager.LoadSceneAsync(
            miniGameSceneName, LoadSceneMode.Additive);
        while (!op.isDone) yield return null;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private IEnumerator UnloadRoutine()
    {
        AsyncOperation op = SceneManager.UnloadSceneAsync(miniGameSceneName);
        while (!op.isDone) yield return null;

        FreezePlayer(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        isMiniGameActive = false;
    }

    private void FreezePlayer(bool freeze)
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        var tpc = player.GetComponent<StarterAssets.ThirdPersonController>();
        if (tpc != null) tpc.enabled = !freeze;

        var input = player.GetComponent<UnityEngine.InputSystem.PlayerInput>();
        if (input != null) input.enabled = !freeze;

        var cc = player.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = !freeze;
    }

    public bool IsMiniGameActive() => isMiniGameActive;
}


/*
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MiniGameManager : MonoBehaviour
{
    public static MiniGameManager Instance { get; private set; }
    public string miniGameSceneName = "lvl_03";
    private bool isMiniGameActive = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    // ★★★ 关键：每帧强制保持光标解锁 ★★★
    void Update()
    {
        if (isMiniGameActive)
        {
            if (Cursor.lockState != CursorLockMode.None)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }
    }

    public void OpenMiniGame()
    {
        if (isMiniGameActive) return;
        StartCoroutine(LoadRoutine());
    }

    public void CloseMiniGame()
    {
        if (!isMiniGameActive) return;
        StartCoroutine(UnloadRoutine());
    }

    private IEnumerator LoadRoutine()
    {
        isMiniGameActive = true;
        FreezePlayer(true);

        AsyncOperation op = SceneManager.LoadSceneAsync(
            miniGameSceneName, LoadSceneMode.Additive);
        while (!op.isDone) yield return null;

        // ★ 禁用主场景的 EventSystem，避免冲突
        var mainES = UnityEngine.EventSystems.EventSystem.current;
        if (mainES != null && mainES.gameObject.scene !=
            SceneManager.GetSceneByName(miniGameSceneName))
        {
            mainES.gameObject.SetActive(false);
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private IEnumerator UnloadRoutine()
    {
        // ★ 重新启用主场景的 EventSystem
        var allES = FindObjectsOfType<UnityEngine.EventSystems.EventSystem>(true);

        AsyncOperation op = SceneManager.UnloadSceneAsync(miniGameSceneName);
        while (!op.isDone) yield return null;

        // 恢复主场景的 EventSystem
        foreach (var es in allES)
        {
            if (es != null && es.gameObject.scene !=
                SceneManager.GetSceneByName(miniGameSceneName))
            {
                es.gameObject.SetActive(true);
            }
        }

        FreezePlayer(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        isMiniGameActive = false;
    }

    private void FreezePlayer(bool freeze)
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        var tpc = player.GetComponent<StarterAssets.ThirdPersonController>();
        if (tpc != null) tpc.enabled = !freeze;

        var input = player.GetComponent<UnityEngine.InputSystem.PlayerInput>();
        if (input != null) input.enabled = !freeze;

        var cc = player.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = !freeze;
    }

    public bool IsMiniGameActive() => isMiniGameActive;
}

*/
/*
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MiniGameManager : MonoBehaviour
{
    public static MiniGameManager Instance { get; private set; }

    // ★ 填你的 lvl_03 场景名称
    public string miniGameSceneName = "lvl_03";

    private bool isMiniGameActive = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    /// <summary>
    /// NPC 交互时调用，Additive 加载小游戏
    /// </summary>
    public void OpenMiniGame()
    {
        if (isMiniGameActive) return;
        StartCoroutine(LoadRoutine());
    }

    /// <summary>
    /// 小游戏通关或点关闭按钮时调用
    /// </summary>
    public void CloseMiniGame()
    {
        if (!isMiniGameActive) return;
        StartCoroutine(UnloadRoutine());
    }

    private IEnumerator LoadRoutine()
    {
        isMiniGameActive = true;

        // 1. 冻结玩家
        FreezePlayer(true);

        // 2. Additive 加载 — 主场景不受影响 [1][2]
        AsyncOperation op = SceneManager.LoadSceneAsync(
            miniGameSceneName, LoadSceneMode.Additive);
        while (!op.isDone) yield return null;

        // 3. 解锁鼠标（翻牌需要点击）
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Debug.Log("MiniGame loaded (Additive)");
    }

    private IEnumerator UnloadRoutine()
    {
        // 1. 卸载小游戏场景
        AsyncOperation op = SceneManager.UnloadSceneAsync(miniGameSceneName);
        while (!op.isDone) yield return null;

        // 2. 恢复玩家
        FreezePlayer(false);

        // 3. 锁定鼠标（第三人称游戏）
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        isMiniGameActive = false;
        Debug.Log("MiniGame unloaded");
    }

    private void FreezePlayer(bool freeze)
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        var tpc = player.GetComponent<StarterAssets.ThirdPersonController>();
        if (tpc != null) tpc.enabled = !freeze;

        var input = player.GetComponent<UnityEngine.InputSystem.PlayerInput>();
        if (input != null) input.enabled = !freeze;

        var cc = player.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = !freeze;
    }

    public bool IsMiniGameActive() => isMiniGameActive;
}
*/
