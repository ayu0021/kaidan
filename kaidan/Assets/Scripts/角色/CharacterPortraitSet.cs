using UnityEngine;

[CreateAssetMenu(menuName = "Dialogue/Character Portrait Set")]
public class CharacterPortraitSet : ScriptableObject
{
    public string displayName;

    [Header("Portraits")]
    public Sprite normal;
    public Sprite alt;

    [Header("Per Character UI Override")]
    [Tooltip("相對於 Dialogue prefab 裡 Portrait 基準大小的倍率。1,1,1 代表不變。")]
    public Vector3 uiScale = Vector3.one;

    [Tooltip("相對於 Dialogue prefab 裡 Portrait 基準位置的偏移。")]
    public Vector2 uiOffset = Vector2.zero;

    [Tooltip("只有需要左右翻轉的角色勾這個。")]
    public bool flipX = false;

    public Sprite Get(PortraitVariant v)
        => v == PortraitVariant.Alt ? alt : normal;
}