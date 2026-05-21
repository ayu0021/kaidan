using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BattleObjectiveUI : MonoBehaviour
{
    [Header("Refs")]
    public BossShellController bossShell;
    public Sprite objectiveSprite;
    public Sprite remainingSprite;

    [Header("Text")]
    public TMP_FontAsset fontAsset;
    public string titleText = "主要任務：擊敗虎姑婆";
    public string breakShellFormat = "擊破碎片：剩餘 {0} 片";
    public string attackWeakPointText = "攻擊虎姑婆身前的弱點";

    [Header("Layout")]
    public Vector2 anchoredPosition = new Vector2(32f, -32f);
    public Vector2 panelSize = new Vector2(360f, 132f);

    GameObject _remainingRow;
    Image _objectiveImage;
    Image _remainingImage;
    TextMeshProUGUI _titleFallback;
    TextMeshProUGUI _subtitle;
    TextMeshProUGUI _remainingCount;
    string _lastSubtitle;
    bool _lastShellCleared;

    void Awake()
    {
        if (!bossShell)
            bossShell = GetComponent<BossShellController>();

        if (!bossShell)
            bossShell = FindObjectOfType<BossShellController>();

        BuildUI();
        Refresh(true);
    }

    void Update()
    {
        Refresh(false);
    }

    void BuildUI()
    {
        GameObject canvasObject = new GameObject("BattleObjectiveCanvas_Runtime");
        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 40;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObject.AddComponent<GraphicRaycaster>();

        GameObject panelObject = new GameObject("ObjectivePanel");
        panelObject.transform.SetParent(canvasObject.transform, false);

        RectTransform panelRect = panelObject.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0f, 1f);
        panelRect.anchorMax = new Vector2(0f, 1f);
        panelRect.pivot = new Vector2(0f, 1f);
        panelRect.anchoredPosition = anchoredPosition;
        panelRect.sizeDelta = panelSize;

        Image panelImage = panelObject.AddComponent<Image>();
        panelImage.color = new Color(0f, 0f, 0f, 0f);

        GameObject titleObject = new GameObject("ObjectiveImage");
        titleObject.transform.SetParent(panelObject.transform, false);
        _objectiveImage = titleObject.AddComponent<Image>();
        _objectiveImage.sprite = objectiveSprite;
        _objectiveImage.preserveAspect = true;
        _objectiveImage.raycastTarget = false;
        _objectiveImage.color = objectiveSprite ? Color.white : Color.clear;

        RectTransform titleRect = _objectiveImage.rectTransform;
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(0f, 1f);
        titleRect.pivot = new Vector2(0f, 1f);
        titleRect.anchoredPosition = Vector2.zero;
        titleRect.sizeDelta = new Vector2(260f, 90f);

        GameObject titleFallbackObject = new GameObject("TitleFallback");
        titleFallbackObject.transform.SetParent(panelObject.transform, false);
        _titleFallback = titleFallbackObject.AddComponent<TextMeshProUGUI>();
        SetupText(_titleFallback, 24f, FontStyles.Bold, new Color(1f, 0.94f, 0.78f, 1f));
        RectTransform fallbackRect = _titleFallback.rectTransform;
        fallbackRect.anchorMin = new Vector2(0f, 1f);
        fallbackRect.anchorMax = new Vector2(1f, 1f);
        fallbackRect.pivot = new Vector2(0f, 1f);
        fallbackRect.anchoredPosition = new Vector2(18f, -16f);
        fallbackRect.sizeDelta = new Vector2(-36f, 34f);
        titleFallbackObject.SetActive(!objectiveSprite);

        _remainingRow = new GameObject("RemainingRow");
        _remainingRow.transform.SetParent(panelObject.transform, false);
        RectTransform remainingRowRect = _remainingRow.AddComponent<RectTransform>();
        remainingRowRect.anchorMin = new Vector2(0f, 1f);
        remainingRowRect.anchorMax = new Vector2(0f, 1f);
        remainingRowRect.pivot = new Vector2(0f, 1f);
        remainingRowRect.anchoredPosition = new Vector2(8f, -72f);
        remainingRowRect.sizeDelta = new Vector2(310f, 56f);

        GameObject remainingImageObject = new GameObject("RemainingImage");
        remainingImageObject.transform.SetParent(_remainingRow.transform, false);
        _remainingImage = remainingImageObject.AddComponent<Image>();
        _remainingImage.sprite = remainingSprite;
        _remainingImage.preserveAspect = true;
        _remainingImage.raycastTarget = false;
        _remainingImage.color = remainingSprite ? Color.white : Color.clear;

        RectTransform remainingImageRect = _remainingImage.rectTransform;
        remainingImageRect.anchorMin = new Vector2(0f, 0.5f);
        remainingImageRect.anchorMax = new Vector2(0f, 0.5f);
        remainingImageRect.pivot = new Vector2(0f, 0.5f);
        remainingImageRect.anchoredPosition = Vector2.zero;
        remainingImageRect.sizeDelta = new Vector2(148f, 56f);

        GameObject countObject = new GameObject("RemainingCount");
        countObject.transform.SetParent(_remainingRow.transform, false);
        _remainingCount = countObject.AddComponent<TextMeshProUGUI>();
        SetupText(_remainingCount, 34f, FontStyles.Bold, Color.white);
        _remainingCount.alignment = TextAlignmentOptions.Left;
        _remainingCount.overflowMode = TextOverflowModes.Overflow;

        RectTransform countRect = _remainingCount.rectTransform;
        countRect.anchorMin = new Vector2(0f, 0.5f);
        countRect.anchorMax = new Vector2(0f, 0.5f);
        countRect.pivot = new Vector2(0f, 0.5f);
        countRect.anchoredPosition = new Vector2(150f, 0f);
        countRect.sizeDelta = new Vector2(120f, 56f);

        GameObject subtitleObject = new GameObject("Subtitle");
        subtitleObject.transform.SetParent(panelObject.transform, false);
        _subtitle = subtitleObject.AddComponent<TextMeshProUGUI>();
        SetupText(_subtitle, 23f, FontStyles.Normal, new Color(0.86f, 0.96f, 1f, 1f));

        RectTransform subtitleRect = _subtitle.rectTransform;
        subtitleRect.anchorMin = new Vector2(0f, 1f);
        subtitleRect.anchorMax = new Vector2(1f, 1f);
        subtitleRect.pivot = new Vector2(0f, 1f);
        subtitleRect.anchoredPosition = new Vector2(18f, -82f);
        subtitleRect.sizeDelta = new Vector2(-36f, 34f);
        subtitleObject.SetActive(false);
    }

    void SetupText(TextMeshProUGUI text, float size, FontStyles style, Color color)
    {
        text.raycastTarget = false;
        ApplyFont(text);

        text.enableWordWrapping = false;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.alignment = TextAlignmentOptions.Left;
        text.fontSize = size;
        text.fontStyle = style;
        text.color = color;
        text.SetAllDirty();
        text.ForceMeshUpdate();
    }

    void ApplyFont(TextMeshProUGUI text)
    {
        if (!fontAsset || !text) return;

        text.font = fontAsset;

        if (fontAsset.material)
            text.fontSharedMaterial = fontAsset.material;
    }

    void Refresh(bool force)
    {
        if (_titleFallback)
            _titleFallback.text = titleText;

        string nextSubtitle = GetSubtitle();
        bool shellCleared = bossShell && bossShell.ShellCleared;
        if (!force && nextSubtitle == _lastSubtitle && shellCleared == _lastShellCleared) return;

        _lastSubtitle = nextSubtitle;
        _lastShellCleared = shellCleared;

        if (_remainingRow)
            _remainingRow.SetActive(!shellCleared);

        if (_subtitle)
        {
            _subtitle.text = nextSubtitle;
            _subtitle.gameObject.SetActive(shellCleared);
        }

        if (_remainingCount)
            _remainingCount.text = GetRemainingCountText();
    }

    string GetSubtitle()
    {
        if (!bossShell)
            return breakShellFormat.Replace("{0}", "?");

        if (bossShell.ShellCleared)
            return attackWeakPointText;

        return string.Format(breakShellFormat, bossShell.RemainingShellCount);
    }

    string GetRemainingCountText()
    {
        if (!bossShell)
            return "?";

        return bossShell.RemainingShellCount.ToString();
    }
}
