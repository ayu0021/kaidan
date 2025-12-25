using System.Collections;
using UnityEngine;

public class BossShellController : MonoBehaviour
{
    [Header("Refs")]
    public Transform shellPiecesRoot;          // 指到 WeakShell
    public InnerShieldBarrier innerShield;     // 指到半圓護罩 InnerShield(掛此腳本)
    public WeakPoint weakPoint;               // 指到 WeakPoint(掛WeakPoint腳本)

    int _remaining;

    void Awake()
    {
        if (!shellPiecesRoot) shellPiecesRoot = transform;

        _remaining = shellPiecesRoot.GetComponentsInChildren<ShellShard>(true).Length;

        if (weakPoint) weakPoint.SetDamageEnabled(false);
        if (innerShield) innerShield.SetLocked(true);
    }

    public void OnShardDetached(ShellShard shard)
    {
        _remaining = Mathf.Max(0, _remaining - 1);

        if (_remaining == 0)
        {
            StartCoroutine(ClearInnerShieldThenUnlock());
        }
    }

    IEnumerator ClearInnerShieldThenUnlock()
    {
        if (innerShield)
            yield return innerShield.Vanish();  // 半圓護罩消失（含粒子/淡出）

        if (weakPoint)
            weakPoint.SetDamageEnabled(true);
    }

    public bool ShellCleared => _remaining == 0;

    // 相容舊版：避免你專案裡有東西還在呼叫它造成編譯錯
    public void OnInnerShieldBroken()
    {
        if (weakPoint) weakPoint.SetDamageEnabled(true);
    }
}


