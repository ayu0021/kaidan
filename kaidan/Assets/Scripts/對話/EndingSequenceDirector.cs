using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class EndingSequenceDirector : MonoBehaviour
{
    [Header("Dialogue")]
    public DialogueManager dialogueSystemPrefab;
    public DialogueAsset openingDialogue;
    public DialogueAsset storytellerDialogue;

    [Header("Storyteller UI")]
    public Sprite storytellerSprite;
    public Vector2 storytellerAnchoredPosition = new Vector2(0f, -40f);
    public Vector2 storytellerSize = new Vector2(720f, 720f);

    [Header("Spotlight UI")]
    public Color spotlightColor = new Color(0.88f, 0.94f, 1f, 0.92f);
    public Vector2 spotlightSize = new Vector2(980f, 980f);

    [Header("Lantern UI")]
    public Color lanternColor = new Color(0.12f, 0.55f, 1f, 1f);
    public Vector2 leftLanternPosition = new Vector2(-180f, 85f);
    public Vector2 rightLanternPosition = new Vector2(180f, 85f);
    public Vector2 lanternGlowSize = new Vector2(220f, 220f);
    public float lanternBaseAlpha = 0.45f;
    public float lanternFlickerAmount = 0.4f;
    public float lanternFlickerSpeed = 7f;

    [Header("Timing")]
    public float initialBlackHold = 0.8f;
    public float glitchDuration = 0.55f;
    public float revealDuration = 1.2f;
    public float endingFadeDuration = 2.2f;

    [Header("Credits")]
    public TMP_FontAsset creditsFont;
    [TextArea(4, 12)]
    public string creditsText = "製作團隊\n\n感謝每一位同行的工作夥伴\n\n謝謝遊玩";
    public float creditsScrollDuration = 18f;
    public string restartSceneName = "start";

    DialogueManager dialogueManager;
    Canvas overlayCanvas;
    RectTransform blackRect;
    CanvasGroup blackGroup;
    RawImage glitchImage;
    CanvasGroup glitchGroup;
    Image storytellerImage;
    RawImage spotlightImage;
    RawImage[] lanternGlowImages;
    TextMeshProUGUI creditsLabel;
    RectTransform creditsRect;
    CanvasGroup creditsGroup;
    float[] lanternSeeds;

    void Start()
    {
        StartCoroutine(RunSequence());
    }

    IEnumerator RunSequence()
    {
        PrepareScene();
        EnsureDialogueManager();
        EnsureOverlay();

        SetStorytellerVisible(false);
        SetUiEffectsVisible(false);
        blackGroup.alpha = 1f;

        if (initialBlackHold > 0f)
            yield return new WaitForSeconds(initialBlackHold);

        yield return PlayDialogue(openingDialogue);
        yield return PlayGlitch();

        SetStorytellerVisible(true);
        SetUiEffectsVisible(true);
        yield return FadeBlack(0f, revealDuration);

        StartCoroutine(FlickerLanterns());
        yield return PlayDialogue(storytellerDialogue);

        blackRect.SetAsLastSibling();
        yield return FadeBlack(1f, endingFadeDuration);
        yield return RollCredits();
        yield return FadeCredits(0f, 1.2f);
        ResetRunState();

        if (!string.IsNullOrWhiteSpace(restartSceneName))
            SceneManager.LoadScene(restartSceneName);
    }

    void PrepareScene()
    {
        if (Camera.main)
        {
            Camera.main.clearFlags = CameraClearFlags.SolidColor;
            Camera.main.backgroundColor = Color.black;
        }
    }

    void EnsureDialogueManager()
    {
        dialogueManager = FindObjectOfType<DialogueManager>(true);

        Canvas dialogueCanvas = dialogueManager ? dialogueManager.GetComponentInParent<Canvas>() : null;
        if (!dialogueCanvas)
            dialogueCanvas = CreateDialogueCanvas();

        if (!dialogueManager && dialogueSystemPrefab)
            dialogueManager = Instantiate(dialogueSystemPrefab, dialogueCanvas.transform);
        else if (dialogueManager && !dialogueManager.GetComponentInParent<Canvas>())
            dialogueManager.transform.SetParent(dialogueCanvas.transform, false);
    }

    Canvas CreateDialogueCanvas()
    {
        GameObject canvasObject = new GameObject("對話Canvas");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObject.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    void EnsureOverlay()
    {
        GameObject canvasObject = new GameObject("EndingOverlayCanvas");
        overlayCanvas = canvasObject.AddComponent<Canvas>();
        overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        overlayCanvas.sortingOrder = 150;
        canvasObject.AddComponent<CanvasScaler>();
        canvasObject.AddComponent<GraphicRaycaster>();

        GameObject blackObject = CreateFullscreenChild("BlackOverlay");
        blackRect = blackObject.GetComponent<RectTransform>();
        blackGroup = blackObject.AddComponent<CanvasGroup>();
        Image blackImage = blackObject.AddComponent<Image>();
        blackImage.color = Color.black;

        GameObject glitchObject = CreateFullscreenChild("GlitchOverlay");
        glitchGroup = glitchObject.AddComponent<CanvasGroup>();
        glitchGroup.alpha = 0f;
        glitchImage = glitchObject.AddComponent<RawImage>();
        glitchImage.color = Color.white;

        GameObject spotlightObject = new GameObject("Spotlight");
        spotlightObject.transform.SetParent(overlayCanvas.transform, false);
        RectTransform spotlightRect = spotlightObject.AddComponent<RectTransform>();
        spotlightRect.anchorMin = spotlightRect.anchorMax = new Vector2(0.5f, 0.5f);
        spotlightRect.sizeDelta = spotlightSize;
        spotlightRect.anchoredPosition = storytellerAnchoredPosition;
        spotlightImage = spotlightObject.AddComponent<RawImage>();
        spotlightImage.texture = CreateRadialTexture(256, Color.white);
        spotlightImage.color = spotlightColor;

        GameObject storytellerObject = new GameObject("Storyteller");
        storytellerObject.transform.SetParent(overlayCanvas.transform, false);
        RectTransform storytellerRect = storytellerObject.AddComponent<RectTransform>();
        storytellerRect.anchorMin = storytellerRect.anchorMax = new Vector2(0.5f, 0.5f);
        storytellerRect.sizeDelta = storytellerSize;
        storytellerRect.anchoredPosition = storytellerAnchoredPosition;
        storytellerImage = storytellerObject.AddComponent<Image>();
        storytellerImage.sprite = storytellerSprite;
        storytellerImage.preserveAspect = true;

        lanternGlowImages = new RawImage[2];
        lanternGlowImages[0] = CreateLanternGlow("LanternGlowLeft", leftLanternPosition);
        lanternGlowImages[1] = CreateLanternGlow("LanternGlowRight", rightLanternPosition);
        lanternSeeds = new[] { Random.Range(0f, 100f), Random.Range(0f, 100f) };

        GameObject creditsObject = new GameObject("Credits");
        creditsObject.transform.SetParent(overlayCanvas.transform, false);
        creditsRect = creditsObject.AddComponent<RectTransform>();
        creditsRect.anchorMin = new Vector2(0.1f, 0f);
        creditsRect.anchorMax = new Vector2(0.9f, 0f);
        creditsRect.pivot = new Vector2(0.5f, 0f);
        creditsRect.anchoredPosition = new Vector2(0f, -180f);
        creditsRect.sizeDelta = new Vector2(0f, 900f);
        creditsGroup = creditsObject.AddComponent<CanvasGroup>();
        creditsGroup.alpha = 1f;

        creditsLabel = creditsObject.AddComponent<TextMeshProUGUI>();
        creditsLabel.text = creditsText;
        creditsLabel.alignment = TextAlignmentOptions.Center;
        creditsLabel.fontSize = 42f;
        creditsLabel.color = Color.white;
        creditsLabel.enableWordWrapping = true;
        if (creditsFont)
            creditsLabel.font = creditsFont;
        creditsLabel.gameObject.SetActive(false);
    }

    RawImage CreateLanternGlow(string name, Vector2 anchoredPosition)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(overlayCanvas.transform, false);
        RectTransform rect = go.AddComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = lanternGlowSize;
        rect.anchoredPosition = storytellerAnchoredPosition + anchoredPosition;

        RawImage image = go.AddComponent<RawImage>();
        image.texture = CreateRadialTexture(128, Color.white);
        image.color = new Color(lanternColor.r, lanternColor.g, lanternColor.b, lanternBaseAlpha);
        return image;
    }

    Texture2D CreateRadialTexture(int size, Color tint)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Bilinear;
        Color[] pixels = new Color[size * size];
        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        float maxDistance = center.magnitude;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center) / maxDistance;
                float alpha = Mathf.Clamp01(1f - distance);
                alpha = alpha * alpha;
                pixels[y * size + x] = new Color(tint.r, tint.g, tint.b, alpha);
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();
        return texture;
    }

    GameObject CreateFullscreenChild(string name)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(overlayCanvas.transform, false);
        RectTransform rect = go.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return go;
    }

    IEnumerator PlayDialogue(DialogueAsset asset)
    {
        if (!dialogueManager || !asset)
            yield break;

        dialogueManager.Play(asset);
        yield return null;

        while (dialogueManager && dialogueManager.IsPlaying)
            yield return null;
    }

    IEnumerator PlayGlitch()
    {
        Texture2D texture = new Texture2D(64, 64, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
        glitchImage.texture = texture;
        glitchGroup.alpha = 1f;

        float elapsed = 0f;
        while (elapsed < glitchDuration)
        {
            elapsed += Time.deltaTime;
            FillGlitchTexture(texture);
            glitchGroup.alpha = Random.Range(0.25f, 0.95f);
            glitchImage.rectTransform.anchoredPosition = new Vector2(Random.Range(-20f, 20f), Random.Range(-8f, 8f));
            yield return new WaitForSeconds(Random.Range(0.03f, 0.1f));
        }

        glitchGroup.alpha = 0f;
        Destroy(texture);
    }

    void FillGlitchTexture(Texture2D texture)
    {
        Color[] pixels = new Color[texture.width * texture.height];
        for (int y = 0; y < texture.height; y++)
        {
            Color rowColor = Random.value > 0.7f
                ? new Color(Random.value, Random.value, Random.value, Random.Range(0.2f, 0.9f))
                : new Color(0f, 0f, 0f, Random.Range(0.2f, 0.8f));

            for (int x = 0; x < texture.width; x++)
                pixels[y * texture.width + x] = rowColor;
        }

        texture.SetPixels(pixels);
        texture.Apply();
    }

    IEnumerator FadeBlack(float targetAlpha, float duration)
    {
        float startAlpha = blackGroup.alpha;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            blackGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }

        blackGroup.alpha = targetAlpha;
    }

    IEnumerator FlickerLanterns()
    {
        while (true)
        {
            for (int i = 0; i < lanternGlowImages.Length; i++)
            {
                if (!lanternGlowImages[i]) continue;
                float noise = Mathf.PerlinNoise(lanternSeeds[i], Time.time * lanternFlickerSpeed);
                float alpha = lanternBaseAlpha + (noise - 0.5f) * 2f * lanternFlickerAmount;
                lanternGlowImages[i].color = new Color(lanternColor.r, lanternColor.g, lanternColor.b, Mathf.Clamp01(alpha));
            }
            yield return null;
        }
    }

    void SetStorytellerVisible(bool visible)
    {
        if (storytellerImage)
            storytellerImage.gameObject.SetActive(visible);
    }

    void SetUiEffectsVisible(bool visible)
    {
        if (spotlightImage)
            spotlightImage.gameObject.SetActive(visible);

        for (int i = 0; i < lanternGlowImages.Length; i++)
        {
            if (lanternGlowImages[i])
                lanternGlowImages[i].gameObject.SetActive(visible);
        }
    }

    IEnumerator RollCredits()
    {
        creditsRect.SetAsLastSibling();
        creditsLabel.gameObject.SetActive(true);
        Vector2 from = new Vector2(0f, -180f);
        Vector2 to = new Vector2(0f, Screen.height + 180f);
        float elapsed = 0f;

        while (elapsed < creditsScrollDuration)
        {
            elapsed += Time.deltaTime;
            creditsRect.anchoredPosition = Vector2.Lerp(from, to, Mathf.Clamp01(elapsed / creditsScrollDuration));
            yield return null;
        }

        creditsRect.anchoredPosition = to;
    }

    IEnumerator FadeCredits(float targetAlpha, float duration)
    {
        float startAlpha = creditsGroup.alpha;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            creditsGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }

        creditsGroup.alpha = targetAlpha;
    }

    void ResetRunState()
    {
        if (GameProgressState.Instance)
            GameProgressState.Instance.ResetAllProgress();

        if (InventoryManager.Instance)
            InventoryManager.Instance.ClearAllItems();
    }
}
