using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DialogueUIRefs : MonoBehaviour
{
    [Header("Root (optional)")]
    public GameObject uiRoot; // 可留空；留空就用自己 gameObject

    [Header("Active / Fallback Refs")]
    [Tooltip("如果你只有單一套 UI，可直接填這組。若你有紅框/藍框兩套，這組會當 fallback。")]
    public TMP_Text nameText;
    public TMP_Text bodyText;
    public TMP_Text hintText;
    public Image portraitImage;

    [Header("Frames (optional, choose one to show)")]
    [Tooltip("紅框容器（GameObject）。沒有就留空。")]
    public GameObject redBox;

    [Tooltip("藍框容器（GameObject）。沒有就留空。")]
    public GameObject blueBox;

    [Header("Red Frame Refs (optional)")]
    public TMP_Text redNameText;
    public TMP_Text redBodyText;
    public TMP_Text redHintText;
    public Image redPortraitImage;

    [Header("Blue Frame Refs (optional)")]
    public TMP_Text blueNameText;
    public TMP_Text blueBodyText;
    public TMP_Text blueHintText;
    public Image bluePortraitImage;

    [Header("Portrait")]
    [Tooltip("自動開啟 Preserve Aspect，避免不同尺寸立繪被硬拉變形。")]
    public bool forcePreserveAspect = true;

    public GameObject RootGO => uiRoot != null ? uiRoot : gameObject;

    private bool _usingBlue;

    // 保留一開始 Inspector 指到的 fallback 參考，避免 ShowFrame 後被覆蓋丟失
    private TMP_Text _fallbackNameText;
    private TMP_Text _fallbackBodyText;
    private TMP_Text _fallbackHintText;
    private Image _fallbackPortraitImage;

    private void Awake()
    {
        CacheFallbackRefs();
        ResolveActiveRefs(_usingBlue);
        ApplyPortraitSettings();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // 編輯器下，方便你調整時就看到正確引用
        if (!Application.isPlaying)
        {
            CacheFallbackRefs(true);
            ResolveActiveRefs(_usingBlue);
            ApplyPortraitSettings();
        }
    }
#endif

    private void CacheFallbackRefs(bool overwrite = false)
    {
        if (overwrite || _fallbackNameText == null) _fallbackNameText = nameText;
        if (overwrite || _fallbackBodyText == null) _fallbackBodyText = bodyText;
        if (overwrite || _fallbackHintText == null) _fallbackHintText = hintText;
        if (overwrite || _fallbackPortraitImage == null) _fallbackPortraitImage = portraitImage;
    }

    private void ResolveActiveRefs(bool useBlue)
    {
        CacheFallbackRefs();

        if (useBlue)
        {
            nameText = blueNameText != null ? blueNameText : _fallbackNameText;
            bodyText = blueBodyText != null ? blueBodyText : _fallbackBodyText;
            hintText = blueHintText != null ? blueHintText : _fallbackHintText;
            portraitImage = bluePortraitImage != null ? bluePortraitImage : _fallbackPortraitImage;
        }
        else
        {
            nameText = redNameText != null ? redNameText : _fallbackNameText;
            bodyText = redBodyText != null ? redBodyText : _fallbackBodyText;
            hintText = redHintText != null ? redHintText : _fallbackHintText;
            portraitImage = redPortraitImage != null ? redPortraitImage : _fallbackPortraitImage;
        }
    }

    private void ApplyPortraitSettings()
    {
        if (!forcePreserveAspect) return;

        if (_fallbackPortraitImage) _fallbackPortraitImage.preserveAspect = true;
        if (redPortraitImage) redPortraitImage.preserveAspect = true;
        if (bluePortraitImage) bluePortraitImage.preserveAspect = true;
        if (portraitImage) portraitImage.preserveAspect = true;
    }

    public void ClearAll()
    {
        var textSet = new HashSet<TMP_Text>();
        var imageSet = new HashSet<Image>();

        AddIfValid(textSet, _fallbackNameText);
        AddIfValid(textSet, _fallbackBodyText);
        AddIfValid(textSet, _fallbackHintText);
        AddIfValid(textSet, redNameText);
        AddIfValid(textSet, redBodyText);
        AddIfValid(textSet, redHintText);
        AddIfValid(textSet, blueNameText);
        AddIfValid(textSet, blueBodyText);
        AddIfValid(textSet, blueHintText);
        AddIfValid(textSet, nameText);
        AddIfValid(textSet, bodyText);
        AddIfValid(textSet, hintText);

        AddIfValid(imageSet, _fallbackPortraitImage);
        AddIfValid(imageSet, redPortraitImage);
        AddIfValid(imageSet, bluePortraitImage);
        AddIfValid(imageSet, portraitImage);

        foreach (var t in textSet)
            t.text = "";

        foreach (var img in imageSet)
            img.sprite = null;
    }

    /// <summary>
    /// 若你在同一個 prefab 裡同時放了紅框/藍框，就用這個切換顯示，
    /// 並且把 name/body/hint/portrait 引用切到對應那一套。
    /// </summary>
    public void ShowFrame(bool useBlue)
    {
        _usingBlue = useBlue;

        if (redBox != null) redBox.SetActive(!useBlue);
        if (blueBox != null) blueBox.SetActive(useBlue);

        ResolveActiveRefs(useBlue);
        ApplyPortraitSettings();
    }

    private static void AddIfValid<T>(HashSet<T> set, T obj) where T : Object
    {
        if (obj != null) set.Add(obj);
    }
}