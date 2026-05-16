using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class BasementBookcaseSequence : MonoBehaviour
{
    [Header("References")]
    public DialogueManager dialogueManager;
    public Transform bookcase;
    public string autoFindBookcaseName = "書櫃_0";
    public DialogueAsset mechanismDialogue;
    public DialogueAsset enterOtherSpaceDialogue;

    [Header("Bookcase Move")]
    public Vector3 localMoveOffset = new Vector3(-3f, 0f, 0f);
    public float moveDuration = 1.2f;

    [Header("Trigger")]
    public string playerTag = "Player";
    public bool triggerOnce = true;
    public bool rememberTriggeredEvent = true;
    public string eventId = "虎姑婆_地下室:書櫃機關";

    [Header("Scene Load")]
    public string targetSceneName = "虎姑婆_地下室";

    [Header("Fade Transition")]
    public bool useFadeTransition = true;
    public float fadeDuration = 0.8f;
    public Color fadeColor = Color.black;
    public int fadeSortingOrder = 9999;

    Vector3 startLocalPosition;
    Vector3 openedLocalPosition;
    bool initialized;
    bool running;
    bool triggered;

    void Start()
    {
        Initialize();
    }

    void Initialize()
    {
        if (initialized) return;

        ResolveDialogueManager();

        if (!bookcase)
        {
            GameObject found = GameObject.Find(autoFindBookcaseName);
            if (found)
                bookcase = found.transform;
        }

        if (!bookcase)
        {
            Debug.LogWarning($"[BasementBookcaseSequence] 找不到書櫃 '{autoFindBookcaseName}'。", this);
            return;
        }

        startLocalPosition = bookcase.localPosition;
        openedLocalPosition = startLocalPosition + localMoveOffset;
        EnsureBookcaseTrigger();

        if (rememberTriggeredEvent && GameProgressState.Instance != null && GameProgressState.Instance.HasCompletedEvent(eventId))
        {
            triggered = true;
            bookcase.localPosition = openedLocalPosition;
        }

        initialized = true;
    }

    void ResolveDialogueManager()
    {
        if (IsSceneInstance(dialogueManager))
            return;

        DialogueManager[] managers = FindObjectsOfType<DialogueManager>(true);
        for (int i = 0; i < managers.Length; i++)
        {
            if (IsSceneInstance(managers[i]))
            {
                dialogueManager = managers[i];
                return;
            }
        }

        dialogueManager = null;
        Debug.LogWarning("[BasementBookcaseSequence] 找不到場景內的 DialogueManager。", this);
    }

    static bool IsSceneInstance(Component component)
    {
        return component && component.gameObject.scene.IsValid();
    }

    void EnsureBookcaseTrigger()
    {
        Collider col = bookcase.GetComponent<Collider>();
        if (!col)
            col = bookcase.gameObject.AddComponent<BoxCollider>();

        col.isTrigger = true;

        BookcaseSequenceTrigger relay = bookcase.GetComponent<BookcaseSequenceTrigger>();
        if (!relay)
            relay = bookcase.gameObject.AddComponent<BookcaseSequenceTrigger>();

        relay.owner = this;
    }

    public void NotifyPlayerEntered(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        if ((triggerOnce || rememberTriggeredEvent) && triggered) return;
        if (running) return;

        StartCoroutine(RunSequence());
    }

    IEnumerator RunSequence()
    {
        Initialize();
        if (!initialized || running) yield break;

        running = true;
        triggered = true;

        if (rememberTriggeredEvent && GameProgressState.Instance != null)
            GameProgressState.Instance.MarkEventCompleted(eventId);

        yield return PlayDialogueAndWait(mechanismDialogue);
        yield return MoveBookcase();
        yield return PlayDialogueAndWait(enterOtherSpaceDialogue);

        if (!string.IsNullOrWhiteSpace(targetSceneName))
        {
            if (useFadeTransition)
                BookcaseSceneFadeTransition.Begin(targetSceneName, fadeDuration, fadeColor, fadeSortingOrder);
            else
                SceneManager.LoadScene(targetSceneName);
        }

        running = false;
    }

    IEnumerator PlayDialogueAndWait(DialogueAsset asset)
    {
        ResolveDialogueManager();

        if (!dialogueManager || !asset)
            yield break;

        dialogueManager.Play(asset);
        yield return null;

        while (dialogueManager && dialogueManager.IsPlaying)
            yield return null;
    }

    IEnumerator MoveBookcase()
    {
        if (!bookcase)
            yield break;

        if (moveDuration <= 0f)
        {
            bookcase.localPosition = openedLocalPosition;
            yield break;
        }

        float t = 0f;
        Vector3 from = bookcase.localPosition;

        while (t < moveDuration)
        {
            t += Time.deltaTime;
            float eased = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / moveDuration));
            bookcase.localPosition = Vector3.LerpUnclamped(from, openedLocalPosition, eased);
            yield return null;
        }

        bookcase.localPosition = openedLocalPosition;
    }
}

public class BookcaseSequenceTrigger : MonoBehaviour
{
    [HideInInspector] public BasementBookcaseSequence owner;

    void OnTriggerEnter(Collider other)
    {
        if (owner)
            owner.NotifyPlayerEntered(other);
    }
}

public class BookcaseSceneFadeTransition : MonoBehaviour
{
    CanvasGroup canvasGroup;
    Image fadeImage;

    public static void Begin(string sceneName, float duration, Color color, int sortingOrder)
    {
        GameObject root = new GameObject("BookcaseSceneFadeTransition");
        DontDestroyOnLoad(root);

        BookcaseSceneFadeTransition transition = root.AddComponent<BookcaseSceneFadeTransition>();
        transition.CreateFadeCanvas(color, sortingOrder);
        transition.StartCoroutine(transition.Run(sceneName, duration));
    }

    IEnumerator Run(string sceneName, float duration)
    {
        yield return FadeTo(1f, duration);
        SceneManager.LoadScene(sceneName);
        yield return null;
        yield return FadeTo(0f, duration);
        Destroy(gameObject);
    }

    IEnumerator FadeTo(float targetAlpha, float duration)
    {
        float startAlpha = canvasGroup.alpha;

        if (duration <= 0f)
        {
            canvasGroup.alpha = targetAlpha;
            yield break;
        }

        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, Mathf.Clamp01(t / duration));
            yield return null;
        }

        canvasGroup.alpha = targetAlpha;
    }

    void CreateFadeCanvas(Color color, int sortingOrder)
    {
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;

        gameObject.AddComponent<CanvasScaler>();
        gameObject.AddComponent<GraphicRaycaster>();

        canvasGroup = gameObject.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = true;
        canvasGroup.interactable = false;

        GameObject imageGO = new GameObject("FadeImage");
        imageGO.transform.SetParent(transform, false);

        RectTransform rt = imageGO.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        fadeImage = imageGO.AddComponent<Image>();
        fadeImage.color = color;
    }
}
