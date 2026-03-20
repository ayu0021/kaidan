using UnityEngine;

public class InventoryUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private InventorySystem inventorySystem;
    [SerializeField] private InventorySlotUI[] slotUIs;

    private void Start()
    {
        if (inventorySystem == null)
        {
            Debug.LogError("[InventoryUI] inventorySystem 未指定。");
            return;
        }

        for (int i = 0; i < slotUIs.Length; i++)
        {
            if (slotUIs[i] != null)
            {
                slotUIs[i].Initialize(this, i);
            }
        }

        inventorySystem.OnInventoryChanged += RefreshUI;
        RefreshUI();
    }

    private void OnDestroy()
    {
        if (inventorySystem != null)
        {
            inventorySystem.OnInventoryChanged -= RefreshUI;
        }
    }

    public void RefreshUI()
    {
        if (inventorySystem == null) return;

        for (int i = 0; i < slotUIs.Length; i++)
        {
            if (slotUIs[i] == null) continue;

            if (i < inventorySystem.Slots.Count)
            {
                slotUIs[i].Refresh(inventorySystem.Slots[i].itemData);
            }
            else
            {
                slotUIs[i].Refresh(null);
            }
        }
    }

    public void RequestDrop(int slotIndex)
    {
        if (inventorySystem == null) return;

        inventorySystem.DropItemAt(slotIndex);
    }
}