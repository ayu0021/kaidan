using UnityEngine;

public class WinOnWeakPointGone : MonoBehaviour
{
    [Header("Target")]
    public WeakPoint weakPoint;              // 拖你的 WeakPoint 物件（或掛 WeakPoint 的那個）

    [Header("UI")]
    public GameObject winTextObject;         // 拖 Canvas 裡的 Text(TMP) 物件
    public string winMessage = "YOU WIN";

    [Header("End Game")]
    public bool freezeTime = true;           // 停止時間（彈幕/移動會停）
    public bool disableInput = false;        // 你若有 Player input 腳本可再自己關
    public MonoBehaviour[] scriptsToDisable; // 想一起關掉的腳本（可不填）

    bool _won;

    void Start()
    {
        if (winTextObject) winTextObject.SetActive(false);
    }

    void Update()
    {
        if (_won) return;

        // WeakPoint 被 Destroy 或被 SetActive(false)
        if (weakPoint == null || !weakPoint.gameObject.activeInHierarchy)
        {
            TriggerWin();
        }
    }

    void TriggerWin()
    {
        _won = true;

        // 顯示 Win 文字
        if (winTextObject)
        {
            winTextObject.SetActive(true);

            // 若你用 TextMeshProUGUI，這段會自動改字；不是 TMP 也沒關係
            var tmp = winTextObject.GetComponent<TMPro.TMP_Text>();
            if (tmp) tmp.text = winMessage;

            var uiText = winTextObject.GetComponent<UnityEngine.UI.Text>();
            if (uiText) uiText.text = winMessage;
        }

        // 關掉指定腳本（例如 BulletSpawner、PlayerCutTwoStage）
        if (scriptsToDisable != null)
        {
            foreach (var s in scriptsToDisable)
                if (s) s.enabled = false;
        }

        // 停止遊戲時間
        if (freezeTime) Time.timeScale = 0f;

        Debug.Log("[WIN] WeakPoint is gone -> WIN");
    }
}
