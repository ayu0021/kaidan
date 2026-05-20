using UnityEngine;

[RequireComponent(typeof(Collider))]
public class NeedleBullet : MonoBehaviour
{
    [Header("Particle Visual")]
    public bool replaceVisualWithParticles = true;
    public bool hideExistingVisualRenderers = true;
    public Color particleColorA = new Color(1f, 0.05f, 0.1f, 1f);
    public Color particleColorB = new Color(0.55f, 0f, 1f, 1f);
    public int particleRate = 85;
    public float particleRadius = 0.45f;
    public Vector2 particleSizeRange = new Vector2(0.16f, 0.42f);
    public Vector2 particleLifetimeRange = new Vector2(0.16f, 0.38f);
    public Vector2 particleSpeedRange = new Vector2(0.25f, 1.25f);
    public bool addFlickerLight = true;
    public float flickerLightRange = 2.4f;
    public float flickerLightIntensity = 1.2f;
    public float flickerSpeed = 22f;

    private Vector3 moveDir;
    private float moveSpeed;
    private float lifeTime;
    private int damage;

    private float timer;
    private bool initialized;
    private ParticleSystem generatedParticles;
    private Light generatedLight;
    private float flickerSeed;

    void Awake()
    {
        flickerSeed = Random.value * 100f;
        EnsureParticleVisual();
    }

    public void Initialize(Vector3 direction, float speed, float life, int dmg)
    {
        moveDir = direction.normalized;
        moveSpeed = speed;
        lifeTime = life;
        damage = dmg;
        timer = 0f;
        initialized = true;
    }

    void Update()
    {
        UpdateParticleFlicker();

        if (!initialized) return;

        transform.position += moveDir * moveSpeed * Time.deltaTime;

        timer += Time.deltaTime;
        if (timer >= lifeTime)
            Destroy(gameObject);
    }

    void EnsureParticleVisual()
    {
        if (!replaceVisualWithParticles || generatedParticles != null) return;

        if (hideExistingVisualRenderers)
            HideExistingRenderers();

        GameObject particleObject = new GameObject("NeedleDangerParticles");
        particleObject.transform.SetParent(transform, false);
        particleObject.transform.localPosition = Vector3.zero;
        particleObject.transform.localRotation = Quaternion.identity;
        particleObject.transform.localScale = Vector3.one;

        generatedParticles = particleObject.AddComponent<ParticleSystem>();

        ParticleSystem.MainModule main = generatedParticles.main;
        main.loop = true;
        main.playOnAwake = true;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.maxParticles = 140;
        main.startLifetime = new ParticleSystem.MinMaxCurve(
            Mathf.Max(0.02f, particleLifetimeRange.x),
            Mathf.Max(particleLifetimeRange.x, particleLifetimeRange.y)
        );
        main.startSpeed = new ParticleSystem.MinMaxCurve(
            Mathf.Max(0f, particleSpeedRange.x),
            Mathf.Max(particleSpeedRange.x, particleSpeedRange.y)
        );
        main.startSize = new ParticleSystem.MinMaxCurve(
            Mathf.Max(0.01f, particleSizeRange.x),
            Mathf.Max(particleSizeRange.x, particleSizeRange.y)
        );
        main.startColor = new ParticleSystem.MinMaxGradient(particleColorA, particleColorB);

        ParticleSystem.EmissionModule emission = generatedParticles.emission;
        emission.enabled = true;
        emission.rateOverTime = new ParticleSystem.MinMaxCurve(Mathf.Max(1, particleRate));
        emission.SetBursts(new[]
        {
            new ParticleSystem.Burst(0f, (short)Mathf.Clamp(particleRate / 3, 6, 60))
        });

        ParticleSystem.ShapeModule shape = generatedParticles.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = Mathf.Max(0.02f, particleRadius);

        ParticleSystem.ColorOverLifetimeModule colorOverLifetime = generatedParticles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(particleColorA, 0f),
                new GradientColorKey(Color.Lerp(particleColorA, particleColorB, 0.5f), 0.45f),
                new GradientColorKey(particleColorB, 1f)
            },
            new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(1f, 0.12f),
                new GradientAlphaKey(0.75f, 0.65f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);

        ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = generatedParticles.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve(
            new Keyframe(0f, 0.4f),
            new Keyframe(0.25f, 1.25f),
            new Keyframe(1f, 0f)
        );
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        ParticleSystem.NoiseModule noise = generatedParticles.noise;
        noise.enabled = true;
        noise.strength = 1.1f;
        noise.frequency = 3.2f;
        noise.scrollSpeed = new ParticleSystem.MinMaxCurve(1.5f);

        ParticleSystemRenderer particleRenderer = particleObject.GetComponent<ParticleSystemRenderer>();
        if (particleRenderer != null)
        {
            particleRenderer.renderMode = ParticleSystemRenderMode.Billboard;
            particleRenderer.sortingOrder = 40;
            particleRenderer.material = CreateParticleMaterial();
        }

        if (addFlickerLight)
        {
            GameObject lightObject = new GameObject("NeedleDangerFlickerLight");
            lightObject.transform.SetParent(transform, false);
            lightObject.transform.localPosition = Vector3.zero;
            generatedLight = lightObject.AddComponent<Light>();
            generatedLight.type = LightType.Point;
            generatedLight.color = Color.Lerp(particleColorA, particleColorB, 0.35f);
            generatedLight.range = flickerLightRange;
            generatedLight.intensity = flickerLightIntensity;
        }

        generatedParticles.Play(true);
    }

    void HideExistingRenderers()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        foreach (Renderer renderer in renderers)
        {
            if (renderer == null || renderer is ParticleSystemRenderer) continue;
            renderer.enabled = false;
        }
    }

    void UpdateParticleFlicker()
    {
        if (generatedLight == null) return;

        float pulse = Mathf.PerlinNoise(flickerSeed, Time.time * Mathf.Max(0.1f, flickerSpeed));
        float spike = Mathf.Abs(Mathf.Sin((Time.time + flickerSeed) * flickerSpeed * 0.9f));
        generatedLight.intensity = flickerLightIntensity * Mathf.Lerp(0.25f, 1.65f, Mathf.Max(pulse, spike * 0.85f));
    }

    Material CreateParticleMaterial()
    {
        Shader shader = Shader.Find("Particles/Standard Unlit");
        if (!shader)
            shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (!shader)
            shader = Shader.Find("Sprites/Default");

        if (!shader)
            return null;

        Material material = new Material(shader);
        material.color = Color.white;
        return material;
    }

    private void OnTriggerEnter(Collider other)
    {
        BattlePlayerController player = other.GetComponent<BattlePlayerController>();
        if (player == null)
            player = other.GetComponentInParent<BattlePlayerController>();

        if (player != null)
        {
            player.TakeDamage(damage);
            Destroy(gameObject);
        }
    }
}
