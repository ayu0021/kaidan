using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

[DisallowMultipleComponent]
public class TransitionControllerVideo : MonoBehaviour
{
    [Header("Video")]
    public VideoPlayer videoPlayer;

    [Tooltip("若沒設 VideoPlayer/Clip，才用這個保底等待秒數")]
    public float fallbackDelay = 1.0f;

    bool _loading;

    void Awake()
    {
        // 避免有人把 Time.timeScale 改成 0，導致等待/播放行為怪怪的
        Time.timeScale = 1f;
    }

    void OnEnable()
    {
        if (videoPlayer != null)
            videoPlayer.loopPointReached += OnVideoFinished;
    }

    void OnDisable()
    {
        if (videoPlayer != null)
            videoPlayer.loopPointReached -= OnVideoFinished;
    }

    IEnumerator Start()
    {
        if (string.IsNullOrWhiteSpace(TransitionData.NextScene))
        {
            Debug.LogError("[Transition] NextScene is empty. 你可能是直接進轉場場景，沒有從 Portal 進來。");
            yield break;
        }

        // ✅ 有影片就播影片，播完靠事件切場（不靠猜秒數）
        if (videoPlayer != null && videoPlayer.clip != null)
        {
            videoPlayer.playOnAwake = false;
            videoPlayer.isLooping = false;
            videoPlayer.waitForFirstFrame = true;

            videoPlayer.Prepare();
            while (!videoPlayer.isPrepared) yield return null;

            videoPlayer.Play();
            yield break; // 影片播完會走 OnVideoFinished
        }

        // ✅ 沒影片才用保底等待
        yield return new WaitForSeconds(fallbackDelay);
        LoadNext();
    }

    void OnVideoFinished(VideoPlayer vp)
    {
        LoadNext();
    }

    void LoadNext()
    {
        if (_loading) return;
        _loading = true;

        string target = TransitionData.NextScene;

        // 清掉避免下次誤用
        TransitionData.NextScene = null;
        TransitionData.Delay = 0f;

        SceneManager.LoadScene(target);
    }
}
