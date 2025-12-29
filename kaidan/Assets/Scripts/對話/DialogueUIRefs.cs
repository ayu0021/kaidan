using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DialogueUIRefs : MonoBehaviour
{
    [Header("Root (optional)")]
    public GameObject uiRoot; // 可留空；留空就用自己 gameObject

    [Header("Text")]
    public TMP_Text nameText;
    public TMP_Text bodyText;
    public TMP_Text hintText;

    [Header("Portrait")]
    public Image portraitImage;

    public GameObject RootGO => uiRoot != null ? uiRoot : gameObject;

    public void ClearAll()
    {
        if (nameText) nameText.text = "";
        if (bodyText) bodyText.text = "";
        if (hintText) hintText.text = "";
        if (portraitImage) portraitImage.sprite = null;
    }
}
