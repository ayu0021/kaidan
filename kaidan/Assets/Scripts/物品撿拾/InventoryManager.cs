using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 背包管理器（Singleton，跨場景存活）
/// 掛在一個空 GameObject 上，命名為 "InventoryManager"
/// </summary>
public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    // ── 資料結構 ──────────────────────────────────────────────
    [System.Serializable]
    public class ItemStack
    {
        public ItemData data;
        public int count;
    }

    // 目前背包內容（可在 Inspector 觀察）
    [SerializeField] private List<ItemStack> items = new List<ItemStack>();

    // 背包變動時廣播（InventoryUI 監聽此事件）
    public UnityEvent onInventoryChanged = new UnityEvent();

    // ── 生命週期 ──────────────────────────────────────────────
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ── 公開 API ──────────────────────────────────────────────

    /// <summary>加入物品，支援堆疊</summary>
    public void AddItem(ItemData item, int amount = 1)
    {
        if (item == null) return;

        if (item.maxStack > 1)
        {
            var existing = items.Find(s => s.data == item);
            if (existing != null)
            {
                existing.count = Mathf.Min(existing.count + amount, item.maxStack);
                onInventoryChanged?.Invoke();
                return;
            }
        }

        items.Add(new ItemStack { data = item, count = Mathf.Min(amount, item.maxStack) });
        onInventoryChanged?.Invoke();
    }

    /// <summary>移除物品</summary>
    public bool RemoveItem(ItemData item, int amount = 1)
    {
        var stack = items.Find(s => s.data == item);
        if (stack == null) return false;

        stack.count -= amount;
        if (stack.count <= 0) items.Remove(stack);

        onInventoryChanged?.Invoke();
        return true;
    }

    /// <summary>查詢是否持有</summary>
    public bool HasItem(ItemData item) => items.Exists(s => s.data == item);

    /// <summary>取得唯讀清單（供 UI 使用）</summary>
    public IReadOnlyList<ItemStack> GetItems() => items.AsReadOnly();
}
