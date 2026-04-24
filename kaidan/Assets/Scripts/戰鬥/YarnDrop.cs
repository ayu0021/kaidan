using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class YarnDrop : MonoBehaviour
{
    [Header("Refs")]
    public GameObject warningDecal;
    public Transform yarnVisual;

    private Collider damageTrigger;
    private int damage;
    private float startHeight;
    private float warningTime;
    private float fallTime;
    private float activeDamageTime;

    private bool setupDone;
    private bool damageWindowOpen;
    private bool alreadyHitPlayer;

    public void Setup(int dmg, float height, float warnTime, float fallDuration, float activeTime)
    {
        damage = dmg;
        startHeight = height;
        warningTime = warnTime;
        fallTime = fallDuration;
        activeDamageTime = activeTime;
        setupDone = true;
    }

    void Awake()
    {
        damageTrigger = GetComponent<Collider>();
        damageTrigger.isTrigger = true;
        damageTrigger.enabled = false;

        if (warningDecal != null)
            warningDecal.SetActive(false);

        if (yarnVisual != null)
            yarnVisual.gameObject.SetActive(false);
    }

    void Start()
    {
        if (!setupDone)
        {
            // 保底預設值，避免忘記 Setup
            Setup(1, 5f, 0.8f, 0.25f, 0.2f);
        }

        StartCoroutine(DropRoutine());
    }

    private IEnumerator DropRoutine()
    {
        alreadyHitPlayer = false;
        damageWindowOpen = false;

        if (warningDecal != null)
            warningDecal.SetActive(true);

        if (yarnVisual != null)
        {
            yarnVisual.gameObject.SetActive(true);
            yarnVisual.localPosition = Vector3.up * startHeight;
        }

        yield return new WaitForSeconds(warningTime);

        if (warningDecal != null)
            warningDecal.SetActive(false);

        float t = 0f;
        Vector3 startLocalPos = Vector3.up * startHeight;

        while (t < fallTime)
        {
            t += Time.deltaTime;

            if (yarnVisual != null)
            {
                float p = Mathf.Clamp01(t / fallTime);
                yarnVisual.localPosition = Vector3.Lerp(startLocalPos, Vector3.zero, p);
            }

            yield return null;
        }

        if (yarnVisual != null)
            yarnVisual.localPosition = Vector3.zero;

        damageWindowOpen = true;
        damageTrigger.enabled = true;

        yield return new WaitForSeconds(activeDamageTime);

        damageWindowOpen = false;
        damageTrigger.enabled = false;

        yield return new WaitForSeconds(0.1f);
        Destroy(gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!damageWindowOpen) return;
        if (alreadyHitPlayer) return;

        BattlePlayerController player = other.GetComponent<BattlePlayerController>();
        if (player == null)
            player = other.GetComponentInParent<BattlePlayerController>();

        if (player != null)
        {
            alreadyHitPlayer = true;
            player.TakeDamage(damage);
        }
    }
}