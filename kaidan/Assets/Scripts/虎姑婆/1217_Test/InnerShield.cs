using UnityEngine;

[RequireComponent(typeof(Collider))]
public class InnerShield : MonoBehaviour
{
    public int hp = 3;
    public bool unlocked;

    Collider _col;
    Renderer[] _renderers;
    BossShellController _controller;

    void Awake()
    {
        _col = GetComponent<Collider>();
        _renderers = GetComponentsInChildren<Renderer>(true);
        _controller = GetComponentInParent<BossShellController>();
        unlocked = false;
    }

    public void SetUnlocked(bool on) => unlocked = on;

    public void TakeHit(int dmg = 1)
    {
        if (!unlocked) return;
        hp -= dmg;
        if (hp <= 0) Break();
    }

    void Break()
    {
        if (_col) _col.enabled = false;
        foreach (var r in _renderers) if (r) r.enabled = false;

        if (_controller) _controller.OnInnerShieldBroken();
    }
}
