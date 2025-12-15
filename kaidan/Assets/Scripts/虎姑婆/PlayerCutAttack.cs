using UnityEngine;

public class PlayerCutAttack : MonoBehaviour
{
    [Header("Attack")]
    public Transform attackPoint;
    public float range = 4f;
    public float radius = 0.15f;               // SphereCast 半徑（剪刀寬度）
    public LayerMask hitMask;                  // 要包含 Shell + WeakPoint
    public KeyCode key = KeyCode.Space;

    [Header("Refs")]
    public ShellCluster shellCluster;
    public int weakpointDamage = 1;

    [Header("Aim")]
    [Tooltip("可選：強制瞄準指定目標（不填就用 ShellCluster 的 weakPoint）")]
    public Transform aimTargetOverride;

    [Tooltip("2D/平面戰鬥：忽略 Y 方向，避免往上/往下打歪")]
    public bool flattenY = true;

    [Header("Debug")]
    public bool drawDebug = true;

    int _shellLayer;
    int _weakLayer;

    void Awake()
    {
        _shellLayer = LayerMask.NameToLayer("Shell");
        _weakLayer = LayerMask.NameToLayer("WeakPoint");
    }

    void Update()
    {
        if (Input.GetKeyDown(key))
            DoCut();
    }

    void DoCut()
    {
        if (!attackPoint) return;

        Vector3 origin = attackPoint.position;

        // 方向：優先朝 weakPoint（或你指定的 aimTargetOverride）
        Vector3 dir = GetAimDirection(origin);

        if (drawDebug)
        {
            Debug.DrawLine(origin, origin + dir * range, Color.red, 0.2f);
        }

        if (Physics.SphereCast(origin, radius, dir, out RaycastHit hit, range, hitMask, QueryTriggerInteraction.Ignore))
        {
            int hitLayer = hit.collider.gameObject.layer;

            // 1) 殼：直接剪
            if (hitLayer == _shellLayer)
            {
                if (shellCluster)
                {
                    shellCluster.CutByCollider(hit.collider, hit.point);
                    Debug.Log($"[Cut] Shell hit: {hit.collider.name} point={hit.point}");
                }
                else
                {
                    Debug.LogWarning("[Cut] Shell hit but shellCluster is null.");
                }
                return;
            }

            // 2) 弱點：殼沒清完不給打
            if (hitLayer == _weakLayer)
            {
                if (shellCluster && !shellCluster.AllShellDestroyed)
                {
                    Debug.Log("[Cut] WeakPoint is still covered (shell not fully removed).");
                    return;
                }

                var wp = hit.collider.GetComponent<WeakPoint>();
                if (wp) wp.TakeDamage(weakpointDamage);

                Debug.Log($"[Cut] WeakPoint hit! point={hit.point}");
                return;
            }

            // 其他：你有勾到別的 layer 的話，方便你抓
            Debug.Log($"[Cut] Hit other: {hit.collider.name} layer={LayerMask.LayerToName(hitLayer)} point={hit.point}");
        }
        else
        {
            Debug.Log("[Cut] nothing hit");
        }
    }

    Vector3 GetAimDirection(Vector3 origin)
    {
        Transform target = aimTargetOverride;

        // 若沒手動指定，就用 shellCluster 的 weakPoint（你之前說想自動朝 weakPoint）
        if (target == null && shellCluster != null && shellCluster.weakPoint != null)
            target = shellCluster.weakPoint;

        Vector3 dir;

        if (target != null)
        {
            dir = target.position - origin;
            if (flattenY) dir.y = 0f;

            if (dir.sqrMagnitude < 0.0001f)
                dir = attackPoint.forward;
        }
        else
        {
            // 沒目標就退回原本 forward
            dir = attackPoint.forward;
            if (flattenY) dir.y = 0f;
        }

        dir.Normalize();
        return dir;
    }

    void OnDrawGizmosSelected()
    {
        if (!attackPoint) return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, radius);

        // Gizmo 用 forward 畫就好（真實方向以 Debug.DrawLine 為準）
        Gizmos.DrawLine(attackPoint.position, attackPoint.position + attackPoint.forward * range);
    }
}

