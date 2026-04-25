using UnityEngine;

public class PickupItem : MonoBehaviour
{
    [Header("物品資料")]
    public ItemData itemData;

    [Header("提示設定")]
    public GameObject promptPrefab;
    public float promptOffsetY = 1.8f;

    [Header("偵測")]
    public string playerTag = "Player";

    private GameObject promptInstance;
    private bool playerNearby = false;
    private bool pickedUp = false;

    void Start()
    {
        if (promptPrefab != null)
        {
            promptInstance = Instantiate(
                promptPrefab,
                transform.position + Vector3.up * promptOffsetY,
                Quaternion.identity
            );

            promptInstance.SetActive(false);
        }
    }

    void Update()
    {
        if (playerNearby && !pickedUp)
        {
            if (Input.GetKeyDown(KeyCode.E))
            {
                Pickup();
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;

        Debug.Log("玩家進入範圍");

        playerNearby = true;

        if (promptInstance != null)
            promptInstance.SetActive(true);
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;

        Debug.Log("玩家離開範圍");

        playerNearby = false;

        if (promptInstance != null)
            promptInstance.SetActive(false);
    }

    void Pickup()
    {
        pickedUp = true;

        Debug.Log("撿起物品：" + gameObject.name);

        if (promptInstance != null)
            Destroy(promptInstance);

        // 👉 之後你可以接 Inventory
        // InventoryManager.Instance.Add(itemData);

        Destroy(gameObject);
    }
}