using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class BattleDeathMenu : MonoBehaviour
{
    [Header("Scenes")]
    public string battleSceneName = "戰鬥場景";
    public string mainMenuSceneName = "start";

    [Header("Font")]
    public TMP_FontAsset fontAsset;

    [Header("Look")]
    [Range(0f, 1f)] public float overlayAlpha = 0.72f;
    public Color textColor = Color.white;
    public Color buttonColor = new Color(0.08f, 0.08f, 0.08f, 0.82f);
    public Color buttonHoverColor = new Color(0.18f, 0.18f, 0.18f, 0.92f);
    public Color warningColor = new Color(1f, 0.86f, 0.58f, 1f);

    GameObject _root;
    TextMeshProUGUI _warningText;
    bool _shown;

    public void Show()
    {
        if (_shown) return;
        _shown = true;

        Time.timeScale = 1f;
        BuildUI();
        _root.SetActive(true);
    }

    void BuildUI()
    {
        if (_root) return;

        GameObject canvasObj = new GameObject("BattleDeathMenuCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 300;
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        canvasObj.AddComponent<GraphicRaycaster>();

        _root = canvasObj;

        GameObject overlayObj = new GameObject("Overlay");
        overlayObj.transform.SetParent(canvasObj.transform, false);
        RectTransform overlayRt = overlayObj.AddComponent<RectTransform>();
        overlayRt.anchorMin = Vector2.zero;
        overlayRt.anchorMax = Vector2.one;
        overlayRt.offsetMin = Vector2.zero;
        overlayRt.offsetMax = Vector2.zero;
        Image overlay = overlayObj.AddComponent<Image>();
        overlay.color = new Color(0f, 0f, 0f, overlayAlpha);

        GameObject panelObj = new GameObject("Panel");
        panelObj.transform.SetParent(canvasObj.transform, false);
        RectTransform panelRt = panelObj.AddComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(0.5f, 0.5f);
        panelRt.anchorMax = new Vector2(0.5f, 0.5f);
        panelRt.pivot = new Vector2(0.5f, 0.5f);
        panelRt.anchoredPosition = Vector2.zero;
        panelRt.sizeDelta = new Vector2(620f, 360f);

        VerticalLayoutGroup layout = panelObj.AddComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.spacing = 22f;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;

        AddText(panelObj.transform, "挑戰失敗", 42f, FontStyles.Bold, textColor, 58f);
        AddButton(panelObj.transform, "重新挑戰", RestartBattle);
        AddButton(panelObj.transform, "回到主畫面", ReturnToMainMenu, true);
        _warningText = AddText(panelObj.transform, "若回主畫面後無法繼續進度", 24f, FontStyles.Normal, warningColor, 42f);
        _warningText.gameObject.SetActive(false);
    }

    TextMeshProUGUI AddText(Transform parent, string text, float size, FontStyles style, Color color, float height)
    {
        GameObject obj = new GameObject(text);
        obj.transform.SetParent(parent, false);
        LayoutElement layout = obj.AddComponent<LayoutElement>();
        layout.preferredHeight = height;

        TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.font = fontAsset;
        tmp.fontSize = size;
        tmp.fontStyle = style;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
        return tmp;
    }

    void AddButton(Transform parent, string label, UnityEngine.Events.UnityAction action, bool showMainMenuWarning = false)
    {
        GameObject obj = new GameObject(label);
        obj.transform.SetParent(parent, false);
        LayoutElement layout = obj.AddComponent<LayoutElement>();
        layout.preferredHeight = 68f;
        layout.preferredWidth = 360f;

        Image image = obj.AddComponent<Image>();
        image.color = buttonColor;

        Button button = obj.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = buttonColor;
        colors.highlightedColor = buttonHoverColor;
        colors.pressedColor = Color.black;
        colors.selectedColor = buttonHoverColor;
        button.colors = colors;
        button.onClick.AddListener(action);

        GameObject textObj = new GameObject("Label");
        textObj.transform.SetParent(obj.transform, false);
        RectTransform rt = textObj.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.font = fontAsset;
        tmp.fontSize = 30f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = textColor;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;

        if (showMainMenuWarning)
            AddWarningTriggers(obj);
    }

    void AddWarningTriggers(GameObject buttonObject)
    {
        EventTrigger trigger = buttonObject.AddComponent<EventTrigger>();
        AddTrigger(trigger, EventTriggerType.PointerEnter, () => SetWarningVisible(true));
        AddTrigger(trigger, EventTriggerType.Select, () => SetWarningVisible(true));
        AddTrigger(trigger, EventTriggerType.PointerExit, () => SetWarningVisible(false));
        AddTrigger(trigger, EventTriggerType.Deselect, () => SetWarningVisible(false));
    }

    void AddTrigger(EventTrigger trigger, EventTriggerType type, UnityEngine.Events.UnityAction action)
    {
        EventTrigger.Entry entry = new EventTrigger.Entry();
        entry.eventID = type;
        entry.callback.AddListener(_ => action());
        trigger.triggers.Add(entry);
    }

    void SetWarningVisible(bool visible)
    {
        if (_warningText)
            _warningText.gameObject.SetActive(visible);
    }

    void RestartBattle()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(string.IsNullOrEmpty(battleSceneName) ? SceneManager.GetActiveScene().name : battleSceneName);
    }

    void ReturnToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }
}
