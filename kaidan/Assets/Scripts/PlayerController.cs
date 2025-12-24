using UnityEngine;

[RequireComponent(typeof(Animator))]
public class PlayerController : MonoBehaviour
{
    [Header("移動速度")]
    public float moveSpeed = 3f;

    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private Rigidbody rb;

    private Vector3 moveInput;

    // Idle 要保持的初始方向
    private bool initialFlipX;

    void Start()
    {
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        rb = GetComponent<Rigidbody>();

        if (rb != null)
            rb.constraints = RigidbodyConstraints.FreezeRotation;

        // ★ 記住一開始的朝向（在 Inspector 裡調到你要的 Idle 方向）
        if (spriteRenderer != null)
            initialFlipX = spriteRenderer.flipX;
    }

    void Update()
    {
        float moveX = Input.GetAxisRaw("Horizontal");
        float moveZ = Input.GetAxisRaw("Vertical");

        moveInput = new Vector3(moveX, 0f, moveZ).normalized;

        bool isMoving = moveInput.sqrMagnitude > 0.0001f;

        // 給 Animator 用的 Speed 參數（如果你有做走路／待機切換）
        animator.SetFloat("Speed", isMoving ? 1f : 0f);

        if (isMoving)
        {
            // ===== Walk 狀態：依左右決定方向 =====
            if (moveX > 0f)
            {
                // 往「世界右邊」走的時候要長怎樣
                spriteRenderer.flipX = true;   // 如果這樣翅膀在錯邊，就改成 false
            }
            else if (moveX < 0f)
            {
                // 往「世界左邊」走的時候要長怎樣
                spriteRenderer.flipX = false;  // 如果這樣錯，就改成 true
            }
            // moveX == 0 例如只前後走，就不改方向
        }
        else
        {
            // ===== Idle 狀態：永遠回到初始設定 =====
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
