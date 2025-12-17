using UnityEngine;

public class PlayerCutTwoStage : MonoBehaviour
{
    [Header("Refs")]
    public Transform attackPoint;            // 只拿位置
    public Transform aimSource;              // 方向來源：建議拖 MainCamera 或 BattlePlayer
    public BossShellController boss;

    [Header("Detect")]
    public LayerMask shellMask;             // Shell
    public LayerMask innerShieldMask;       // InnerShield
    public float range = 2.5f;
    public float castRadius = 0.25f;
    [Tooltip("只鎖前方錐形，180=幾乎不限制")]
    public float halfAngle = 120f;

    [Header("Input")]
    public KeyCode cutKey = KeyCode.Space;

    [Header("Hover refresh")]
    public float hoverRefreshInterval = 0.05f;

    ShellShard _hoverShard;
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
            DoCut();
    }

    Vector3 GetAimForward()
    {
        Transform src = aimSource ? aimSource : transform;
        Vector3 fwd = src.forward;

        // Top-down 常見：忽略Y，避免往天空/地板射
        fwd.y = 0f;
        if (fwd.sqrMagnitude < 0.0001f) fwd = Vector3.forward;
        return fwd.normalized;
    }

    void UpdateHover()
    {
        var next = FindBestShard();
        if (next == _hoverShard) return;

        if (_hoverShard) _hoverShard.SetHover(false);
        _hoverShard = next;
        if (_hoverShard) _hoverShard.SetHover(true);
    }

    void DoCut()
    {
        if (_hoverShard)
        {
            _hoverShard.Detach(attackPoint.position);
            _hoverShard = null;
            UpdateHover();
            return;
        }

        // 外殼全清完才打內盾
        if (boss && boss.ShellCleared)
        {
            Vector3 origin = attackPoint.position;
            Vector3 dir = GetAimForward();

            if (Physics.SphereCast(origin, castRadius, dir, out RaycastHit hit,
                range, innerShieldMask, QueryTriggerInteraction.Collide))
            {
                var shield = hit.collider.GetComponentInParent<InnerShield>();
                if (shield) shield.TakeHit(1);
                return;
            }
        }

        Debug.Log("[Cut] nothing hit");
    }

    ShellShard FindBestShard()
    {
        Vector3 origin = attackPoint.position;
        Vector3 dir = GetAimForward();

        // ① 先用 SphereCast 取「你前方指到的那片」
        if (Physics.SphereCast(origin, castRadius, dir, out RaycastHit hit,
            range, shellMask, QueryTriggerInteraction.Collide))
        {
            var shard = hit.collider.GetComponentInParent<ShellShard>();
            if (shard && !shard.detached) return shard;
        }

        // ② 備援：OverlapSphere + 前方錐形
        Collider[] hits = Physics.OverlapSphere(origin, range, shellMask, QueryTriggerInteraction.Collide);
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

    void OnDrawGizmosSelected()
    {
        if (!attackPoint) return;
        Gizmos.DrawWireSphere(attackPoint.position, range);

        Vector3 dir = aimSource ? aimSource.forward : transform.forward;
        dir.y = 0f;
        Gizmos.DrawLine(attackPoint.position, attackPoint.position + dir.normalized * range);
    }
}
