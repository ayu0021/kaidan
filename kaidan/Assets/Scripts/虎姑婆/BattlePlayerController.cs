using System.Collections;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;     // 只在 Editor 裡才會編譯
#endif

[RequireComponent(typeof(Rigidbody))]
public class BattlePlayerController : MonoBehaviour
{
    [Header("移動")]
    public float moveSpeed = 5f;
    public Vector2 moveLimits = new Vector2(5f, 5f);   // X / Z 邊界

    [Header("玩家血量")]
    public int maxHP = 3;
    private int currentHP;

    [Header("生命 UI（蠟燭）")]
    public Image[] lifeIcons;   // 在 Inspector 把三根蠟燭 Image 拖進來

    [Header("受擊視覺效果")]
    public Color hitColor = Color.red;   // 玩家被打時變成的顏色
    public float flashTotalTime = 0.3f;  // 整個閃爍動畫總長
    public int flashTimes = 2;           // 閃幾下

    [Header("Game Over")]
    public GameObject gameOverPanel;     // 拖 GameOverPanel 進來
    public float gameOverDelay = 2f;     // 顯示 GameOver 幾秒後退出

    private Rigidbody rb;
    private Vector3 moveInput;

    // 玩家本體的 Renderer（用來改顏色）
    private Renderer playerRenderer;
    private Color originalPlayerColor;

    // 蠟燭原本的顏色
    private Color[] originalLifeColors;

    private bool isFlashing = false;
    private bool isDead = false;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezeRotation;

        // 找到玩家身上的 Renderer（BattlePlayer 或子物件 Capsule 都可以）
        playerRenderer = GetComponentInChildren<Renderer>();
        if (playerRenderer != null && playerRenderer.material.HasProperty("_Color"))
        {
            originalPlayerColor = playerRenderer.material.color;
        }

        // 記錄每根蠟燭原本的顏色
        if (lifeIcons != null && lifeIcons.Length > 0)
        {
            originalLifeColors = new Color[lifeIcons.Length];
            for (int i = 0; i < lifeIcons.Length; i++)
            {
                if (lifeIcons[i] != null)
                    originalLifeColors[i] = lifeIcons[i].color;
            }
        }

        currentHP = maxHP;
        UpdateLifeUI();

        // 確保一開始 GameOverPanel 是關掉的
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
    }

    void Update()
    {
        if (isDead) return;  // 死亡後就不再接受操作

        float h = Input.GetAxisRaw("Horizontal");  // A / D
        float v = Input.GetAxisRaw("Vertical");    // W / S

        moveInput = new Vector3(h, 0f, v).normalized;
    }

    void FixedUpdate()
    {
        if (isDead) return;

        Vector3 newPos = rb.position + moveInput * moveSpeed * Time.fixedDeltaTime;

        newPos.x = Mathf.Clamp(newPos.x, -moveLimits.x, moveLimits.x);
        newPos.z = Mathf.Clamp(newPos.z, -moveLimits.y, moveLimits.y);

        rb.MovePosition(newPos);
    }

    // ====== 扣血相關 ======
    public void TakeDamage(int damage)
    {
        if (isDead) return;   // 已經死了就不要再扣

        currentHP -= damage;
        if (currentHP < 0) currentHP = 0;

        Debug.Log($"Player Hit! HP = {currentHP}");
        UpdateLifeUI();

        // 播受擊閃爍（避免重複疊加）
        if (!isFlashing)
        {
            StartCoroutine(HitFlashRoutine());
        }

        if (currentHP <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;

        Debug.Log("Player Dead - Game Over");

        // 顯示 GameOver UI
        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);

        // 停止移動
        moveInput = Vector3.zero;

        // 幾秒後退出遊戲 / 停止 Play
        StartCoroutine(QuitGameAfterDelay());
    }

    IEnumerator QuitGameAfterDelay()
    {
        yield return new WaitForSeconds(gameOverDelay);

        #if UNITY_EDITOR
        // 在 Editor 裡：只是停止 Play 模式
        EditorApplication.isPlaying = false;
        #else
        // Build 出去：真正關閉程式
        Application.Quit();
        #endif
    }

    // 更新右下角蠟燭顯示
    void UpdateLifeUI()
    {
        if (lifeIcons == null || lifeIcons.Length == 0) return;

        for (int i = 0; i < lifeIcons.Length; i++)
        {
            if (lifeIcons[i] == null) continue;

            // i < currentHP → 這根蠟燭「亮著」
            lifeIcons[i].enabled = (i < currentHP);
        }
    }

    // 被子彈撞到
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Bullet"))
        {
            TakeDamage(1);
            Destroy(other.gameObject);
        }
    }

    // ====== 受擊閃爍協程（玩家 + 蠟燭一起閃） ======
    IEnumerator HitFlashRoutine()
    {
        isFlashing = true;

        float singleFlashTime = flashTotalTime / (flashTimes * 2f); // 亮 / 暗 交替

        for (int i = 0; i < flashTimes; i++)
        {
            // 變成受擊顏色
            SetPlayerColor(hitColor);
            SetLifeIconsColor(hitColor);

            yield return new WaitForSeconds(singleFlashTime);

            // 回復原色
            RestorePlayerColor();
            RestoreLifeIconsColor();

            yield return new WaitForSeconds(singleFlashTime);
        }

        isFlashing = false;
    }

    void SetPlayerColor(Color c)
    {
        if (playerRenderer != null && playerRenderer.material.HasProperty("_Color"))
        {
            playerRenderer.material.color = c;
        }
    }

    void RestorePlayerColor()
    {
        if (playerRenderer != null && playerRenderer.material.HasProperty("_Color"))
        {
            playerRenderer.material.color = originalPlayerColor;
        }
    }

    void SetLifeIconsColor(Color c)
    {
        if (lifeIcons == null) return;

        for (int i = 0; i < lifeIcons.Length; i++)
        {
            if (lifeIcons[i] == null) continue;
            if (!lifeIcons[i].enabled) continue;  // 已熄掉的不閃

            lifeIcons[i].color = c;
        }
    }

    void RestoreLifeIconsColor()
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
