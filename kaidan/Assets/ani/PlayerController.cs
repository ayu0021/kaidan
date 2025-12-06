using UnityEngine;

[RequireComponent(typeof(Animator))]
public class PlayerController3D_NoIdleFlip : MonoBehaviour
{
    [Header("移動速度")]
    public float moveSpeed = 3f;

    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private Rigidbody rb;

    private Vector3 moveInput;

    // Idle 時要維持的初始方向
    private bool initialFlipX;

    void Start()
    {
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        rb = GetComponent<Rigidbody>();

        if (rb != null)
        {
            // 防止物理把角色轉歪
            rb.constraints = RigidbodyConstraints.FreezeRotation;
        }

        // 記住一開始在 Inspector 設好的方向
        if (spriteRenderer != null)
        {
            initialFlipX = spriteRenderer.flipX;
        }
    }

    void Update()
    {
        // 取得輸入
        float moveX = Input.GetAxisRaw("Horizontal");
        float moveZ = Input.GetAxisRaw("Vertical");

        moveInput = new Vector3(moveX, 0f, moveZ).normalized;
        bool isMoving = moveInput.sqrMagnitude > 0.0001f;

        // 用 Speed 讓 Animator 切換 Idle / Walk
        if (animator != null)
        {
            animator.SetFloat("Speed", isMoving ? 1f : 0f);
        }

        if (spriteRenderer == null) return;

        if (isMoving)
        {
            // ★ 只有在移動時依左右改方向
            if (moveX > 0.01f)
            {
                // 往右走時的方向（如果畫面反了，就把 true / false 對調）
                spriteRenderer.flipX = true;
            }
            else if (moveX < -0.01f)
            {
                // 往左走時的方向
                spriteRenderer.flipX = false;
            }
            // moveX == 0（只前/後走）就不要改方向
        }
        else
        {
            // ★ 完全沒在移動 → 一律回到初始 Idle 方向
            spriteRenderer.flipX = initialFlipX;
        }
    }

    void FixedUpdate()
    {
        if (rb != null)
        {
            Vector3 newPosition = rb.position + moveInput * moveSpeed * Time.fixedDeltaTime;
            rb.MovePosition(newPosition);
        }
        else
        {
            transform.position += moveInput * moveSpeed * Time.fixedDeltaTime;
        }
    }
}
