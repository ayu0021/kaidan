using UnityEngine;

public class ShieldBehavior : MonoBehaviour
{
    public int maxHP = 10;
    private int currentHP;

    [Header("剪到時是否同時挖洞")]
    public bool cutOnHit = true;

    private DestructibleShield destructible;

    void Start()
    {
        currentHP = maxHP;
        destructible = GetComponent<DestructibleShield>();
    }

    /// <summary>
    /// 給玩家攻擊呼叫：扣血 +（可選）在命中點挖洞
    /// </summary>
    public void ApplyCutHit(int dmg, Vector3 hitPoint, float cutRadiusWorld)
    {
        currentHP -= dmg;
        Debug.Log($"Shield hit! HP = {currentHP}, hitPoint={hitPoint}");

        if (cutOnHit && destructible != null)
        {
            destructible.CutAt(hitPoint, cutRadiusWorld);
        }

        if (currentHP <= 0)
        {
            BreakShield();
        }
    }

    void BreakShield()
    {
        gameObject.SetActive(false);
        Debug.Log("Shield destroyed!");
    }
}

