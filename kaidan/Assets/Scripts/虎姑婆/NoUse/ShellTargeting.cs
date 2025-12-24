using UnityEngine;

public class ShellTargeting : MonoBehaviour
{
    [Header("Refs")]
    public Transform attackPoint;

    [Header("Detect")]
    public LayerMask shellMask;
    public float range = 1.5f;
    [Tooltip("只選前方的目標，60 表示前方 ±60 度")]
    public float halfAngle = 60f;

    [Header("Input")]
    public KeyCode cutKey = KeyCode.Space;

    ShellPlate current;

    void Update()
    {
        // 1) 每幀找目前可鎖定的殼片
        ShellPlate next = FindBestShellPlate();

        // 2) Hover 高亮切換
        if (next != current)
        {
            if (current != null) current.SetHover(false);
            current = next;
            if (current != null) current.SetHover(true);
        }

        // 3) 按鍵剪下
        if (Input.GetKeyDown(cutKey) && current != null)
        {
            current.Detach();
            current = null;
        }
    }

    ShellPlate FindBestShellPlate()
    {
        if (attackPoint == null) return null;

        var hits = Physics.OverlapSphere(
            attackPoint.position,
            range,
            shellMask,
            QueryTriggerInteraction.Collide
        );

        if (hits == null || hits.Length == 0) return null;

        ShellPlate best = null;
        float bestScore = float.MaxValue;

        Vector3 origin = attackPoint.position;
        Vector3 forward = attackPoint.forward;

        float cosLimit = Mathf.Cos(halfAngle * Mathf.Deg2Rad);

        foreach (var h in hits)
        {
            if (h == null) continue;

            // 取到那片殼的 ShellPlate（建議 ShellPlate 直接掛在 Plane.xxx 上）
            var plate = h.GetComponentInParent<ShellPlate>();
            if (plate == null || plate.detached) continue;

            Vector3 to = (plate.transform.position - origin);
            float dist = to.magnitude;
            if (dist < 0.0001f) continue;

            // 角度限制：只鎖前方錐形範圍
            float cos = Vector3.Dot(forward.normalized, to.normalized);
            if (cos < cosLimit) continue;

            // 分數：越近越優先（你也可以加上角度權重）
            float score = dist;

            if (score < bestScore)
            {
                bestScore = score;
                best = plate;
            }
        }

        return best;
    }

    void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;
        Gizmos.DrawWireSphere(attackPoint.position, range);
    }
}
