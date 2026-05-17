using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BattleRulesUI : MonoBehaviour
{
    [Header("Rule Image")]
    public Sprite rulesSprite;
    public string buttonLabel = "戰鬥說明";

    [Header("Optional References")]
    public GlobalSettingsUI settingsUI;
    public TMP_FontAsset fontAsset;

    Canvas canvas;
    GameObject overlayRoot;
    Button closeButton;
    Button settingsRulesButton;

    public bool IsOpen => overlayRoot && overlayRoot.activeSelf;

    void Awake()
    {
        EnsureUI();
    }

    void Start()
    {
        AddSettingsButton();
    }

    public void Show()
    {
        EnsureUI();
        overlayRoot.SetActive(true);
    }

    public void Hide()
    {
        if (overlayRoot)
            overlayRoot.SetActive(false);
    }

    void EnsureUI()
    {
        if (overlayRoot)
            return;

        GameObject canvasObject = new GameObject("BattleRulesCanvas");
        canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9000;
        canvasObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasObject.AddComponent<GraphicRaycaster>();

        overlayRoot = new GameObject("BattleRulesOverlay");
        overlayRoot.transform.SetParent(canvas.transform, false);
        RectTransform rootRect = overlayRoot.AddComponent<RectTransform>();
        Stretch(rootRect);

        Image dim = overlayRoot.AddComponent<Image>();
        dim.color = new Color(0f, 0f, 0f, 0.78f);

        GameObject imageObject = new GameObject("RulesImage");
        imageObject.transform.SetParent(overlayRoot.transform, false);
        RectTransform imageRect = imageObject.AddComponent<RectTransform>();
        imageRect.anchorMin = new Vector2(0.5f, 0.5f);
        imageRect.anchorMax = new Vector2(0.5f, 0.5f);
        imageRect.sizeDelta = new Vector2(1280f, 720f);
        Image image = imageObject.AddComponent<Image>();
        image.sprite = rulesSprite;
        image.preserveAspect = true;
        image.color = rulesSprite ? Color.white : new Color(0.12f, 0.12f, 0.16f, 1f);

        GameObject placeholderText = new GameObject("PlaceholderText");
        placeholderText.transform.SetParent(imageObject.transform, false);
        RectTransform placeholderRect = placeholderText.AddComponent<RectTransform>();
        Stretch(placeholderRect);
        TextMeshProUGUI placeholder = placeholderText.AddComponent<TextMeshProUGUI>();
        placeholder.text = rulesSprite ? "" : "戰鬥規則";
        placeholder.alignment = TextAlignmentOptions.Center;
        placeholder.fontSize = 64f;
        placeholder.color = Color.white;
        if (fontAsset) placeholder.font = fontAsset;

        closeButton = CreateTextButton("CloseButton", overlayRoot.transform, "X", new Vector2(-44f, -44f), new Vector2(72f, 72f));
        RectTransform closeRect = closeButton.GetComponent<RectTransform>();
        closeRect.anchorMin = closeRect.anchorMax = new Vector2(1f, 1f);
        closeRect.pivot = new Vector2(1f, 1f);
        closeButton.onClick.AddListener(Hide);

        overlayRoot.SetActive(false);
    }

    void AddSettingsButton()
    {
        if (!settingsUI)
            settingsUI = FindObjectOfType<GlobalSettingsUI>(true);

        if (!settingsUI || !settingsUI.settingPanel || settingsRulesButton)
            return;

        settingsRulesButton = CreateTextButton(
            "BattleRulesButton",
            settingsUI.settingPanel.transform,
            buttonLabel,
            new Vector2(0f, -150f),
            new Vector2(280f, 72f)
        );

        RectTransform rect = settingsRulesButton.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        settingsRulesButton.onClick.AddListener(Show);
    }

    Button CreateTextButton(string name, Transform parent, string label, Vector2 anchoredPosition, Vector2 size)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);

        RectTransform rect = go.AddComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        Image image = go.AddComponent<Image>();
        image.color = new Color(0.1f, 0.1f, 0.13f, 0.96f);

        Button button = go.AddComponent<Button>();

        GameObject textObject = new GameObject("Label");
        textObject.transform.SetParent(go.transform, false);
        RectTransform textRect = textObject.AddComponent<RectTransform>();
        Stretch(textRect);
        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        text.text = label;
        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = 30f;
        text.color = Color.white;
        if (fontAsset) text.font = fontAsset;

        return button;
    }

    static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
