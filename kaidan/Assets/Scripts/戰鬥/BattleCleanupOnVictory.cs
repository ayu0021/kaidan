using UnityEngine;

public class BattleCleanupOnVictory : MonoBehaviour
{
    [Header("Disable Controllers")]
    public MonoBehaviour[] scriptsToDisable;

    [Header("Cleanup Keywords")]
    public string[] destroyNameContains =
    {
        "Needle",
        "Bullet",
        "Yarn",
        "Drop",
        "Particle System"
    };

    [Header("Keep These Roots")]
    public GameObject[] neverDestroy;

    public void Cleanup()
    {
        Debug.Log("[BattleCleanupOnVictory] Cleanup 被呼叫，開始清場。", this);

        // 1. 停腳本
        foreach (MonoBehaviour script in scriptsToDisable)
        {
            if (script)
            {
                script.enabled = false;
                Debug.Log("[BattleCleanupOnVictory] 停用腳本：" + script.name, script);
            }
        }

        // 2. 停所有粒子
        ParticleSystem[] particles = FindObjectsOfType<ParticleSystem>(true);
        foreach (ParticleSystem ps in particles)
        {
            if (!ps) continue;
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        // 3. 清掉場上 Clone / Bullet / Yarn
        GameObject[] all = FindObjectsOfType<GameObject>(true);

        int count = 0;

        foreach (GameObject obj in all)
        {
            if (!obj) continue;
            if (ShouldKeep(obj)) continue;

            string n = obj.name;

            bool shouldDestroy = false;

            foreach (string key in destroyNameContains)
            {
                if (string.IsNullOrEmpty(key)) continue;

                if (n.Contains(key))
                {
                    shouldDestroy = true;
                    break;
                }
            }

            if (!shouldDestroy) continue;

            // 不要刪掉原始 Root，只刪 Clone 或場上生成物
            if (!n.Contains("(Clone)") && obj.scene.IsValid())
            {
                // 保留場景裡的 Prefab Root，例如 NeedleBulletRoot / YarnDropRoot
                if (n == "NeedleBulletRoot" || n == "YarnDropRoot")
                    continue;
            }

            Destroy(obj);
            count++;
        }

        Debug.Log($"[BattleCleanupOnVictory] 清場完成，刪除物件數：{count}", this);
    }

    bool ShouldKeep(GameObject obj)
    {
        if (neverDestroy == null) return false;

        foreach (GameObject keep in neverDestroy)
        {
            if (!keep) continue;

            if (obj == keep) return true;
            if (obj.transform.IsChildOf(keep.transform)) return true;
        }

        return false;
    }
}