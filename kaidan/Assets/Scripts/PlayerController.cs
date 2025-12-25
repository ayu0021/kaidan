using UnityEngine;

// 強制要求有 Rigidbody，避免程式碼報錯
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(Rigidbody))] 
public class PlayerController : MonoBehaviour
{
    [Header("移動速度")]
    public float moveSpeed = 5f; // 稍微調高一點，3f 有時體感較慢

    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private Rigidbody rb;

    private Vector3 moveInput;
    private bool initialFlipX;

    void Start()
    {
        animator = GetComponent<Animator>();
        // 如果 SpriteRenderer 在子物件，用 GetComponentInChildren；如果在同物件用 GetComponent
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        rb = GetComponent<Rigidbody>();

        if (rb != null)
        {
            // 凍結旋轉，防止角色因為碰撞而倒地
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezeRotationZ;
            // 確保物理運動不會因為摩擦力而卡住
            rb.useGravity = false; 
        }

        if (spriteRenderer != null)
            initialFlipX = spriteRenderer.flipX;
    }

    void Update()
    {
        // 取得輸入
        float moveX = Input.GetAxisRaw("Horizontal");
        float moveZ = Input.GetAxisRaw("Vertical");

        // 計算移動向量並正規化（防止斜向移動變快）
        moveInput = new Vector3(moveX, 0f, moveZ).normalized;

        bool isMoving = moveInput.sqrMagnitude > 0.001f;

        // 設定動畫參數
        if (animator != null)
        {
            animator.SetFloat("Speed", isMoving ? 1f : 0f);
        }

        // 處理翻轉邏輯
        HandleSpriteFlip(moveX, isMoving);
    }

    private void HandleSpriteFlip(float moveX, bool isMoving)
    {
        if (spriteRenderer == null) return;

        if (isMoving && Mathf.Abs(moveX) > 0.1f)
        {
            // 往右走 (moveX > 0) 設為 true，往左走 (moveX < 0) 設為 false
            // 如果方向反了，請自行調換這兩個 true/false
            spriteRenderer.flipX = (moveX > 0f);
        }
        else if (!isMoving)
        {
            // 待機時回到初始朝向
            spriteRenderer.flipX = initialFlipX;
        }
    }

    void FixedUpdate()
    {
        // 使用 MovePosition 是最平滑的物理移動方式
        if (rb != null)
        {
            Vector3 targetPosition = rb.position + moveInput * moveSpeed * Time.fixedDeltaTime;
            rb.MovePosition(targetPosition);
        }
    }
}