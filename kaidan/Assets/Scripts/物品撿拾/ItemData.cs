using UnityEngine;

[CreateAssetMenu(fileName = "NewItemData", menuName = "Inventory/Item Data")]
public class ItemData : ScriptableObject
{
    [Header("Basic Info")]
    public string itemName;
    [TextArea] public string description;
    public Sprite icon;

    [Header("World Drop")]
    [Tooltip("丟棄時要生成的世界物件 Prefab")]
    public GameObject worldPickupPrefab;
}