using UnityEngine;
using UnityEngine.UI;

public class InventoryUI : MonoBehaviour
{
    public static InventoryUI Instance { get; private set; }

    [Header("UI 元件")]
    public GameObject inventoryPanel;
    public Transform slotsParent;
    public GameObject slotPrefab;

    [Header("按鈕")]
    public Button bagButton;
    public Button closeButton;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[InventoryUI] 場景中有重複的 InventoryUI，刪除後建立的這個");
            Destroy(gameObject);
            return;
        }

        Instance = this;

        AutoFindReferences();

        if (inventoryPanel != null)
            inventoryPanel.SetActive(false);
        else
            Debug.LogWarning("[InventoryUI] inventoryPanel 沒指定");

        BindButtons();
    }

    private void OnEnable()
    {
        if (InventoryManager.Instance != null)
            InventoryManager.Instance.onInventoryChanged.AddListener(OnInventoryChanged);
    }

    private void OnDisable()
    {
        if (InventoryManager.Instance != null)
            InventoryManager.Instance.onInventoryChanged.RemoveListener(OnInventoryChanged);
    }

    private void AutoFindReferences()
    {
        if (inventoryPanel == null)
        {
            Transform t = transform.Find("InventoryPanel");
            if (t != null) inventoryPanel = t.gameObject;
        }

        if (slotsParent == null && inventoryPanel != null)
        {
            Transform t = inventoryPanel.transform.Find("SlotsGrid");
            if (t != null) slotsParent = t;
        }

        if (bagButton == null)
        {
            Button[] buttons = GetComponentsInChildren<Button>(true);
            foreach (Button btn in buttons)
            {
                if (btn.name.Contains("背包按鈕") || btn.name.ToLower().Contains("bag"))
                {
                    bagButton = btn;
                    break;
                }
            }
        }

        if (closeButton == null && inventoryPanel != null)
        {
            Button[] buttons = inventoryPanel.GetComponentsInChildren<Button>(true);
            foreach (Button btn in buttons)
            {
                if (btn.name.Contains("Close") || btn.name.Contains("關"))
                {
                    closeButton = btn;
                    break;
                }
            }
        }
    }

    private void BindButtons()
    {
        if (bagButton != null)
        {
            bagButton.onClick.RemoveAllListeners();
            bagButton.onClick.AddListener(ToggleInventory);
            Debug.Log("[InventoryUI] bagButton 綁定完成");
        }
        else
        {
            Debug.LogWarning("[InventoryUI] 找不到 bagButton");
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(CloseInventory);
            Debug.Log("[InventoryUI] closeButton 綁定完成");
        }
        else
        {
            Debug.LogWarning("[InventoryUI] 找不到 closeButton");
        }
    }

    public void ToggleInventory()
    {
        Debug.Log("[InventoryUI] ToggleInventory 被呼叫");

        if (inventoryPanel == null)
        {
            Debug.LogWarning("[InventoryUI] inventoryPanel 是空的");
            return;
        }

        bool willOpen = !inventoryPanel.activeSelf;
        inventoryPanel.SetActive(willOpen);

        Debug.Log("[InventoryUI] 背包狀態 => " + (willOpen ? "開啟" : "關閉"));

        if (willOpen)
            RefreshUI();
    }

    public void CloseInventory()
    {
        Debug.Log("[InventoryUI] CloseInventory 被呼叫");

        if (inventoryPanel != null)
            inventoryPanel.SetActive(false);
    }

    private void OnInventoryChanged()
    {
        if (inventoryPanel != null && inventoryPanel.activeSelf)
            RefreshUI();
    }

    public void RefreshUI()
    {
        Debug.Log("[InventoryUI] RefreshUI 被呼叫");

        if (slotsParent == null)
        {
            Debug.LogWarning("[InventoryUI] slotsParent 沒指定");
            return;
        }

        if (slotPrefab == null)
        {
            Debug.LogWarning("[InventoryUI] slotPrefab 沒指定");
            return;
        }

        foreach (Transform child in slotsParent)
            Destroy(child.gameObject);

        if (InventoryManager.Instance == null)
        {
            Debug.LogWarning("[InventoryUI] InventoryManager.Instance 為 null");
            return;
        }

        var items = InventoryManager.Instance.GetItems();
        Debug.Log("[InventoryUI] 物品數量 = " + items.Count);

        foreach (var stack in items)
        {
            GameObject slotGO = Instantiate(slotPrefab, slotsParent);
            InventorySlotUI slot = slotGO.GetComponent<InventorySlotUI>();

            if (slot != null)
                slot.SetSlot(stack.data, stack.count);
            else
                Debug.LogWarning("[InventoryUI] slotPrefab 沒有 InventorySlotUI");
        }
    }

    public void OpenInventory()
    {
        if (inventoryPanel == null) return;

        inventoryPanel.SetActive(true);
        RefreshUI();
    }
}