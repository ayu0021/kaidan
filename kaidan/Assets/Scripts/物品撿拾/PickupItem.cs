using UnityEngine;

public class PickupItem : MonoBehaviour
{
    [Header("物品資料")]
    public ItemData itemData;

    [Header("提示設定")]
    public GameObject promptPrefab;
    public float promptOffsetY = 1.8f;

    [Header("選取光效果")]
    public GameObject selectionGlow;
    public bool useTintIfNoGlowObject = true;
    [Range(0f, 1f)] public float tintStrength = 0.35f;
    public Color highlightColor = new Color(1f, 0.95f, 0.6f, 1f);

    [Header("偵測")]
    public string playerTag = "Player";

    private GameObject promptInstance;
    private bool playerNearby = false;
    private bool pickedUp = false;

    private bool isScreenSpaceUI = false;
    private bool isWorldPrompt = false;

    private SpriteRenderer[] spriteRenderers;
    private Color[] originalSpriteColors;

    private MeshRenderer[] meshRenderers;
    private Color[][] originalMeshColors;

    void Start()
    {
        SetupPrompt();
        SetupHighlight();
    }

    void SetupPrompt()
    {
        if (promptPrefab == null) return;

        Canvas canvas = promptPrefab.GetComponent<Canvas>();

        if (canvas != null && canvas.renderMode != RenderMode.WorldSpace)
        {
            isScreenSpaceUI = true;
            isWorldPrompt = false;

            promptInstance = Instantiate(promptPrefab);
            promptInstance.SetActive(false);
        }
        else
        {
            isScreenSpaceUI = false;
            isWorldPrompt = true;

            promptInstance = Instantiate(
                promptPrefab,
                transform.position + Vector3.up * promptOffsetY,
                Quaternion.identity
            );

            promptInstance.SetActive(false);
        }
    }

    void SetupHighlight()
    {
        if (selectionGlow != null)
            selectionGlow.SetActive(false);

        // SpriteRenderer 記錄原色
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        originalSpriteColors = new Color[spriteRenderers.Length];
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            originalSpriteColors[i] = spriteRenderers[i].color;
        }

        // MeshRenderer 記錄原色
        meshRenderers = GetComponentsInChildren<MeshRenderer>(true);
        originalMeshColors = new Color[meshRenderers.Length][];

        for (int i = 0; i < meshRenderers.Length; i++)
        {
            Material[] mats = meshRenderers[i].materials;
            originalMeshColors[i] = new Color[mats.Length];

            for (int j = 0; j < mats.Length; j++)
            {
                if (mats[j] != null && mats[j].HasProperty("_Color"))
                    originalMeshColors[i][j] = mats[j].color;
                else
                    originalMeshColors[i][j] = Color.white;
            }
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

    void LateUpdate()
    {
        if (promptInstance == null || pickedUp) return;

        if (isWorldPrompt)
        {
            promptInstance.transform.position = transform.position + Vector3.up * promptOffsetY;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;

        Debug.Log("玩家進入範圍");

        playerNearby = true;

        if (promptInstance != null)
            promptInstance.SetActive(true);

        SetHighlight(true);
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;

        Debug.Log("玩家離開範圍");

        playerNearby = false;

        if (promptInstance != null)
            promptInstance.SetActive(false);

        SetHighlight(false);
    }

    void SetHighlight(bool active)
    {
        if (selectionGlow != null)
        {
            selectionGlow.SetActive(active);
        }

        if (!useTintIfNoGlowObject)
            return;

        // SpriteRenderer 變亮
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] == null) continue;

            spriteRenderers[i].color = active
                ? Color.Lerp(originalSpriteColors[i], highlightColor, tintStrength)
                : originalSpriteColors[i];
        }

        // MeshRenderer 材質變亮
        for (int i = 0; i < meshRenderers.Length; i++)
        {
            if (meshRenderers[i] == null) continue;

            Material[] mats = meshRenderers[i].materials;
            for (int j = 0; j < mats.Length; j++)
            {
                if (mats[j] == null || !mats[j].HasProperty("_Color")) continue;

                mats[j].color = active
                    ? Color.Lerp(originalMeshColors[i][j], highlightColor, tintStrength)
                    : originalMeshColors[i][j];
            }
        }
    }

    void Pickup()
    {
        pickedUp = true;

        Debug.Log("撿起物品：" + gameObject.name);

        if (itemData != null && InventoryManager.Instance != null)
        {
            InventoryManager.Instance.AddItem(itemData);
        }

        if (promptInstance != null)
            Destroy(promptInstance);

        if (selectionGlow != null)
            selectionGlow.SetActive(false);

        Destroy(gameObject);
    }
}