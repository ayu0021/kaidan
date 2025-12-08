using UnityEngine;

public class MouseFollow : MonoBehaviour
{
    [Header("Camera")]
    public Camera cam;

    [Header("Movement Settings")]
    public float moveSpeed = 15f;

    [Header("Movement Limits (X / Z)")]
    public Vector2 moveLimits = new Vector2(5f, 5f);

    void Update()
    {
        if (cam == null)
            cam = Camera.main;

        // 1. 滑鼠螢幕座標 → 世界座標
        Vector3 mousePos = Input.mousePosition;

        // 必須給 z 值（距離 camera 多遠）
        mousePos.z = Mathf.Abs(cam.transform.position.y - transform.position.y);

        Vector3 worldPos = cam.ScreenToWorldPoint(mousePos);

        // 2. 限制範圍
        worldPos.x = Mathf.Clamp(worldPos.x, -moveLimits.x, moveLimits.x);
        worldPos.z = Mathf.Clamp(worldPos.z, -moveLimits.y, moveLimits.y);

        // 3. 平滑移動
        transform.position = Vector3.Lerp(
            transform.position,
            worldPos,
            moveSpeed * Time.deltaTime
        );
    }
}
