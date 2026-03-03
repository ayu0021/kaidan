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

    [Header("Frames (optional, choose one to show)")]
    [Tooltip("紅框容器（GameObject）。沒有就留空。")]
    public GameObject redBox;

    [Tooltip("藍框容器（GameObject）。沒有就留空。")]
    public GameObject blueBox;

    public GameObject RootGO => uiRoot != null ? uiRoot : gameObject;

    public void ClearAll()
    {
        if (nameText) nameText.text = "";
        if (bodyText) bodyText.text = "";
        if (hintText) hintText.text = "";
        if (portraitImage) portraitImage.sprite = null;
    }

    /// <summary>
    /// 若你在同一個 prefab 裡同時放了紅框/藍框，就用這個切換顯示。
    /// 沒有設任何框時會自動略過（不做事）。
    /// </summary>
    public void ShowFrame(bool useBlue)
    {
        if (redBox == null && blueBox == null) return;

        if (redBox) redBox.SetActive(!useBlue);
        if (blueBox) blueBox.SetActive(useBlue);
    }
}