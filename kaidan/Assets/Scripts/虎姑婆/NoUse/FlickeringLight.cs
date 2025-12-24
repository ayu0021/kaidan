using UnityEngine;

[RequireComponent(typeof(Light))]
public class FlickeringLight : MonoBehaviour
{
    [Header("亮度範圍")]
    public float minIntensity = 0.5f;   // 最暗時
    public float maxIntensity = 2.0f;   // 最亮時

    [Header("平常小抖動速度")]
    public float noiseSpeed = 5f;       // 越大變化越快

    [Header("偶爾整顆熄掉設定")]
    [Range(0f, 1f)]
    public float glitchChance = 0.05f;  // 每秒有多少機率出現「壞掉閃一下」
    public float glitchOffTimeMin = 0.05f;
    public float glitchOffTimeMax = 0.2f;

    private Light _light;
    private float _baseIntensity;
    private float _noiseSeed;
    private bool _inGlitch = false;

    void Awake()
    {
        _light = GetComponent<Light>();
        _baseIntensity = _light.intensity;
        _noiseSeed = Random.Range(0f, 1000f);
    }

    void Update()
    {
        if (_inGlitch) return;

        // 1. 平常的小抖動（用 Perlin Noise 做柔和變化）
        float t = Time.time * noiseSpeed + _noiseSeed;
        float noise = Mathf.PerlinNoise(t, 0f);        // 0~1
        float flicker = Mathf.Lerp(minIntensity, maxIntensity, noise);

        _light.intensity = _baseIntensity * flicker;

        // 2. 偶爾突然整顆燈壞掉一下
        if (Random.value < glitchChance * Time.deltaTime)
        {
            StartCoroutine(GlitchOff());
        }
    }

    System.Collections.IEnumerator GlitchOff()
    {
        _inGlitch = true;

        // 記住目前亮度
        float current = _light.intensity;

        // 先突然暗掉
        _light.intensity = 0f;
        float offTime = Random.Range(glitchOffTimeMin, glitchOffTimeMax);
        yield return new WaitForSeconds(offTime);

        // 再突然亮回來
        _light.intensity = current;
        _inGlitch = false;
    }
}
