using System.Collections;
using UnityEngine;

public class BossShellController : MonoBehaviour
{
    [Header("Refs")]
    public Transform shellPiecesRoot;
    public InnerShieldBarrier innerShield;
    public WeakPoint weakPoint;

    [Header("Debug")]
    public bool showDebugLog = true;

    int _remaining;
    bool _initialized;
    bool _clearingStarted;
    bool _weakPointUnlocked;

    void Start()
    {
        Init();
    }

    void Init()
    {
        if (_initialized) return;
        _initialized = true;

        if (!shellPiecesRoot)
            shellPiecesRoot = transform;

        ShellShard[] shards = shellPiecesRoot.GetComponentsInChildren<ShellShard>(true);
        _remaining = shards.Length;

        foreach (ShellShard shard in shards)
        {
            if (shard)
                shard.BindController(this);
        }

        if (weakPoint)
            weakPoint.SetDamageEnabled(false);

        if (innerShield)
            innerShield.SetLocked(true);

        if (showDebugLog)
            Debug.Log($"[BossShellController] 初始化完成，ShellShard 數量 = {_remaining}", this);

        if (_remaining <= 0)
        {
            if (showDebugLog)
                Debug.LogWarning("[BossShellController] 沒有找到 ShellShard，直接開啟 WeakPoint。", this);

            UnlockWeakPoint();
        }
    }

    public void OnShardDetached(ShellShard shard)
    {
        Init();

        if (_weakPointUnlocked) return;

        _remaining = Mathf.Max(0, _remaining - 1);

        if (showDebugLog)
            Debug.Log($"[BossShellController] 殼被剪下：{shard.name}，剩餘 {_remaining} 片", shard);

        if (_remaining <= 0 && !_clearingStarted)
            StartCoroutine(ClearInnerShieldThenUnlock());
    }

    IEnumerator ClearInnerShieldThenUnlock()
    {
        _clearingStarted = true;

        if (showDebugLog)
            Debug.Log("[BossShellController] 全部外殼已清除，開始解除護盾。", this);

        if (innerShield)
            yield return innerShield.Vanish();

        UnlockWeakPoint();
    }

    public void UnlockWeakPoint()
    {
        if (_weakPointUnlocked) return;

        _weakPointUnlocked = true;
        _remaining = 0;

        if (innerShield)
            innerShield.SetLocked(false);

        if (weakPoint)
            weakPoint.SetDamageEnabled(true);

        if (showDebugLog)
            Debug.Log("[BossShellController] WeakPoint 已開放攻擊。", this);
    }

    public bool ShellCleared => _weakPointUnlocked || _remaining <= 0;

    public int RemainingShellCount => _remaining;

    public void OnInnerShieldBroken()
    {
        UnlockWeakPoint();
    }

    [ContextMenu("TEST/Force Unlock WeakPoint")]
    public void ForceUnlockWeakPoint()
    {
        StopAllCoroutines();
        UnlockWeakPoint();
    }
}