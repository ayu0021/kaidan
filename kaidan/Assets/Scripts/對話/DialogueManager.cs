using System.Collections;
using UnityEngine;

public class DialogueManager : MonoBehaviour
{
    [Header("Skins")]
    public DialogueUISkin defaultSkin;
    public DialogueUISkin secondarySkin;

    [Tooltip("UI 生成後要掛在哪個父物件下（通常是 Canvas）。強烈建議指定。")]
    public Transform uiParent;

    [Header("Input")]
    public bool allowSpace = true; // 保留欄位名稱，避免舊 Inspector 資料失效
    public bool allowMouseClick = true;
    public KeyCode advanceKey = KeyCode.F;

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

    private DialogueUISkin _currentUISkin;

    // Portrait base（每次切框 / 重建 UI 都重新抓）
    private bool _portraitBaseCaptured;
    private Vector3 _portraitBaseLocalScale;
    private Vector2 _portraitBaseAnchoredPos;
    private Vector2 _portraitBaseSizeDelta;
    private Vector2 _portraitBaseAnchorMin;
    private Vector2 _portraitBaseAnchorMax;
    private Vector2 _portraitBasePivot;

    private void Awake()
    {
        if (advanceKey == KeyCode.Space)
            advanceKey = KeyCode.F;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (advanceKey == KeyCode.Space)
            advanceKey = KeyCode.F;
    }
#endif

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

        bool startUseBlueFrame = (startSkin == secondarySkin);
        _ui.ShowFrame(startUseBlueFrame);
        CapturePortraitBase();

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

        if (_ui != null && _currentUISkin != skin)
        {
            if (debugLog) Debug.Log($"[DialogueManager] Rebuild UI: {_currentUISkin?.name} -> {skin.name}");
            Destroy(_ui.gameObject);
            _ui = null;
            _portraitBaseCaptured = false;
        }

        if (_ui != null) return;

        Transform parent = uiParent != null ? uiParent : transform;

        if (uiParent == null && debugLog)
        {
            Debug.LogWarning(
                "[DialogueManager] uiParent is NULL. UI will be instantiated under DialogueManager itself. " +
                "如果 DialogueManager 在 DontDestroyOnLoad 或其父物件 scale 不是 1，UI 可能看起來被拉伸。"
            );
        }

        _ui = Instantiate(skin.uiPrefab, parent);
        _ui.name = $"DialogueUI({skin.name})";
        _currentUISkin = skin;

        RectTransform rootRt = _ui.transform as RectTransform;
        if (rootRt != null)
        {
            rootRt.localScale = Vector3.one;
            rootRt.localRotation = Quaternion.identity;
        }
        else
        {
            _ui.transform.localScale = Vector3.one;
            _ui.transform.localRotation = Quaternion.identity;
        }

        if (debugLog)
        {
            Vector3 ls = parent.lossyScale;
            Debug.Log($"[DialogueManager] Instantiate UI under parent={parent.name}, parentLossyScale={ls}");
        }

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

        if (_ui == null || _ui.portraitImage == null)
            return;

        RectTransform rt = _ui.portraitImage.rectTransform;

        _portraitBaseAnchoredPos = rt.anchoredPosition;
        _portraitBaseSizeDelta = rt.sizeDelta;
        _portraitBaseAnchorMin = rt.anchorMin;
        _portraitBaseAnchorMax = rt.anchorMax;
        _portraitBasePivot = rt.pivot;

        // 只保留 prefab 原始朝向，不吃殘留的大倍率
        float baseSign = Mathf.Sign(rt.localScale.x);
        if (baseSign == 0f) baseSign = 1f;

        _portraitBaseLocalScale = new Vector3(baseSign, 1f, 1f);

        // 順手把當前 Portrait scale 歸正
        rt.localScale = _portraitBaseLocalScale;

        _portraitBaseCaptured = true;

        if (debugLog)
        {
            Debug.Log(
                $"[DialogueManager] CapturePortraitBase skin={_currentUISkin?.name} " +
                $"portrait={_ui.portraitImage.name} baseScale={_portraitBaseLocalScale} " +
                $"basePos={_portraitBaseAnchoredPos} size={_portraitBaseSizeDelta}"
            );
        }
    }

    private void RestorePortraitRect(RectTransform rt)
    {
        rt.anchorMin = _portraitBaseAnchorMin;
        rt.anchorMax = _portraitBaseAnchorMax;
        rt.pivot = _portraitBasePivot;
        rt.sizeDelta = _portraitBaseSizeDelta;
        rt.anchoredPosition = _portraitBaseAnchoredPos;
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

            DialogueUISkin lineSkin = ResolveSkinForLine(line);
            if (lineSkin == null) lineSkin = defaultSkin;
            if (lineSkin == null) lineSkin = secondarySkin;

            if (lineSkin != null && lineSkin.uiPrefab != null)
            {
                EnsureUIInstance(lineSkin);

                bool useBlueFrame = (lineSkin == secondarySkin);
                _ui.ShowFrame(useBlueFrame);
                CapturePortraitBase();

                _ui.ClearAll();
                _ui.RootGO.SetActive(true);
                ApplyCpsFromSkin(lineSkin);
            }

            if (debugLog)
            {
                Debug.Log(
                    $"[DialogueManager] Line {i}: skin={(lineSkin ? lineSkin.name : "null")} " +
                    $"ui={(_ui ? _ui.name : "null")} portrait={(_ui && _ui.portraitImage ? _ui.portraitImage.name : "null")}"
                );
            }

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

        RectTransform rt = _ui.portraitImage.rectTransform;

        if (!_portraitBaseCaptured)
            CapturePortraitBase();

        if (!_portraitBaseCaptured)
            return;

        // 先回到 prefab 裡 Portrait 的基準狀態
        RestorePortraitRect(rt);

        Vector2 offset = Vector2.zero;
        Vector3 charScale = Vector3.one;
        bool flipX = false;

        if (character != null)
        {
            offset = character.uiOffset;
            charScale = character.uiScale;
            flipX = character.flipX;
        }

        rt.anchoredPosition = _portraitBaseAnchoredPos + offset;

        float baseSign = Mathf.Sign(_portraitBaseLocalScale.x);
        if (baseSign == 0f) baseSign = 1f;

        float finalSign = flipX ? -baseSign : baseSign;

        float scaleX = Mathf.Abs(charScale.x) <= 0.0001f ? 1f : Mathf.Abs(charScale.x);
        float scaleY = Mathf.Abs(charScale.y) <= 0.0001f ? 1f : Mathf.Abs(charScale.y);
        float scaleZ = Mathf.Abs(charScale.z) <= 0.0001f ? 1f : Mathf.Abs(charScale.z);

        rt.localScale = new Vector3(finalSign * scaleX, scaleY, scaleZ);
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