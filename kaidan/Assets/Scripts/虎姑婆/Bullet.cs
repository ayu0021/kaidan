using UnityEngine;

public class Bullet : MonoBehaviour
{
    [Header("移動速度")]
    public float speed = 6f;

    [Header("活動範圍 (X / Z)")]
    public Vector2 moveLimits = new Vector2(4f, 4f);

    [Header("平面高度 (Y)")]
    public float planeY = 0f;

    private Vector3 direction;

    /// <summary>
    /// 由 Spawner 呼叫，設定子彈的初始方向與活動邊界
    /// </summary>
    public void Init(Vector3 dir, Vector2 limits, float y)
    {
        direction = dir.normalized;
        moveLimits = limits;
        planeY = y;

        // 鎖在指定平面高度
        Vector3 pos = transform.position;
        pos.y = planeY;
        transform.position = pos;
    }

    void Update()
    {
        // 1. 移動
        Vector3 pos = transform.position;
        pos += direction * speed * Time.deltaTime;

        // 2. 碰到邊界就反彈
        if (pos.x > moveLimits.x)
        {
            pos.x = moveLimits.x;
            direction.x *= -1f;
        }
        else if (pos.x < -moveLimits.x)
        {
            pos.x = -moveLimits.x;
            direction.x *= -1f;
        }

        if (pos.z > moveLimits.y)
        {
            pos.z = moveLimits.y;
            direction.z *= -1f;
        }
        else if (pos.z < -moveLimits.y)
        {
            pos.z = -moveLimits.y;
            direction.z *= -1f;
        }

        pos.y = planeY; // 高度固定
        transform.position = pos;
    }
}
