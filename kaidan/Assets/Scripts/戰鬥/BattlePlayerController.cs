using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Rigidbody))]
public class BattlePlayerController : MonoBehaviour
{
    [Header("移動")]
    public float moveSpeed = 5f;
    public Vector2 moveLimits = new Vector2(5f, 5f);

    [Header("玩家血量")]
    public int maxHP = 3;
    public float invincibleTime = 1f;

    [Header("傷害免疫")]
    public bool damageImmune;

    [Header("生命 UI")]
    public Image[] lifeIcons;

    [Header("受擊顏色")]
    public Renderer[] playerRenderers;
    public Color hitColor = Color.red;
    public float flashTotalTime = 0.25f;
    public int flashTimes = 2;

    [Header("Game Over")]
    public GameObject gameOverPanel;

    [Header("蠟燭暗時禁止移動")]
    public float forbiddenInputDeadZone = 0.15f;
    public float allowedDriftDistance = 0.03f;
    public bool killImmediatelyOnForbiddenMove = true;

    public event Action OnPlayerDied;

    public int CurrentHP => currentHP;
    public bool IsDead => isDead;
    public bool IsInvincible => damageImmune || invincibleTimer > 0f;

    private Rigidbody rb;
    private Vector3 moveInput;
    private int currentHP;
    private bool isDead;
    private bool isFlashing;
    private float invincibleTimer;

    private bool movementLocked;
    private bool forbidMovementCheck;
    private Vector3 forbiddenStartPos;

    private Color[] originalLifeColors;
    private MaterialPropertyBlock mpb;

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezeRotation;

        mpb = new MaterialPropertyBlock();

        if (playerRenderers == null || playerRenderers.Length == 0)
            playerRenderers = GetComponentsInChildren<Renderer>(true);

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
    }

    void Start()
    {
        currentHP = maxHP;

        if (lifeIcons != null && lifeIcons.Length > 0)
        {
            originalLifeColors = new Color[lifeIcons.Length];
            for (int i = 0; i < lifeIcons.Length; i++)
            {
                if (lifeIcons[i] != null)
                    originalLifeColors[i] = lifeIcons[i].color;
            }
        }

        UpdateLifeUI();
        RestorePlayerColor();
    }

    void Update()
    {
        if (isDead) return;

        if (invincibleTimer > 0f)
            invincibleTimer -= Time.deltaTime;

        float rawH = Input.GetAxisRaw("Horizontal");
        float rawV = Input.GetAxisRaw("Vertical");

        if (forbidMovementCheck)
        {
            bool pressedInput =
                Mathf.Abs(rawH) > forbiddenInputDeadZone ||
                Mathf.Abs(rawV) > forbiddenInputDeadZone;

            bool drifted =
                Vector3.Distance(transform.position, forbiddenStartPos) > allowedDriftDistance;

            if (pressedInput || drifted)
            {
                if (killImmediatelyOnForbiddenMove)
                    DieImmediately();
                else
                    TakeDamage(maxHP);
                return;
            }
        }

        if (movementLocked)
        {
            moveInput = Vector3.zero;
            return;
        }

        moveInput = new Vector3(rawH, 0f, rawV).normalized;
    }

    void FixedUpdate()
    {
        if (isDead) return;
        if (movementLocked) return;

        Vector3 newPos = rb.position + moveInput * moveSpeed * Time.fixedDeltaTime;
        newPos.x = Mathf.Clamp(newPos.x, -moveLimits.x, moveLimits.x);
        newPos.z = Mathf.Clamp(newPos.z, -moveLimits.y, moveLimits.y);

        rb.MovePosition(newPos);
    }

    public void SetMovementLocked(bool locked)
    {
        movementLocked = locked;

        if (locked)
        {
            moveInput = Vector3.zero;
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    public void BeginNoMoveCheck()
    {
        forbidMovementCheck = true;
        forbiddenStartPos = transform.position;
    }

    public void EndNoMoveCheck()
    {
        forbidMovementCheck = false;
    }

    public void SetDamageImmune(bool immune)
    {
        damageImmune = immune;
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;
        if (IsInvincible) return;

        currentHP -= damage;
        if (currentHP < 0) currentHP = 0;

        invincibleTimer = invincibleTime;
        UpdateLifeUI();

        if (!isFlashing)
            StartCoroutine(HitFlashRoutine());

        if (currentHP <= 0)
            Die();
    }

    public void DieImmediately()
    {
        if (isDead) return;

        currentHP = 0;
        UpdateLifeUI();
        Die();
    }

    private void Die()
    {
        if (isDead) return;

        isDead = true;
        moveInput = Vector3.zero;
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);

        OnPlayerDied?.Invoke();
    }

    private void UpdateLifeUI()
    {
        if (lifeIcons == null) return;

        for (int i = 0; i < lifeIcons.Length; i++)
        {
            if (lifeIcons[i] == null) continue;
            lifeIcons[i].enabled = (i < currentHP);
        }
    }

    private IEnumerator HitFlashRoutine()
    {
        isFlashing = true;

        float singleFlashTime = flashTotalTime / (flashTimes * 2f);

        for (int i = 0; i < flashTimes; i++)
        {
            SetPlayerColor(hitColor);
            SetLifeIconsColor(hitColor);
            yield return new WaitForSeconds(singleFlashTime);

            RestorePlayerColor();
            RestoreLifeIconsColor();
            yield return new WaitForSeconds(singleFlashTime);
        }

        isFlashing = false;
    }

    private void SetPlayerColor(Color color)
    {
        if (playerRenderers == null) return;

        foreach (var r in playerRenderers)
        {
            if (r == null || r.sharedMaterial == null) continue;

            r.GetPropertyBlock(mpb);

            if (r.sharedMaterial.HasProperty(BaseColorId))
                mpb.SetColor(BaseColorId, color);
            else if (r.sharedMaterial.HasProperty(ColorId))
                mpb.SetColor(ColorId, color);

            r.SetPropertyBlock(mpb);
        }
    }

    private void RestorePlayerColor()
    {
        if (playerRenderers == null) return;

        foreach (var r in playerRenderers)
        {
            if (r == null || r.sharedMaterial == null) continue;

            Color baseColor = Color.white;

            if (r.sharedMaterial.HasProperty(BaseColorId))
                baseColor = r.sharedMaterial.GetColor(BaseColorId);
            else if (r.sharedMaterial.HasProperty(ColorId))
                baseColor = r.sharedMaterial.GetColor(ColorId);

            r.GetPropertyBlock(mpb);

            if (r.sharedMaterial.HasProperty(BaseColorId))
                mpb.SetColor(BaseColorId, baseColor);
            else if (r.sharedMaterial.HasProperty(ColorId))
                mpb.SetColor(ColorId, baseColor);

            r.SetPropertyBlock(mpb);
        }
    }

    private void SetLifeIconsColor(Color c)
    {
        if (lifeIcons == null) return;

        for (int i = 0; i < lifeIcons.Length; i++)
        {
            if (lifeIcons[i] == null) continue;
            if (!lifeIcons[i].enabled) continue;
            lifeIcons[i].color = c;
        }
    }

    private void RestoreLifeIconsColor()
    {
        if (lifeIcons == null || originalLifeColors == null) return;

        for (int i = 0; i < lifeIcons.Length; i++)
        {
            if (lifeIcons[i] == null) continue;
            if (!lifeIcons[i].enabled) continue;
            lifeIcons[i].color = originalLifeColors[i];
        }
    }
}
