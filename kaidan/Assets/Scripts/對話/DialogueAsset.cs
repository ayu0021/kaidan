using System;
using System.Collections.Generic;
using UnityEngine;

public enum PortraitVariant { Normal, Alt }

public enum HintMode { None, Default, Custom }

[Serializable]
public class DialogueLineData
{
    [Header("UI Skin（這句要用哪個對話筐）")]
    public DialogueUISkin uiSkin;

    [Header("Speaker / Portrait")]
    public CharacterPortraitSet character;
    public PortraitVariant variant = PortraitVariant.Normal;

    [Tooltip("勾選：這句不換立繪（沿用上一句）。")]
    public bool keepPreviousPortrait = false;

    [Header("Text Mapping (Only what you write will show)")]
    [Tooltip("留空＝不顯示名字（會清空/隱藏 NameText）。")]
    public string nameOverride;

    [TextArea(2, 6)]
    [Tooltip("對話內容（會顯示在 BodyText）。")]
    public string body;

    [Header("Hint")]
    public HintMode hintMode = HintMode.None;

    [Tooltip("HintMode=Custom 時才會顯示這段。")]
    public string hintCustom;
}

[CreateAssetMenu(menuName = "Dialogue/Dialogue Asset")]
public class DialogueAsset : ScriptableObject
{
    public string assetTitle = "New Dialogue";

    [Tooltip("這段對話結束後要不要自動換場（留空=不換場）")]
    public string loadSceneOnEnd = "";

    public List<DialogueLineData> lines = new List<DialogueLineData>();
}
