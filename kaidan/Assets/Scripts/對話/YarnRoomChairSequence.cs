using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class YarnRoomChairSequence : MonoBehaviour
{
    [Header("Trigger")]
    public string playerTag = "Player";
    public KeyCode interactKey = KeyCode.F;
    public bool triggerOnce = true;
    public bool rememberTriggeredEvent = true;
    public string eventId = "虎姑婆_毛線球房:椅子事件";

    [Header("World Prompt")]
    public Vector3 promptOffset = new Vector3(0.25f, 1.3f, 0f);
    public string promptText = "按 F 互動";
    public bool billboardToCamera = true;
    public float activationRadius = 2.5f;

    [Header("Dialogue")]
    public DialogueManager dialogueManager;
    public DialogueAsset chairDialogue;
    public DialogueAsset leaveDialogue;
    public DialogueUISkin skinOverride;

    [Header("Blackout")]
    public AudioSource audioSource;
    public AudioClip bulbBreakClip;
    public Light[] lightsToTurnOff;
    public bool findAllSceneLightsIfEmpty = true;
    public float afterBlackoutDelay = 0.45f;

    [Header("Scene Load")]
    public string targetSceneName = "虎姑婆_臥室";
    public bool useFadeBeforeSceneLoad = true;
    public float fadeDuration = 0.8f;
    public Color fadeColor = Color.black;
    public int fadeSortingOrder = 9999;

    [Header("Debug")]
    public bool debugLog = false;

    bool playerInside;
    bool running;
    bool used;
    Transform playerTransform;
    CanvasGroup fadeCanvasGroup;
    Image fadeImage;

    void Awake()
    {
        if (TryGetComponent<Collider>(out var col) && !col.isTrigger)
            col.isTrigger = true;

        if (rememberTriggeredEvent && GameProgressState.GetOrCreateInstance().HasCompletedEvent(eventId))
            used = true;
    }

    void OnDisable()
    {
        HidePrompt();
    }

    void OnTriggerEnter(Collider other)
    {
        if (IsUsedOrRunning()) return;
        if (!other.CompareTag(playerTag)) return;

        playerInside = true;
        playerTransform = other.transform;
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;

        playerInside = false;
        playerTransform = null;
        HidePrompt();
    }

    void Update()
    {
        if (IsUsedOrRunning()) return;

        RefreshPlayerByDistance();

        if (!playerInside || playerTransform == null)
        {
            HidePrompt();
            return;
        }

        ShowPrompt();

        if (Input.GetKeyDown(interactKey))
        {
            HidePrompt();
            StartCoroutine(SequenceRoutine());
        }
    }

    IEnumerator SequenceRoutine()
    {
        running = true;
        used = true;

        if (rememberTriggeredEvent)
            GameProgressState.GetOrCreateInstance().MarkEventCompleted(eventId);

        ResolveReferences();

        yield return PlayDialogue(chairDialogue);

        PlayBreakSoundAndTurnOffLights();

        if (afterBlackoutDelay > 0f)
            yield return new WaitForSeconds(afterBlackoutDelay);

        yield return PlayDialogue(leaveDialogue);

        if (!string.IsNullOrWhiteSpace(targetSceneName))
        {
            if (useFadeBeforeSceneLoad)
                yield return FadeOut();

            SceneManager.LoadScene(targetSceneName);
        }
    }

    IEnumerator PlayDialogue(DialogueAsset dialogue)
    {
        ResolveReferences();

        if (dialogueManager == null || dialogue == null)
        {
            if (debugLog)
                Debug.LogWarning($"[YarnRoomChairSequence] Missing dialogue manager or dialogue asset on {name}", this);
            yield break;
        }

        dialogueManager.Play(dialogue, skinOverride);
        yield return null;

        while (dialogueManager != null && dialogueManager.IsPlaying)
            yield return null;
    }

    void PlayBreakSoundAndTurnOffLights()
    {
        ResolveReferences();

        if (audioSource != null && bulbBreakClip != null)
            audioSource.PlayOneShot(bulbBreakClip);

        Light[] lights = lightsToTurnOff;
        if ((lights == null || lights.Length == 0) && findAllSceneLightsIfEmpty)
            lights = FindObjectsOfType<Light>(true);

        if (lights == null) return;

        foreach (Light sceneLight in lights)
        {
            if (sceneLight == null) continue;

            sceneLight.intensity = 0f;
            sceneLight.enabled = false;
        }
    }

    void ResolveReferences()
    {
        if (dialogueManager == null)
            dialogueManager = FindObjectOfType<DialogueManager>(true);

        if (audioSource == null)
            audioSource = FindObjectOfType<AudioSource>(true);
    }

    void RefreshPlayerByDistance()
    {
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag(playerTag);
            if (player != null)
                playerTransform = player.transform;
        }

        if (playerTransform == null)
        {
            playerInside = false;
            return;
        }

        if (activationRadius <= 0f)
            return;

        Vector3 delta = playerTransform.position - transform.position;
        playerInside = delta.sqrMagnitude <= activationRadius * activationRadius;
    }

    void ShowPrompt()
    {
        InteractionPromptWorld prompt = FindPrompt();
        if (prompt == null) return;

        prompt.faceCamera = billboardToCamera;
        prompt.Show(transform, promptText, promptOffset, true, transform);
    }

    void HidePrompt()
    {
        InteractionPromptWorld prompt = FindPrompt();
        if (prompt != null)
            prompt.Hide(transform);
    }

    InteractionPromptWorld FindPrompt()
    {
        InteractionPromptWorld prompt = InteractionPromptWorld.Instance;
        if (prompt == null)
            prompt = FindObjectOfType<InteractionPromptWorld>(true);

        InteractionPromptWorld.Instance = prompt;
        return prompt;
    }

    bool IsUsedOrRunning()
    {
        return running || (used && (triggerOnce || rememberTriggeredEvent));
    }

    IEnumerator FadeOut()
    {
        EnsureFadeCanvas();

        if (fadeCanvasGroup == null)
            yield break;

        fadeImage.color = fadeColor;
        fadeCanvasGroup.alpha = 0f;

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            fadeCanvasGroup.alpha = Mathf.Clamp01(elapsed / fadeDuration);
            yield return null;
        }

        fadeCanvasGroup.alpha = 1f;
    }

    void EnsureFadeCanvas()
    {
        if (fadeCanvasGroup != null) return;

        GameObject root = new GameObject("YarnRoomChairFadeCanvas");
        Canvas canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = fadeSortingOrder;

        root.AddComponent<CanvasScaler>();
        root.AddComponent<GraphicRaycaster>();

        fadeCanvasGroup = root.AddComponent<CanvasGroup>();
        fadeCanvasGroup.alpha = 0f;
        fadeCanvasGroup.blocksRaycasts = true;
        fadeCanvasGroup.interactable = false;

        GameObject imageObject = new GameObject("FadeImage");
        imageObject.transform.SetParent(root.transform, false);

        RectTransform rect = imageObject.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        fadeImage = imageObject.AddComponent<Image>();
        fadeImage.color = fadeColor;
    }
}
