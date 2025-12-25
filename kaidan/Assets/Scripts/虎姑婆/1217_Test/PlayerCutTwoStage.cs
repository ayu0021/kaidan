using UnityEngine;

public class PlayerCutTwoStage : MonoBehaviour
{
    [Header("Refs")]
    public Transform attackPoint;      // 只拿位置
    public Transform aimSource;        // 拖 MainCamera（建議）
    public BossShellController boss;

    [Header("Detect")]
    public LayerMask shellMask;        // Shell
    public LayerMask weakPointMask;    // WeakPoint（如果沒設，程式會自動用 AllLayers 並提醒）
    public float range = 2.5f;
    public float castRadius = 0.22f;
    [Range(0f, 180f)] public float halfAngle = 120f;

    [Header("WeakPoint Aim")]
    [Tooltip("殼清空後，弱點階段是否自動瞄準 WeakPoint（強烈建議開）")]
    public bool autoAimWeakPoint = true;

    [Header("Input")]
    public KeyCode cutKey = KeyCode.Space;

    [Header("Hover Refresh")]
    public float hoverRefreshInterval = 0.05f;

    [Header("Debug")]
    public bool drawDebug = false;

    ShellShard _hover;
    float _t;

    void Update()
    {
        if (!attackPoint) return;

        _t -= Time.deltaTime;
        if (_t <= 0f)
        {
            _t = hoverRefreshInterval;
            UpdateHover();
        }

        if (Input.GetKeyDown(cutKey))
            DoAction();
    }

    Vector3 GetDirFromAimSource()
    {
        Transform src = aimSource ? aimSource : transform;
        Vector3 dir = src.forward;
        if (dir.sqrMagnitude < 0.0001f) dir = Vector3.forward;
        return dir.normalized;
    }

    LayerMask EffectiveMask(LayerMask m, string name)
    {
        if (m.value == 0)
        {
            Debug.LogWarning($"[Cut] {name} is EMPTY (0). Using AllLayers temporarily. Please set it in Inspector.");
            return ~0; // AllLayers
        }
        return m;
    }

    void UpdateHover()
    {
        var next = FindBestShard();
        if (next == _hover) return;

        if (_hover) _hover.SetHover(false);
        _hover = next;
        if (_hover) _hover.SetHover(true);
    }

    void DoAction()
    {
        Vector3 origin = attackPoint.position;

        // 1) 有殼就剪殼
        if (_hover)
        {
            _hover.Detach(origin);
            _hover = null;
            UpdateHover();
            return;
        }

        // 2) 殼清光後：打 WeakPoint
        if (boss && boss.ShellCleared)
        {
            if (TryHitWeakPoint(origin))
                return;

            DebugNearestWeakPoint(origin);
        }

        Debug.Log("[Cut] nothing hit");
    }

    bool TryHitWeakPoint(Vector3 origin)
    {
        var mask = EffectiveMask(weakPointMask, nameof(weakPointMask));

        // 決定弱點階段的方向：自動瞄準 > 相機 forward
        Vector3 dir = GetDirFromAimSource();

        if (autoAimWeakPoint && boss && boss.weakPoint)
        {
            // 盡量取 collider center，沒有就用 transform
            var wpCol = boss.weakPoint.GetComponentInChildren<Collider>();
            Vector3 target = wpCol ? wpCol.bounds.center : boss.weakPoint.transform.position;

            Vector3 to = target - origin;
            if (to.sqrMagnitude > 0.0001f)
                dir = to.normalized;
        }

        if (drawDebug)
            Debug.DrawRay(origin, dir * range, Color.yellow, 0.25f);

        // (A) 前方 SphereCast：最準
        if (Physics.SphereCast(origin, castRadius, dir, out RaycastHit hit,
            range, mask, QueryTriggerInteraction.Collide))
        {
            if (drawDebug)
                Debug.Log($"[Cut] WeakPoint hit: {hit.collider.name}, layer={LayerMask.LayerToName(hit.collider.gameObject.layer)}");

            var wp = hit.collider.GetComponentInParent<WeakPoint>();
            if (wp != null)
            {
                wp.TakeDamage(1f, hit.point, hit.normal);
                return true;
            }
        }

        // (B) 備援：OverlapSphere + 錐形
        var hits = Physics.OverlapSphere(origin, range, mask, QueryTriggerInteraction.Collide);
        if (hits == null || hits.Length == 0) return false;

        float cosLimit = Mathf.Cos(halfAngle * Mathf.Deg2Rad);

        WeakPoint bestWp = null;
        Collider bestCol = null;
        float bestDist = float.MaxValue;

        foreach (var col in hits)
        {
            if (!col) continue;
            var wp = col.GetComponentInParent<WeakPoint>();
            if (!wp) continue;

            Vector3 to = col.bounds.center - origin;
            float dist = to.magnitude;
            if (dist < 0.0001f) continue;

            float cos = Vector3.Dot(dir, to.normalized);
            if (cos < cosLimit) continue;

            if (dist < bestDist)
            {
                bestDist = dist;
                bestWp = wp;
                bestCol = col;
            }
        }

        if (bestWp != null && bestCol != null)
        {
            Vector3 p = bestCol.ClosestPoint(origin);
            Vector3 n = (origin - p).sqrMagnitude > 0.0001f ? (origin - p).normalized : -dir;

            if (drawDebug)
                Debug.Log($"[Cut] WeakPoint hit (Fallback): {bestCol.name} dist={bestDist:0.00}");

            bestWp.TakeDamage(1f, p, n);
            return true;
        }

        return false;
    }

    void DebugNearestWeakPoint(Vector3 origin)
    {
        // 這段是偵錯用：不管 layer mask，直接找最近的 WeakPoint，印出距離與 layer
        var all = Physics.OverlapSphere(origin, range, ~0, QueryTriggerInteraction.Collide);
        WeakPoint best = null;
        float bestDist = float.MaxValue;

        foreach (var c in all)
        {
            var wp = c.GetComponentInParent<WeakPoint>();
            if (!wp) continue;

            float d = Vector3.Distance(origin, c.bounds.center);
            if (d < bestDist)
            {
                bestDist = d;
                best = wp;
            }
        }

        if (best)
        {
            int layer = best.gameObject.layer;
            Debug.LogWarning($"[Cut][Debug] Nearest WeakPoint found at dist={bestDist:0.00}, layer={LayerMask.LayerToName(layer)}. " +
                             $"If still not hittable, check PlayerCutTwoStage.weakPointMask includes that layer and the Collider object is on it.");
        }
        else
        {
            Debug.LogWarning("[Cut][Debug] No WeakPoint collider found within range at all.");
        }
    }

    ShellShard FindBestShard()
    {
        Vector3 origin = attackPoint.position;
        Vector3 dir = GetDirFromAimSource();

        var mask = EffectiveMask(shellMask, nameof(shellMask));

        // 前方優先
        if (Physics.SphereCast(origin, castRadius, dir, out RaycastHit hit,
            range, mask, QueryTriggerInteraction.Collide))
        {
            var shard = hit.collider.GetComponentInParent<ShellShard>();
            if (shard && !shard.detached) return shard;
        }

        // 備援：近距離錐形
        var hits = Physics.OverlapSphere(origin, range, mask, QueryTriggerInteraction.Collide);
        if (hits == null || hits.Length == 0) return null;

        float cosLimit = Mathf.Cos(halfAngle * Mathf.Deg2Rad);

        ShellShard best = null;
        float bestScore = float.MaxValue;

        foreach (var h in hits)
        {
            var shard = h.GetComponentInParent<ShellShard>();
            if (!shard || shard.detached) continue;

            Vector3 to = shard.transform.position - origin;
            float dist = to.magnitude;
            if (dist < 0.0001f) continue;

            float cos = Vector3.Dot(dir, to.normalized);
            if (cos < cosLimit) continue;

            if (dist < bestScore)
            {
                bestScore = dist;
                best = shard;
            }
        }

        return best;
    }
}




