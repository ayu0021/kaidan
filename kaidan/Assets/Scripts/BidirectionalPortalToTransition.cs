using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class BidirectionalPortalToTransition : MonoBehaviour
{
    public enum TriggerMode { OnEnter, OnInteract }

    [Header("Transition (固定共用)")]
    public string transitionScene = "轉場布幕_";

    [Header("Two-way Pair (A <-> B)")]
    public string sceneA = "虎姑婆_客廳";
    public string sceneB = "虎姑婆_走廊";

    [Header("Trigger")]
    public TriggerMode mode = TriggerMode.OnEnter;
    public string playerTag = "Player";
    public KeyCode interactKey = KeyCode.E;

    [Header("Optional")]
    public float loadDelay = 0f;
    public bool autoSetupTrigger = true;
    public bool logToConsole = true;

    bool _inside;
    bool _loading;

    void Reset()
    {
        if (autoSetupTrigger) AutoSetupPhysics();
    }

    void OnValidate()
    {
        if (autoSetupTrigger) AutoSetupPhysics();
    }

    void AutoSetupPhysics()
    {
        // 3D
        var col3D = GetComponent<Collider>();
        if (col3D != null)
        {
            col3D.isTrigger = true;
            var rb = GetComponent<Rigidbody>();
            if (rb == null) rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            var meshCol = col3D as MeshCollider;
            if (meshCol != null) meshCol.convex = true;
        }

        // 2D
        var col2D = GetComponent<Collider2D>();
        if (col2D != null)
        {
            col2D.isTrigger = true;
            var rb2d = GetComponent<Rigidbody2D>();
            if (rb2d == null) rb2d = gameObject.AddComponent<Rigidbody2D>();
            rb2d.bodyType = RigidbodyType2D.Kinematic;
        }
    }

    void Update()
    {
        if (_loading) return;
        if (mode == TriggerMode.OnInteract && _inside && Input.GetKeyDown(interactKey))
            StartLoad();
    }

    // 3D
    void OnTriggerEnter(Collider other)
    {
        if (_loading) return;
        if (!other.CompareTag(playerTag)) return;

        _inside = true;
        if (mode == TriggerMode.OnEnter) StartLoad();
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        _inside = false;
    }

    // 2D
    void OnTriggerEnter2D(Collider2D other)
    {
        if (_loading) return;
        if (!other.CompareTag(playerTag)) return;

        _inside = true;
        if (mode == TriggerMode.OnEnter) StartLoad();
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;
        _inside = false;
    }

    // 給你現有互動系統呼叫（可選）
    public void Interact() => StartLoad();

    void StartLoad()
    {
        if (_loading) return;

        string current = SceneManager.GetActiveScene().name;
        string next = ResolveOtherSide(current);

        if (string.IsNullOrWhiteSpace(next))
        {
            Debug.LogError($"[BiPortal] Current scene '{current}' is not sceneA/sceneB. Check inspector: A='{sceneA}', B='{sceneB}'.", this);
            return;
        }

        _loading = true;

        TransitionData.NextScene = next;
        TransitionData.Delay = loadDelay;

        if (logToConsole) Debug.Log($"[BiPortal] {current} -> (Transition) -> {next}", this);

        SceneManager.LoadScene(transitionScene);
    }

    string ResolveOtherSide(string current)
    {
        if (current == sceneA) return sceneB;
        if (current == sceneB) return sceneA;
        return null;
    }
}
