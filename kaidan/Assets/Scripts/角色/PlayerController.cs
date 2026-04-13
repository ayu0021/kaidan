using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class PlayerController : MonoBehaviour
{
    [Header("移動速度")]
    public float moveSpeed = 3f;

    [Header("防穿模距離")]
    public float skinWidth = 0.02f;

    [Header("障礙層")]
    public LayerMask obstacleMask = ~0;

    [Header("動畫參數")]
    public string paramSpeed = "Speed";
    public string paramIsWalking = "isWalking";
    public string paramIsWalkingUp = "isWalkingUp";
    public string paramFacingRight = "IsFacingRight";

    [Header("角色原圖是否預設朝右")]
    public bool spriteFacesRightByDefault = true;

    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private Rigidbody rb;
    private CapsuleCollider capsule;

    private Vector3 moveInput;

    // 記住一開始待機時的翻轉狀態
    private bool initialFlipX;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        capsule = GetComponent<CapsuleCollider>();

        animator = GetComponentInChildren<Animator>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezeRotation;
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

        // 三種方向動畫判斷
        bool walkingFrontAnim = isMoving && moveZ < -0.1f; // 正面
        bool walkingBackAnim  = isMoving && moveZ >  0.1f; // 背面
        bool walkingLR        = isMoving && !walkingFrontAnim && !walkingBackAnim; // 左右

        HandleSpriteFlip(moveX, isMoving);

        if (animator != null)
        {
            animator.SetFloat(paramSpeed, isMoving ? 1f : 0f);

            // 狀態組合：
            // Idle      = isWalking false, isWalkingUp false
            // Walk      = isWalking true,  isWalkingUp false
            // 行走_正面   = isWalking false, isWalkingUp true
            // Walk_Back = isWalking true,  isWalkingUp true

            if (walkingBackAnim)
            {
                animator.SetBool(paramIsWalking, true);
                animator.SetBool(paramIsWalkingUp, true);
            }
            else if (walkingFrontAnim)
            {
                animator.SetBool(paramIsWalking, false);
                animator.SetBool(paramIsWalkingUp, true);
            }
            else if (walkingLR)
            {
                animator.SetBool(paramIsWalking, true);
                animator.SetBool(paramIsWalkingUp, false);
            }
            else
            {
                animator.SetBool(paramIsWalking, false);
                animator.SetBool(paramIsWalkingUp, false);
            }

            if (spriteRenderer != null)
            {
                bool facingRight = !spriteRenderer.flipX;
                animator.SetBool(paramFacingRight, facingRight);
            }
        }
    }

    private void HandleSpriteFlip(float moveX, bool isMoving)
    {
        if (spriteRenderer == null) return;

        // 只有左右移動時才翻面
        if (isMoving && Mathf.Abs(moveX) > 0.1f)
        {
            if (spriteFacesRightByDefault)
                spriteRenderer.flipX = moveX < 0f;
            else
                spriteRenderer.flipX = moveX > 0f;
        }
        else
        {
            // 待機或純前後移動時，永遠回到初始待機方向
            spriteRenderer.flipX = initialFlipX;
        }
    }

    void FixedUpdate()
    {
        if (rb == null || capsule == null) return;

        Vector3 desiredMove = moveInput * moveSpeed * Time.fixedDeltaTime;
        if (desiredMove.sqrMagnitude < 0.000001f) return;

        Vector3 center = rb.position + capsule.center;

        float scaleXZ = Mathf.Max(transform.localScale.x, transform.localScale.z);
        float radius = capsule.radius * scaleXZ;
        float height = capsule.height * transform.localScale.y;
        height = Mathf.Max(height, radius * 2f);

        float half = Mathf.Max(0f, height * 0.5f - radius);
        Vector3 p1 = center + Vector3.up * half;
        Vector3 p2 = center - Vector3.up * half;

        Vector3 dir = desiredMove.normalized;
        float dist = desiredMove.magnitude;

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