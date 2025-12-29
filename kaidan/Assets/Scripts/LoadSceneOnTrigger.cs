using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoadSceneWithFade : MonoBehaviour
{
    [Header("要跳轉的場景名稱")]
    public string targetScene;

    [Header("只有指定 Tag 可觸發")]
    public string playerTag = "Player";

    [Header("Fade Panel（Image）")]
    public Image fadeImage;

    [Header("漸暗時間（秒）")]
    public float fadeDuration = 1.0f;

    private bool isTransitioning = false;

    private void Start()
    {
        // 確保一開始是透明的
        if (fadeImage != null)
        {
            Color c = fadeImage.color;
            c.a = 0f;
            fadeImage.color = c;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isTransitioning) return;
        if (!other.CompareTag(playerTag)) return;

        StartCoroutine(FadeAndLoadScene());
    }

    IEnumerator FadeAndLoadScene()
    {
        isTransitioning = true;

        float timer = 0f;
        Color color = fadeImage.color;

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            color.a = Mathf.Lerp(0f, 1f, timer / fadeDuration);
            fadeImage.color = color;
            yield return null;
        }

        color.a = 1f;
        fadeImage.color = color;

        SceneManager.LoadScene(targetScene);
    }
}
