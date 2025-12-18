using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class SpriteRendererToQuad
{
    // 共用材質快取（同一張貼圖只建一個材質）
    static Dictionary<Texture2D, Material> materialCache = new Dictionary<Texture2D, Material>();

    [MenuItem("Tools/SpriteRenderer/Convert To Quad (URP)")]
    static void ConvertSelectedSpriteRenderers()
    {
        GameObject[] selected = Selection.gameObjects;

        if (selected.Length == 0)
        {
            Debug.LogWarning("請先在 Scene 中選擇含有 SpriteRenderer 的物件");
            return;
        }

        Shader litShader = Shader.Find("URP/Lit");
        if (litShader == null)
            litShader = Shader.Find("Universal Render Pipeline/Lit");

        if (litShader == null)
        {
            Debug.LogError("找不到 URP Lit Shader，請確認專案正在使用 URP");
            return;
        }

        int convertedCount = 0;

        foreach (GameObject go in selected)
        {
            SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
            if (sr == null || sr.sprite == null) continue;

            Sprite sprite = sr.sprite;

            // 建立 Quad
            GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = go.name + "_Mesh";

            // 保留 Transform
            quad.transform.SetParent(go.transform.parent);
            quad.transform.position = go.transform.position;
            quad.transform.rotation = go.transform.rotation;
            quad.transform.localScale = GetWorldScaleFromSprite(sr);

            // 移除 Collider
            Object.DestroyImmediate(quad.GetComponent<Collider>());

            // 建立 / 取得共用材質
            Material mat = GetOrCreateMaterial(sprite.texture, litShader);

            // 套用材質
            quad.GetComponent<MeshRenderer>().sharedMaterial = mat;

            // 關掉原本 SpriteRenderer
            sr.enabled = false;

            convertedCount++;
        }

        Debug.Log($"SpriteRenderer → Quad 轉換完成，共轉換 {convertedCount} 個物件");
    }

    static Material GetOrCreateMaterial(Texture2D tex, Shader shader)
    {
        if (materialCache.TryGetValue(tex, out Material cached))
            return cached;

        Material mat = new Material(shader);
        mat.name = "M_SpriteMesh_" + tex.name;

        mat.SetTexture("_BaseMap", tex);
        mat.SetFloat("_Smoothness", 0f);
        mat.SetFloat("_Metallic", 0f);

        // 透明設定（URP Lit 正確做法）
        mat.SetFloat("_Surface", 1); // 1 = Transparent
        mat.SetFloat("_Blend", 0);   // Alpha
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.renderQueue = 3000;

        // 關閉 3D 感
        mat.DisableKeyword("_SPECULARHIGHLIGHTS_ON");
        mat.DisableKeyword("_ENVIRONMENTREFLECTIONS_ON");

        materialCache.Add(tex, mat);
        return mat;
    }

    static Vector3 GetWorldScaleFromSprite(SpriteRenderer sr)
    {
        Sprite sprite = sr.sprite;
        float width = sprite.rect.width / sprite.pixelsPerUnit;
        float height = sprite.rect.height / sprite.pixelsPerUnit;

        Vector3 lossy = sr.transform.lossyScale;
        return new Vector3(width * lossy.x, height * lossy.y, 1f);
    }
}
