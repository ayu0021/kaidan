using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class SceneStartFadeIn : MonoBehaviour
{
    [Header("Fade In")]
    public Color fadeColor = Color.black;
    public float holdSeconds = 0.25f;
    public float fadeInDuration = 1.2f;
    public int sortingOrder = 10000;
    public bool useUnscaledTime = true;

    CanvasGroup canvasGroup;
    GameObject fadeRoot;

    void Start()
    {
        StartCoroutine(FadeInRoutine());
    }

    IEnumerator FadeInRoutine()
    {
        EnsureFadeCanvas();

        if (!canvasGroup)
            yield break;

        canvasGroup.alpha = 1f;

        if (holdSeconds > 0f)
            yield return Wait(holdSeconds);

        float duration = Mathf.Max(0.01f, fadeInDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            canvasGroup.alpha = 1f - Mathf.Clamp01(elapsed / duration);
            yield return null;
        }

        canvasGroup.alpha = 0f;

        if (fadeRoot)
            Destroy(fadeRoot);
    }

    IEnumerator Wait(float seconds)
    {
        if (useUnscaledTime)
        {
            float elapsed = 0f;
            while (elapsed < seconds)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
        }
        else
        {
            yield return new WaitForSeconds(seconds);
        }
    }

    void EnsureFadeCanvas()
    {
        if (canvasGroup)
            return;

        fadeRoot = new GameObject("SceneStartFadeCanvas");

        Canvas canvas = fadeRoot.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;

        CanvasScaler scaler = fadeRoot.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        fadeRoot.AddComponent<GraphicRaycaster>();

        canvasGroup = fadeRoot.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;
        canvasGroup.interactable = false;

        GameObject imageObject = new GameObject("FadeImage");
        imageObject.transform.SetParent(fadeRoot.transform, false);

        RectTransform rect = imageObject.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image image = imageObject.AddComponent<Image>();
        image.color = fadeColor;
    }
}
