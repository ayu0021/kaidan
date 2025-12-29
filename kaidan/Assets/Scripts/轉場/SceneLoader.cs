using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneLoader : MonoBehaviour
{
    [Header("UI 組件")]
    public Animator transitionAnimator; // 拖入 FadePanel 的 Animator
    public float transitionTime = 1f;   // 漸暗動畫的時間長度

    // --- 給「開始按鈕」使用的方法 ---
    public void StartGame(string sceneName)
    {
        StartCoroutine(LoadLevel(sceneName));
    }

    // --- 給「Trigger 觸發區」使用的方法 ---
    public void LoadNextLevel(string sceneName)
    {
        StartCoroutine(LoadLevel(sceneName));
    }

    IEnumerator LoadLevel(string sceneName)
    {
        // 1. 播放動畫 (觸發 StartFade 參數)
        if (transitionAnimator != null)
        {
            transitionAnimator.SetTrigger("StartFade");
        }

        // 2. 等待動畫播完
        yield return new WaitForSeconds(transitionTime);

        // 3. 切換場景
        SceneManager.LoadScene(sceneName);
    }
}