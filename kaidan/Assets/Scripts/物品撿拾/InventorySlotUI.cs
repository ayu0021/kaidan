using UnityEngine;
using UnityEngine.UI;

public class InventorySlotUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image iconImage;
    [SerializeField] private GameObject emptyStateObject;
    [SerializeField] private GameObject filledStateObject;
    [SerializeField] private Button dropButton;

    private int slotIndex;
    private InventoryUI inventoryUI;

    public void Initialize(InventoryUI owner, int index)
    {
        inventoryUI = owner;
        slotIndex = index;

        if (dropButton != null)
        {
            dropButton.onClick.RemoveAllListeners();
            dropButton.onClick.AddListener(OnClickDrop);
        }
    }

    public void Refresh(ItemData itemData)
    {
        bool hasItem = itemData != null;

        if (emptyStateObject != null)
            emptyStateObject.SetActive(!hasItem);

        if (filledStateObject != null)
            filledStateObject.SetActive(hasItem);

        if (iconImage != null)
        {
            iconImage.enabled = hasItem;
            iconImage.sprite = hasItem ? itemData.icon : null;
        }

        if (dropButton != null)
            dropButton.interactable = hasItem;
    }

    private void OnClickDrop()
    {
        if (inventoryUI != null)
        {
            inventoryUI.RequestDrop(slotIndex);
        }
    }
}