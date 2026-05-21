using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class CandleAttackController : MonoBehaviour
{
    [Header("Refs")]
    public BattlePlayerController player;
    public CanvasGroup darkOverlay;
    public GameObject stopWarningText;
    public VideoClip darkVideoClip;

    [Header("Dark Settings")]
    [Range(0f, 1f)] public float darkAlpha = 0.75f;
    public float fadeTime = 0.25f;
    public bool useVideoOverlay = true;
    public bool loopVideoOverlay = true;
    public bool muteVideoAudio = true;
    public int videoSortingOrder = 8500;

    [Header("Warning Timing")]
    [Tooltip("文字出現後，幾秒後才開始禁止移動")]
    public float warningLeadTime = 1.0f;

    CanvasGroup videoCanvasGroup;
    RawImage videoImage;
    VideoPlayer videoPlayer;
    RenderTexture videoTexture;

    void Awake()
    {
        ForceLight();
    }

    /// <summary>
    /// restrictionDuration = 真正開始「不能動」後，持續多久
    /// </summary>
    public IEnumerator PlayDarkPhase(float restrictionDuration)
    {
        // 1. 先顯示預告，但此時還沒暗下來，也還沒限制玩家
        if (stopWarningText != null)
            stopWarningText.SetActive(true);

        // 2. 給玩家幾秒反應時間
        float warningTimer = 0f;
        while (warningTimer < warningLeadTime)
        {
            if (player != null && player.IsDead)
                yield break;

            warningTimer += Time.deltaTime;
            yield return null;
        }

        // 3. 預告結束後才開始漸暗
        yield return FadeOverlay(darkAlpha);

        if (player != null)
            player.SetDamageImmune(true);

        // 4. 變暗後才正式開始「不能動」
        if (player != null)
        {
            player.SetMovementLocked(true);
            player.BeginNoMoveCheck();
        }

        // 5. 正式限制階段
        float timer = 0f;
        while (timer < restrictionDuration)
        {
            if (player != null && player.IsDead)
                break;

            timer += Time.deltaTime;
            yield return null;
        }

        // 6. 回到正常
        if (player != null)
        {
            player.EndNoMoveCheck();
            player.SetMovementLocked(false);
        }

        if (stopWarningText != null)
            stopWarningText.SetActive(false);

        yield return FadeOverlay(0f);

        if (player != null)
            player.SetDamageImmune(false);
    }

    public void ForceLight()
    {
        if (darkOverlay != null)
            darkOverlay.alpha = 0f;

        HideVideoOverlayImmediate();

        if (stopWarningText != null)
            stopWarningText.SetActive(false);

        if (player != null)
        {
            player.EndNoMoveCheck();
            player.SetMovementLocked(false);
            player.SetDamageImmune(false);
        }
    }

    private IEnumerator FadeOverlay(float targetAlpha)
    {
        if (useVideoOverlay && darkVideoClip != null)
        {
            yield return FadeVideoOverlay(targetAlpha);
            yield break;
        }

        if (darkOverlay == null || fadeTime <= 0f)
        {
            if (darkOverlay != null)
                darkOverlay.alpha = targetAlpha;
            yield break;
        }

        float startAlpha = darkOverlay.alpha;
        float t = 0f;

        while (t < fadeTime)
        {
            t += Time.deltaTime;
            darkOverlay.alpha = Mathf.Lerp(startAlpha, targetAlpha, t / fadeTime);
            yield return null;
        }

        darkOverlay.alpha = targetAlpha;
    }

    IEnumerator FadeVideoOverlay(float targetAlpha)
    {
        EnsureVideoOverlay();

        if (videoCanvasGroup == null)
            yield break;

        if (darkOverlay != null)
            darkOverlay.alpha = 0f;

        if (targetAlpha > 0f)
            PlayVideoOverlay();

        if (fadeTime <= 0f)
        {
            videoCanvasGroup.alpha = targetAlpha;
            if (targetAlpha <= 0f)
                HideVideoOverlayImmediate();
            yield break;
        }

        float startAlpha = videoCanvasGroup.alpha;
        float t = 0f;

        while (t < fadeTime)
        {
            t += Time.deltaTime;
            videoCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t / fadeTime);
            yield return null;
        }

        videoCanvasGroup.alpha = targetAlpha;

        if (targetAlpha <= 0f)
            HideVideoOverlayImmediate();
    }

    void EnsureVideoOverlay()
    {
        if (videoCanvasGroup != null) return;

        GameObject canvasObj = new GameObject("CandleVideoOverlayCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = videoSortingOrder;
        canvasObj.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasObj.AddComponent<GraphicRaycaster>();

        videoCanvasGroup = canvasObj.AddComponent<CanvasGroup>();
        videoCanvasGroup.alpha = 0f;
        videoCanvasGroup.blocksRaycasts = false;
        videoCanvasGroup.interactable = false;

        GameObject videoObj = new GameObject("CandleEyeMoveVideo");
        videoObj.transform.SetParent(canvasObj.transform, false);
        RectTransform videoRect = videoObj.AddComponent<RectTransform>();
        videoRect.anchorMin = Vector2.zero;
        videoRect.anchorMax = Vector2.one;
        videoRect.offsetMin = Vector2.zero;
        videoRect.offsetMax = Vector2.zero;

        videoImage = videoObj.AddComponent<RawImage>();
        videoImage.color = Color.white;
        videoImage.raycastTarget = false;

        videoPlayer = videoObj.AddComponent<VideoPlayer>();
        videoPlayer.playOnAwake = false;
        videoPlayer.source = VideoSource.VideoClip;
        videoPlayer.clip = darkVideoClip;
        videoPlayer.isLooping = loopVideoOverlay;
        videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        videoPlayer.audioOutputMode = muteVideoAudio ? VideoAudioOutputMode.None : VideoAudioOutputMode.Direct;

        videoTexture = new RenderTexture(1920, 1080, 0, RenderTextureFormat.ARGB32);
        videoTexture.name = "CandleEyeMoveVideoTexture";
        videoPlayer.targetTexture = videoTexture;
        videoImage.texture = videoTexture;

        canvasObj.SetActive(false);
    }

    void PlayVideoOverlay()
    {
        EnsureVideoOverlay();

        if (videoCanvasGroup == null || videoPlayer == null) return;

        videoCanvasGroup.gameObject.SetActive(true);

        if (videoPlayer.clip != darkVideoClip)
            videoPlayer.clip = darkVideoClip;

        if (!videoPlayer.isPlaying)
        {
            videoPlayer.time = 0;
            videoPlayer.Play();
        }
    }

    void HideVideoOverlayImmediate()
    {
        if (videoPlayer != null)
            videoPlayer.Stop();

        if (videoCanvasGroup != null)
        {
            videoCanvasGroup.alpha = 0f;
            videoCanvasGroup.gameObject.SetActive(false);
        }
    }
}
