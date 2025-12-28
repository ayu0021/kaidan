using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Intro 影片跳過整合版（可覆蓋）
/// - 播放中按 Space（可選 Esc）立即跳過
/// - Skip 按鈕可淡入（淡入前不可點）
/// - 影片播完自動切場景
/// - 可選：跳過/播完先 FadeOut 再切場景
/// - 同時支援舊 Input Manager 與 New Input System（自動編譯切換）
/// </summary>
public class IntroVideoSkipper : MonoBehaviour
{
    [Header("Video")]
    public VideoPlayer videoPlayer;

    [Header("Next Scene")]
    [Tooltip("下一個場景名稱。若留空，會使用 defaultNextSceneName。")]
    public string nextSceneName = ""; // 你也可以直接在 Inspector 填

    [Tooltip("預設下一個場景（當 nextSceneName 留空時使用）")]
    public string defaultNextSceneName = "虎姑婆_客廳";

    [Header("Keyboard Skip")]
    public bool allowKeyboardSkip = true;
    public bool allowSpaceToSkip = true;
    public bool allowEscToSkip = false;

    [Tooltip("若勾選：鍵盤跳過也要等到 Skip 出現後才允許；不勾：鍵盤從 0 秒就能跳過（推薦不勾）。")]
    public bool keyboardRespectsSkipDelay = false;

    [Header("Skip UI (Optional)")]
    public Button skipButton;

    [Tooltip("Skip 物件上的 CanvasGroup（用來淡入、控制能不能點）。可不填，會自動抓 skipButton 的 CanvasGroup。")]
    public CanvasGroup skipCanvasGroup;

    [Tooltip("把播放影片的 RawImage（或任何會擋住點擊的 UI Graphic）拖進來，腳本會自動關閉 Raycast Target。")]
    public Graphic graphicToDisableRaycast; // RawImage / Image 都是 Graphic

    [Header("Skip Appear (淡入)")]
    [Tooltip("影片開始後幾秒才顯示 Skip 按鈕（淡入）")]
    public float skipButtonDelay = 1.5f;

    [Tooltip("Skip 淡入秒數")]
    public float skipFadeInTime = 0.4f;

    [Header("Optional Fade Out (更像正式遊戲)")]
    [Tooltip("想要跳過/播完時先整個畫面淡出，就把 FadePanel 的 CanvasGroup 丟進來（Alpha=0 起始）。")]
    public CanvasGroup fadeOutCanvasGroup;

    public float fadeOutTime = 0.25f;

    private bool isLoading = false;
    private bool skipUIShown = false;

    void Awake()
    {
        // 若沒填 nextSceneName，就用預設值
        if (string.IsNullOrWhiteSpace(nextSceneName))
            nextSceneName = defaultNextSceneName;

        // 自動補抓引用（避免你忘了拖）
        if (videoPlayer == null) videoPlayer = FindFirstObjectByType<VideoPlayer>();
        if (skipCanvasGroup == null && skipButton != null) skipCanvasGroup = skipButton.GetComponent<CanvasGroup>();

        // 關閉會擋住點擊的 Graphic Raycast（避免 Skip 點不到）
        if (graphicToDisableRaycast != null)
            graphicToDisableRaycast.raycastTarget = false;

        // 綁按鈕
        if (skipButton != null)
            skipButton.onClick.AddListener(TriggerSkip);

        // 影片播完事件
        if (videoPlayer != null)
            videoPlayer.loopPointReached += _ => TriggerSkip();

        // 初始化 Skip UI 狀態：不可見、不可點
        HideSkipUIImmediate();

        // 初始化 FadeOut（如果有）
        if (fadeOutCanvasGroup != null)
        {
            fadeOutCanvasGroup.alpha = 0f;
            fadeOutCanvasGroup.interactable = false;
            fadeOutCanvasGroup.blocksRaycasts = false;
        }
    }

    void Start()
    {
        StartCoroutine(ShowSkipUIAfterDelay());
    }

    void Update()
    {
        if (isLoading) return;
        if (!allowKeyboardSkip) return;

        // 若鍵盤也要尊重延遲，則 UI 還沒出現前先不給跳
        if (keyboardRespectsSkipDelay && !skipUIShown) return;

        if (allowSpaceToSkip && GetKeyDown_Space())
            TriggerSkip();

        if (allowEscToSkip && GetKeyDown_Esc())
            TriggerSkip();
    }

    /// <summary>外部可呼叫（Button OnClick 也可直接拉這個）</summary>
    public void TriggerSkip()
    {
        if (isLoading) return;
        StartCoroutine(LoadNextSceneRoutine());
    }

    IEnumerator ShowSkipUIAfterDelay()
    {
        if (skipCanvasGroup == null) yield break;

        // 延遲
        float t = 0f;
        while (t < skipButtonDelay)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        // 淡入
        float f = 0f;
        while (f < skipFadeInTime)
        {
            f += Time.unscaledDeltaTime;
            skipCanvasGroup.alpha = Mathf.Clamp01(f / Mathf.Max(0.0001f, skipFadeInTime));
            yield return null;
        }

        skipCanvasGroup.alpha = 1f;
        skipCanvasGroup.interactable = true;
        skipCanvasGroup.blocksRaycasts = true;
        skipUIShown = true;
    }

    void HideSkipUIImmediate()
    {
        if (skipCanvasGroup == null) return;

        skipCanvasGroup.alpha = 0f;
        skipCanvasGroup.interactable = false;
        skipCanvasGroup.blocksRaycasts = false;
        skipUIShown = false;
    }

    IEnumerator LoadNextSceneRoutine()
    {
        isLoading = true;

        // 停影片，避免音訊還在唱
        if (videoPlayer != null)
            videoPlayer.Stop();

        // 先淡出（如果有）
        if (fadeOutCanvasGroup != null && fadeOutTime > 0f)
        {
            fadeOutCanvasGroup.blocksRaycasts = true;

            float t = 0f;
            while (t < fadeOutTime)
            {
                t += Time.unscaledDeltaTime;
                fadeOutCanvasGroup.alpha = Mathf.Clamp01(t / Mathf.Max(0.0001f, fadeOutTime));
                yield return null;
            }
            fadeOutCanvasGroup.alpha = 1f;
        }

        SceneManager.LoadScene(nextSceneName);
    }

    // -------- Input (支援新舊輸入系統) --------
    bool GetKeyDown_Space()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.Space);
#endif
    }

    bool GetKeyDown_Esc()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.Escape);
#endif
    }
}


