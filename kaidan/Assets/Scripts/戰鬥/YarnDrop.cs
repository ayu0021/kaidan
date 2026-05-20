using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class YarnDrop : MonoBehaviour
{
    [Header("Refs")]
    public GameObject warningDecal;
    public Transform yarnVisual;
    public GameObject fallingVisualPrefab;

    [Header("Warning Circle")]
    public Color warningColor = new Color(1f, 0f, 0f, 0.48f);
    public float warningStartScale = 2.6f;
    public float warningEndScale = 0.55f;
    public float warningY = 0.02f;

    [Header("Visual")]
    public Vector3 visualLocalOffset = Vector3.zero;
    public Vector3 visualRotationEuler = Vector3.zero;
    public Vector3 visualScale = new Vector3(3f, 3f, 3f);
    public float attackScale = 1f;
    public bool forceVisualRenderersOn = true;
    public bool addFallbackScissorVisual = true;
    public Vector3 fallbackScissorScale = new Vector3(1.8f, 1.8f, 1.8f);

    private Collider damageTrigger;
    private GameObject generatedWarningDecal;
    private GameObject spawnedVisual;
    private Transform activeVisual;
    private int damage;
    private float startHeight;
    private float warningTime;
    private float fallTime;
    private float activeDamageTime;

    private bool setupDone;
    private bool damageWindowOpen;
    private bool alreadyHitPlayer;
    private float baseSphereRadius;
    private Vector3 baseBoxSize;

    public void Setup(int dmg, float height, float warnTime, float fallDuration, float activeTime)
    {
        damage = dmg;
        startHeight = height;
        warningTime = warnTime;
        fallTime = fallDuration;
        activeDamageTime = activeTime;
        setupDone = true;
        ApplyScaleToDamageTrigger();
        ApplyScaleToVisual();
    }

    void Awake()
    {
        damageTrigger = GetComponent<Collider>();
        damageTrigger.isTrigger = true;
        damageTrigger.enabled = false;
        CacheDamageTriggerSize();

        if (warningDecal != null)
            warningDecal.SetActive(false);

        EnsureVisual();

        if (activeVisual != null)
            activeVisual.gameObject.SetActive(false);
    }

    void Start()
    {
        if (!setupDone)
        {
            // 保底預設值，避免忘記 Setup
            Setup(1, 5f, 0.8f, 0.25f, 0.2f);
        }

        StartCoroutine(DropRoutine());
    }

    private IEnumerator DropRoutine()
    {
        alreadyHitPlayer = false;
        damageWindowOpen = false;

        GameObject warning = GetWarningDecal();
        if (warning != null)
        {
            warning.SetActive(true);
            SetWarningScale(warningStartScale);
        }

        EnsureVisual();

        if (activeVisual != null)
        {
            activeVisual.gameObject.SetActive(true);
            activeVisual.localPosition = visualLocalOffset + Vector3.up * startHeight;
        }

        yield return new WaitForSeconds(warningTime);

        float t = 0f;
        Vector3 startLocalPos = visualLocalOffset + Vector3.up * startHeight;

        while (t < fallTime)
        {
            t += Time.deltaTime;

            float p = Mathf.Clamp01(t / fallTime);

            if (activeVisual != null)
            {
                activeVisual.localPosition = Vector3.Lerp(startLocalPos, visualLocalOffset, p);
            }

            SetWarningScale(Mathf.Lerp(warningStartScale, warningEndScale, p));

            yield return null;
        }

        if (activeVisual != null)
            activeVisual.localPosition = visualLocalOffset;

        if (warning != null)
            warning.SetActive(false);

        damageWindowOpen = true;
        damageTrigger.enabled = true;

        yield return new WaitForSeconds(activeDamageTime);

        damageWindowOpen = false;
        damageTrigger.enabled = false;

        yield return new WaitForSeconds(0.1f);
        Destroy(gameObject);
    }

    GameObject GetWarningDecal()
    {
        if (warningDecal != null)
            return warningDecal;

        if (generatedWarningDecal != null)
            return generatedWarningDecal;

        generatedWarningDecal = CreateWarningCircle();
        return generatedWarningDecal;
    }

    GameObject CreateWarningCircle()
    {
        GameObject circle = new GameObject("ScissorDropWarningCircle");
        circle.name = "ScissorDropWarningCircle";
        circle.transform.SetParent(transform, false);
        circle.transform.localPosition = new Vector3(0f, warningY, 0f);
        circle.transform.localScale = Vector3.one;

        MeshFilter meshFilter = circle.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = circle.AddComponent<MeshRenderer>();
        meshFilter.sharedMesh = CreateCircleMesh(64);

        LineRenderer line = circle.AddComponent<LineRenderer>();
        line.useWorldSpace = false;
        line.loop = true;
        line.positionCount = 64;
        line.widthMultiplier = 0.12f;
        line.numCornerVertices = 4;
        line.numCapVertices = 4;

        Shader shader = Shader.Find("Sprites/Default");
        if (!shader)
            shader = Shader.Find("Universal Render Pipeline/Unlit");

        if (shader)
        {
            Color fillColor = warningColor;
            fillColor.a *= 0.35f;

            Material fillMaterial = new Material(shader);
            fillMaterial.color = fillColor;
            meshRenderer.sharedMaterial = fillMaterial;

            Material lineMaterial = new Material(shader);
            lineMaterial.color = warningColor;
            line.sharedMaterial = lineMaterial;
        }

        meshRenderer.sortingOrder = 20;
        line.startColor = warningColor;
        line.endColor = warningColor;
        line.sortingOrder = 21;

        for (int i = 0; i < line.positionCount; i++)
        {
            float angle = Mathf.PI * 2f * i / line.positionCount;
            line.SetPosition(i, new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)));
        }

        circle.SetActive(false);
        return circle;
    }

    Mesh CreateCircleMesh(int segments)
    {
        Mesh mesh = new Mesh();
        mesh.name = "GeneratedDropWarningDisc";

        Vector3[] vertices = new Vector3[segments + 1];
        int[] triangles = new int[segments * 3];

        vertices[0] = Vector3.zero;

        for (int i = 0; i < segments; i++)
        {
            float angle = Mathf.PI * 2f * i / segments;
            vertices[i + 1] = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
        }

        for (int i = 0; i < segments; i++)
        {
            int tri = i * 3;
            triangles[tri] = 0;
            triangles[tri + 1] = i == segments - 1 ? 1 : i + 2;
            triangles[tri + 2] = i + 1;
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    void SetWarningScale(float scale)
    {
        GameObject warning = GetWarningDecal();
        if (warning == null) return;

        float scaled = scale * Mathf.Max(0.05f, attackScale);
        warning.transform.localScale = new Vector3(scaled, scaled, scaled);
    }

    void EnsureVisual()
    {
        if (activeVisual != null) return;

        if (fallingVisualPrefab != null)
        {
            spawnedVisual = Instantiate(fallingVisualPrefab, transform);
            spawnedVisual.name = fallingVisualPrefab.name;
            activeVisual = spawnedVisual.transform;
            activeVisual.localPosition = visualLocalOffset;
            activeVisual.localRotation = Quaternion.Euler(visualRotationEuler);
            ApplyScaleToVisual();
            PrepareVisual(spawnedVisual);
            EnsureFallbackVisualIfNeeded(spawnedVisual.transform);
            return;
        }

        if (yarnVisual != null)
        {
            activeVisual = yarnVisual;
            ApplyScaleToVisual();
            PrepareVisual(activeVisual.gameObject);
            EnsureFallbackVisualIfNeeded(activeVisual);
        }
    }

    void CacheDamageTriggerSize()
    {
        if (damageTrigger is SphereCollider sphere)
            baseSphereRadius = sphere.radius;
        else if (damageTrigger is BoxCollider box)
            baseBoxSize = box.size;
    }

    void ApplyScaleToDamageTrigger()
    {
        float scale = Mathf.Max(0.05f, attackScale);

        if (damageTrigger is SphereCollider sphere)
        {
            if (baseSphereRadius <= 0f)
                baseSphereRadius = sphere.radius;

            sphere.radius = baseSphereRadius * scale;
        }
        else if (damageTrigger is BoxCollider box)
        {
            if (baseBoxSize == Vector3.zero)
                baseBoxSize = box.size;

            box.size = baseBoxSize * scale;
        }
    }

    void ApplyScaleToVisual()
    {
        if (activeVisual == null) return;

        float scale = Mathf.Max(0.05f, attackScale);
        activeVisual.localScale = visualScale * scale;
    }

    void EnsureFallbackVisualIfNeeded(Transform visualRoot)
    {
        if (!addFallbackScissorVisual || !visualRoot) return;

        CreateFallbackScissor(visualRoot);
    }

    void CreateFallbackScissor(Transform parent)
    {
        GameObject root = new GameObject("FallbackScissorVisual");
        root.transform.SetParent(parent, false);
        root.transform.localPosition = Vector3.zero;
        root.transform.localRotation = Quaternion.Euler(0f, 0f, 35f);
        root.transform.localScale = fallbackScissorScale;

        Material bladeMaterial = CreateUnlitMaterial(new Color(0.8f, 0.95f, 1f, 1f));
        Material handleMaterial = CreateUnlitMaterial(new Color(1f, 0.12f, 0.12f, 1f));

        CreateBoxPart(root.transform, "BladeA", new Vector3(0.11f, 1.35f, 0.08f), new Vector3(-0.18f, 0.35f, 0f), new Vector3(0f, 0f, -18f), bladeMaterial);
        CreateBoxPart(root.transform, "BladeB", new Vector3(0.11f, 1.35f, 0.08f), new Vector3(0.18f, 0.35f, 0f), new Vector3(0f, 0f, 18f), bladeMaterial);
        CreateBoxPart(root.transform, "HandleA", new Vector3(0.38f, 0.38f, 0.08f), new Vector3(-0.34f, -0.5f, 0f), Vector3.zero, handleMaterial);
        CreateBoxPart(root.transform, "HandleB", new Vector3(0.38f, 0.38f, 0.08f), new Vector3(0.34f, -0.5f, 0f), Vector3.zero, handleMaterial);
        CreateBoxPart(root.transform, "Joint", new Vector3(0.22f, 0.22f, 0.1f), Vector3.zero, Vector3.zero, handleMaterial);
    }

    void CreateBoxPart(Transform parent, string partName, Vector3 scale, Vector3 localPosition, Vector3 localEuler, Material material)
    {
        GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
        part.name = partName;
        part.transform.SetParent(parent, false);
        part.transform.localPosition = localPosition;
        part.transform.localRotation = Quaternion.Euler(localEuler);
        part.transform.localScale = scale;

        Renderer renderer = part.GetComponent<Renderer>();
        if (renderer && material)
            renderer.sharedMaterial = material;

        Collider collider = part.GetComponent<Collider>();
        if (collider)
            Destroy(collider);
    }

    Material CreateUnlitMaterial(Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (!shader)
            shader = Shader.Find("Sprites/Default");
        if (!shader)
            shader = Shader.Find("Standard");

        if (!shader)
            return null;

        Material material = new Material(shader);
        material.color = color;
        return material;
    }

    void PrepareVisual(GameObject visual)
    {
        if (!visual) return;

        if (forceVisualRenderersOn)
        {
            Transform[] children = visual.GetComponentsInChildren<Transform>(true);
            foreach (Transform child in children)
            {
                if (child)
                    child.gameObject.SetActive(true);
            }

            Renderer[] renderers = visual.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer r in renderers)
            {
                if (!r) continue;

                r.enabled = true;
                r.forceRenderingOff = false;
            }
        }

        Collider[] colliders = visual.GetComponentsInChildren<Collider>(true);
        foreach (Collider c in colliders)
        {
            if (c)
                c.enabled = false;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!damageWindowOpen) return;
        if (alreadyHitPlayer) return;

        BattlePlayerController player = other.GetComponent<BattlePlayerController>();
        if (player == null)
            player = other.GetComponentInParent<BattlePlayerController>();

        if (player != null)
        {
            alreadyHitPlayer = true;
            player.TakeDamage(damage);
        }
    }
}
