using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class DialogueTriggerAsset : MonoBehaviour
{
    public enum TriggerMode
    {
        PlayerEnter,
        ManualOnly
    }

    [Header("References")]
    public DialogueManager dialogueManager;
    public DialogueAsset dialogueAsset;

    [Header("Trigger")]
    public TriggerMode triggerMode = TriggerMode.PlayerEnter;
    public string playerTag = "Player";
    public bool triggerOnce = true;
    public bool rememberTriggeredEvent = true;
    public string eventId;

    [Header("Delay")]
    public float delaySeconds = 0f;

    [Header("Load Scene After Dialogue")]
    public bool loadSceneAfterDialogue = false;

    [Tooltip("對話全部結束後要切換到的場景名稱。場景必須加入 Build Settings。")]
    public string sceneName = "";

    [Tooltip("對話結束後，等待幾秒再切換場景。")]
    public float sceneLoadDelaySeconds = 0f;

    [Header("Fade Transition")]
    [Tooltip("切場景前是否先漸暗")]
    public bool useFadeBeforeSceneLoad = true;

    [Tooltip("漸暗秒數")]
    public float fadeDuration = 0.8f;

    [Tooltip("漸暗顏色")]
    public Color fadeColor = Color.black;

    [Tooltip("Fade UI 排序，越大越上層")]
    public int fadeSortingOrder = 9999;

    private bool triggered = false;

    private CanvasGroup fadeCanvasGroup;
    private Image fadeImage;
    private GameObject fadeRoot;

    void Awake()
    {
        EnsureEventId();

        if (rememberTriggeredEvent && GameProgressState.Instance != null && GameProgressState.Instance.HasCompletedEvent(eventId))
            triggered = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (triggerMode != TriggerMode.PlayerEnter) return;
        if (!other.CompareTag(playerTag)) return;

        TriggerDialogue();
    }

    public void TriggerDialogue()
    {
        if ((triggerOnce || rememberTriggeredEvent) && triggered) return;

        triggered = true;
        if (rememberTriggeredEvent && GameProgressState.Instance != null)
            GameProgressState.Instance.MarkEventCompleted(eventId);

        StartCoroutine(TriggerRoutine());
    }

    public void ResetTrigger()
    {
        triggered = false;
    }

    IEnumerator TriggerRoutine()
    {
        if (delaySeconds > 0f)
            yield return new WaitForSeconds(delaySeconds);

        DialogueManager manager = ResolveDialogueManager();

        if (manager != null && dialogueAsset != null)
        {
            // 等一幀，確保剛被打開的物件完成啟用
            yield return null;

            manager.Play(dialogueAsset);

            if (loadSceneAfterDialogue)
            {
                yield return null;

                while (manager != null && manager.IsPlaying)
                {
                    yield return null;
                }

                if (sceneLoadDelaySeconds > 0f)
                    yield return new WaitForSeconds(sceneLoadDelaySeconds);

                if (!string.IsNullOrEmpty(sceneName))
                {
                    if (useFadeBeforeSceneLoad)
                    {
                        yield return StartCoroutine(FadeOutRoutine());
                    }

                    SceneManager.LoadScene(sceneName);
                }
            }
        }
    }

    DialogueManager ResolveDialogueManager()
    {
        // 先用 Inspector 拖進來的
        if (dialogueManager != null)
        {
            EnsureActiveChain(dialogueManager.gameObject);

            if (dialogueManager.isActiveAndEnabled)
                return dialogueManager;
        }

        // 再自動找場景中（包含 inactive）的 DialogueManager
        DialogueManager found = FindObjectOfType<DialogueManager>(true);
        if (found != null)
        {
            EnsureActiveChain(found.gameObject);
            dialogueManager = found;
            return dialogueManager;
        }

        Debug.LogWarning("[DialogueTriggerAsset] 找不到可用的 DialogueManager。");
        return null;
    }

    void EnsureActiveChain(GameObject obj)
    {
        if (obj == null) return;

        Transform current = obj.transform;
        while (current != null)
        {
            if (!current.gameObject.activeSelf)
                current.gameObject.SetActive(true);

            current = current.parent;
        }
    }

    void EnsureEventId()
    {
        if (!string.IsNullOrWhiteSpace(eventId)) return;

        string sceneName = gameObject.scene.IsValid() ? gameObject.scene.name : "NoScene";
        eventId = $"{sceneName}:{gameObject.name}";
    }

    IEnumerator FadeOutRoutine()
    {
        EnsureFadeCanvas();

        if (fadeCanvasGroup == null)
            yield break;

        fadeImage.color = fadeColor;
        fadeCanvasGroup.alpha = 0f;

        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            float normalized = Mathf.Clamp01(t / fadeDuration);
            fadeCanvasGroup.alpha = normalized;
            yield return null;
        }

        fadeCanvasGroup.alpha = 1f;
    }

    void EnsureFadeCanvas()
    {
        if (fadeCanvasGroup != null) return;

        fadeRoot = new GameObject("AutoFadeCanvas");

        Canvas canvas = fadeRoot.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = fadeSortingOrder;

        fadeRoot.AddComponent<CanvasScaler>();
        fadeRoot.AddComponent<GraphicRaycaster>();

        fadeCanvasGroup = fadeRoot.AddComponent<CanvasGroup>();
        fadeCanvasGroup.alpha = 0f;
        fadeCanvasGroup.blocksRaycasts = false;
        fadeCanvasGroup.interactable = false;

        GameObject imageGO = new GameObject("FadeImage");
        imageGO.transform.SetParent(fadeRoot.transform, false);

        RectTransform rt = imageGO.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        fadeImage = imageGO.AddComponent<Image>();
        fadeImage.color = fadeColor;
    }
}
