using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshCollider))]
public class DestructibleShield : MonoBehaviour
{
    [Header("每次剪刀的剪洞半徑（世界座標）")]
    public float cutRadius = 1.5f;   // 先設大一點，洞比較明顯

    private Mesh _mesh;
    private Vector3[] _vertices;
    private int[] _triangles;

    private bool[] _triangleAlive;

    private MeshCollider _meshCollider;

    void Start()
    {
        var mf = GetComponent<MeshFilter>();

        // Clone 一份自己的 mesh 避免動到原始 asset
        _mesh = Instantiate(mf.sharedMesh);
        mf.mesh = _mesh;

        _meshCollider = GetComponent<MeshCollider>();
        _meshCollider.sharedMesh = _mesh;

        _vertices = _mesh.vertices;
        _triangles = _mesh.triangles;

        int triCount = _triangles.Length / 3;
        _triangleAlive = new bool[triCount];
        for (int i = 0; i < triCount; i++)
            _triangleAlive[i] = true;
    }

    /// <summary>
    /// 在 worldPos 附近剪一個洞（世界座標）
    /// </summary>
    public void CutAt(Vector3 worldPos, float radiusWorld)
    {
        if (_mesh == null) return;

        // 世界座標 → 盾的 local 座標
        Vector3 localPos = transform.InverseTransformPoint(worldPos);

        float scale = transform.lossyScale.x;
        float localRadius = radiusWorld / Mathf.Max(scale, 0.0001f);
        float r2 = localRadius * localRadius;

        List<int> newTriangles = new List<int>();

        int triCount = _triangles.Length / 3;
        int removed = 0;

        for (int t = 0; t < triCount; t++)
        {
            if (!_triangleAlive[t]) continue;

            int i0 = _triangles[t * 3 + 0];
            int i1 = _triangles[t * 3 + 1];
            int i2 = _triangles[t * 3 + 2];

            Vector3 v0 = _vertices[i0];
            Vector3 v1 = _vertices[i1];
            Vector3 v2 = _vertices[i2];

            // ★ 比原本寬鬆很多：只要「任一個」頂點在圓內就剪掉
            bool inside0 = (v0 - localPos).sqrMagnitude <= r2;
            bool inside1 = (v1 - localPos).sqrMagnitude <= r2;
            bool inside2 = (v2 - localPos).sqrMagnitude <= r2;

            if (inside0 || inside1 || inside2)
            {
                _triangleAlive[t] = false;
                removed++;
                continue;
            }

            newTriangles.Add(i0);
            newTriangles.Add(i1);
            newTriangles.Add(i2);
        }

        _mesh.triangles = newTriangles.ToArray();
        _mesh.RecalculateBounds();
        _mesh.RecalculateNormals();

        _meshCollider.sharedMesh = null;
        _meshCollider.sharedMesh = _mesh;

        Debug.Log($"[DestructibleShield] CutAt {worldPos}, removed {removed} triangles");
    }
}
