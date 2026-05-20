using System.Collections;
using UnityEngine;

public class YarnAttackController : MonoBehaviour
{
    [System.Serializable]
    public class WaveSettings
    {
        public float spawnInterval = 1.2f;
        public int dropsPerWave = 2;

        [Tooltip("剪刀從光圈上方多高的位置開始落下。")]
        public float startHeight = 5f;

        public float warningTime = 0.8f;
        public float fallTime = 0.25f;
        public float activeDamageTime = 0.2f;
        public int damage = 1;
    }

    [Header("Refs")]
    public YarnDrop yarnDropPrefab;
    public GameObject dropVisualPrefab;
    public Transform dropParent;
    public Transform target;

    [Header("落點範圍")]
    [Tooltip("下落範圍中心點。X/Z 決定場地中心，Y 通常跟 groundY 一樣。")]
    public Vector3 arenaCenter = new Vector3(-6f, 7.1f, 4f);

    [Tooltip("下落範圍半徑。X 是左右寬度，Y 是前後深度。")]
    public Vector2 arenaXZ = new Vector2(5f, 5f);

    [Tooltip("光圈與傷害 Trigger 生成的地板高度。")]
    public float groundY = 0f;

    [Header("追擊落點")]
    public bool spawnNearTarget = true;
    public int targetDropsPerWave = 1;
    public float targetDropRadius = 1.6f;
    public bool showDebugLog = true;

    [Header("剪刀視覺調整")]
    public Vector3 dropVisualOffset = Vector3.zero;
    public Vector3 dropVisualRotationEuler = Vector3.zero;
    public Vector3 dropVisualScale = new Vector3(3f, 3f, 3f);
    public bool forceVisualRenderersOn = true;
    public bool useFallbackScissorWhenNoPrefab = true;
    public Vector3 fallbackScissorScale = new Vector3(1.8f, 1.8f, 1.8f);

    [Header("剪刀大小變化")]
    [Tooltip("開啟後每把剪刀會在小尺寸和大尺寸之間隨機。小尺寸就是上面 dropVisualScale 目前的大小。")]
    public bool randomizeDropSize = true;
    [Range(0f, 1f)]
    public float largeDropChance = 0.35f;
    public float smallDropScaleMultiplier = 1f;
    public float largeDropScaleMultiplier = 1.55f;

    [Header("一般模式")]
    public WaveSettings normalSettings = new WaveSettings();

    [Header("第二階段 / 狂暴模式")]
    public WaveSettings aggressiveSettings = new WaveSettings();

    private bool aggressiveMode;
    private Coroutine attackCoroutine;

    private WaveSettings Current => aggressiveMode ? aggressiveSettings : normalSettings;

    void Awake()
    {
        if (!target)
        {
            BattlePlayerController player = FindObjectOfType<BattlePlayerController>();
            if (player)
                target = player.transform;
        }
    }

    public void ApplyAggressiveMode(bool aggressive)
    {
        aggressiveMode = aggressive;
    }

    public void BeginAttack()
    {
        if (attackCoroutine != null) return;
        attackCoroutine = StartCoroutine(AttackLoop());
    }

    public void StopAttack()
    {
        if (attackCoroutine != null)
        {
            StopCoroutine(attackCoroutine);
            attackCoroutine = null;
        }
    }

    private IEnumerator AttackLoop()
    {
        while (true)
        {
            SpawnWave();
            yield return new WaitForSeconds(Current.spawnInterval);
        }
    }

    private void SpawnWave()
    {
        if (yarnDropPrefab == null)
        {
            Debug.LogWarning("[YarnAttackController] 沒有設定 yarnDropPrefab，無法生成下落攻擊。", this);
            return;
        }

        for (int i = 0; i < Current.dropsPerWave; i++)
        {
            Vector3 spawnPos = GetDropPosition(i);

            YarnDrop drop = Instantiate(
                yarnDropPrefab,
                spawnPos,
                Quaternion.identity,
                dropParent
            );

            if (dropVisualPrefab != null)
                drop.fallingVisualPrefab = dropVisualPrefab;

            drop.visualLocalOffset = dropVisualOffset;
            drop.visualRotationEuler = dropVisualRotationEuler;
            drop.visualScale = dropVisualScale;
            drop.attackScale = PickDropScaleMultiplier();
            drop.forceVisualRenderersOn = forceVisualRenderersOn;
            drop.addFallbackScissorVisual = useFallbackScissorWhenNoPrefab && drop.fallingVisualPrefab == null;
            drop.fallbackScissorScale = fallbackScissorScale;

            drop.Setup(
                Current.damage,
                Current.startHeight,
                Current.warningTime,
                Current.fallTime,
                Current.activeDamageTime
            );

            if (!drop.gameObject.activeSelf)
                drop.gameObject.SetActive(true);

            if (showDebugLog)
                Debug.Log($"[YarnAttackController] 生成下落攻擊：{drop.name} at {spawnPos}", drop);
        }
    }

    private Vector3 GetDropPosition(int dropIndex)
    {
        bool shouldTrackTarget =
            spawnNearTarget &&
            target != null &&
            dropIndex < Mathf.Min(targetDropsPerWave, Current.dropsPerWave);

        if (shouldTrackTarget)
        {
            Vector2 offset = Random.insideUnitCircle * targetDropRadius;
            return new Vector3(target.position.x + offset.x, groundY, target.position.z + offset.y);
        }

        float x = arenaCenter.x + Random.Range(-arenaXZ.x, arenaXZ.x);
        float z = arenaCenter.z + Random.Range(-arenaXZ.y, arenaXZ.y);
        return new Vector3(x, groundY, z);
    }

    private float PickDropScaleMultiplier()
    {
        if (!randomizeDropSize)
            return Mathf.Max(0.05f, smallDropScaleMultiplier);

        bool useLarge = Random.value < Mathf.Clamp01(largeDropChance);
        float scale = useLarge ? largeDropScaleMultiplier : smallDropScaleMultiplier;
        return Mathf.Max(0.05f, scale);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0f, 0f, 0.22f);
        Vector3 center = new Vector3(arenaCenter.x, groundY, arenaCenter.z);
        Vector3 size = new Vector3(arenaXZ.x * 2f, 0.05f, arenaXZ.y * 2f);
        Gizmos.DrawCube(center, size);

        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(center, size);
    }
}
