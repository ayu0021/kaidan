using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 單格物品 UI
/// 掛在 Slot Prefab 的根物件上
/// </summary>
public class InventorySlotUI : MonoBehaviour
{
    [Header("UI 元件（在 Prefab 上設定）")]
    public Image iconImage;
    public TextMeshProUGUI itemNameText;
    public TextMeshProUGUI countText;

    // 可選：點擊顯示物品描述
    [Header("描述框（可選）")]
    public TextMeshProUGUI descriptionText;
    public GameObject descriptionPanel;

    private ItemData _data;

    public void SetSlot(ItemData data, int count)
    {
        _data = data;
        if (data == null) return;

        if (iconImage != null)
        {
            iconImage.sprite = data.icon;
            iconImage.enabled = data.icon != null;
        }

        if (itemNameText != null)
            itemNameText.text = data.itemName;

        if (countText != null)
            countText.text = count > 1 ? $"x{count}" : "";
    }

    // 點擊 Slot 顯示描述（可選功能）
    public void OnClick()
    {
        if (_data == null) return;
        if (descriptionPanel != null) descriptionPanel.SetActive(true);
        if (descriptionText != null) descriptionText.text = _data.description;
    }
}
