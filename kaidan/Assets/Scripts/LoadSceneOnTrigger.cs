using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadSceneOnTrigger : MonoBehaviour
{
    [Header("轉場布幕_")]
    public string targetScene = "轉場布幕_";

    [Header("限制只有 Player 會觸發")]
    public string playerTag = "Player";

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        SceneManager.LoadScene(targetScene);
    }
}
