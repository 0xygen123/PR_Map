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


    public async Task<PathResult> FindOptimalPathAsync(int startNodeId, BuildingData buildingData, string roomKey, GameObject buildingInstance, NavMeshController navMeshController)
    {
        RoomInfo destinationRoom = buildingData.GetRoomByKey(roomKey);
        if (destinationRoom == null)
        {
            Debug.LogError($"部屋が見つかりません: {roomKey}");
            return null;
        }
        if (navMeshController == null)
        {
            Debug.LogError("NavMeshControllerが見つかりません");
            return null;
        }
        if (buildingInstance == null)
        {
            Debug.LogError("建物オブジェクトのインスタンス化に失敗しました");
            return null;
        }

        // 現在地のノードIDを取得
        RuntimeNode startNode = userLocation.GetStartNodeForPathfinding();
        if (startNode == null)
        {
            Debug.Log("現在地を取得できませんでした");
            return null;
        }

        // 各入り口までの合計コスト
        List<PathResult> results = new List<PathResult>();
        Transform buildingTransform = buildingInstance.transform;

        List<Task<PathResult>> tasks = new List<Task<PathResult>>();
        foreach (var entrance in buildingData.entrances)
        {
            // 屋外経路
            var outdoorPathNodes = AStarFinder.FindPath(roadNetwork, startNodeId, entrance.outdoorNodeId);
            if (outdoorPathNodes == null || outdoorPathNodes.Count == 0) { continue; }
            float outdoorCost = (float)outdoorPathNodes.Last().gCost;

            // 屋内経路
            Vector3 entranceWorldPos = buildingTransform.TransformPoint(entrance.indoorPosition);
            Vector3 roomWorldPos = buildingTransform.TransformPoint(destinationRoom.roomPosition);

            (List<Vector3> indoorCoords, float indoorCost) = navMeshController.FindPathAndCost(entrance.indoorPosition, destinationRoom.roomPosition);
            if (indoorCost < 0) { return null; }

            results.Add(new PathResult
            {
                Entrance = entrance,
                OutdoorPath = outdoorPathNodes,
                IndoorPathCoordinates = indoorCoords,
                OutdoorCost = outdoorCost,
                IndoorCost = indoorCost
            });
        }

        // タスクの完了を待つ
        PathResult[] completedResults = await Task.WhenAll(tasks);

        // 非nullのデータをリストに追加
        results = completedResults.Where(r => r != null).ToList();

        // 最小コストを計算
        if (results.Count == 0)
        {
            Debug.Log("有効なルートが存在しません");
            return null;
        }

        return results.OrderBy(r => r.TotalCost).First();
    }

    public PathResult FindOptimalPath(int startNodeId, BuildingData buildingData)
    {
        List<PathResult> results = new List<PathResult>();
        foreach (EntranceInfo entrance in buildingData.entrances)
        {
            List<RuntimeNode> outdoorPathNodes = AStarFinder.FindPath(roadNetwork, startNodeId, entrance.outdoorNodeId);
            if (outdoorPathNodes == null || outdoorPathNodes.Count == 0) { continue; }

            float outdoorCost = (float)outdoorPathNodes.Last().gCost;

            results.Add(new PathResult
            {
                Entrance = entrance,
                OutdoorPath = outdoorPathNodes,
                IndoorPathCoordinates = null,
                OutdoorCost = outdoorCost,
                IndoorCost = 0
            });
        }
        if (results.Count == 0)
        {
            Debug.LogError("有効なルートが存在しません");
            return null;
        }

        return results.OrderBy(r => r.TotalCost).First();
    }
}