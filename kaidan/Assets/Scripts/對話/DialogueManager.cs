using System.Collections;
using TMPro;
using UnityEngine;

public class DialogueManager : MonoBehaviour
{
    [Header("Skins")]
    public DialogueUISkin defaultSkin;
    public DialogueUISkin secondarySkin;

    [Tooltip("UI 生成後要掛在哪個父物件下（通常是 Canvas）。可不填，不填就放在自己底下。")]
    public Transform uiParent;

    [Header("Input")]
    public bool allowSpace = true;
    public bool allowMouseClick = true;
    public KeyCode advanceKey = KeyCode.Space;

    [Header("Typewriter")]
    public bool useTypewriter = true;
    public float charsPerSecond = 40f;

    [Header("No auto text")]
    public bool autoFillNameFromCharacter = false;

    [Header("Debug")]
    public bool debugLog = true;

    private DialogueUIRefs _ui;
    private Coroutine _routine;
    private bool _isPlaying;
    private bool _isTyping;
    private bool _waitingExtraClose;
    private float _activeCps;

    // 目前畫面上那套 UI 是用哪個 skin 建出來的
    private DialogueUISkin _currentUISkin;

    // Portrait base（每次重建 UI 都會重新抓，避免不同 prefab 互相污染）
    private bool _portraitBaseCaptured;
    private Vector3 _portraitBaseLocalScale;
    private Vector2 _portraitBaseAnchoredPos;

    public void Play(DialogueAsset asset, DialogueUISkin skinOverride = null)
    {
        if (asset == null)
        {
            Debug.LogWarning("[DialogueManager] Play called with null asset.");
            return;
        }

        Stop();

        var startSkin = skinOverride != null ? skinOverride : defaultSkin;
        if (startSkin == null) startSkin = secondarySkin;

        if (startSkin == null || startSkin.uiPrefab == null)
        {
            Debug.LogError("[DialogueManager] No DialogueUISkin / uiPrefab assigned.");
            return;
        }

        EnsureUIInstance(startSkin);

        _ui.ClearAll();
        _ui.RootGO.SetActive(true);

        ApplyCpsFromSkin(startSkin);

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

        if (_ui != null && _currentUISkin != null && _currentUISkin.hideRootOnClose)
        {
            _ui.RootGO.SetActive(false);
        }
    }

    private void EnsureUIInstance(DialogueUISkin skin)
    {
        if (skin == null || skin.uiPrefab == null)
        {
            Debug.LogError("[DialogueManager] EnsureUIInstance: skin/uiPrefab is null.");
            return;
        }

        // 已經有 UI，但 skin 不同 => Destroy 重建
        if (_ui != null && _currentUISkin != skin)
        {
            if (debugLog) Debug.Log($"[DialogueManager] Rebuild UI: {_currentUISkin?.name} -> {skin.name}");
            Destroy(_ui.gameObject);
            _ui = null;
            _portraitBaseCaptured = false;
        }

        if (_ui != null) return;

        Transform parent = uiParent != null ? uiParent : transform;
        _ui = Instantiate(skin.uiPrefab, parent);
        _ui.name = $"DialogueUI({skin.name})";
        _currentUISkin = skin;

        CapturePortraitBase();
    }

    private void ApplyCpsFromSkin(DialogueUISkin skin)
    {
        _activeCps = (skin != null && skin.charsPerSecond > 0f) ? skin.charsPerSecond : charsPerSecond;
        if (_activeCps <= 0f) _activeCps = 40f;
    }

    private void CapturePortraitBase()
    {
        _portraitBaseCaptured = false;
        if (_ui == null || _ui.portraitImage == null) return;

        var rt = _ui.portraitImage.rectTransform;
        _portraitBaseLocalScale = rt.localScale;
        _portraitBaseAnchoredPos = rt.anchoredPosition;
        _portraitBaseCaptured = true;

        if (debugLog)
            Debug.Log($"[DialogueManager] CapturePortraitBase skin={_currentUISkin?.name} baseScale={_portraitBaseLocalScale}");
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

            // 每句決定要用哪個 skin（line > character > default > secondary）
            DialogueUISkin lineSkin = ResolveSkinForLine(line);
            if (lineSkin == null) lineSkin = defaultSkin;
            if (lineSkin == null) lineSkin = secondarySkin;

            if (lineSkin != null && lineSkin.uiPrefab != null)
            {
                EnsureUIInstance(lineSkin);

                _ui.ClearAll();
                _ui.RootGO.SetActive(true);
                ApplyCpsFromSkin(lineSkin);

                // ✅ ✅ ✅ 把「換框框」加回來：
                // 若你的 UI prefab 裡有 redBox / blueBox，這裡會切換。
                // 沒有就什麼都不做（不影響你用兩套 prefab 的方式）
                bool useBlueFrame = (lineSkin == secondarySkin);
                _ui.ShowFrame(useBlueFrame);
            }

            if (debugLog)
                Debug.Log($"[DialogueManager] Line {i}: skin={(lineSkin ? lineSkin.name : "null")} ui={(_ui ? _ui.name : "null")}");

            ApplyLineToUI(line);

            yield return StartCoroutine(TypeLine(GetBodyText(line)));
            yield return StartCoroutine(WaitForAdvance());
        }

        if (_currentUISkin != null && _currentUISkin.requireExtraPressToClose)
        {
            _waitingExtraClose = true;
            yield return StartCoroutine(WaitForAdvance());
        }

        CloseUI();
    }

    private DialogueUISkin ResolveSkinForLine(object line)
    {
        var skinFromLine = TryGetObject(line, "uiSkin") as DialogueUISkin;
        if (skinFromLine != null) return skinFromLine;

        var character = TryGetObject(line, "character");
        if (character != null)
        {
            var t = character.GetType();

            var f = t.GetField("defaultSkin");
            if (f != null && typeof(DialogueUISkin).IsAssignableFrom(f.FieldType))
            {
                var v = f.GetValue(character) as DialogueUISkin;
                if (v != null) return v;
            }

            var p = t.GetProperty("defaultSkin");
            if (p != null && typeof(DialogueUISkin).IsAssignableFrom(p.PropertyType))
            {
                var v = p.GetValue(character) as DialogueUISkin;
                if (v != null) return v;
            }
        }

        return defaultSkin;
    }

    private void CloseUI()
    {
        _isPlaying = false;
        _isTyping = false;
        _waitingExtraClose = false;

        if (_currentUISkin != null && _currentUISkin.hideRootOnClose && _ui != null)
            _ui.RootGO.SetActive(false);
    }

    private void ApplyLineToUI(object line)
    {
        string nameOverride = TryGetString(line, "nameOverride");
        if (string.IsNullOrEmpty(nameOverride) && autoFillNameFromCharacter)
            nameOverride = "";

        if (_ui.nameText) _ui.nameText.text = nameOverride ?? "";

        string hintCustom = TryGetString(line, "hintCustom");
        if (_ui.hintText) _ui.hintText.text = hintCustom ?? "";

        var character = TryGetObject(line, "character") as CharacterPortraitSet;
        bool keepPrev = TryGetBool(line, "keepPreviousPortrait");
        int variantEnum = TryGetEnumInt(line, "variant");

        if (_ui.portraitImage)
        {
            if (keepPrev)
            {
                // keep
            }
            else if (character != null)
            {
                var v = (PortraitVariant)variantEnum;
                _ui.portraitImage.sprite = character.Get(v);
                ApplyPortraitTransform(character);
            }
            else
            {
                _ui.portraitImage.sprite = null;
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

        Vector2 offset = (character != null) ? character.uiOffset : Vector2.zero;
        rt.anchoredPosition = _portraitBaseAnchoredPos + offset;

        Vector3 scaleMul = (character != null) ? character.uiScale : Vector3.one;
        Vector3 finalScale = Vector3.Scale(_portraitBaseLocalScale, scaleMul);

        // ✅ 修正：以 prefab 原本方向為基準再 flip，避免換 skin 後方向亂跳
        float baseSign = Mathf.Sign(_portraitBaseLocalScale.x);
        if (baseSign == 0) baseSign = 1f;
        float sign = baseSign * ((character != null && character.flipX) ? -1f : 1f);
        finalScale.x = Mathf.Abs(finalScale.x) * sign;

        rt.localScale = finalScale;
    }

    private IEnumerator TypeLine(string full)
    {
        if (_ui.bodyText == null) yield break;
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
            if (IsAdvancePressed()) yield break;
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