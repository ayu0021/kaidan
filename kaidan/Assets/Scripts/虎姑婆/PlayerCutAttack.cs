using UnityEngine;

public class PlayerCutAttack : MonoBehaviour
{
    public enum AimMode
    {
        ToWeakPoint,   // 永遠指向 WeakPoint（推薦）
        WorldDown,     // 永遠往世界座標下方
        AttackPointForward
    }

    [Header("Points")]
    public Transform attackPoint;
    public Transform weakPoint; // 拖 BossRoot/WeakPoint 進來

    [Header("Aim")]
    public AimMode aimMode = AimMode.ToWeakPoint;

    [Header("Cut Cast")]
    public float range = 4f;
    public float radius = 0.15f;      // SphereCast 半徑
    public LayerMask shellMask;       // 只勾 Shell
    public LayerMask weakMask;        // 只勾 WeakPoint
    public KeyCode key = KeyCode.Space;

    [Header("Hover (走到哪裡哪裡亮)")]
    public float hoverRadius = 0.45f;          // 玩家附近掃描半徑
    public float hoverFlashInterval = 0.12f;   // 多久閃一次避免狂刷
    public bool hoverOnlyWhenShellAlive = true;

    [Header("Damage")]
    public int weakpointDamage = 1;

    [Header("Refs")]
    public ShellCluster shellCluster;

    Collider _hovered;
    float _hoverTimer;

    void Update()
    {
        UpdateHover();

        if (Input.GetKeyDown(key))
            DoCut();
    }

    Vector3 GetAimDir(Vector3 origin)
    {
        switch (aimMode)
        {
            case AimMode.WorldDown:
                return Vector3.down;

            case AimMode.AttackPointForward:
                return attackPoint ? attackPoint.forward : Vector3.forward;

            case AimMode.ToWeakPoint:
            default:
                if (weakPoint)
                {
                    Vector3 d = (weakPoint.position - origin);
                    if (d.sqrMagnitude > 1e-6f) return d.normalized;
                }
                // 沒設定 weakPoint 時，退回用 forward
                return attackPoint ? attackPoint.forward : Vector3.forward;
        }
    }

    void UpdateHover()
    {
        if (!attackPoint) return;

        if (hoverOnlyWhenShellAlive && shellCluster && shellCluster.AllShellDestroyed)
            return;

        _hoverTimer -= Time.deltaTime;
        if (_hoverTimer > 0f) return;
        _hoverTimer = hoverFlashInterval;

        var hits = Physics.OverlapSphere(attackPoint.position, hoverRadius, shellMask, QueryTriggerInteraction.Ignore);
        if (hits == null || hits.Length == 0)
        {
            _hovered = null;
            return;
        }

        // 挑最近的那片殼
        Collider best = null;
        float bestD = float.MaxValue;
        Vector3 p = attackPoint.position;
        foreach (var c in hits)
        {
            if (!c) continue;
            float d = (c.ClosestPoint(p) - p).sqrMagnitude;
            if (d < bestD) { bestD = d; best = c; }
        }

        _hovered = best;
        if (_hovered)
        {
            var piece = _hovered.GetComponent<ShellPiece>();
            if (piece) piece.Flash(); // 走到哪裡哪裡亮
        }
    }

    void DoCut()
    {
        if (!attackPoint) return;

        Vector3 origin = attackPoint.position;
        Vector3 dir = GetAimDir(origin);

        // ✅ 畫出偵測線（Scene 視窗看得到）
        Debug.DrawLine(origin, origin + dir * range, Color.cyan, 0.2f);

        // 1) 先找殼（Shell）
        if (Physics.SphereCast(origin, radius, dir, out RaycastHit hitS, range, shellMask, QueryTriggerInteraction.Ignore))
        {
            if (shellCluster)
            {
                shellCluster.CutByCollider(hitS.collider, hitS.point);
                Debug.Log($"[Cut] Shell hit: {hitS.collider.name} point={hitS.point}");
            }
            else
            {
                Debug.LogWarning("[Cut] Shell hit but shellCluster is NULL.");
            }
            return;
        }

        // 2) 找弱點（WeakPoint）— 但殼沒清光就不扣血
        if (Physics.SphereCast(origin, radius, dir, out RaycastHit hitW, range, weakMask, QueryTriggerInteraction.Ignore))
        {
            if (shellCluster && !shellCluster.AllShellDestroyed)
            {
                Debug.Log("[Cut] WeakPoint is still covered (shell not fully removed).");
                return;
            }

            var wp = hitW.collider.GetComponent<WeakPoint>();
            if (wp) wp.TakeDamage(weakpointDamage);

            Debug.Log($"[Cut] WeakPoint hit! point={hitW.point}");
            return;
        }

        Debug.Log("[Cut] nothing hit");
    }

    void OnDrawGizmosSelected()
    {
        if (!attackPoint) return;

        // hover 範圍
        Gizmos.DrawWireSphere(attackPoint.position, hoverRadius);

        // cut 範圍與方向
        Vector3 origin = attackPoint.position;
        Vector3 dir = GetAimDir(origin);
        Gizmos.DrawWireSphere(origin, radius);
        Gizmos.DrawLine(origin, origin + dir * range);
    }
}

