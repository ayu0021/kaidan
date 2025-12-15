using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShellCluster : MonoBehaviour
{
    [Header("References")]
    public Transform weakPoint;

    [Header("Layers")]
    public LayerMask shellMask;

    [Header("Adjacency Build (碰到就算鄰居)")]
    [Tooltip("鄰居判定的外擴距離，越大越容易連在一起（建議 0.01~0.05）")]
    public float neighborPadding = 0.02f;

    [Header("Core Anchor (核心附近算根)")]
    [Tooltip("離弱點多近的碎片算「核心連結根」(建議 0.3~1.0，依模型大小調)")]
    public float anchorRadius = 0.8f;

    [Header("Cut Behavior (命中那片)")]
    public bool cutPieceFalls = true;
    public bool cutPieceDisableCollider = true;
    public float cutDetachImpulse = 1.2f;
    public float cutFlashTime = 0.15f;
    public float cutHideDelay = 0.2f;      // 0 = 不隱藏（只掉落）
    public float cutAutoDestroyAfter = 0f; // >0 才自動刪

    [Header("Detach Effect (for disconnected chunks)")]
    public float detachImpulse = 1.5f;
    public float autoDestroyAfter = 0f; // >0 才自動刪

    [Header("Safety")]
    [Tooltip("避免一開始 anchors 判不到導致整坨崩壞：只有「剪過一次」才允許斷開塊掉落")]
    public bool onlyDetachAfterFirstCut = true;

    private readonly List<ShellPiece> _pieces = new();
    private readonly Dictionary<Collider, ShellPiece> _col2piece = new();
    private readonly Dictionary<ShellPiece, List<ShellPiece>> _neighbors = new();

    private bool _hasEverCut = false;

    public bool AllShellDestroyed { get; private set; }

    void Awake()
    {
        CachePieces();
        BuildAdjacency();

        // 開場只算連通性、不做掉落（避免你說的「突然整塊消失」）
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

        // 先標記「不算連通殼」(但先不要立刻隱藏，讓你看得到亮/掉)
        piece.alive = false;

        // 命中高亮
        piece.Flash();

        // 命中那片：掉落/禁碰撞
        if (cutPieceDisableCollider) piece.SetCollidable(false);

        if (cutPieceFalls)
        {
            Vector3 dir = (piece.transform.position - (weakPoint ? weakPoint.position : hitPoint));
            if (dir.sqrMagnitude < 1e-6f) dir = Vector3.up;
            piece.Detach(dir + Vector3.up * 0.2f, cutDetachImpulse, enableGravity: true);
        }

        // 讓你看得到亮一下，再決定要不要隱藏 / 刪掉
        StartCoroutine(CoAfterCut(piece, hitPoint));
    }

    IEnumerator CoAfterCut(ShellPiece piece, Vector3 hitPoint)
    {
        yield return new WaitForSeconds(Mathf.Max(0.01f, cutFlashTime));

        if (cutHideDelay > 0f)
        {
            yield return new WaitForSeconds(cutHideDelay);
            piece.SetVisible(false);
        }

        if (cutAutoDestroyAfter > 0f)
            Destroy(piece.gameObject, cutAutoDestroyAfter);

        // 再算一次連通性：剪掉後，連不到核心的整串崩壞
        RecomputeConnectivity(hitPoint, allowDetachDisconnected: true);
    }

    public void RecomputeConnectivity(Vector3? cutPoint, bool allowDetachDisconnected)
    {
        // BFS：從 anchors 出發，標記所有「還連得到核心」的碎片
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

        int aliveCount = 0;

        // 沒被 visited 的 => 代表「已經連不到核心」
        if (onlyDetachAfterFirstCut && !_hasEverCut) allowDetachDisconnected = false;

        Vector3 impulseDir = Vector3.up;
        if (weakPoint) impulseDir = (cutPoint ?? weakPoint.position) - weakPoint.position;
        if (impulseDir.sqrMagnitude < 1e-6f) impulseDir = Vector3.up;
        impulseDir = impulseDir.normalized;

        foreach (var p in _pieces)
        {
            if (!p.alive) continue;
            aliveCount++;

            if (allowDetachDisconnected && !visited.Contains(p) && !p.detached)
            {
                p.SetCollidable(false); // 掉落的不用再被剪
                p.Detach(-impulseDir + Vector3.up * 0.3f, detachImpulse, enableGravity: true);

                if (autoDestroyAfter > 0f)
                    Destroy(p.gameObject, autoDestroyAfter);
            }
        }

        AllShellDestroyed = (aliveCount == 0);
    }
}

