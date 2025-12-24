using UnityEngine;

public class PlayerCutTargetingCut25 : MonoBehaviour
{
    [Header("Refs")]
    public Transform attackPoint;

    [Header("Detect")]
    public LayerMask shellMask;     // 只勾 Shell
    public float range = 1.6f;      // 近距離才剪得到
    [Tooltip("只鎖 attackPoint 前方的目標，60=前方±60度")]
    public float halfAngle = 70f;

    [Header("Input")]
    public KeyCode cutKey = KeyCode.Space;

    [Header("Hover refresh")]
    public float hoverRefreshInterval = 0.05f;

    ShellPlate _current;
    float _timer;

    void Update()
    {
        if (!attackPoint) return;

        // 定期更新 hover（避免每幀掃太重）
        _timer -= Time.deltaTime;
        if (_timer <= 0f)
        {
            _timer = hoverRefreshInterval;
            UpdateHoverTarget();
        }

        if (Input.GetKeyDown(cutKey))
            DoCut();
    }

    void UpdateHoverTarget()
    {
        var next = FindBestPlate();
        if (next == _current) return;

        if (_current) _current.SetHover(false);
        _current = next;
        if (_current) _current.SetHover(true);
    }

    void DoCut()
    {
        if (_current == null)
        {
            Debug.Log("[Cut] nothing hit");
            return;
        }

        _current.Detach();
        _current = null;
    }

    ShellPlate FindBestPlate()
    {
        Vector3 origin = attackPoint.position;
        Vector3 fwd = attackPoint.forward;

        Collider[] hits = Physics.OverlapSphere(
            origin,
            range,
            shellMask,
            QueryTriggerInteraction.Collide
        );

        if (hits == null || hits.Length == 0) return null;

        float cosLimit = Mathf.Cos(halfAngle * Mathf.Deg2Rad);

        ShellPlate best = null;
        float bestScore = float.MaxValue;

        foreach (var h in hits)
        {
            if (!h) continue;

            // 目標：每一片殼上都要有 ShellPlate
            var plate = h.GetComponentInParent<ShellPlate>();
            if (!plate || plate.detached) continue;

            Vector3 to = plate.transform.position - origin;
            float dist = to.magnitude;
            if (dist < 0.0001f) continue;

            // 只鎖前方錐形：避免你站很遠也剪到固定幾片
            float cos = Vector3.Dot(fwd.normalized, to.normalized);
            if (cos < cosLimit) continue;

            // 分數：越近越優先（你也可以加角度權重）
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
        if (!attackPoint) return;
        Gizmos.DrawWireSphere(attackPoint.position, range);
        Gizmos.DrawLine(attackPoint.position, attackPoint.position + attackPoint.forward * range);
    }
}
