using UnityEngine;

public class WorldPickup : MonoBehaviour
{
    [Header("Item Data")]
    public ItemData itemData;

    [Header("Optional")]
    [Tooltip("若不指定，會用自己的 transform.position 當作掉落參考點")]
    public Transform dropReferencePoint;

    public ItemData GetItemData()
    {
        return itemData;
    }

    public Vector3 GetDropPosition()
    {
        if (dropReferencePoint != null)
            return dropReferencePoint.position;

        return transform.position;
    }

    public void OnPickedUp()
    {
        Destroy(gameObject);
    }
}