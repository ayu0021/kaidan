using UnityEngine;

public class ShieldBehavior : MonoBehaviour
{
    public int maxHP = 10;
    private int currentHP;

    void Start()
    {
        currentHP = maxHP;
    }

    public void TakeDamage(int dmg)
    {
        currentHP -= dmg;

        Debug.Log("Shield hit! HP = " + currentHP);

        if (currentHP <= 0)
        {
            BreakShield();
        }
    }

    void BreakShield()
    {
        // 之後可改成動畫或碎裂效果
        gameObject.SetActive(false);
        Debug.Log("Shield destroyed!");
    }
}
