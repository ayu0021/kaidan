using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public class DialogueTextAnimator : MonoBehaviour
{
    private class ResolvedEffect
    {
        public DialogueTextEffectType effectType;
        public int startIndex;
        public int length;
        public Color color;
        public float scaleMultiplier;
        public float amplitude;
        public float frequency;
        public float speed;

        public bool Contains(int charIndex)
        {
            return charIndex >= startIndex && charIndex < startIndex + length;
        }
    }

    [SerializeField] private TMP_Text targetText;
    [SerializeField] private bool useUnscaledTime = true;

    private readonly List<ResolvedEffect> _resolvedEffects = new();
    private TMP_MeshInfo[] _cachedMeshInfo;
    private string _currentText = string.Empty;
    private bool _isReady;

    public int CharacterCount
    {
        get
        {
            if (targetText == null) return 0;
            targetText.ForceMeshUpdate(true, true);
            return targetText.textInfo != null ? targetText.textInfo.characterCount : 0;
        }
    }

    public void Bind(TMP_Text tmp)
    {
        targetText = tmp;
    }

    public void SetContent(string content, List<DialogueTextEffectData> effects)
    {
        if (targetText == null)
            targetText = GetComponent<TMP_Text>();

        if (targetText == null)
            return;

        _currentText = content ?? string.Empty;
        targetText.text = _currentText;

        // 先把字全部生成出來，再快取 mesh
        targetText.maxVisibleCharacters = int.MaxValue;
        targetText.ForceMeshUpdate(true, true);

        ResolveEffects(_currentText, effects);
        CacheMeshInfo();

        // 再交回打字機
        targetText.maxVisibleCharacters = 0;

        _isReady = _cachedMeshInfo != null && _resolvedEffects.Count > 0;
    }

    public void SetVisibleCharacters(int count)
    {
        if (targetText == null) return;
        targetText.maxVisibleCharacters = count;
    }

    public void ClearEffects()
    {
        _resolvedEffects.Clear();
        _cachedMeshInfo = null;
        _isReady = false;
    }

    private void ResolveEffects(string rawText, List<DialogueTextEffectData> effects)
    {
        _resolvedEffects.Clear();

        if (string.IsNullOrEmpty(rawText) || effects == null || effects.Count == 0)
            return;

        for (int i = 0; i < effects.Count; i++)
        {
            var e = effects[i];
            if (e == null || !e.enabled || string.IsNullOrEmpty(e.targetText))
                continue;

            var matches = FindAllMatches(rawText, e.targetText);
            if (matches.Count == 0)
                continue;

            if (e.applyToAllMatches)
            {
                for (int m = 0; m < matches.Count; m++)
                    AddResolvedEffect(e, matches[m].start, matches[m].length);
            }
            else
            {
                int idx = Mathf.Clamp(e.occurrenceIndex, 0, matches.Count - 1);
                AddResolvedEffect(e, matches[idx].start, matches[idx].length);
            }
        }
    }

    private void AddResolvedEffect(DialogueTextEffectData e, int start, int length)
    {
        _resolvedEffects.Add(new ResolvedEffect
        {
            effectType = e.effectType,
            startIndex = start,
            length = length,
            color = e.color,
            scaleMultiplier = Mathf.Max(0.1f, e.scaleMultiplier),
            amplitude = Mathf.Max(0f, e.amplitude),
            frequency = Mathf.Max(0.1f, e.frequency),
            speed = Mathf.Max(0.1f, e.speed)
        });
    }

    private List<(int start, int length)> FindAllMatches(string source, string target)
    {
        List<(int start, int length)> result = new();
        int startIndex = 0;

        while (startIndex < source.Length)
        {
            int idx = source.IndexOf(target, startIndex, StringComparison.Ordinal);
            if (idx < 0) break;

            result.Add((idx, target.Length));
            startIndex = idx + target.Length;
        }

        return result;
    }

    private void CacheMeshInfo()
    {
        _cachedMeshInfo = null;

        if (targetText == null)
            return;

        targetText.ForceMeshUpdate(true, true);

        if (targetText.textInfo == null)
            return;

        if (targetText.textInfo.meshInfo == null || targetText.textInfo.meshInfo.Length == 0)
            return;

        try
        {
            _cachedMeshInfo = targetText.textInfo.CopyMeshInfoVertexData();
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[DialogueTextAnimator] CacheMeshInfo failed: {ex.Message}");
            _cachedMeshInfo = null;
        }
    }

    private void LateUpdate()
    {
        if (!_isReady || targetText == null || _resolvedEffects.Count == 0)
            return;

        targetText.ForceMeshUpdate(true, false);

        var textInfo = targetText.textInfo;
        if (textInfo == null || textInfo.characterCount == 0)
            return;

        if (_cachedMeshInfo == null || _cachedMeshInfo.Length != textInfo.meshInfo.Length)
        {
            CacheMeshInfo();
            if (_cachedMeshInfo == null) return;
        }

        for (int i = 0; i < textInfo.meshInfo.Length; i++)
        {
            if (i >= _cachedMeshInfo.Length) continue;
            if (_cachedMeshInfo[i].vertices == null || textInfo.meshInfo[i].vertices == null) continue;
            if (_cachedMeshInfo[i].colors32 == null || textInfo.meshInfo[i].colors32 == null) continue;

            Array.Copy(_cachedMeshInfo[i].vertices, textInfo.meshInfo[i].vertices, _cachedMeshInfo[i].vertices.Length);
            Array.Copy(_cachedMeshInfo[i].colors32, textInfo.meshInfo[i].colors32, _cachedMeshInfo[i].colors32.Length);
        }

        float time = useUnscaledTime ? Time.unscaledTime : Time.time;
        int visibleCount = Mathf.Clamp(targetText.maxVisibleCharacters, 0, textInfo.characterCount);

        for (int charIndex = 0; charIndex < visibleCount; charIndex++)
        {
            TMP_CharacterInfo charInfo = textInfo.characterInfo[charIndex];
            if (!charInfo.isVisible) continue;

            int materialIndex = charInfo.materialReferenceIndex;
            int vertexIndex = charInfo.vertexIndex;

            if (materialIndex < 0 || materialIndex >= textInfo.meshInfo.Length) continue;
            if (materialIndex >= _cachedMeshInfo.Length) continue;

            Vector3[] srcVertices = _cachedMeshInfo[materialIndex].vertices;
            Vector3[] dstVertices = textInfo.meshInfo[materialIndex].vertices;
            Color32[] dstColors = textInfo.meshInfo[materialIndex].colors32;

            if (srcVertices == null || dstVertices == null || dstColors == null) continue;
            if (vertexIndex + 3 >= srcVertices.Length || vertexIndex + 3 >= dstVertices.Length || vertexIndex + 3 >= dstColors.Length)
                continue;

            Vector3 offset = Vector3.zero;
            Vector3 scale = Vector3.one;
            bool hasColor = false;
            Color32 finalColor = new(255, 255, 255, 255);

            for (int e = 0; e < _resolvedEffects.Count; e++)
            {
                var effect = _resolvedEffects[e];
                if (!effect.Contains(charIndex)) continue;

                switch (effect.effectType)
                {
                    case DialogueTextEffectType.Color:
                        hasColor = true;
                        finalColor = effect.color;
                        break;

                    case DialogueTextEffectType.Scale:
                        scale = Vector3.Scale(scale, new Vector3(effect.scaleMultiplier, effect.scaleMultiplier, 1f));
                        break;

                    case DialogueTextEffectType.Shake:
                    {
                        float seedX = (charIndex + 1) * 0.173f;
                        float seedY = (charIndex + 1) * 0.731f;

                        float px = (Mathf.PerlinNoise(seedX, time * effect.speed) - 0.5f) * 2f * effect.amplitude;
                        float py = (Mathf.PerlinNoise(seedY, time * effect.speed) - 0.5f) * 2f * effect.amplitude;

                        offset += new Vector3(px, py, 0f);
                        break;
                    }

                    case DialogueTextEffectType.Wave:
                    {
                        float wave = Mathf.Sin((charIndex * effect.frequency * 0.15f) + (time * effect.speed)) * effect.amplitude;
                        offset += new Vector3(0f, wave, 0f);
                        break;
                    }
                }
            }

            Vector3 center = (srcVertices[vertexIndex] + srcVertices[vertexIndex + 2]) * 0.5f;

            for (int j = 0; j < 4; j++)
            {
                Vector3 v = srcVertices[vertexIndex + j];
                v -= center;
                v = Vector3.Scale(v, scale);
                v += center + offset;
                dstVertices[vertexIndex + j] = v;

                if (hasColor)
                    dstColors[vertexIndex + j] = finalColor;
            }
        }

        for (int i = 0; i < textInfo.meshInfo.Length; i++)
        {
            if (textInfo.meshInfo[i].mesh == null) continue;
            textInfo.meshInfo[i].mesh.vertices = textInfo.meshInfo[i].vertices;
            textInfo.meshInfo[i].mesh.colors32 = textInfo.meshInfo[i].colors32;
            targetText.UpdateGeometry(textInfo.meshInfo[i].mesh, i);
        }
    }
}