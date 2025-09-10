// using UnityEngine;
// using UnityEngine.AI;
// using System.Collections.Generic;
// using System.Linq;

// namespace Assets.Scripts.Solid
// {
//     public class NavMeshController : MonoBehaviour
//     {
//         public (List<Vector3> indoorCoords, float indoorCost) FindPathAndCost(Vector3 startPosition, Vector3 endPosition)
//         {
//             NavMeshPath path = new NavMeshPath();

//             if (NavMesh.CalculatePath(startPosition, endPosition, NavMesh.AllAreas, path))
//             {
//                 // 経路が完全に見つかった場合のみ処理
//                 if (path.status == NavMeshPathStatus.PathComplete)
//                 {
//                     // 経路の角（コーナー）の座標を取得
//                     List<Vector3> corners = path.corners.ToList();
//                     float totalCost = 0f;

//                     // 経路の総距離を計算
//                     for (int i = 0; i < corners.Count - 1; i++)
//                     {
//                         totalCost += Vector3.Distance(corners[i], corners[i + 1]);
//                     }

//                     return (corners, totalCost);
//                 }
//             }

//             // 経路が見つからなかった場合
//             Debug.LogWarning($"NavMesh path not found from {startPosition} to {endPosition}.");
//             return (new List<Vector3>(), -1f);
//         }
//     }
// }
using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;
using System.Linq;

namespace Assets.Scripts.Solid
{
    public class NavMeshController : MonoBehaviour
    {
        // 経路探索とコスト計算を行う
        public (List<Vector3> indoorCoords, float indoorCost) FindPathAndCost(Vector3 startPosition, Vector3 endPosition)
        {
            NavMeshPath path = new NavMeshPath();

            if (NavMesh.CalculatePath(startPosition, endPosition, NavMesh.AllAreas, path))
            {
                if (path.status == NavMeshPathStatus.PathComplete)
                {
                    List<Vector3> corners = path.corners.ToList();
                    
                    // 経路を詳細化して地形に沿わせる
                    List<Vector3> refinedPath = RefinePath(corners, 1.0f); // 第2引数で補間距離を調整
                    
                    float totalCost = 0f;
                    for (int i = 0; i < refinedPath.Count - 1; i++)
                    {
                        totalCost += Vector3.Distance(refinedPath[i], refinedPath[i + 1]);
                    }

                    return (refinedPath, totalCost);
                }
            }

            Debug.LogWarning($"NavMesh path not found from {startPosition} to {endPosition}.");
            return (new List<Vector3>(), -1f);
        }
        
        /// <summary>
        /// NavMeshの経路コーナーリストを、地形に沿った詳細なポイントリストに変換する
        /// </summary>
        /// <param name="corners">NavMeshPath.cornersから取得したコーナーのリスト</param>
        /// <param name="maxSegmentLength">ポイント間の最大距離</param>
        /// <returns>詳細化された経路上のポイントリスト</returns>
        private List<Vector3> RefinePath(List<Vector3> corners, float maxSegmentLength)
        {
            if (corners.Count < 2)
            {
                return corners;
            }

            var refinedPath = new List<Vector3>();
            refinedPath.Add(corners[0]);

            for (int i = 0; i < corners.Count - 1; i++)
            {
                Vector3 start = corners[i];
                Vector3 end = corners[i + 1];
                float distance = Vector3.Distance(start, end);
                int segments = Mathf.CeilToInt(distance / maxSegmentLength);

                if (segments <= 1)
                {
                     refinedPath.Add(end);
                     continue;
                }
                
                for (int j = 1; j <= segments; j++)
                {
                    float t = (float)j / segments;
                    Vector3 pointOnLine = Vector3.Lerp(start, end, t);

                    // NavMesh上の最も近い点をサンプリングする
                    if (NavMesh.SamplePosition(pointOnLine, out NavMeshHit hit, 2.0f, NavMesh.AllAreas))
                    {
                        refinedPath.Add(hit.position);
                    }
                    else
                    {
                        // サンプリングに失敗した場合は、元の線上の点をそのまま使う
                        refinedPath.Add(pointOnLine);
                    }
                }
            }
            return refinedPath;
        }
    }
}