using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BattleObjectiveUI : MonoBehaviour
{
    [Header("Refs")]
    public BossShellController bossShell;

    [Header("Text")]
    public TMP_FontAsset fontAsset;
    public string titleText = "主要任務：擊敗虎姑婆";
    public string breakShellFormat = "擊破碎片：剩餘 {0} 片";
    public string attackWeakPointText = "攻擊虎姑婆身前的弱點";

    [Header("Layout")]
    public Vector2 anchoredPosition = new Vector2(32f, -32f);
    public Vector2 panelSize = new Vector2(460f, 126f);

    TextMeshProUGUI _title;
    TextMeshProUGUI _subtitle;
    string _lastSubtitle;

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
        panelImage.color = new Color(0.03f, 0.035f, 0.045f, 0.72f);

        GameObject titleObject = new GameObject("Title");
        titleObject.transform.SetParent(panelObject.transform, false);
        _title = titleObject.AddComponent<TextMeshProUGUI>();
        SetupText(_title, 28f, FontStyles.Bold, new Color(1f, 0.94f, 0.78f, 1f));

        RectTransform titleRect = _title.rectTransform;
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0f, 1f);
        titleRect.anchoredPosition = new Vector2(22f, -18f);
        titleRect.sizeDelta = new Vector2(-44f, 40f);

        GameObject subtitleObject = new GameObject("Subtitle");
        subtitleObject.transform.SetParent(panelObject.transform, false);
        _subtitle = subtitleObject.AddComponent<TextMeshProUGUI>();
        SetupText(_subtitle, 23f, FontStyles.Normal, new Color(0.86f, 0.96f, 1f, 1f));

        RectTransform subtitleRect = _subtitle.rectTransform;
        subtitleRect.anchorMin = new Vector2(0f, 1f);
        subtitleRect.anchorMax = new Vector2(1f, 1f);
        subtitleRect.pivot = new Vector2(0f, 1f);
        subtitleRect.anchoredPosition = new Vector2(22f, -70f);
        subtitleRect.sizeDelta = new Vector2(-44f, 34f);
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
        if (_title)
            _title.text = titleText;

        string nextSubtitle = GetSubtitle();
        if (!force && nextSubtitle == _lastSubtitle) return;

        _lastSubtitle = nextSubtitle;

        if (_subtitle)
            _subtitle.text = nextSubtitle;
    }

    string GetSubtitle()
    {
        if (!bossShell)
            return breakShellFormat.Replace("{0}", "?");

        if (bossShell.ShellCleared)
            return attackWeakPointText;

        return string.Format(breakShellFormat, bossShell.RemainingShellCount);
    }
}
