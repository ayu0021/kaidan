using UnityEngine;

public class PlayerCutAttack_OnePiece : MonoBehaviour
{
    [Header("Refs")]
    public Transform attackPoint;

    [Header("Detect")]
    public float range = 1.2f;
    public LayerMask shellMask;
    public KeyCode key = KeyCode.Space;

    void Update()
    {
        if (!Input.GetKeyDown(key)) return;
        if (attackPoint == null) return;

        // 這個最穩：不管對方 collider 是不是 trigger 都能抓到（因為我們指定 Collide）
        Collider[] hits = Physics.OverlapSphere(
            attackPoint.position,
            range,
            shellMask,
            QueryTriggerInteraction.Collide
        );

        if (hits.Length == 0)
        {
            Debug.Log("[Cut] nothing hit");
            return;
        }

        // 找最近的那片
        Collider best = hits[0];
        float bestDist = (best.transform.position - attackPoint.position).sqrMagnitude;

        for (int i = 1; i < hits.Length; i++)
        {
            float d = (hits[i].transform.position - attackPoint.position).sqrMagnitude;
            if (d < bestDist) { bestDist = d; best = hits[i]; }
        }

        // 你每片殼上加一個 ShellPlate（我下面會給）
        var plate = best.GetComponentInParent<ShellPlate>();
        if (plate == null)
        {
            Debug.Log("[Cut] hit something, but no ShellPlate on it.");
            return;
        }

        plate.Detach();
    }

    void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;
        Gizmos.DrawWireSphere(attackPoint.position, range);
    }
}
