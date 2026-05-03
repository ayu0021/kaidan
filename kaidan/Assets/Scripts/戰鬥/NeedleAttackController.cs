using System.Collections;
using UnityEngine;

public class NeedleAttackController : MonoBehaviour
{
    public enum FirePointMode
    {
        FirstOnly,
        RandomActive,
        CycleActive,
        AllActive
    }

    public enum NeedlePattern
    {
        StraightLine,
        SpreadShot,
        CircleBurst,
        AimBurst
    }

    [System.Serializable]
    public class NeedleSettings
    {
        public NeedlePattern pattern = NeedlePattern.SpreadShot;
        public float fireInterval = 0.6f;
        public float bulletSpeed = 9f;
        public float bulletLifetime = 5f;
        public int damage = 1;

        [Header("Spread / AimBurst")]
        public int bulletsPerShot = 5;
        public float spreadAngle = 45f;

        [Header("CircleBurst")]
        public int circleCount = 12;
    }

    [Header("Refs")]
    public BattlePlayerController player;
    public NeedleBullet bulletPrefab;
    public Transform[] firePoints;

    [Header("多生成點")]
    public FirePointMode firePointMode = FirePointMode.CycleActive;
    public Vector3[] extraFirePointOffsets =
    {
        new Vector3(-5f, 0f, 0f),
        new Vector3(5f, 0f, 0f),
        new Vector3(-8f, 0f, -4f),
        new Vector3(8f, 0f, -4f)
    };

    [Header("5 分鐘難度曲線")]
    public bool rampDifficultyOverTime = true;
    public float fullDifficultyTime = 300f;
    public float minFireIntervalMultiplier = 0.65f;
    public float bulletSpeedBonusAtMax = 1.6f;
    public int extraBulletsAtMax = 2;
    public int extraCircleBulletsAtMax = 8;

    [Header("一般模式")]
    public NeedleSettings normalSettings = new NeedleSettings();

    [Header("第二階段 / 狂暴模式")]
    public NeedleSettings aggressiveSettings = new NeedleSettings();

    private bool aggressiveMode;
    private Coroutine attackCoroutine;
    private float difficultyTimer;
    private int cycleIndex;

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

    void Update()
    {
        if (!rampDifficultyOverTime) return;
        if (player != null && player.IsDead) return;

        difficultyTimer += Time.deltaTime;
    }

    private IEnumerator AttackLoop()
    {
        while (true)
        {
            FireOnce();
            float interval = GetFireInterval();
            yield return new WaitForSeconds(interval);
        }
    }

    private NeedleSettings Current => aggressiveMode ? aggressiveSettings : normalSettings;

    private void FireOnce()
    {
        if (bulletPrefab == null) return;

        switch (Current.pattern)
        {
            case NeedlePattern.StraightLine:
                FireStraightLine();
                break;

            case NeedlePattern.SpreadShot:
                FireSpreadShot();
                break;

            case NeedlePattern.CircleBurst:
                FireCircleBurst();
                break;

            case NeedlePattern.AimBurst:
                FireAimBurst();
                break;
        }
    }

    private void FireStraightLine()
    {
        foreach (FireOrigin origin in GetSelectedFireOrigins())
        {
            Vector3 dir = FlatDir(origin.forward);
            if (dir.sqrMagnitude < 0.0001f)
                dir = Vector3.forward;

            SpawnBullet(origin.position, dir);
        }
    }

    private void FireSpreadShot()
    {
        foreach (FireOrigin origin in GetSelectedFireOrigins())
        {
            Vector3 baseDir = FlatDir(origin.forward);
            if (baseDir.sqrMagnitude < 0.0001f)
                baseDir = Vector3.forward;

            int count = Mathf.Max(1, GetBulletsPerShot());
            float totalAngle = Current.spreadAngle;

            if (count == 1)
            {
                SpawnBullet(origin.position, baseDir);
                continue;
            }

            float startAngle = -totalAngle * 0.5f;
            float step = totalAngle / (count - 1);

            for (int i = 0; i < count; i++)
            {
                float angle = startAngle + step * i;
                Vector3 dir = RotateY(baseDir, angle);
                SpawnBullet(origin.position, dir);
            }
        }
    }

    private void FireCircleBurst()
    {
        foreach (FireOrigin origin in GetSelectedFireOrigins())
        {
            int count = Mathf.Max(4, GetCircleCount());

            for (int i = 0; i < count; i++)
            {
                float angle = (360f / count) * i;
                Vector3 dir = RotateY(Vector3.forward, angle);
                SpawnBullet(origin.position, dir);
            }
        }
    }

    private void FireAimBurst()
    {
        foreach (FireOrigin origin in GetSelectedFireOrigins())
        {
            Vector3 baseDir = GetAimDirection(origin.position, origin.forward);

            int count = Mathf.Max(1, GetBulletsPerShot());
            float totalAngle = Current.spreadAngle;

            if (count == 1)
            {
                SpawnBullet(origin.position, baseDir);
                continue;
            }

            float startAngle = -totalAngle * 0.5f;
            float step = totalAngle / (count - 1);

            for (int i = 0; i < count; i++)
            {
                float angle = startAngle + step * i;
                Vector3 dir = RotateY(baseDir, angle);
                SpawnBullet(origin.position, dir);
            }
        }
    }

    private void SpawnBullet(Vector3 position, Vector3 direction)
    {
        direction = FlatDir(direction);
        if (direction.sqrMagnitude < 0.0001f)
            direction = Vector3.forward;

        NeedleBullet bullet = Instantiate(
            bulletPrefab,
            position,
            Quaternion.LookRotation(direction, Vector3.up)
        );

        bullet.Initialize(
            direction,
            GetBulletSpeed(),
            Current.bulletLifetime,
            Current.damage
        );
    }

    private FireOrigin GetMainFireOrigin()
    {
        if (firePoints != null && firePoints.Length > 0 && firePoints[0] != null)
            return new FireOrigin(firePoints[0].position, firePoints[0].forward);

        return new FireOrigin(transform.position, transform.forward);
    }

    private FireOrigin[] GetSelectedFireOrigins()
    {
        FireOrigin[] all = GetActiveFireOrigins();
        if (all.Length <= 1 || firePointMode == FirePointMode.AllActive)
            return all;

        int index = 0;

        if (firePointMode == FirePointMode.RandomActive)
            index = Random.Range(0, all.Length);
        else if (firePointMode == FirePointMode.CycleActive)
        {
            index = cycleIndex % all.Length;
            cycleIndex++;
        }

        return new[] { all[Mathf.Clamp(index, 0, all.Length - 1)] };
    }

    private FireOrigin[] GetActiveFireOrigins()
    {
        FireOrigin main = GetMainFireOrigin();
        int total = 1 + (extraFirePointOffsets == null ? 0 : extraFirePointOffsets.Length);
        int active = Mathf.Clamp(GetActiveFirePointCount(total), 1, total);

        FireOrigin[] origins = new FireOrigin[active];
        origins[0] = main;

        for (int i = 1; i < active; i++)
        {
            Vector3 offset = extraFirePointOffsets[i - 1];
            origins[i] = new FireOrigin(main.position + offset, main.forward);
        }

        return origins;
    }

    private int GetActiveFirePointCount(int total)
    {
        if (!rampDifficultyOverTime) return total;
        return 1 + Mathf.FloorToInt(Difficulty01 * (total - 1));
    }

    private float GetFireInterval()
    {
        float multiplier = rampDifficultyOverTime
            ? Mathf.Lerp(1f, Mathf.Clamp(minFireIntervalMultiplier, 0.1f, 1f), Difficulty01)
            : 1f;

        return Mathf.Max(0.08f, Current.fireInterval * multiplier);
    }

    private float GetBulletSpeed()
    {
        float bonus = rampDifficultyOverTime ? bulletSpeedBonusAtMax * Difficulty01 : 0f;
        return Current.bulletSpeed + bonus;
    }

    private int GetBulletsPerShot()
    {
        int bonus = rampDifficultyOverTime ? Mathf.RoundToInt(extraBulletsAtMax * Difficulty01) : 0;
        return Current.bulletsPerShot + bonus;
    }

    private int GetCircleCount()
    {
        int bonus = rampDifficultyOverTime ? Mathf.RoundToInt(extraCircleBulletsAtMax * Difficulty01) : 0;
        return Current.circleCount + bonus;
    }

    private float Difficulty01
    {
        get
        {
            if (!rampDifficultyOverTime || fullDifficultyTime <= 0f)
                return aggressiveMode ? 1f : 0f;

            float baseDifficulty = Mathf.Clamp01(difficultyTimer / fullDifficultyTime);
            return aggressiveMode ? Mathf.Max(baseDifficulty, 0.55f) : baseDifficulty;
        }
    }

    private Vector3 GetAimDirection(Vector3 fromPosition, Vector3 fallbackForward)
    {
        if (player != null)
        {
            Vector3 dir = player.transform.position - fromPosition;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.0001f)
                return dir.normalized;
        }

        Vector3 fallback = FlatDir(fallbackForward);
        return fallback.sqrMagnitude > 0.0001f ? fallback : Vector3.forward;
    }

    private Vector3 FlatDir(Vector3 v)
    {
        v.y = 0f;
        return v.normalized;
    }

    private Vector3 RotateY(Vector3 dir, float angleDeg)
    {
        return Quaternion.Euler(0f, angleDeg, 0f) * dir;
    }

    private readonly struct FireOrigin
    {
        public readonly Vector3 position;
        public readonly Vector3 forward;

        public FireOrigin(Vector3 position, Vector3 forward)
        {
            this.position = position;
            this.forward = forward;
        }
    }
}
