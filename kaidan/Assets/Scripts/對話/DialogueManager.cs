using System.Collections;
using TMPro;
using UnityEngine;

public class DialogueManager : MonoBehaviour
{
    [Header("Skin")]
    public DialogueUISkin defaultSkin;
    [Tooltip("UI 生成後要掛在哪個父物件下（通常是 Canvas）。可不填，不填就放在自己底下。")]
    public Transform uiParent;

    [Header("Input")]
    public bool allowSpace = true;
    public bool allowMouseClick = true;
    public KeyCode advanceKey = KeyCode.Space;

    [Header("Typewriter")]
    public bool useTypewriter = true;
    [Tooltip("字/秒（當 Skin.charsPerSecond <= 0 時用這個）")]
    public float charsPerSecond = 40f;

    [Header("No auto text")]
    [Tooltip("不自動帶 Character 名字；Name 欄位沒寫就顯示空")]
    public bool autoFillNameFromCharacter = false;

    private DialogueUIRefs _ui;
    private Coroutine _routine;
    private bool _isPlaying;
    private bool _isTyping;
    private bool _waitingExtraClose;
    private float _activeCps;
    private DialogueUISkin _activeSkin;

    // 記住 Portrait 在 prefab 的「原始」位置/縮放，避免每句累加 or 被上一個角色污染
    private bool _portraitBaseCaptured;
    private Vector3 _portraitBaseLocalScale;
    private Vector2 _portraitBaseAnchoredPos;

    // ===== Public API =====

    public void Play(DialogueAsset asset, DialogueUISkin skinOverride = null)
    {
        if (asset == null)
        {
            Debug.LogWarning("[DialogueManager] Play called with null asset.");
            return;
        }

        Stop();

        _activeSkin = skinOverride != null ? skinOverride : defaultSkin;
        if (_activeSkin == null || _activeSkin.uiPrefab == null)
        {
            Debug.LogError("[DialogueManager] No DialogueUISkin / uiPrefab assigned.");
            return;
        }

        EnsureUIInstance(_activeSkin);

        _ui.ClearAll();
        _ui.RootGO.SetActive(true);

        _activeCps = (_activeSkin.charsPerSecond > 0f) ? _activeSkin.charsPerSecond : charsPerSecond;
        if (_activeCps <= 0f) _activeCps = 40f;

        _routine = StartCoroutine(PlayRoutine(asset));
    }

    public void Stop()
    {
        if (_routine != null)
        {
            StopCoroutine(_routine);
            _routine = null;
        }

        _isPlaying = false;
        _isTyping = false;
        _waitingExtraClose = false;

        if (_ui != null && _activeSkin != null && _activeSkin.hideRootOnClose)
        {
            _ui.RootGO.SetActive(false);
        }
    }

    // ===== Internals =====

    private void EnsureUIInstance(DialogueUISkin skin)
    {
        if (_ui != null) return;

        Transform parent = uiParent != null ? uiParent : transform;
        _ui = Instantiate(skin.uiPrefab, parent);
        _ui.name = $"DialogueUI({skin.name})";

        CapturePortraitBase();
    }

    private void CapturePortraitBase()
    {
        _portraitBaseCaptured = false;

        if (_ui == null || _ui.portraitImage == null) return;

        var rt = _ui.portraitImage.rectTransform;
        _portraitBaseLocalScale = rt.localScale;
        _portraitBaseAnchoredPos = rt.anchoredPosition;
        _portraitBaseCaptured = true;
    }

    private IEnumerator PlayRoutine(DialogueAsset asset)
    {
        _isPlaying = true;

        if (asset.lines == null || asset.lines.Count == 0)
        {
            Debug.LogWarning("[DialogueManager] DialogueAsset has no lines.");
            CloseUI();
            yield break;
        }

        for (int i = 0; i < asset.lines.Count; i++)
        {
            var line = asset.lines[i];
            ApplyLineToUI(line);

            yield return StartCoroutine(TypeLine(GetBodyText(line)));

            yield return StartCoroutine(WaitForAdvance());
        }

        if (_activeSkin != null && _activeSkin.requireExtraPressToClose)
        {
            _waitingExtraClose = true;
            yield return StartCoroutine(WaitForAdvance());
        }

        CloseUI();
    }

    private void CloseUI()
    {
        _isPlaying = false;
        _isTyping = false;
        _waitingExtraClose = false;

        if (_activeSkin != null && _activeSkin.hideRootOnClose && _ui != null)
            _ui.RootGO.SetActive(false);
    }

    private void ApplyLineToUI(object line)
    {
        // ===== Name =====
        string nameOverride = TryGetString(line, "nameOverride");
        if (string.IsNullOrEmpty(nameOverride) && autoFillNameFromCharacter)
        {
            // 若要從 CharacterPortraitSet.displayName 自動帶，請自己決定什麼時候要帶
            nameOverride = "";
        }
        if (_ui.nameText) _ui.nameText.text = nameOverride ?? "";

        // ===== Hint =====
        string hintCustom = TryGetString(line, "hintCustom");
        if (_ui.hintText) _ui.hintText.text = hintCustom ?? "";

        // ===== Portrait =====
        var character = TryGetObject(line, "character") as CharacterPortraitSet;
        bool keepPrev = TryGetBool(line, "keepPreviousPortrait");
        int variantEnum = TryGetEnumInt(line, "variant"); // PortraitVariant

        if (_ui.portraitImage)
        {
            if (keepPrev)
            {
                // 沿用上一句：Sprite/Transform 都不動
            }
            else if (character != null)
            {
                // 換圖
                var v = (PortraitVariant)variantEnum;
                _ui.portraitImage.sprite = character.Get(v);

                // 套用該角色 UI 設定（位置/縮放/翻轉），並且「重設後再套用」
                ApplyPortraitTransform(character);
            }
            else
            {
                _ui.portraitImage.sprite = null;

                // 沒角色就回到 base（避免上一個角色留下翻轉/偏移）
                ApplyPortraitTransform(null);
            }
        }
    }

    private void ApplyPortraitTransform(CharacterPortraitSet character)
    {
        if (_ui == null || _ui.portraitImage == null) return;

        var rt = _ui.portraitImage.rectTransform;

        if (!_portraitBaseCaptured)
        {
            _portraitBaseLocalScale = rt.localScale;
            _portraitBaseAnchoredPos = rt.anchoredPosition;
            _portraitBaseCaptured = true;
        }

        // 1) 位置：base + offset
        Vector2 offset = (character != null) ? character.uiOffset : Vector2.zero;
        rt.anchoredPosition = _portraitBaseAnchoredPos + offset;

        // 2) 縮放：base * uiScale
        Vector3 scaleMul = (character != null) ? character.uiScale : Vector3.one;
        Vector3 finalScale = Vector3.Scale(_portraitBaseLocalScale, scaleMul);

        // 3) 翻轉：只在 flipX = true 時把 X 變成負，否則確保是正（避免污染別人）
        if (character != null && character.flipX)
            finalScale.x = -Mathf.Abs(finalScale.x);
        else
            finalScale.x = Mathf.Abs(finalScale.x);

        rt.localScale = finalScale;
    }

    private IEnumerator TypeLine(string full)
    {
        if (_ui.bodyText == null)
            yield break;

        full ??= "";

        if (!useTypewriter)
        {
            _ui.bodyText.text = full;
            yield break;
        }

        _isTyping = true;
        _ui.bodyText.text = "";

        float t = 0f;
        int idx = 0;

        while (idx < full.Length)
        {
            if (IsAdvancePressed())
            {
                _ui.bodyText.text = full;
                _isTyping = false;
                yield break;
            }

            t += Time.deltaTime * _activeCps;
            int newIdx = Mathf.Clamp(Mathf.FloorToInt(t), 0, full.Length);

            if (newIdx != idx)
            {
                idx = newIdx;
                _ui.bodyText.text = full.Substring(0, idx);
            }

            yield return null;
        }

        _isTyping = false;
    }

    private IEnumerator WaitForAdvance()
    {
        yield return null;

        while (true)
        {
            if (IsAdvancePressed())
                yield break;
            yield return null;
        }
    }

    private bool IsAdvancePressed()
    {
        if (!_isPlaying) return false;

        if (allowMouseClick && Input.GetMouseButtonDown(0)) return true;
        if (allowSpace && Input.GetKeyDown(advanceKey)) return true;
        return false;
    }

    // ===== Data helpers =====

    private string GetBodyText(object line)
    {
        string body = TryGetString(line, "body");
        if (!string.IsNullOrEmpty(body)) return body;

        return TryGetString(line, "text") ?? "";
    }

    private static string TryGetString(object obj, string field)
    {
        if (obj == null) return null;
        var t = obj.GetType();
        var f = t.GetField(field);
        if (f != null && f.FieldType == typeof(string)) return (string)f.GetValue(obj);
        var p = t.GetProperty(field);
        if (p != null && p.PropertyType == typeof(string)) return (string)p.GetValue(obj);
        return null;
    }

    private static bool TryGetBool(object obj, string field)
    {
        if (obj == null) return false;
        var t = obj.GetType();
        var f = t.GetField(field);
        if (f != null && f.FieldType == typeof(bool)) return (bool)f.GetValue(obj);
        var p = t.GetProperty(field);
        if (p != null && p.PropertyType == typeof(bool)) return (bool)p.GetValue(obj);
        return false;
    }

    private static int TryGetEnumInt(object obj, string field)
    {
        if (obj == null) return 0;
        var t = obj.GetType();
        var f = t.GetField(field);
        if (f != null && f.FieldType.IsEnum) return (int)f.GetValue(obj);
        var p = t.GetProperty(field);
        if (p != null && p.PropertyType.IsEnum) return (int)p.GetValue(obj);
        return 0;
    }

    private static object TryGetObject(object obj, string field)
    {
        if (obj == null) return null;
        var t = obj.GetType();
        var f = t.GetField(field);
        if (f != null) return f.GetValue(obj);
        var p = t.GetProperty(field);
        if (p != null) return p.GetValue(obj);
        return null;
    }
}
