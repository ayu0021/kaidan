using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class FadeManager : MonoBehaviour
{
    public static FadeManager Instance;

    [Header("FadePanel 的 Animator")]
    public Animator fadeAnimator;

    [Header("FadeOut 動畫時間（秒）")]
    public float fadeDuration = 1f;

    private bool isFading = false;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void FadeToScene(string sceneName)
    {
        if (isFading) return;
        StartCoroutine(FadeAndLoad(sceneName));
    }

    private IEnumerator FadeAndLoad(string sceneName)
    {
        isFading = true;

        fadeAnimator.SetTrigger("FadeOut");

        yield return new WaitForSeconds(fadeDuration);

        SceneManager.LoadScene(sceneName);

        isFading = false;
    }
}
