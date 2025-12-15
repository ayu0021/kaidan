using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshCollider))]
public class DestructibleShield : MonoBehaviour
{
    [Header("每次剪刀的剪洞半徑（世界座標）")]
    public float cutRadius = 1.0f;

    private Mesh _mesh;
    private Vector3[] _vertices;
    private int[] _triangles;
    private bool[] _triangleAlive;
    private MeshCollider _meshCollider;

    void Awake()
    {
        // 用 Awake 比 Start 更保險（有些物件啟用/停用會影響初始化時機）
        var mf = GetComponent<MeshFilter>();
        if (mf == null || mf.sharedMesh == null)
        {
            Debug.LogError("[DestructibleShield] MeshFilter or sharedMesh missing.");
            enabled = false;
            return;
        }

        // Clone 一份自己的 mesh 避免動到原始 asset
        _mesh = Instantiate(mf.sharedMesh);
        mf.mesh = _mesh;

        _meshCollider = GetComponent<MeshCollider>();
        _meshCollider.sharedMesh = _mesh;

        _vertices = _mesh.vertices;
        _triangles = _mesh.triangles;

        int triCount = _triangles.Length / 3;
        _triangleAlive = new bool[triCount];
        for (int i = 0; i < triCount; i++) _triangleAlive[i] = true;
    }

    /// <summary>
    /// 在 worldPos 附近剪一個洞（世界座標）
    /// radiusWorld：世界座標半徑
    /// </summary>
    public void CutAt(Vector3 worldPos, float radiusWorld)
    {
        if (_mesh == null) return;

        float r2 = radiusWorld * radiusWorld;

        List<int> newTriangles = new List<int>(_triangles.Length);
        int triCount = _triangles.Length / 3;
        int removed = 0;

        for (int t = 0; t < triCount; t++)
        {
            if (!_triangleAlive[t]) continue;

            int i0 = _triangles[t * 3 + 0];
            int i1 = _triangles[t * 3 + 1];
            int i2 = _triangles[t * 3 + 2];

            // 取得世界座標的三角形頂點（解決非等比縮放）
            Vector3 w0 = transform.TransformPoint(_vertices[i0]);
            Vector3 w1 = transform.TransformPoint(_vertices[i1]);
            Vector3 w2 = transform.TransformPoint(_vertices[i2]);

            // 用「點到三角形最近點」判斷刀圈是否碰到三角形
            Vector3 closest = ClosestPointOnTriangle(worldPos, w0, w1, w2);
            if ((closest - worldPos).sqrMagnitude <= r2)
            {
                _triangleAlive[t] = false;
                removed++;
                continue;
            }

            // 還活著的三角形保留
            newTriangles.Add(i0);
            newTriangles.Add(i1);
            newTriangles.Add(i2);
        }

        _mesh.triangles = newTriangles.ToArray();
        _mesh.RecalculateBounds();
        _mesh.RecalculateNormals();

        // collider 跟著更新
        _meshCollider.sharedMesh = null;
        _meshCollider.sharedMesh = _mesh;

        Debug.Log($"[DestructibleShield] CutAt {worldPos}, removed {removed} triangles");
    }

    /// <summary>
    /// 便捷：用 Inspector 的 cutRadius
    /// </summary>
    public void CutAt(Vector3 worldPos)
    {
        CutAt(worldPos, cutRadius);
    }

    // ===== 幾何工具：點到三角形最近點（Ericson RTCD） =====
    static Vector3 ClosestPointOnTriangle(Vector3 p, Vector3 a, Vector3 b, Vector3 c)
    {
        Vector3 ab = b - a;
        Vector3 ac = c - a;
        Vector3 ap = p - a;

        float d1 = Vector3.Dot(ab, ap);
        float d2 = Vector3.Dot(ac, ap);
        if (d1 <= 0f && d2 <= 0f) return a;

        Vector3 bp = p - b;
        float d3 = Vector3.Dot(ab, bp);
        float d4 = Vector3.Dot(ac, bp);
        if (d3 >= 0f && d4 <= d3) return b;

        float vc = d1 * d4 - d3 * d2;
        if (vc <= 0f && d1 >= 0f && d3 <= 0f)
        {
            float v = d1 / (d1 - d3);
            return a + v * ab;
        }

        Vector3 cp = p - c;
        float d5 = Vector3.Dot(ab, cp);
        float d6 = Vector3.Dot(ac, cp);
        if (d6 >= 0f && d5 <= d6) return c;

        float vb = d5 * d2 - d1 * d6;
        if (vb <= 0f && d2 >= 0f && d6 <= 0f)
        {
            float w = d2 / (d2 - d6);
            return a + w * ac;
        }

        float va = d3 * d6 - d5 * d4;
        if (va <= 0f && (d4 - d3) >= 0f && (d5 - d6) >= 0f)
        {
            float w = (d4 - d3) / ((d4 - d3) + (d5 - d6));
            return b + w * (c - b);
        }

        float denom = 1f / (va + vb + vc);
        float v2 = vb * denom;
        float w2 = vc * denom;
        return a + ab * v2 + ac * w2;
    }
}
