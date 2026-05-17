using UnityEngine;

public class ActivateAfterPickupCollected : MonoBehaviour
{
    [Header("條件")]
    [Tooltip("要先被撿起的物品 ID，例如：虎姑婆_臥室:老虎玩偶")]
    public string requiredPickupId;

    [Header("目標")]
    [Tooltip("條件達成後要打開的場景物件")]
    public GameObject targetObject;

    [Tooltip("條件尚未達成時，是否先把目標關閉")]
    public bool hideUntilCollected = true;

    void Start()
    {
        RefreshState();
    }

    [ContextMenu("立即更新狀態")]
    public void RefreshState()
    {
        if (!targetObject)
            return;

        bool collected = GameProgressState.GetOrCreateInstance().HasCollectedPickup(requiredPickupId);

        if (collected)
        {
            targetObject.SetActive(true);
        }
        else if (hideUntilCollected)
        {
            targetObject.SetActive(false);
        }
    }
}
