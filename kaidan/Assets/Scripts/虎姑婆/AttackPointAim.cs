using UnityEngine;

public class AttackPointAim : MonoBehaviour
{
    public Transform weakPoint;
    public bool lockY = true; // 俯視遊戲通常鎖Y

    void LateUpdate()
    {
        if (!weakPoint) return;

        Vector3 dir = weakPoint.position - transform.position;

        if (lockY) dir.y = 0f; // 只在水平面轉向

        if (dir.sqrMagnitude < 1e-6f) return;
        transform.forward = dir.normalized;
    }
}
