using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Assets.Scripts.Plane.Road;
using Assets.Scripts.Core;
using Assets.Scripts.Plane;
using Assets.Scripts.Solid;

public class PathfindingManager : MonoBehaviour
{
    [SerializeField] RoadNetworkBuilder roadNetwork;
    [SerializeField] UserLocationManager userLocation;

    // 計算結果を格納するクラス
    public class PathResult
    {
        public EntranceInfo Entrance { get; set; }
        public List<RuntimeNode> OutdoorPath { get; set; } 
        public List<Vector3> IndoorPathCoordinates { get; set; }
        public float OutdoorCost { get; set; }
        public float IndoorCost { get; set; }
        public float TotalCost => OutdoorCost + IndoorCost;
    }


    public async Task<PathResult> FindOptimalPathAsync(int startNodeId, BuildingData buildingData, string roomKey, NavMeshController navMeshController)
    {
        RoomInfo destinationRoom = buildingData.GetRoomByKey(roomKey);
        if (destinationRoom == null)
        {
            Debug.LogError($"部屋が見つかりません: {roomKey}");
            return null;
        }

        // 現在地のノードIDを取得
        var startNode = userLocation.GetStartNodeForPathfinding();
        if (startNode == null)
        {
            Debug.Log("現在地を取得できませんでした");
            return null;
        }

        // 各入り口までの合計コスト
        List<PathResult> results = new List<PathResult>();
        foreach (var entrance in buildingData.entrances)
        {
            // --- 屋外経路 ---
            var outdoorPathNodes = AStarFinder.FindPath(roadNetwork, startNodeId, entrance.outdoorNodeId);
            if (outdoorPathNodes == null || outdoorPathNodes.Count == 0) continue;

            float outdoorCost = (float)outdoorPathNodes.Last().gCost;

            // --- 屋内経路 ---
            (List<Vector3> indoorCoords, float indoorCost) = navMeshController.FindPathAndCost(entrance.indoorPosition, destinationRoom.roomPosition);
            if (indoorCost < 0) continue;

            results.Add(new PathResult
            {
                Entrance = entrance,
                OutdoorPath = outdoorPathNodes,
                IndoorPathCoordinates = indoorCoords,
                OutdoorCost = outdoorCost,
                IndoorCost = indoorCost
            });
        }

        // 最小コストを計算
        if (results.Count == 0)
        {
            Debug.Log("有効なルートが存在しません");
            return null;
        }

        return results.OrderBy(r => r.TotalCost).First();
    }
}