using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;
using System.Linq;

public class DEVNavMeshDebugger : MonoBehaviour
{
    [SerializeField] private Transform startPosition;
    [SerializeField] private Transform endPosition;
    [SerializeField] private bool autoStartRouteSearch;

    private NavMeshPath path;
    private float totalPathDistance;
    private bool pathFound = false;

    void Start()
    {
        if (autoStartRouteSearch)
        {
            StartRouteSearch();
        }
    }

    void StartRouteSearch()
    {
        path = new NavMeshPath();
        if (NavMesh.CalculatePath(startPosition.position, endPosition.position, NavMesh.AllAreas, path))
        {
            // 経路が完全に見つかった場合のみ処理
            if (path.status == NavMeshPathStatus.PathComplete)
            {
                pathFound = true;
                // 経路の総距離を計算
                totalPathDistance = 0f;
                List<Vector3> corners = path.corners.ToList();
                for (int i = 0; i < corners.Count - 1; i++)
                {
                    totalPathDistance += Vector3.Distance(corners[i], corners[i + 1]);
                }
                Debug.Log($"経路が見つかりました！ 総距離: {totalPathDistance:F2}m");
            }
            else
            {
                pathFound = false;
                Debug.LogWarning("経路が見つかりませんでした。");
            }
        }
    }

    // ギズモとして経路をレンダリング
    private void OnDrawGizmos()
    {
        if (pathFound && path != null && path.corners.Length > 1)
        {
            Gizmos.color = Color.cyan;
            Vector3 previousCorner = path.corners[0];
            // 経路の各セグメントを線で描画
            for (int i = 1; i < path.corners.Length; i++)
            {
                Gizmos.DrawLine(previousCorner, path.corners[i]);
                Gizmos.DrawSphere(path.corners[i], 0.1f); // 角に小さな球体を描画
                previousCorner = path.corners[i];
            }

            // スタートとゴール地点のマーク
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(startPosition.position, 0.2f);
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(endPosition.position, 0.2f);
        }
    }
}