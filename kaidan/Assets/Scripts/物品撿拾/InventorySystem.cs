using System;
using System.Collections.Generic;
using UnityEngine;

public class InventorySystem : MonoBehaviour
{
    [Serializable]
    public class InventorySlot
    {
        public ItemData itemData;

        public bool IsEmpty => itemData == null;

        public void Clear()
        {
            itemData = null;
        }

        public void SetItem(ItemData newItem)
        {
            itemData = newItem;
        }
    }

    [Header("Inventory Settings")]
    [SerializeField] private int maxSlots = 3;

    [Header("Drop Settings")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private float dropForwardDistance = 1.2f;
    [SerializeField] private float dropHeightOffset = 0.2f;

    private List<InventorySlot> slots = new List<InventorySlot>();

    public Action OnInventoryChanged;

    public int MaxSlots => maxSlots;
    public IReadOnlyList<InventorySlot> Slots => slots;

    private void Awake()
    {
        InitializeSlots();
    }

    private void InitializeSlots()
    {
        slots.Clear();

        for (int i = 0; i < maxSlots; i++)
        {
            slots.Add(new InventorySlot());
        }
    }

    public bool HasEmptySlot()
    {
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i].IsEmpty)
                return true;
        }
        return false;
    }

    public bool TryAddItem(ItemData itemData)
    {
        if (itemData == null) return false;

        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i].IsEmpty)
            {
                slots[i].SetItem(itemData);
                OnInventoryChanged?.Invoke();
                return true;
            }
        }

        return false;
    }

    public bool RemoveItemAt(int index)
    {
        if (!IsValidIndex(index)) return false;
        if (slots[index].IsEmpty) return false;

        slots[index].Clear();
        OnInventoryChanged?.Invoke();
        return true;
    }

    public bool DropItemAt(int index)
    {
        if (!IsValidIndex(index)) return false;
        if (slots[index].IsEmpty) return false;

        ItemData itemToDrop = slots[index].itemData;

        if (itemToDrop == null || itemToDrop.worldPickupPrefab == null)
        {
            Debug.LogWarning($"[InventorySystem] 無法丟棄：Slot {index} 沒有 worldPickupPrefab。");
            return false;
        }

        Vector3 spawnPosition = GetDropSpawnPosition();

        Instantiate(itemToDrop.worldPickupPrefab, spawnPosition, Quaternion.identity);

        slots[index].Clear();
        OnInventoryChanged?.Invoke();
        return true;
    }

    private Vector3 GetDropSpawnPosition()
    {
        if (playerTransform == null)
        {
            Debug.LogWarning("[InventorySystem] playerTransform 未指定，將使用世界原點。");
            return Vector3.zero;
        }

        Vector3 forward = playerTransform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.001f)
            forward = Vector3.forward;

        forward.Normalize();

        Vector3 spawnPos = playerTransform.position + forward * dropForwardDistance;
        spawnPos.y += dropHeightOffset;

        return spawnPos;
    }

    private bool IsValidIndex(int index)
    {
        return index >= 0 && index < slots.Count;
    }
}