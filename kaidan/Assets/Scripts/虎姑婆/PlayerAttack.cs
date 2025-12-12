using UnityEngine;

public class PlayerCutAttack : MonoBehaviour
{
    [Header("剪刀攻擊設定")]
    public Transform attackPoint;        // 剪刀位置
    public float cutRadius = 0.7f;       // 圓形剪刀半徑（世界座標）
    public LayerMask shieldLayer;        // 只碰到 Shield 用的 Layer

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            DoCut();
        }
    }

    void DoCut()
    {
        if (attackPoint == null) return;

        Vector3 pos = attackPoint.position;

        // 用物理球去找「盾」的碰撞器
        Collider[] hits = Physics.OverlapSphere(pos, cutRadius, shieldLayer);

        foreach (var hit in hits)
        {
            DestructibleShield shield = hit.GetComponent<DestructibleShield>();
            if (shield != null)
            {
                shield.CutAt(pos, cutRadius);
            }
        }

        // TODO：這裡可以之後加剪刀動畫 / 音效
        Debug.Log("DoCut at " + pos);
    }

    // 在 Scene 視窗中顯示剪刀範圍，方便你調整
    void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, cutRadius);
    }
}
