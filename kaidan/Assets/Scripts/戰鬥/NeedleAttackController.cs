using System.Collections;
using UnityEngine;

public class NeedleAttackController : MonoBehaviour
{
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

    [Header("一般模式")]
    public NeedleSettings normalSettings = new NeedleSettings();

    [Header("第二階段 / 狂暴模式")]
    public NeedleSettings aggressiveSettings = new NeedleSettings();

    private bool aggressiveMode;
    private Coroutine attackCoroutine;

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
            FireOnce();
            yield return new WaitForSeconds(Current.fireInterval);
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
        if (firePoints != null && firePoints.Length > 0)
        {
            foreach (var fp in firePoints)
            {
                if (fp == null) continue;
                Vector3 dir = FlatDir(fp.forward);
                if (dir.sqrMagnitude < 0.0001f)
                    dir = Vector3.forward;

                SpawnBullet(fp.position, dir);
            }
        }
        else
        {
            Vector3 dir = FlatDir(transform.forward);
            if (dir.sqrMagnitude < 0.0001f)
                dir = Vector3.forward;

            SpawnBullet(transform.position, dir);
        }
    }

    private void FireSpreadShot()
    {
        Transform fp = GetMainFirePoint();
        Vector3 baseDir = FlatDir(fp.forward);
        if (baseDir.sqrMagnitude < 0.0001f)
            baseDir = Vector3.forward;

        int count = Mathf.Max(1, Current.bulletsPerShot);
        float totalAngle = Current.spreadAngle;

        if (count == 1)
        {
            SpawnBullet(fp.position, baseDir);
            return;
        }

        float startAngle = -totalAngle * 0.5f;
        float step = totalAngle / (count - 1);

        for (int i = 0; i < count; i++)
        {
            float angle = startAngle + step * i;
            Vector3 dir = RotateY(baseDir, angle);
            SpawnBullet(fp.position, dir);
        }
    }

    private void FireCircleBurst()
    {
        Transform fp = GetMainFirePoint();
        int count = Mathf.Max(4, Current.circleCount);

        for (int i = 0; i < count; i++)
        {
            float angle = (360f / count) * i;
            Vector3 dir = RotateY(Vector3.forward, angle);
            SpawnBullet(fp.position, dir);
        }
    }

    private void FireAimBurst()
    {
        Transform fp = GetMainFirePoint();
        Vector3 baseDir = GetAimDirection(fp);

        int count = Mathf.Max(1, Current.bulletsPerShot);
        float totalAngle = Current.spreadAngle;

        if (count == 1)
        {
            SpawnBullet(fp.position, baseDir);
            return;
        }

        float startAngle = -totalAngle * 0.5f;
        float step = totalAngle / (count - 1);

        for (int i = 0; i < count; i++)
        {
            float angle = startAngle + step * i;
            Vector3 dir = RotateY(baseDir, angle);
            SpawnBullet(fp.position, dir);
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
            Current.bulletSpeed,
            Current.bulletLifetime,
            Current.damage
        );
    }

    private Transform GetMainFirePoint()
    {
        if (firePoints != null && firePoints.Length > 0 && firePoints[0] != null)
            return firePoints[0];

        return transform;
    }

    private Vector3 GetAimDirection(Transform from)
    {
        if (player != null)
        {
            Vector3 dir = player.transform.position - from.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.0001f)
                return dir.normalized;
        }

        Vector3 fallback = FlatDir(from.forward);
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
}