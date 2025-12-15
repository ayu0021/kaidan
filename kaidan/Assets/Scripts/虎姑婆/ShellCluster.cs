using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShellCluster : MonoBehaviour
{
    [Header("References")]
    public Transform weakPoint;

    [Header("Layers")]
    public LayerMask shellMask;

    [Tooltip("掉落碎片會切到這個 Layer，避免再被當作 Shell 目標")]
    public string debrisLayerName = "Debris";

    [Header("Adjacency Build (碰到就算鄰居)")]
    [Tooltip("鄰居判定的外擴距離，越大越容易連在一起（建議 0.01~0.05）")]
    public float neighborPadding = 0.02f;

    [Header("Core Anchor (核心附近算根)")]
    [Tooltip("離弱點多近的碎片算「核心連結根」(建議 0.3~1.0，依模型大小調)")]
    public float anchorRadius = 0.8f;

    [Header("Cut Behavior (命中那片)")]
    public bool cutPieceFalls = true;
    [Tooltip("✅ 建議關掉：命中那片若關 collider，你會覺得「剪完兩片就不會剪了」")]
    public bool cutPieceDisableCollider = false;
    public float cutDetachImpulse = 1.2f;
    public float cutFlashTime = 0.15f;
    [Tooltip("0 = 不隱藏（只掉落）；>0 = 延遲後隱藏 renderer")]
    public float cutHideDelay = 0.0f;
    [Tooltip(">0 才會自動刪除命中那片")]
    public float cutAutoDestroyAfter = 0f;

    [Header("Detach Effect (for disconnected chunks)")]
    public float detachImpulse = 1.5f;
    [Tooltip(">0 才自動刪除斷鏈整串")]
    public float autoDestroyAfter = 0f;

    [Header("Safety")]
    [Tooltip("避免開場 anchors 判不到導致整坨崩壞：只有「剪過一次」才允許斷開塊掉落")]
    public bool onlyDetachAfterFirstCut = true;

    private readonly List<ShellPiece> _pieces = new();
    private readonly Dictionary<Collider, ShellPiece> _col2piece = new();
    private readonly Dictionary<ShellPiece, List<ShellPiece>> _neighbors = new();

    private bool _hasEverCut = false;

    public bool AllShellDestroyed { get; private set; }

    int _shellLayer;
    int _debrisLayer;

    void Awake()
    {
        _shellLayer = LayerMask.NameToLayer("Shell");
        _debrisLayer = LayerMask.NameToLayer(debrisLayerName); // 可能是 -1

        CachePieces();
        BuildAdjacency();

        // 開場只算連通性、不做掉落（避免突然整塊崩壞）
        RecomputeConnectivity(null, allowDetachDisconnected: !onlyDetachAfterFirstCut);
    }

    void CachePieces()
    {
        _pieces.Clear();
        _col2piece.Clear();

        foreach (Transform child in transform)
        {
            var p = child.GetComponent<ShellPiece>();
            if (!p) p = child.gameObject.AddComponent<ShellPiece>();

            if (!p.col) p.col = child.GetComponent<Collider>();
            if (!p.rb)  p.rb  = child.GetComponent<Rigidbody>();

            _pieces.Add(p);
            if (p.col) _col2piece[p.col] = p;

            // 確保還活著的 piece 在 Shell layer（避免你手動調亂）
            if (p.alive && _shellLayer >= 0)
                p.gameObject.layer = _shellLayer;
        }
    }

    void BuildAdjacency()
    {
        _neighbors.Clear();
        foreach (var p in _pieces) _neighbors[p] = new List<ShellPiece>();

        foreach (var p in _pieces)
        {
            if (!p.col) continue;

            var b = p.col.bounds;
            var center = b.center;
            var halfExtents = b.extents + Vector3.one * neighborPadding;

            var hits = Physics.OverlapBox(center, halfExtents, Quaternion.identity, shellMask, QueryTriggerInteraction.Ignore);
            foreach (var h in hits)
            {
                if (!h || h == p.col) continue;
                if (!_col2piece.TryGetValue(h, out var q)) continue;
                if (q == p) continue;

                if (!_neighbors[p].Contains(q)) _neighbors[p].Add(q);
                if (!_neighbors[q].Contains(p)) _neighbors[q].Add(p);
            }
        }
    }

    List<ShellPiece> GetAnchors()
    {
        var anchors = new List<ShellPiece>();
        if (!weakPoint) return anchors;

        Vector3 wp = weakPoint.position;
        float r2 = anchorRadius * anchorRadius;

        foreach (var p in _pieces)
        {
            if (!p.alive || p.detached || !p.col) continue;

            Vector3 closest = p.col.ClosestPoint(wp);
            if ((closest - wp).sqrMagnitude <= r2)
                anchors.Add(p);
        }

        return anchors;
    }

    public void CutByCollider(Collider hitCol, Vector3 hitPoint)
    {
        if (!hitCol) return;
        if (!_col2piece.TryGetValue(hitCol, out var piece)) return;
        CutPiece(piece, hitPoint);
    }

    public void CutPiece(ShellPiece piece, Vector3 hitPoint)
    {
        if (!piece || !piece.alive) return;
        _hasEverCut = true;

        // 命中先亮一下（你才看得到）
        piece.Flash();

        // 這片從「殼連通性」移除
        piece.alive = false;

        // 變 Debris layer：避免再次被 Player hover/cut
        SetToDebris(piece);

        // 命中那片：掉落（建議保留 collider，才會真的是「碎片」）
        if (cutPieceDisableCollider) piece.SetCollidable(false);

        if (cutPieceFalls)
        {
            Vector3 from = weakPoint ? weakPoint.position : hitPoint;
            Vector3 dir = (piece.transform.position - from);
            if (dir.sqrMagnitude < 1e-6f) dir = Vector3.up;

            piece.Detach(dir.normalized + Vector3.up * 0.2f, cutDetachImpulse, enableGravity: true);
        }

        StartCoroutine(CoAfterCut(piece, hitPoint));
    }

    IEnumerator CoAfterCut(ShellPiece piece, Vector3 hitPoint)
    {
        // 留時間給 Flash 表現
        yield return new WaitForSeconds(Mathf.Max(0.01f, cutFlashTime));

        if (cutHideDelay > 0f)
        {
            yield return new WaitForSeconds(cutHideDelay);
            if (piece) piece.SetVisible(false);
        }

        if (cutAutoDestroyAfter > 0f && piece)
            Destroy(piece.gameObject, cutAutoDestroyAfter);

        // 剪掉後，重新算連通性：斷鏈整串崩壞/掉落
        RecomputeConnectivity(hitPoint, allowDetachDisconnected: true);
    }

    void SetToDebris(ShellPiece p)
    {
        if (!p) return;

        // Debris layer 不存在就退回 Default
        int layer = _debrisLayer >= 0 ? _debrisLayer : 0;
        p.gameObject.layer = layer;
    }

    public void RecomputeConnectivity(Vector3? cutPoint, bool allowDetachDisconnected)
    {
        var anchors = GetAnchors();
        var visited = new HashSet<ShellPiece>();
        var q = new Queue<ShellPiece>();

        foreach (var a in anchors)
        {
            visited.Add(a);
            q.Enqueue(a);
        }

        while (q.Count > 0)
        {
            var cur = q.Dequeue();
            if (!_neighbors.TryGetValue(cur, out var neigh)) continue;

            foreach (var nb in neigh)
            {
                if (!nb.alive || nb.detached) continue;
                if (visited.Add(nb))
                    q.Enqueue(nb);
            }
        }

        if (onlyDetachAfterFirstCut && !_hasEverCut)
            allowDetachDisconnected = false;

        Vector3 impulseDir = Vector3.up;
        if (weakPoint)
        {
            impulseDir = (cutPoint ?? weakPoint.position) - weakPoint.position;
            if (impulseDir.sqrMagnitude < 1e-6f) impulseDir = Vector3.up;
            impulseDir = impulseDir.normalized;
        }

        int aliveCount = 0;

        foreach (var p in _pieces)
        {
            if (!p.alive) continue;
            aliveCount++;

            // 連不到核心的 => 掉落崩壞
            if (allowDetachDisconnected && !visited.Contains(p) && !p.detached)
            {
                // 變 Debris，避免後續又被當成 Shell 目標
                SetToDebris(p);

                // 建議保留 collider，掉落才有「碎片感」
                p.Detach(-impulseDir + Vector3.up * 0.3f, detachImpulse, enableGravity: true);

                if (autoDestroyAfter > 0f)
                    Destroy(p.gameObject, autoDestroyAfter);
            }
        }

        AllShellDestroyed = (aliveCount == 0);
    }
}
