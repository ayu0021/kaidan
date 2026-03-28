using UnityEngine;

/// <summary>
/// 可撿取物品元件
/// 掛在場景中的可互動物品 GameObject 上
/// 需要：一個設為 Is Trigger 的 Collider（建議 SphereCollider 偵測範圍）
/// </summary>
public class PickupItem : MonoBehaviour
{
    [Header("物品資料")]
    public ItemData itemData;

    [Header("提示設定")]
    [Tooltip("世界空間的提示 Prefab（含 Canvas World Space + 提示文字）")]
    public GameObject promptPrefab;
    public float promptOffsetY = 1.8f;

    [Header("偵測")]
    public string playerTag = "Player";

    // ── 私有狀態 ──────────────────────────────────────────────
    private GameObject _promptInstance;
    private bool _playerNearby = false;
    private bool _pickedUp = false;

    // ── 生命週期 ──────────────────────────────────────────────
    void Start()
    {
        if (promptPrefab != null)
        {
            _promptInstance = Instantiate(
                promptPrefab,
                transform.position + Vector3.up * promptOffsetY,
                Quaternion.identity
            );
            _promptInstance.SetActive(false);
        }
    }

    void Update()
    {
        if (_pickedUp || !_playerNearby) return;

        // 讓提示跟隨物品（物品若是靜態也沒關係）
        if (_promptInstance != null)
            _promptInstance.transform.position = transform.position + Vector3.up * promptOffsetY;

        if (Input.GetKeyDown(KeyCode.E))
            Pickup();
    }

    // ── 觸發器 ──────────────────────────────────────────────
    void OnTriggerEnter(Collider other)
    {
        if (_pickedUp) return;
        if (!other.CompareTag(playerTag)) return;

        _playerNearby = true;
        if (_promptInstance != null) _promptInstance.SetActive(true);
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;

        _playerNearby = false;
        if (_promptInstance != null) _promptInstance.SetActive(false);
    }

    // ── 撿取邏輯 ──────────────────────────────────────────────
    void Pickup()
    {
        if (itemData == null)
        {
            Debug.LogWarning($"[PickupItem] {name} 沒有設定 ItemData！");
            return;
        }

        if (InventoryManager.Instance == null)
        {
            Debug.LogError("[PickupItem] 找不到 InventoryManager！請確認場景中有此物件。");
            return;
        }

        _pickedUp = true;
        InventoryManager.Instance.AddItem(itemData);

        if (_promptInstance != null) Destroy(_promptInstance);
        Destroy(gameObject);
    }

    void OnDestroy()
    {
        if (_promptInstance != null) Destroy(_promptInstance);
    }
}
