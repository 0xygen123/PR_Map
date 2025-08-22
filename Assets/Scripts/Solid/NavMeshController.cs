using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;
using System.Linq;

namespace Assets.Scripts.Solid
{
    public class NavMeshController : MonoBehaviour
    {
        public (List<Vector3> indoorCoords, float indoorCost) FindPathAndCost(Vector3 startPosition, Vector3 endPosition)
        {
            NavMeshPath path = new NavMeshPath();

            if (NavMesh.CalculatePath(startPosition, endPosition, NavMesh.AllAreas, path))
            {
                // 経路が完全に見つかった場合のみ処理
                if (path.status == NavMeshPathStatus.PathComplete)
                {
                    // 経路の角（コーナー）の座標を取得
                    List<Vector3> corners = path.corners.ToList();
                    float totalCost = 0f;

                    // 経路の総距離を計算
                    for (int i = 0; i < corners.Count - 1; i++)
                    {
                        totalCost += Vector3.Distance(corners[i], corners[i + 1]);
                    }

                    return (corners, totalCost);
                }
            }

            // 経路が見つからなかった場合
            Debug.LogWarning($"NavMesh path not found from {startPosition} to {endPosition}.");
            return (new List<Vector3>(), -1f);
        }
    }
}