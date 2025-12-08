using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections; // ← 這行一定要加！

public class MenuManager : MonoBehaviour
{
    public Animator fadeAnimator; // 指向 FadePanel 的 Animator

    public void StartGame()
    {
        StartCoroutine(FadeAndLoad());
    }

    private IEnumerator FadeAndLoad()
    {
        fadeAnimator.SetTrigger("FadeOut");

        yield return new WaitForSeconds(1f);

        SceneManager.LoadScene("FirstWoods");
    }

    public void QuitGame()
    {
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
