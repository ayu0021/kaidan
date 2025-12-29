using UnityEngine;
using UnityEngine.SceneManagement;

public class ChangeSceneOnTrigger : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        // 確認是玩家碰到
        if (other.CompareTag("Player"))
        {
            SceneManager.LoadScene("start");
        }
    }
}
