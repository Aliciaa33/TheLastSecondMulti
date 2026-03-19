using UnityEngine;
using UnityEngine.UI;
using TMPro;

public enum LoadingState { Connecting, Success, Failed }

/// <summary>
/// Attach to the Canvas in your Loading scene.
/// Finds all child UI elements automatically by name — no Inspector wiring needed.
/// Matches the neon circuit aesthetic of The Last Second.
/// </summary>
public class LoadingSceneUI : MonoBehaviour
{
    // ── Found automatically by name ───────────────────────────────────────
    private TMP_Text statusText;
    private TMP_Text subStatusText;
    private LoadingIconRenderer iconRenderer; // handles spinner/success/failed drawing
    private Image progressBar;
    private GameObject retryButton;

    // Step rows: each has a label TMP and a status TMP
    private StepRow[] steps;

    [Header("Animation")]
    [SerializeField] private float dotAnimInterval = 0.35f;

    private float dotTimer;
    private int dotCount = 1;
    private bool animatingDots;
    private float progressTarget;
    private float progressCurrent;

    // Palette
    static readonly Color NeonBlue = new Color(0.10f, 0.89f, 1.00f, 1f);
    static readonly Color NeonGreen = new Color(0.22f, 1.00f, 0.43f, 1f);
    static readonly Color NeonRed = new Color(1.00f, 0.24f, 0.24f, 1f);

    // ── Step data ─────────────────────────────────────────────────────────
    struct StepRow
    {
        public TMP_Text label;
        public TMP_Text status;
        public Image dotIcon;
        public StepState state;
    }

    enum StepState { Pending, Active, Done, Failed }

    static readonly string[] StepLabels = {
        "INITIALISING NETWORK",
        "CONNECTING TO PHOTON",
        "AUTHENTICATING",
        "READY"
    };

    void Awake()
    {
        statusText = FindTMP("StatusText");
        subStatusText = FindTMP("SubStatusText");
        retryButton = GameObject.Find("RetryButton");
        iconRenderer = FindObjectOfType<LoadingIconRenderer>();

        // Progress bar fill image
        GameObject pbGO = GameObject.Find("ProgressBar");
        if (pbGO != null) progressBar = pbGO.GetComponent<Image>();

        // Wire retry button
        if (retryButton != null)
        {
            Button btn = retryButton.GetComponent<Button>();
            LoadingSceneManager mgr = FindObjectOfType<LoadingSceneManager>();
            if (btn != null && mgr != null)
                btn.onClick.AddListener(mgr.RetryConnection);
        }

        BuildStepRows();
        SetInitialState();
    }

    void BuildStepRows()
    {
        steps = new StepRow[StepLabels.Length];
        for (int i = 0; i < StepLabels.Length; i++)
        {
            string idx = i.ToString();
            steps[i] = new StepRow
            {
                label = FindTMP("StepLabel" + idx),
                status = FindTMP("StepStatus" + idx),
                dotIcon = FindImage("StepDot" + idx),
                state = StepState.Pending
            };
            if (steps[i].label != null)
                steps[i].label.text = StepLabels[i];
        }
    }

    void SetInitialState()
    {
        iconRenderer?.ShowIcon(LoadingIconState.Connecting);
        if (retryButton != null) retryButton.SetActive(false);
        if (subStatusText != null) subStatusText.gameObject.SetActive(true);

        SetProgressRaw(0f);
        foreach (var s in steps) ApplyStepVisual(s);
    }

    void Update()
    {
        // Animate dots
        if (animatingDots)
        {
            dotTimer += Time.deltaTime;
            if (dotTimer >= dotAnimInterval)
            {
                dotTimer = 0f;
                dotCount = (dotCount % 3) + 1;
                if (statusText != null)
                    statusText.text = "CONNECTING" + new string('.', dotCount);
            }
        }

        // Smooth progress bar
        if (progressBar != null && Mathf.Abs(progressCurrent - progressTarget) > 0.001f)
        {
            progressCurrent = Mathf.MoveTowards(progressCurrent, progressTarget,
                Time.deltaTime * 0.6f);
            progressBar.fillAmount = progressCurrent;
        }
    }

    // ── Public API called by LoadingSceneManager ──────────────────────────

    public void SetState(LoadingState state)
    {
        switch (state)
        {
            case LoadingState.Connecting:
                iconRenderer?.ShowIcon(LoadingIconState.Connecting);
                animatingDots = true;
                dotTimer = 0; dotCount = 1;
                SetStatus("CONNECTING.", "Establishing connection to master server", Color.white,
                    new Color(NeonBlue.r, NeonBlue.g, NeonBlue.b, 0.5f));
                SetProgressTarget(0.05f, NeonBlue);
                ActivateStep(0);
                break;

            case LoadingState.Success:
                animatingDots = false;
                iconRenderer?.ShowIcon(LoadingIconState.Success);
                SetStatus("CONNECTED", "Loading main menu...", NeonGreen,
                    new Color(NeonGreen.r, NeonGreen.g, NeonGreen.b, 0.6f));
                SetProgressTarget(1f, NeonGreen);
                CompleteAllSteps();
                if (retryButton != null) retryButton.SetActive(false);
                break;

            case LoadingState.Failed:
                animatingDots = false;
                iconRenderer?.ShowIcon(LoadingIconState.Failed);
                SetStatus("CONNECTION FAILED", "Could not reach master server", NeonRed,
                    new Color(NeonRed.r, NeonRed.g, NeonRed.b, 0.6f));
                SetProgressTarget(0.2f, NeonRed);
                FailCurrentStep();
                if (retryButton != null) retryButton.SetActive(true);
                break;
        }
    }

    public void SetStatusMessage(string message)
    {
        if (subStatusText == null) return;
        subStatusText.text = message ?? "";
        subStatusText.gameObject.SetActive(!string.IsNullOrEmpty(message));
    }

    // Advance the step indicator (called optionally from LoadingSceneManager)
    public void AdvanceStep(int stepIndex)
    {
        if (stepIndex > 0 && stepIndex - 1 < steps.Length)
            SetStepState(stepIndex - 1, StepState.Done);
        if (stepIndex < steps.Length)
            SetStepState(stepIndex, StepState.Active);

        float pct = (stepIndex + 1f) / (steps.Length + 1f);
        SetProgressTarget(pct * 0.9f, NeonBlue);
    }

    // ── Internal helpers ──────────────────────────────────────────────────

    void SetStatus(string main, string sub, Color mainColor, Color subColor)
    {
        if (statusText != null)
        { statusText.text = main; statusText.color = mainColor; }
        if (subStatusText != null)
        { subStatusText.text = sub; subStatusText.color = subColor; }
    }

    void SetProgressTarget(float t, Color color)
    {
        progressTarget = t;
        if (progressBar != null) progressBar.color = color;
    }

    void SetProgressRaw(float t)
    {
        progressCurrent = progressTarget = t;
        if (progressBar != null) progressBar.fillAmount = t;
    }

    void ActivateStep(int i)
    {
        for (int j = 0; j < steps.Length; j++)
            SetStepState(j, j == i ? StepState.Active : StepState.Pending);
    }

    void CompleteAllSteps()
    {
        for (int i = 0; i < steps.Length; i++) SetStepState(i, StepState.Done);
    }

    void FailCurrentStep()
    {
        for (int i = 0; i < steps.Length; i++)
            if (steps[i].state == StepState.Active)
            { SetStepState(i, StepState.Failed); break; }
    }

    void SetStepState(int i, StepState state)
    {
        if (i < 0 || i >= steps.Length) return;
        steps[i].state = state;
        ApplyStepVisual(steps[i]);
    }

    void ApplyStepVisual(StepRow row)
    {
        if (row.dotIcon != null)
        {
            switch (row.state)
            {
                case StepState.Pending: row.dotIcon.color = new Color(1, 1, 1, 0.18f); break;
                case StepState.Active: row.dotIcon.color = NeonBlue; break;
                case StepState.Done: row.dotIcon.color = NeonGreen; break;
                case StepState.Failed: row.dotIcon.color = NeonRed; break;
            }
        }
        if (row.status != null)
        {
            switch (row.state)
            {
                case StepState.Pending: row.status.text = "---"; row.status.color = new Color(1, 1, 1, 0.25f); break;
                case StepState.Active: row.status.text = "IN PROGRESS"; row.status.color = NeonBlue; break;
                case StepState.Done: row.status.text = "[DONE]"; row.status.color = NeonGreen; break;
                case StepState.Failed: row.status.text = "[FAIL]"; row.status.color = NeonRed; break;
            }
        }
        if (row.label != null)
        {
            row.label.color = row.state switch
            {
                StepState.Active => Color.white,
                StepState.Done => new Color(NeonGreen.r, NeonGreen.g, NeonGreen.b, 0.7f),
                StepState.Failed => NeonRed,
                _ => new Color(1, 1, 1, 0.28f)
            };
        }
    }

    TMP_Text FindTMP(string name)
    {
        GameObject go = GameObject.Find(name);
        return go != null ? go.GetComponent<TMP_Text>() : null;
    }

    Image FindImage(string name)
    {
        GameObject go = GameObject.Find(name);
        return go != null ? go.GetComponent<Image>() : null;
    }
}