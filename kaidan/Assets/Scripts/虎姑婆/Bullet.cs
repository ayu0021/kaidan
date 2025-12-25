using UnityEngine;

public class Bullet : MonoBehaviour
{
    [Header("移動速度")]
    public float speed = 6f;

    [Header("活動範圍 (X / Z)")]
    public Vector2 moveLimits = new Vector2(4f, 4f);

    [Header("平面高度 (Y)")]
    public float planeY = 0f;

    [Header("行為模式 (預設=反彈)")]
    [Tooltip("開啟：碰到邊界反彈（你目前的行為）\n關閉：不反彈，出界就消失（適合 Bullet3 右→左）")]
    public bool bounceOnBounds = true;

    [Tooltip("不反彈模式下：出界多少距離後才消失（避免一出界就砍掉看起來太突然）")]
    public float despawnPadding = 0.5f;

    [Header("Bullet3 右→左設定（只要 Bullet3 勾即可）")]
    [Tooltip("Init 時強制把子彈放到右邊界外側（+X 方向）")]
    public bool spawnFromRightEdge = false;

    [Tooltip("右邊界外側偏移量（越大越像從畫面外飛進來）")]
    public float spawnRightPadding = 0.5f;

    [Tooltip("無視 spawner 給的方向，強制往左飛（-X）")]
    public bool forceMoveLeft = false;

    [Tooltip("如果要每發的高度都固定在 planeY，保持勾選")]
    public bool lockToPlaneY = true;

    private Vector3 direction;

    /// <summary>
    /// 由 Spawner 呼叫，設定子彈的初始方向與活動邊界
    /// </summary>
    public void Init(Vector3 dir, Vector2 limits, float y)
    {
        moveLimits = limits;
        planeY = y;

        // 方向：一般用 spawner 給的；若是 Bullet3 模式則強制往左
        direction = forceMoveLeft ? Vector3.left : dir.normalized;

        // 位置：一般不動；若是 Bullet3 模式則強制從右側外面生成
        Vector3 pos = transform.position;

        if (spawnFromRightEdge)
        {
            // 從右側外側進場（世界座標 +X）
            pos.x = moveLimits.x + spawnRightPadding;
            // Z 可以維持 spawner 給的位置（通常 pattern 會安排），不用亂改
        }

        if (lockToPlaneY)
            pos.y = planeY;

        transform.position = pos;
    }

    void Update()
    {
        Vector3 pos = transform.position;
        pos += direction * speed * Time.deltaTime;

        if (bounceOnBounds)
        {
            // 反彈模式（你原本行為）
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
        }
        else
        {
            // 不反彈：出界就消失（Bullet3 右→左最適合）
            if (pos.x < -moveLimits.x - despawnPadding ||
                pos.x >  moveLimits.x + despawnPadding ||
                pos.z < -moveLimits.y - despawnPadding ||
                pos.z >  moveLimits.y + despawnPadding)
            {
                Destroy(gameObject);
                return;
            }
        }

        if (lockToPlaneY)
            pos.y = planeY;

        transform.position = pos;
    }
}

