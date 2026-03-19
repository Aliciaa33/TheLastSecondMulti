using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using StarterAssets; // 添加这行！
using Photon.Pun;
using Photon.Realtime;

public class BombInteraction : MonoBehaviourPun
{
    [Header("UI References")]
    public GameObject confirmDefuseUI; // 确认拆弹界面
    public GameObject passwordInputUI; // 密码输入界面

    public Button confirmButton;       // 确认拆弹按钮
    public Button cancelButton;        // 取消拆弹按钮

    public TMP_InputField passwordInput; // 密码输入框
    public Button passwordConfirmButton; // 密码确认按钮
    public Button passwordCancelButton;  // 密码取消按钮

    [Header("Interaction Settings")]
    public float interactDistance = 5f;

    private BombFuse nearestBomb; // 当前最近的炸弹
    private bool isPlayerInRange = false;
    //private ThirdPersonController tpc;
    //private StarterAssetsInputs starterInputs;



    void Awake()
    {
        // 添加调试信息
        //Debug.Log("=== BombInteraction Start ===");
        //Debug.Log($"confirmDefuseUI是否赋值: {confirmDefuseUI != null}");
        //Debug.Log($"passwordInputUI是否赋值: {passwordInputUI != null}");
        // 隐藏所有UI界面
        // if (confirmDefuseUI != null) confirmDefuseUI.SetActive(false);
        // if (passwordInputUI != null) passwordInputUI.SetActive(false);

        // 绑定按钮事件
        // if (confirmButton != null) confirmButton.onClick.AddListener(OnConfirmDefuse);
        // if (cancelButton != null) cancelButton.onClick.AddListener(OnCancelDefuse);
        // if (passwordConfirmButton != null) passwordConfirmButton.onClick.AddListener(OnConfirmPassword);
        // if (passwordCancelButton != null) passwordCancelButton.onClick.AddListener(OnCancelPassword);

        // 可以留空或放一些不紧急的初始化
        // Debug.Log("BombInteraction Start完成");
        // 订阅拆弹事件
        // BombFuse.OnBombDefused += OnBombDefused;
        // BombFuse.OnBombExploded += OnBombExploded;
    }

    private void Start()
    {
        // zys
        PhotonView pv = GetComponent<PhotonView>();
        bool isLocalPlayer = pv == null || pv.ViewID == 0 || pv.IsMine;

        if (!isLocalPlayer)
        {
            enabled = false;
            return;
        }

        GameObject defuseCanvas = GameObject.Find("DefuseCanvas");
        if (defuseCanvas != null)
        {
            Transform confirmTransform = defuseCanvas.transform.Find("Confirm Defuse UI");
            Transform passwordTransform = defuseCanvas.transform.Find("PasswordInputUI");

            if (confirmTransform != null)
            {
                confirmDefuseUI = confirmTransform.gameObject;
                confirmButton = confirmDefuseUI.transform.Find("ConfirmDefuse").GetComponent<Button>();
                cancelButton = confirmDefuseUI.transform.Find("CancelDefuse").GetComponent<Button>();
            }
            else Debug.LogError("Confirm Defuse UI not found under DefuseCanvas!");

            if (passwordTransform != null)
            {
                passwordInputUI = passwordTransform.gameObject;
                passwordInput = passwordInputUI.transform.Find("Input PW").GetComponent<TMP_InputField>();
                passwordConfirmButton = passwordInputUI.transform.Find("ConfirmPW").GetComponent<Button>();
                passwordCancelButton = passwordInputUI.transform.Find("CancelPW").GetComponent<Button>();
            }
            else Debug.LogError("PasswordInputUI not found under DefuseCanvas!");
        }
        else Debug.LogError("DefuseCanvas not found in scene!");

        // Hide all UI
        if (confirmDefuseUI != null) confirmDefuseUI.SetActive(false);
        if (passwordInputUI != null) passwordInputUI.SetActive(false);

        // Bind button events
        if (confirmButton != null) confirmButton.onClick.AddListener(OnConfirmDefuse);
        if (cancelButton != null) cancelButton.onClick.AddListener(OnCancelDefuse);
        if (passwordConfirmButton != null) passwordConfirmButton.onClick.AddListener(OnConfirmPassword);
        if (passwordCancelButton != null) passwordCancelButton.onClick.AddListener(OnCancelPassword);

        BombFuse.OnBombExploded += OnBombExploded;
        Debug.Log("BombInteraction Start完成");
    }

    void OnBombExploded()
    {
        nearestBomb = null; // ✅ 清空引用
        HideAllUI();
    }

    void Update()
    {
        FindNearestBomb();

        // 靠近炸弹且按E键，显示确认拆弹界面
        if (isPlayerInRange && Input.GetKeyDown(KeyCode.E) && !IsAnyUIActive())
        {
            Debug.Log("按下E键，显示拆弹界面");
            ShowConfirmDefuseUI();
        }

        // 界面导航：Tab键切换选项
        /*
        if (IsAnyUIActive() && Input.GetKeyDown(KeyCode.Tab))
        {
            NavigateUI();
        }

        // 界面确认：Enter键选择
        if (IsAnyUIActive() && Input.GetKeyDown(KeyCode.Return))
        {
            ConfirmSelection();
        }

        // ESC键关闭界面
        if (IsAnyUIActive() && Input.GetKeyDown(KeyCode.Escape))
        {
            if (passwordInputUI != null && passwordInputUI.activeInHierarchy)
                OnCancelPassword();
            else if (confirmDefuseUI != null && confirmDefuseUI.activeInHierarchy)
                OnCancelDefuse();
        }
        */
    }

    void FindNearestBomb()
    {
        // 查找场景中所有炸弹
        BombFuse[] allBombs = FindObjectsOfType<BombFuse>();
        nearestBomb = null;
        float closestDistance = Mathf.Infinity;

        foreach (BombFuse bomb in allBombs)
        {
            // 检查炸弹是否已经被拆除或爆炸
            if (bomb == null) continue;

            float distance = Vector3.Distance(transform.position, bomb.transform.position);
            if (distance < closestDistance && distance <= interactDistance)
            {
                closestDistance = distance;
                nearestBomb = bomb;
            }
        }

        bool wasInRange = isPlayerInRange;
        isPlayerInRange = nearestBomb != null;

        // 调试范围变化
        if (isPlayerInRange != wasInRange)
        {
            Debug.Log($"玩家{(isPlayerInRange ? "进入" : "离开")}炸弹范围，最近炸弹: {nearestBomb != null}");
        }
    }

    bool IsAnyUIActive()
    {
        return (confirmDefuseUI != null && confirmDefuseUI.activeInHierarchy) ||
               (passwordInputUI != null && passwordInputUI.activeInHierarchy);
    }

    void ShowConfirmDefuseUI()
    {
        if (confirmDefuseUI != null && nearestBomb != null)
        {
            // ✅ 先禁用玩家输入
            DisablePlayerInput();

            // ✅ 激活 UI
            confirmDefuseUI.SetActive(true);

            // ✅ 强制刷新 Canvas
            Canvas defuseCanvas = confirmDefuseUI.GetComponentInParent<Canvas>();
            if (defuseCanvas != null)
            {
                defuseCanvas.sortingOrder = 999;
                defuseCanvas.overrideSorting = true;

                // ✅ 强制重建 Canvas
                Canvas.ForceUpdateCanvases();

                Debug.Log($"DefuseCanvas名称: {defuseCanvas.name}");
                Debug.Log($"DefuseCanvas激活状态: {defuseCanvas.gameObject.activeInHierarchy}");
            }

            // ✅ 确保按钮可交互
            if (confirmButton != null)
            {
                confirmButton.interactable = true;
                confirmButton.Select();
                Debug.Log("确认拆弹界面已显示");
            }

            if (cancelButton != null)
            {
                cancelButton.interactable = true;
            }
        }
        else
        {
            Debug.LogError("无法显示拆弹界面: " +
                         $"confirmDefuseUI={confirmDefuseUI != null}, " +
                         $"nearestBomb={nearestBomb != null}");
        }
    }


    /*
    void ShowConfirmDefuseUI()
    {
        //if (nearestBomb == null) Debug.Log("nearest bomb is null"); // new add

        //if (confirmDefuseUI == null) Debug.Log("defuseui is null"); // new add
        if (confirmDefuseUI != null && nearestBomb != null)
        {
            confirmDefuseUI.SetActive(true);

            // 将DefuseCanvas设置为最高层级
            Canvas defuseCanvas = confirmDefuseUI.GetComponentInParent<Canvas>();
            if (defuseCanvas != null)
            {
                defuseCanvas.sortingOrder = 999; // 设置很高的值
                defuseCanvas.overrideSorting = true; // 重要：覆盖其他Canvas的排序
                Debug.Log($"DefuseCanvas名称: {defuseCanvas.name}");
                Debug.Log($"DefuseCanvas激活状态: {defuseCanvas.gameObject.activeInHierarchy}");
                //defuseCanvas.sortingOrder = 100;
            }

            DisablePlayerInput();

            // 默认选中确认按钮
            if (confirmButton != null)
            {
                confirmButton.Select();
                Debug.Log("确认拆弹界面已显示");
            }
            if (cancelButton != null)
            {
                cancelButton.Select();
            }


        }
        else
        {
            Debug.LogError("无法显示拆弹界面: " +
                         $"confirmDefuseUI={confirmDefuseUI != null}, " +
                         $"nearestBomb={nearestBomb != null}");
        }
    }
    */
    /*
    void ShowPasswordInputUI()
    {
        if (passwordInputUI != null)
        {
            passwordInputUI.SetActive(true);

            // 默认选中密码输入框
            if (passwordInput != null)
            {
                passwordInput.text = "";
                passwordInput.Select();
                passwordInput.ActivateInputField();
            }

            Debug.Log("密码输入界面已显示");
        }
    }
    */

    void ShowPasswordInputUI()
    {
        if (passwordInputUI != null)
        {
            // 确保Canvas设置正确
            Canvas passwordCanvas = passwordInputUI.GetComponentInParent<Canvas>();
            if (passwordCanvas != null)
            {
                passwordCanvas.sortingOrder = 1000;
                passwordCanvas.overrideSorting = true;
                Debug.Log($"密码Canvas设置完成: {passwordCanvas.name}");
            }

            // 确保UI缩放正确
            RectTransform rect = passwordInputUI.GetComponent<RectTransform>();
            /*
            if (rect != null)
            {
                rect.localScale = Vector3.one;
            }
            */
            passwordInputUI.SetActive(true);
            Debug.Log($"密码输入界面已显示: {passwordInputUI.activeInHierarchy}");

            // 默认选中密码输入框
            if (passwordInput != null)
            {
                passwordInput.text = "";
                passwordInput.Select();
                passwordInput.ActivateInputField();
                Debug.Log("密码输入框已激活");
            }
            else
            {
                Debug.LogWarning("passwordInput 为 null");
            }

            if (passwordConfirmButton != null)
            {
                passwordConfirmButton.Select();
            }
            else
            {
                passwordCancelButton.Select();
            }
        }
        else
        {
            Debug.LogError("passwordInputUI 为 null");
        }
    }

    void HideAllUI()
    {
        if (confirmDefuseUI != null)
        {
            confirmDefuseUI.SetActive(false);
            // 重置Canvas排序
            Canvas defuseCanvas = confirmDefuseUI.GetComponentInParent<Canvas>();
            if (defuseCanvas != null)
            {
                defuseCanvas.sortingOrder = 0; // 重置为默认值
                defuseCanvas.overrideSorting = false;
            }
            Debug.Log("确认拆弹界面已隐藏");
        }
        if (passwordInputUI != null)
        {
            passwordInputUI.SetActive(false);
            // 重置Canvas排序
            Canvas passwordCanvas = passwordInputUI.GetComponentInParent<Canvas>();
            if (passwordCanvas != null)
            {
                passwordCanvas.sortingOrder = 0; // 重置为默认值
                passwordCanvas.overrideSorting = false;
            }
            Debug.Log("密码输入界面已隐藏");
        }

        // zys: 只有在游戏进行中才锁定鼠标
        if (GameManager.Instance != null && GameManager.Instance.gameActive)
        {
            EnablePlayerInput();
        }
    }

    void NavigateUI()
    {
        if (confirmDefuseUI != null && confirmDefuseUI.activeInHierarchy)
        {
            // 在确认和取消按钮之间切换
            if (confirmButton != null && cancelButton != null)
            {
                if (confirmButton.gameObject == UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject)
                    cancelButton.Select();
                else
                    confirmButton.Select();
            }
        }
        else if (passwordInputUI != null && passwordInputUI.activeInHierarchy)
        {
            // 在密码框、确认、取消之间切换
            // Unity的InputField会自动处理Tab导航
            if (passwordInput != null && passwordConfirmButton != null && passwordCancelButton != null)
            {
                var currentSelected = UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject;

                if (currentSelected == passwordInput.gameObject)
                    passwordConfirmButton.Select();
                else if (currentSelected == passwordConfirmButton.gameObject)
                    passwordCancelButton.Select();
                else if (currentSelected == passwordCancelButton.gameObject)
                    passwordInput.Select();
            }
        }
    }

    /*
    void ConfirmSelection()
    {
        if (confirmDefuseUI != null && confirmDefuseUI.activeInHierarchy)
        {
            if (confirmButton.gameObject == UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject)
                OnConfirmDefuse();
            else if (cancelButton.gameObject == UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject)
                OnCancelDefuse();
        }
        else if (passwordInputUI != null && passwordInputUI.activeInHierarchy)
        {
            if (passwordConfirmButton.gameObject == UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject)
                OnConfirmPassword();
            else if (passwordCancelButton.gameObject == UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject)
                OnCancelPassword();
        }
    }
    */

    void OnConfirmDefuse()
    {
        // 切换到密码输入界面
        if (confirmDefuseUI != null && passwordInputUI != null)
        {
            confirmDefuseUI.SetActive(false);
            ShowPasswordInputUI();
        }
        /*
        // 显示密码输入界面
        if (passwordInputUI != null)
        {
            Debug.Log($"passwordInputUI 存在，准备显示。当前状态: {passwordInputUI.activeInHierarchy}");
            ShowPasswordInputUI();
        }
        else
        {
            Debug.LogError("passwordInputUI 为 null！请在Inspector中分配引用");
        }
        */

        Debug.Log("=== 切换完成 ===");
    }

    void OnCancelDefuse()
    {
        HideAllUI();
        Debug.Log("取消拆弹");
    }

    void OnConfirmPassword()
    {
        string inputPassword = passwordInput != null ? passwordInput.text : "";

        if (CheckPassword(inputPassword))
        {
            //zys
            //GameManager.Instance.defuseBomb();
            Debug.Log("密码正确，拆弹成功！");

            //zhq sync
            HideAllUI();
            EnablePlayerInput();

			if (nearestBomb != null)
			{
                BombFuse fuse = nearestBomb.GetComponent<BombFuse>();
				if (fuse != null)
				{
                    fuse.DefuseBomb();
				}
			}

            //GameManager.Instance.defuseBomb();

            nearestBomb = null;

            /*
            // 调用拆弹逻辑
            Debug.Log($"最近炸弹: {nearestBomb != null}");
            nearestBomb.DefuseBomb();
            */

            /*
            // 通知GameManager
            if (GameManager.Instance != null)
            {
                GameManager.Instance.BombDefused();
            }
            */

            // HideAllUI();
        }
        else
        {
            Debug.Log("密码错误,炸弹爆炸");
            HideAllUI();
            EnablePlayerInput();

            if (nearestBomb != null)
            {
                // —— 触发炸弹爆炸 —— 
                //nearestBomb.Explode();
                BombFuse fuse = nearestBomb.GetComponent<BombFuse>();
                if (fuse != null)
                {
                    fuse.TriggerExplosion(); // 调用BombFuse.cs的func
                }
            }

            nearestBomb = null;
        }
            /*
            if (GameManager.Instance != null)
            {
                // 通知管理器：炸弹已爆炸
                GameManager.Instance.BombExploded();
            }
            if (passwordInput != null)
            {
                passwordInput.text = "";
                passwordInput.Select();
                passwordInput.ActivateInputField();
            }
            */
    }

    void OnCancelPassword()
    {
        // 返回确认拆弹界面
        if (passwordInputUI != null)
        {
            passwordInputUI.SetActive(false);
            confirmDefuseUI.SetActive(true);
            ShowConfirmDefuseUI();
        }
    }

    bool CheckPassword(string inputPassword)
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("GameManager.Instance is null");
            return false;
        }
        // get the current bomb password
        int currentPassword = GameManager.Instance.GetCurrentPassword();
        if (int.TryParse(inputPassword, out int inputNum))
        {
            bool correct = inputNum == currentPassword;
            Debug.Log($"密码检查: 输入={inputPassword}, 正确密码={currentPassword}, 结果={correct}");
            return correct;
        }
        else
        {
            Debug.Log($"密码检查: 输入={inputPassword}, 正确密码={currentPassword} 不是有效的数字");
            return false;
        }
    }

    void DisablePlayerInput()
    {
        // 1. 启用输入系统
        // StarterAssetsInputs inputs = GetComponent<StarterAssetsInputs>();
        // if (inputs != null)
        // {
        //     inputs.SetUIMode(true);
        //     Debug.Log("玩家输入已禁止");
        // }

        // 2. 启用移动控制器
        ThirdPersonController tpc = GetComponent<ThirdPersonController>();
        if (tpc != null)
        {
            tpc.enabled = false;
            Debug.Log("玩家移动已禁止");
        }

        // 3. 启用物理推动
        BasicRigidBodyPush push = GetComponent<BasicRigidBodyPush>();
        if (push != null)
        {
            push.canPush = false;
            Debug.Log("物理推动已禁止");
        }
        /*
        ThirdPersonController tpc = GetComponent<ThirdPersonController>();
        Debug.Log("tpc = " + tpc);
        if (tpc != null)
        {
            tpc.enabled = false;
            Debug.Log("玩家移动已禁用");
        }
        */
        // 1. 屏蔽输入
        //starterInputs?.SetUIMode(true);
        // 2. 禁用角色移动/跳跃/碰撞逻辑
        /*
        if (tpc != null)
		{
            tpc.enabled = false;
            Debug.Log("玩家输入已禁用");
        }
        */
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void EnablePlayerInput()
    {
        /*
        PlayerMovement playerMovement = GetComponent<PlayerMovement>();
        if (playerMovement != null)
        {
            playerMovement.enabled = true;
            Debug.Log("玩家移动已启用");
        }
        */
        // 1. 启用输入系统
        // StarterAssetsInputs inputs = GetComponent<StarterAssetsInputs>();
        // if (inputs != null)
        // {
        //     inputs.SetUIMode(false);
        //     Debug.Log("玩家输入已启用");
        // }

        // 2. 启用移动控制器
        ThirdPersonController tpc = GetComponent<ThirdPersonController>();
        if (tpc != null)
        {
            tpc.enabled = true;
            Debug.Log("玩家移动已启用");
        }

        // 3. 启用物理推动
        BasicRigidBodyPush push = GetComponent<BasicRigidBodyPush>();
        if (push != null)
        {
            push.canPush = true;
            Debug.Log("物理推动已启用");
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        // Debug.Log("恢复鼠标锁定状态");
    }

    // zys
    void OnDestroy()
    {
        // 取消订阅事件
        BombFuse.OnBombExploded -= OnBombExploded;
    }
}