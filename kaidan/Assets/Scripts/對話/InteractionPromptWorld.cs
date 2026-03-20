using TMPro;
using UnityEngine;

public class InteractionPromptWorld : MonoBehaviour
{
    // 用欄位而不是 Property，方便外部強制補回
    public static InteractionPromptWorld Instance;

    [Header("Refs")]
    public Transform root;                 // 可留空，會用自己
    public SpriteRenderer eyeRenderer;     // Eye 的 SpriteRenderer
    public Animator eyeAnimator;           // Eye 的 Animator（眨眼 loop）
    public TextMeshPro text3D;             // 3D TMP

    [Header("Follow")]
    public bool faceCamera = true;
    public Vector3 defaultOffset = new Vector3(0.6f, 1.2f, 0f);

    // runtime
    private Transform _target;
    private Vector3 _offset;
    private Transform _owner;

    private void Awake()
    {
        Instance = this;

        if (root == null) root = transform;

        // 「靠近就一直眨」：Animator 開著，預設 state 播 loop clip 即可
        if (eyeAnimator != null) eyeAnimator.enabled = true;

        // 保險：refs 沒接就自動抓（避免你忘了拖）
        AutoWireRefsIfMissing();

        HideImmediate();
        Debug.Log($"[InteractionPromptWorld] Awake OK: {name}", this);
    }

    private void LateUpdate()
    {
        if (_target == null) return;

        root.position = _target.position + _offset;

        if (faceCamera && Camera.main != null)
            root.forward = Camera.main.transform.forward;
    }

    public void Show(Transform followTarget, string message, Vector3? worldOffset = null, bool showEye = true, Transform owner = null)
    {
        if (followTarget == null) return;

        _target = followTarget;
        _offset = worldOffset ?? defaultOffset;
        _owner = owner != null ? owner : followTarget;

        if (text3D != null) text3D.text = message ?? "";

        if (eyeRenderer != null) eyeRenderer.enabled = showEye;
        if (eyeAnimator != null) eyeAnimator.enabled = showEye;

        if (root != null) root.gameObject.SetActive(true);
    }

    public void Hide(Transform requestFrom = null)
    {
        if (requestFrom != null && requestFrom != _owner) return;

        _target = null;
        _owner = null;

        if (root != null) root.gameObject.SetActive(false);
    }

    private void HideImmediate()
    {
        _target = null;
        _owner = null;
        if (root != null) root.gameObject.SetActive(false);
    }

    private void AutoWireRefsIfMissing()
    {
        // 只在沒接的時候抓，避免覆蓋你手動指定
        if (eyeRenderer == null)
        {
            var sr = GetComponentInChildren<SpriteRenderer>(true);
            if (sr != null && sr.gameObject.name.ToLower().Contains("eye")) eyeRenderer = sr;
        }

        if (eyeAnimator == null)
        {
            var an = GetComponentInChildren<Animator>(true);
            if (an != null && an.gameObject.name.ToLower().Contains("eye")) eyeAnimator = an;
        }

        if (text3D == null)
        {
            var tmp = GetComponentInChildren<TextMeshPro>(true);
            if (tmp != null) text3D = tmp;
        }
    }
}