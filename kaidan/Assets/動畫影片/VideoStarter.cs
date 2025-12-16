using UnityEngine;
using UnityEngine.Video;

public class VideoStarter : MonoBehaviour
{
    // 在 Inspector 視窗中，將你的 VideoPlayer 元件拖曳到此欄位
    public VideoPlayer videoPlayer;

    void Start()
    {
        // Start() 在遊戲物件被啟用時，且在第一幀更新前被調用
        // 這確保了當場景載入時，影片會立即開始播放
        if (videoPlayer != null)
        {
            // 準備並播放影片
            videoPlayer.Prepare();
            videoPlayer.Play();
            
            // 也可以使用以下方式，如果影片已經準備好了:
            // videoPlayer.Play();
            
            Debug.Log("影片已開始播放。");
        }
        else
        {
            Debug.LogError("VideoPlayer 元件未賦值給 VideoStarter 腳本！");
        }
    }
}