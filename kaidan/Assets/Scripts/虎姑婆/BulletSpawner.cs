using UnityEngine;

public class BulletSpawner : MonoBehaviour
{
    // ===== 可用的 Pattern 型別 =====
    public enum BulletPattern
    {
        RandomBounce,   // 隨機生成＋亂彈
        CircleShot,     // 從中心射出一圈
        Rain            // 從上方落下
    }

    // ===== 一個「Pattern 段落」的設定 =====
    [System.Serializable]
    public class PatternStep
    {
        public BulletPattern pattern = BulletPattern.RandomBounce;

        [Header("前置暫停(秒)")]
        public float preDelay = 0f;        // 進入此模式前，先停幾秒（0 代表不暫停）

        [Header("持續時間(秒)")]
        public float duration = 5f;        // 真正開始發射後，持續多久

        [Header("發射間隔(秒)")]
        public float spawnInterval = 0.3f; // 此模式下，多久發射一次彈

        [Header("此模式專用子彈(可空白)")]
        public Bullet bulletPrefabOverride; // 若留空，則用全域 bulletPrefab
    }

    [Header("預設子彈 Prefab")]
    public Bullet bulletPrefab;

    [Header("場上最多子彈數")]
    public int maxBullets = 60;

    [Header("活動範圍 (X / Z)")]
    public Vector2 moveLimits = new Vector2(4f, 4f);

    [Header("平面高度 (Y)")]
    public float planeY = 0f;

    [Header("Pattern 流程設定 (依順序播放並循環)")]
    public PatternStep[] patternSequence;

    [Header("圓形射擊設定")]
    public int circleBulletCount = 16;

    [Header("雨滴模式設定")]
    public int rainCountPerWave = 8;
    public float rainTopZOffset = 4f;

    private int currentStepIndex = 0;
    private float patternTimer = 0f;  // 累計目前這段 pattern 已經過了多久（含 preDelay）
    private float spawnTimer = 0f;    // 發射計時器

    void Update()
    {
        if (bulletPrefab == null) return;
        if (patternSequence == null || patternSequence.Length == 0) return;

        PatternStep step = patternSequence[currentStepIndex];

        // 目前這段 pattern 經過的總時間（含 preDelay）
        patternTimer += Time.deltaTime;

        // 還在 preDelay 期間 → 只計時，不發射
        if (patternTimer < step.preDelay)
        {
            return;
        }

        // 已經過了 preDelay，開始計算發射間隔
        spawnTimer += Time.deltaTime;

        if (spawnTimer >= step.spawnInterval)
        {
            spawnTimer = 0f;
            SpawnByPattern(step);
        }

        // 若「經過的總時間」 >= preDelay + duration → 切到下一段 pattern
        if (patternTimer >= step.preDelay + step.duration)
        {
            patternTimer = 0f;
            spawnTimer = 0f;
            currentStepIndex++;

            if (currentStepIndex >= patternSequence.Length)
                currentStepIndex = 0; // 循環播放

            Debug.Log("Switch Pattern to: " + patternSequence[currentStepIndex].pattern);
        }
    }

    // 根據 PatternStep 來發射子彈
    void SpawnByPattern(PatternStep step)
    {
        int bulletCount = GameObject.FindGameObjectsWithTag("Bullet").Length;
        if (bulletCount >= maxBullets) return;

        // 此段若有指定自己的 Prefab，就用它；否則用預設的
        Bullet prefabToUse = step.bulletPrefabOverride != null ? step.bulletPrefabOverride : bulletPrefab;

        switch (step.pattern)
        {
            case BulletPattern.RandomBounce:
                SpawnRandomBounce(prefabToUse);
                break;

            case BulletPattern.CircleShot:
                SpawnCircleShot(prefabToUse);
                break;

            case BulletPattern.Rain:
                SpawnRain(prefabToUse);
                break;
        }
    }

    // ========== 各種 Pattern 實作 ==========

    // 1. 隨機位置＋隨機方向，丟進場內亂彈
    void SpawnRandomBounce(Bullet prefab)
    {
        float x = Random.Range(-moveLimits.x, moveLimits.x);
        float z = Random.Range(-moveLimits.y, moveLimits.y);
        Vector3 spawnPos = new Vector3(x, planeY, z);

        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        Vector3 dir = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));

        Bullet b = Instantiate(prefab, spawnPos, Quaternion.identity);
        b.Init(dir, moveLimits, planeY);
    }

    // 2. 從中心一次打出一圈
    void SpawnCircleShot(Bullet prefab)
    {
        Vector3 center = new Vector3(0f, planeY, 0f); // 或改成基地核心的位置

        for (int i = 0; i < circleBulletCount; i++)
        {
            float angle = (360f / circleBulletCount) * i * Mathf.Deg2Rad;
            Vector3 dir = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));

            Bullet b = Instantiate(prefab, center, Quaternion.identity);
            b.Init(dir, moveLimits, planeY);
        }
    }

    // 3. 從上方隨機位置往下掉
    void SpawnRain(Bullet prefab)
    {
        for (int i = 0; i < rainCountPerWave; i++)
        {
            float x = Random.Range(-moveLimits.x, moveLimits.x);
            float z = moveLimits.y + rainTopZOffset;   // 螢幕上方一點
            Vector3 spawnPos = new Vector3(x, planeY, z);

            Vector3 dir = Vector3.back; // 往 -Z 方向掉

            Bullet b = Instantiate(prefab, spawnPos, Quaternion.identity);
            b.Init(dir, moveLimits, planeY);
        }
    }
}

