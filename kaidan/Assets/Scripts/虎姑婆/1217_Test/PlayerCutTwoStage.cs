using UnityEngine;

public class PlayerCutTwoStage : MonoBehaviour
{
    [Header("Refs")]
    public Transform attackPoint;
    public Transform aimSource;
    public BossShellController boss;

    [Header("Shell Detect")]
    public LayerMask shellMask;
    public float shellRange = 2.5f;
    public float shellCastRadius = 0.35f;
    [Range(0f, 180f)] public float shellHalfAngle = 120f;

    [Header("WeakPoint Attack")]
    public float weakPointRange = 999f;
    public float weakPointDamage = 1f;
    public bool ignoreWeakPointDistance = true;

    [Header("Input")]
    public KeyCode cutKey = KeyCode.Space;

    [Header("Hover")]
    public float hoverRefreshInterval = 0.05f;

    [Header("Debug")]
    public bool showDebugLog = true;
    public bool drawDebug = true;

    ShellShard _hover;
    float _hoverTimer;

    void Start()
    {
        if (!attackPoint)
            attackPoint = transform;

        if (!boss)
            boss = FindObjectOfType<BossShellController>();

        if (!aimSource && Camera.main)
            aimSource = Camera.main.transform;

        if (showDebugLog)
        {
            Debug.Log("[PlayerCutTwoStage] 初始化完成", this);

            if (!boss)
                Debug.LogWarning("[PlayerCutTwoStage] 找不到 BossShellController，請把 BossRoot 拖到 boss 欄位。", this);
        }
    }

    void Update()
    {
        if (!attackPoint) return;

        _hoverTimer -= Time.deltaTime;

        if (_hoverTimer <= 0f)
        {
            _hoverTimer = hoverRefreshInterval;
            UpdateHover();
        }

        if (Input.GetKeyDown(cutKey))
            DoCutAction();
    }

    void DoCutAction()
    {
        if (!boss)
        {
            boss = FindObjectOfType<BossShellController>();

            if (!boss)
            {
                Debug.LogWarning("[Cut] 沒有 BossShellController。", this);
                return;
            }
        }

        // 第一階段：殼還沒清完，優先剪殼
        if (!boss.ShellCleared)
        {
            if (_hover)
            {
                if (showDebugLog)
                    Debug.Log($"[Cut] 剪掉外殼：{_hover.name}", _hover);

                _hover.Detach(attackPoint.position);
                _hover = null;
                UpdateHover();
                return;
            }

            if (showDebugLog)
                Debug.Log("[Cut] 殼階段：沒有偵測到可剪的 ShellShard。", this);

            return;
        }

        // 第二階段：殼清完，直接攻擊 WeakPoint
        TryDamageWeakPointDirect();
    }

    void TryDamageWeakPointDirect()
    {
        if (!boss || !boss.weakPoint)
        {
            Debug.LogWarning("[Cut] Boss 沒有指定 WeakPoint。", this);
            return;
        }

        WeakPoint wp = boss.weakPoint;

        Vector3 origin = attackPoint.position;
        Vector3 target = wp.transform.position;

        Collider wpCol = wp.GetComponentInChildren<Collider>();
        if (wpCol)
            target = wpCol.bounds.center;

        float dist = Vector3.Distance(origin, target);

        if (!ignoreWeakPointDistance && dist > weakPointRange)
        {
            if (showDebugLog)
                Debug.LogWarning($"[Cut] WeakPoint 太遠，距離 {dist:0.00}，限制 {weakPointRange:0.00}", this);

            return;
        }

        Vector3 hitNormal = (origin - target).sqrMagnitude > 0.001f
            ? (origin - target).normalized
            : Vector3.up;

        if (drawDebug)
            Debug.DrawLine(origin, target, Color.red, 0.4f);

        if (showDebugLog)
            Debug.Log($"[Cut] 直接攻擊 WeakPoint：{wp.name}，距離 {dist:0.00}", wp);

        wp.TakeDamage(weakPointDamage, target, hitNormal);
    }

    void UpdateHover()
    {
        if (!boss) return;

        // 殼清完後不再找 Shell
        if (boss.ShellCleared)
        {
            if (_hover)
            {
                _hover.SetHover(false);
                _hover = null;
            }

            return;
        }

        ShellShard next = FindBestShard();

        if (next == _hover) return;

        if (_hover)
            _hover.SetHover(false);

        _hover = next;

        if (_hover)
            _hover.SetHover(true);
    }

    ShellShard FindBestShard()
    {
        if (!attackPoint) return null;

        Vector3 origin = attackPoint.position;
        Vector3 dir = GetAimDirection();

        LayerMask mask = EffectiveMask(shellMask, "shellMask");

        if (drawDebug)
            Debug.DrawRay(origin, dir * shellRange, Color.yellow, 0.1f);

        // 1. 前方 SphereCast
        if (Physics.SphereCast(
            origin,
            shellCastRadius,
            dir,
            out RaycastHit hit,
            shellRange,
            mask,
            QueryTriggerInteraction.Collide))
        {
            ShellShard shard = hit.collider.GetComponentInParent<ShellShard>();

            if (shard && !shard.detached)
                return shard;
        }

        // 2. 備援：範圍內找最近的
        Collider[] hits = Physics.OverlapSphere(
            origin,
            shellRange,
            mask,
            QueryTriggerInteraction.Collide);

        ShellShard best = null;
        float bestDist = float.MaxValue;

        float cosLimit = Mathf.Cos(shellHalfAngle * Mathf.Deg2Rad);

        foreach (Collider h in hits)
        {
            if (!h) continue;

            ShellShard shard = h.GetComponentInParent<ShellShard>();
            if (!shard || shard.detached) continue;

            Vector3 to = shard.transform.position - origin;
            float dist = to.magnitude;

            if (dist < 0.001f) continue;

            float cos = Vector3.Dot(dir, to.normalized);

            if (cos < cosLimit)
                continue;

            if (dist < bestDist)
            {
                bestDist = dist;
                best = shard;
            }
        }

        return best;
    }

    Vector3 GetAimDirection()
    {
        Transform src = aimSource ? aimSource : transform;

        Vector3 dir = src.forward;

        if (dir.sqrMagnitude < 0.001f)
            dir = transform.forward;

        return dir.normalized;
    }

    LayerMask EffectiveMask(LayerMask mask, string maskName)
    {
        if (mask.value == 0)
        {
            if (showDebugLog)
                Debug.LogWarning($"[Cut] {maskName} 沒有設定，暫時使用 All Layers。", this);

            return ~0;
        }

        return mask;
    }

    void OnDrawGizmosSelected()
    {
        if (!attackPoint) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(attackPoint.position, shellRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, weakPointRange);
    }
}