using System.Collections.Generic;
using UnityEngine;

public class PlayerPickupController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private InventorySystem inventorySystem;

    [Header("Pickup Settings")]
    [SerializeField] private KeyCode pickupKey = KeyCode.E;
    [SerializeField] private float pickupRange = 2f;
    [SerializeField] private LayerMask pickupLayerMask = ~0;

    [Header("Optional Prompt")]
    [SerializeField] private GameObject pickupPromptObject;

    private readonly List<WorldPickup> pickupsInRange = new List<WorldPickup>();

    private void Update()
    {
        CleanupNullPickups();

        WorldPickup nearestPickup = GetNearestPickup();

        if (pickupPromptObject != null)
        {
            pickupPromptObject.SetActive(nearestPickup != null && inventorySystem != null && inventorySystem.HasEmptySlot());
        }

        if (nearestPickup == null) return;
        if (inventorySystem == null) return;

        if (Input.GetKeyDown(pickupKey))
        {
            TryPickup(nearestPickup);
        }
    }

    private void TryPickup(WorldPickup pickup)
    {
        if (pickup == null) return;
        if (!inventorySystem.HasEmptySlot()) return;

        ItemData itemData = pickup.GetItemData();
        if (itemData == null)
        {
            Debug.LogWarning("[PlayerPickupController] 撿取失敗：WorldPickup 沒有 itemData");
            return;
        }

        bool added = inventorySystem.TryAddItem(itemData);
        if (added)
        {
            pickup.OnPickedUp();
        }
    }

    private WorldPickup GetNearestPickup()
    {
        WorldPickup nearest = null;
        float nearestDistance = float.MaxValue;

        for (int i = 0; i < pickupsInRange.Count; i++)
        {
            if (pickupsInRange[i] == null) continue;

            float dist = Vector3.Distance(transform.position, pickupsInRange[i].transform.position);
            if (dist <= pickupRange && dist < nearestDistance)
            {
                nearestDistance = dist;
                nearest = pickupsInRange[i];
            }
        }

        return nearest;
    }

    private void CleanupNullPickups()
    {
        for (int i = pickupsInRange.Count - 1; i >= 0; i--)
        {
            if (pickupsInRange[i] == null)
                pickupsInRange.RemoveAt(i);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsInLayerMask(other.gameObject.layer, pickupLayerMask)) return;

        WorldPickup pickup = other.GetComponent<WorldPickup>();
        if (pickup != null && !pickupsInRange.Contains(pickup))
        {
            pickupsInRange.Add(pickup);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        WorldPickup pickup = other.GetComponent<WorldPickup>();
        if (pickup != null)
        {
            pickupsInRange.Remove(pickup);
        }
    }

    private bool IsInLayerMask(int layer, LayerMask mask)
    {
        return (mask.value & (1 << layer)) != 0;
    }
}