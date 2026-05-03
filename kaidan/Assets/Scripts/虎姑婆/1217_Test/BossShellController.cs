using System.Collections;
using UnityEngine;

public class BossShellController : MonoBehaviour
{
    [Header("Refs")]
    public Transform shellPiecesRoot;
    public InnerShieldBarrier innerShield;
    public WeakPoint weakPoint;

    [Header("Shell Reveal")]
    public bool revealShellsOverTime = true;
    public int initialVisibleShellCount = 3;
    public float revealIntervalMin = 7f;
    public float revealIntervalMax = 10f;

    [Header("Debug")]
    public bool showDebugLog = true;

    ShellShard[] _shards;
    int _remaining;
    int _visibleShellCount;
    bool _initialized;
    bool _revealStarted;
    bool _clearingStarted;
    bool _weakPointUnlocked;

    void Awake()
    {
        Init();
    }

    void Start()
    {
        Init();
        StartRevealRoutine();
    }

    void Init()
    {
        if (_initialized) return;
        _initialized = true;

        if (!shellPiecesRoot)
            shellPiecesRoot = transform;

        _shards = shellPiecesRoot.GetComponentsInChildren<ShellShard>(true);
        _remaining = _shards.Length;
        _visibleShellCount = revealShellsOverTime
            ? Mathf.Clamp(initialVisibleShellCount, 0, _shards.Length)
            : _shards.Length;

        foreach (ShellShard shard in _shards)
        {
            if (shard)
                shard.BindController(this);
        }

        ApplyShellAvailability();

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

    void StartRevealRoutine()
    {
        if (_revealStarted) return;
        if (!revealShellsOverTime) return;
        if (_shards == null || _shards.Length == 0) return;
        if (_visibleShellCount >= _shards.Length) return;

        _revealStarted = true;
        StartCoroutine(RevealShellsOverTime());
    }

    IEnumerator RevealShellsOverTime()
    {
        while (!_weakPointUnlocked && _visibleShellCount < _shards.Length)
        {
            float wait = Random.Range(
                Mathf.Min(revealIntervalMin, revealIntervalMax),
                Mathf.Max(revealIntervalMin, revealIntervalMax)
            );

            yield return new WaitForSeconds(wait);

            if (_weakPointUnlocked)
                yield break;

            RevealNextShell();
        }
    }

    void RevealNextShell()
    {
        if (_shards == null) return;

        while (_visibleShellCount < _shards.Length)
        {
            ShellShard shard = _shards[_visibleShellCount];
            _visibleShellCount++;

            if (!shard || shard.detached)
                continue;

            shard.SetAvailable(true);

            if (showDebugLog)
                Debug.Log($"[BossShellController] 顯示新的殼片：{shard.name}", shard);

            return;
        }
    }

    void ApplyShellAvailability()
    {
        if (_shards == null) return;

        for (int i = 0; i < _shards.Length; i++)
        {
            ShellShard shard = _shards[i];
            if (!shard) continue;

            shard.SetAvailable(i < _visibleShellCount);
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
