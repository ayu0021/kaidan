using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DialogueUIRefs : MonoBehaviour
{
    [Header("Root (optional)")]
    public GameObject uiRoot;

    [Header("Active / Fallback Refs")]
    public TMP_Text nameText;
    public TMP_Text bodyText;
    public TMP_Text hintText;
    public Image portraitImage;

    [Header("Frames (optional, choose one to show)")]
    public GameObject redBox;
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
    public bool forcePreserveAspect = true;

    [Header("CG (optional)")]
    public GameObject cgRoot;
    public Image cgImage;
    public CanvasGroup cgCanvasGroup;
    public bool cgPreserveAspect = true;

    public GameObject RootGO => uiRoot != null ? uiRoot : gameObject;

    private bool _usingBlue;

    private TMP_Text _fallbackNameText;
    private TMP_Text _fallbackBodyText;
    private TMP_Text _fallbackHintText;
    private Image _fallbackPortraitImage;

    private void Awake()
    {
        CacheFallbackRefs();
        ResolveActiveRefs(_usingBlue);
        ApplyPortraitSettings();
        ApplyCgSettings();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!Application.isPlaying)
        {
            CacheFallbackRefs(true);
            ResolveActiveRefs(_usingBlue);
            ApplyPortraitSettings();
            ApplyCgSettings();
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

    private void ApplyCgSettings()
    {
        if (cgImage != null)
            cgImage.preserveAspect = cgPreserveAspect;

        if (cgRoot == null && cgImage != null)
            cgRoot = cgImage.gameObject;

        EnsureCgBehindDialogue();
    }

    public void EnsureCgBehindDialogue()
    {
        if (cgRoot != null)
            cgRoot.transform.SetAsFirstSibling();
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
        {
            img.sprite = null;
            img.enabled = false;
        }
    }

    public void ShowFrame(bool useBlue)
    {
        _usingBlue = useBlue;

        if (redBox != null) redBox.SetActive(!useBlue);
        if (blueBox != null) blueBox.SetActive(useBlue);

        ResolveActiveRefs(useBlue);
        ApplyPortraitSettings();
        ApplyCgSettings();
    }

    private static void AddIfValid<T>(HashSet<T> set, T obj) where T : Object
    {
        if (obj != null) set.Add(obj);
    }
}