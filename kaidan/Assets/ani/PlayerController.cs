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

    void Start()
    {
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        rb = GetComponent<Rigidbody>();

        if (rb != null)
            rb.constraints = RigidbodyConstraints.FreezeRotation;
    }

    void Update()
    {
        // 取得輸入
        float moveX = Input.GetAxisRaw("Horizontal");
        float moveZ = Input.GetAxisRaw("Vertical");

        moveInput = new Vector3(moveX, 0f, moveZ).normalized;

        // 設定 Animator Speed
        animator.SetFloat("Speed", moveInput.magnitude);

        // 只在 Walk 狀態才翻轉
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        if (stateInfo.IsName("Walk"))
        {
            if (moveX > 0) spriteRenderer.flipX = true;     // 朝右翻轉
            else if (moveX < 0) spriteRenderer.flipX = false; // 朝左不翻轉
        }
        // Idle 狀態或其他動畫完全不改 flipX → 保持預設方向
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
