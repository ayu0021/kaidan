using UnityEngine;

[RequireComponent(typeof(Animator))]
public class PlayerController3D_FreeMove : MonoBehaviour
{
    [Header("移動速度")]
    public float moveSpeed = 3f;

    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private Rigidbody rb;

    private Vector3 moveInput;

    void Start()
    {
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        rb = GetComponent<Rigidbody>();

        // Rigidbody 設定
        if (rb != null)
        {
            rb.constraints = RigidbodyConstraints.FreezeRotation; // 防止旋轉
        }
    }

    void Update()
    {
        // 取得水平與垂直輸入
        float moveX = Input.GetAxisRaw("Horizontal");
        float moveZ = Input.GetAxisRaw("Vertical");

        // 建立移動向量
        moveInput = new Vector3(moveX, 0f, moveZ).normalized;

        // 設定 Animator 參數
        animator.SetFloat("Speed", moveInput.magnitude);

        // 左右翻轉（只根據 X 軸移動判斷）
        if (moveX > 0) spriteRenderer.flipX = false;
        else if (moveX < 0) spriteRenderer.flipX = true;
    }

    void FixedUpdate()
    {
        if (rb != null)
        {
            // 使用 Rigidbody 移動，避免 Transform.Translate 與 Animation 衝突
            Vector3 newPosition = rb.position + moveInput * moveSpeed * Time.fixedDeltaTime;
            rb.MovePosition(newPosition);
        }
        else
        {
            // 如果沒有 Rigidbody，回退到 Transform 移動
            transform.position += moveInput * moveSpeed * Time.fixedDeltaTime;
        }
    }
}
