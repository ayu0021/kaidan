using System;
using System.Collections.Generic;
using UnityEngine;

public enum PortraitVariant { Normal, Alt }
public enum HintMode { None, Default, Custom }

public enum DialogueCGMode
{
    None,
    Show,
    KeepPrevious,
    Hide
}

public enum DialogueTextEffectType
{
    Color,
    Scale,
    Shake,
    Wave
}

[Serializable]
public class DialogueTextEffectData
{
    public bool enabled = true;

    [Tooltip("要套效果的字，例如：好臭！、爛掉了！")]
    public string targetText;

    [Tooltip("勾選後，所有相同文字都會套用；不勾則只套用 occurrenceIndex 指定的那一次。")]
    public bool applyToAllMatches = false;

    [Min(0)]
    [Tooltip("當 applyToAllMatches = false 時，第幾次出現（0 = 第一次）")]
    public int occurrenceIndex = 0;

    public DialogueTextEffectType effectType = DialogueTextEffectType.Color;

    [Header("Color")]
    public Color color = Color.red;

    [Header("Scale")]
    [Min(0.1f)]
    public float scaleMultiplier = 1.25f;

    [Header("Motion")]
    [Min(0f)]
    public float amplitude = 6f;

    [Min(0.1f)]
    public float frequency = 8f;

    [Min(0.1f)]
    public float speed = 8f;
}

[Serializable]
public class DialogueLineData
{
    [Header("UI Skin（這句要用哪個對話框）")]
    public DialogueUISkin uiSkin;

    [Header("Speaker / Portrait")]
    public CharacterPortraitSet character;
    public PortraitVariant variant = PortraitVariant.Normal;

    [Tooltip("勾選：這句不換立繪（沿用上一句）。")]
    public bool keepPreviousPortrait = false;

    [Header("Text Mapping")]
    [Tooltip("留空＝不顯示名字")]
    public string nameOverride;

    [TextArea(2, 6)]
    public string body;

    [Header("Hint")]
    public HintMode hintMode = HintMode.None;
    public string hintCustom;

    [Header("CG")]
    public DialogueCGMode cgMode = DialogueCGMode.None;
    public Sprite cgSprite;

    [Min(0f)]
    public float cgFadeDuration = 0.15f;

    [Tooltip("有 CG 時是否隱藏角色立繪")]
    public bool hidePortraitWhenCg = true;

    [Header("Typewriter Override")]
    public bool overrideCharsPerSecond = false;

    [Min(0.1f)]
    public float charsPerSecondOverride = 40f;

    [Header("Body Effects (optional)")]
    public List<DialogueTextEffectData> bodyEffects = new List<DialogueTextEffectData>();
}

[CreateAssetMenu(menuName = "Dialogue/Dialogue Asset")]
public class DialogueAsset : ScriptableObject
{
    public string assetTitle = "New Dialogue";

    [Tooltip("這段對話結束後要不要自動換場（留空=不換場）")]
    public string loadSceneOnEnd = "";

    public List<DialogueLineData> lines = new List<DialogueLineData>();
}