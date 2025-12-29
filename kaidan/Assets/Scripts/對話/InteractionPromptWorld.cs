using TMPro;
using UnityEngine;

public class InteractionPromptWorld : MonoBehaviour
{
    public static InteractionPromptWorld Instance { get; private set; }

    [Header("Refs")]
    public Transform root;                 // 可留空，會用自己
    public SpriteRenderer eyeRenderer;     // Eye 的 SpriteRenderer
    public Animator eyeAnimator;           // Eye 的 Animator
    public TextMeshPro text3D;             // 3D TMP

    [Header("Follow")]
    public bool faceCamera = true;
    public Vector3 defaultOffset = new Vector3(0.6f, 1.2f, 0f);

    Transform target;
    Vector3 offset;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (root == null) root = transform;
        HideImmediate();
    }

    void LateUpdate()
    {
        if (target == null) return;

        root.position = target.position + offset;

        if (faceCamera && Camera.main != null)
        {
            // 讓提示永遠面向鏡頭（Billboard）
            var cam = Camera.main.transform;
            root.forward = cam.forward;
        }
    }

    public void Show(Transform followTarget, string message, Vector3? worldOffset = null, bool playEye = true)
    {
        target = followTarget;
        offset = worldOffset ?? defaultOffset;

        if (text3D != null)
            text3D.text = message ?? "";

        if (eyeRenderer != null)
            eyeRenderer.enabled = playEye;

        if (eyeAnimator != null)
            eyeAnimator.enabled = playEye;

        root.gameObject.SetActive(true);
    }

    public void Hide(Transform requestFrom = null)
    {
        // 只有「目前跟隨的物件」才可以把它關掉，避免多物件搶 UI 時亂關
        if (requestFrom != null && requestFrom != target) return;

        target = null;
        root.gameObject.SetActive(false);
    }

    void HideImmediate()
    {
        target = null;
        if (root != null) root.gameObject.SetActive(false);
    }
}
