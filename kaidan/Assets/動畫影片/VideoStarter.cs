using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using System.Collections; // 需要使用 IEnumerator 來實現協程

public class VideoStarter : MonoBehaviour
{
    // 在 Inspector 視窗中賦值
    public VideoPlayer videoPlayer;
    public Image fadePanel; // 連結到您創建的 FadePanel UI Image
    public float fadeDuration = 2f; // 淡入過渡所需的時間 (秒)

    void Start()
    {
        // 確保影片播放器存在
        if (videoPlayer == null || fadePanel == null)
        {
            Debug.LogError("VideoPlayer 或 FadePanel 未在 Inspector 中賦值！");
            return;
        }

        // 啟動淡入協程
        StartCoroutine(FadeInAndPlayVideo());
    }

    IEnumerator FadeInAndPlayVideo()
    {
        float timer = 0f;
        
        // 步驟 1: 準備影片
        videoPlayer.Prepare();
        // 等待影片準備好 (通常很快，但為了安全等待)
        yield return new WaitUntil(() => videoPlayer.isPrepared);
        
        // 步驟 2: 影片開始播放 (此時畫面仍被黑屏覆蓋)
        videoPlayer.Play(); 
        
        // 步驟 3: 執行淡入效果
        while (timer < fadeDuration)
        {
            // 計算當前的透明度 (從 1 到 0)
            float alpha = Mathf.Lerp(1f, 0f, timer / fadeDuration);
            
            // 設置面板的顏色 (只改變 Alpha 值)
            Color newColor = fadePanel.color;
            newColor.a = alpha;
            fadePanel.color = newColor;
            
            // 增加計時器並等待下一幀
            timer += Time.deltaTime;
            yield return null;
        }

        // 確保最後 Alpha 值精確為 0
        Color finalColor = fadePanel.color;
        finalColor.a = 0f;
        fadePanel.color = finalColor;
        
        // 可選：淡入完成後，禁用黑屏面板 (節省性能)
        fadePanel.gameObject.SetActive(false);

        Debug.Log("淡入完成，影片可見。");
    }
}