using System.Collections;
using UnityEngine;

public class YarnAttackController : MonoBehaviour
{
    [System.Serializable]
    public class WaveSettings
    {
        public float spawnInterval = 1.2f;
        public int dropsPerWave = 2;
        public float startHeight = 5f;
        public float warningTime = 0.8f;
        public float fallTime = 0.25f;
        public float activeDamageTime = 0.2f;
        public int damage = 1;
    }

    [Header("Refs")]
    public YarnDrop yarnDropPrefab;
    public Transform dropParent;

    [Header("場地範圍")]
    public Vector2 arenaXZ = new Vector2(5f, 5f);
    public float groundY = 0f;

    [Header("一般模式")]
    public WaveSettings normalSettings = new WaveSettings();

    [Header("第二階段 / 狂暴模式")]
    public WaveSettings aggressiveSettings = new WaveSettings();

    private bool aggressiveMode;
    private Coroutine attackCoroutine;

    private WaveSettings Current => aggressiveMode ? aggressiveSettings : normalSettings;

    public void ApplyAggressiveMode(bool aggressive)
    {
        aggressiveMode = aggressive;
    }

    public void BeginAttack()
    {
        if (attackCoroutine != null) return;
        attackCoroutine = StartCoroutine(AttackLoop());
    }

    public void StopAttack()
    {
        if (attackCoroutine != null)
        {
            StopCoroutine(attackCoroutine);
            attackCoroutine = null;
        }
    }

    private IEnumerator AttackLoop()
    {
        while (true)
        {
            SpawnWave();
            yield return new WaitForSeconds(Current.spawnInterval);
        }
    }

    private void SpawnWave()
    {
        if (yarnDropPrefab == null) return;

        for (int i = 0; i < Current.dropsPerWave; i++)
        {
            float x = Random.Range(-arenaXZ.x, arenaXZ.x);
            float z = Random.Range(-arenaXZ.y, arenaXZ.y);
            Vector3 spawnPos = new Vector3(x, groundY, z);

            YarnDrop drop = Instantiate(
                yarnDropPrefab,
                spawnPos,
                Quaternion.identity,
                dropParent
            );

            drop.Setup(
                Current.damage,
                Current.startHeight,
                Current.warningTime,
                Current.fallTime,
                Current.activeDamageTime
            );
        }
    }
}