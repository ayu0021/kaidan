using UnityEngine;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class PlayerController : MonoBehaviour
{
    [Header("移動速度")]
    public float moveSpeed = 5f;

    [Header("防穿模：留一點皮膚距離")]
    public float skinWidth = 0.02f;

    [Header("要阻擋的層（家具/牆壁）")]
    public LayerMask obstacleMask = ~0; // 預設全部都算（你也可以只勾 Obstacles）

    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private Rigidbody rb;
    private CapsuleCollider capsule;

    private Vector3 moveInput;
    private bool initialFlipX;

    void Start()
    {
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        rb = GetComponent<Rigidbody>();
        capsule = GetComponent<CapsuleCollider>();

        // 物理設定：穩定、不穿模、不卡
        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezeRotationX |
                         RigidbodyConstraints.FreezeRotationY |
                         RigidbodyConstraints.FreezeRotationZ;

        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        if (spriteRenderer != null)
            initialFlipX = spriteRenderer.flipX;
    }

    void Update()
    {
        float moveX = Input.GetAxisRaw("Horizontal");
        float moveZ = Input.GetAxisRaw("Vertical");

        moveInput = new Vector3(moveX, 0f, moveZ).normalized;

        bool isMoving = moveInput.sqrMagnitude > 0.001f;

        if (animator != null)
            animator.SetFloat("Speed", isMoving ? 1f : 0f);

        HandleSpriteFlip(moveX, isMoving);
    }

    private void HandleSpriteFlip(float moveX, bool isMoving)
    {
        if (spriteRenderer == null) return;

        if (isMoving && Mathf.Abs(moveX) > 0.1f)
            spriteRenderer.flipX = (moveX > 0f);
        else if (!isMoving)
            spriteRenderer.flipX = initialFlipX;
    }

    void FixedUpdate()
    {
        if (rb == null) return;

        Vector3 desiredMove = moveInput * moveSpeed * Time.fixedDeltaTime;
        if (desiredMove.sqrMagnitude < 0.000001f) return;

        // 取得膠囊在世界座標的兩端點
        Vector3 center = rb.position + capsule.center;
        float radius = Mathf.Max(0.0001f, capsule.radius * Mathf.Max(transform.localScale.x, transform.localScale.z));
        float height = Mathf.Max(radius * 2f, capsule.height * transform.localScale.y);

        // 膠囊上下端點（Y軸）
        float half = Mathf.Max(0f, (height * 0.5f) - radius);
        Vector3 p1 = center + Vector3.up * half;
        Vector3 p2 = center - Vector3.up * half;

        Vector3 dir = desiredMove.normalized;
        float dist = desiredMove.magnitude;

        // 先掃掠，看看前方會不會撞到（忽略 Trigger）
        if (Physics.CapsuleCast(p1, p2, radius, dir, out RaycastHit hit, dist + skinWidth, obstacleMask, QueryTriggerInteraction.Ignore))
        {
            float safeDist = Mathf.Max(0f, hit.distance - skinWidth);
            rb.MovePosition(rb.position + dir * safeDist);
        }
        else
        {
            rb.MovePosition(rb.position + desiredMove);
        }
    }
}
