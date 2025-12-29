using UnityEngine;



[CreateAssetMenu(menuName = "Dialogue/Character Portrait Set")]
public class CharacterPortraitSet : ScriptableObject
{
    public string displayName;

    [Header("Portraits")]
    public Sprite normal;
    public Sprite alt;

    [Header("UI")]
    [Tooltip("只會影響對話框內的肖像 Image (RectTransform.localScale)。")]
    public Vector3 uiScale = Vector3.one;

    [Tooltip("只會影響對話框內的肖像 Image (RectTransform.anchoredPosition) 偏移。")]
    public Vector2 uiOffset = Vector2.zero;

    [Tooltip("只有需要翻轉的角色勾這個。")]
    public bool flipX = false;

    public Sprite Get(PortraitVariant v)
        => v == PortraitVariant.Alt ? alt : normal;
}
