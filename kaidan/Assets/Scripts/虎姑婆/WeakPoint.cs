using UnityEngine;

public class WeakPoint : MonoBehaviour
{
    public int hp = 5;

    public void TakeDamage(int dmg)
    {
        hp -= dmg;
        Debug.Log($"[WeakPoint] hit! hp={hp}");

        if (hp <= 0)
        {
            Debug.Log("[Boss] defeated!");
            // TODO: 播放死亡動畫/過場/結算
        }
    }
}
