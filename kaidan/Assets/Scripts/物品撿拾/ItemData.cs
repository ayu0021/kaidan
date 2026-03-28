using UnityEngine;

/// <summary>
/// 物品資料定義（ScriptableObject）
/// 建立方式：Project 視窗右鍵 → Create → 談怪 → Item Data
/// </summary>
[CreateAssetMenu(fileName = "NewItem", menuName = "談怪/Item Data")]
public class ItemData : ScriptableObject
{
    [Header("基本資訊")]
    public string itemName = "未命名物品";

    [TextArea(2, 4)]
    public string description = "";

    public Sprite icon;

    [Header("堆疊")]
    [Min(1)]
    public int maxStack = 1;
}
