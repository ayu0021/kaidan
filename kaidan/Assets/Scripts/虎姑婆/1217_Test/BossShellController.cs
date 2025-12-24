using UnityEngine;

public class BossShellController : MonoBehaviour
{
    [Header("Refs")]
    public Transform shellPiecesRoot;   // 例如：WeakShell 或 ShellPieces
    public InnerShield innerShield;     // InnerShield 物件上的腳本
    public WeakPoint weakPoint;         // WeakPoint 物件上的腳本

    int _remaining;

    void Awake()
    {
        if (!shellPiecesRoot) shellPiecesRoot = transform;

        _remaining = shellPiecesRoot.GetComponentsInChildren<ShellShard>(true).Length;

        if (weakPoint) weakPoint.SetDamageEnabled(false);
        if (innerShield) innerShield.SetUnlocked(false);
    }

    public void OnShardDetached(ShellShard shard)
    {
        _remaining = Mathf.Max(0, _remaining - 1);

        if (_remaining == 0)
        {
            if (innerShield) innerShield.SetUnlocked(true);
        }
    }

    public void OnInnerShieldBroken()
    {
        if (weakPoint) weakPoint.SetDamageEnabled(true);
    }

    public bool ShellCleared => _remaining == 0;
}
