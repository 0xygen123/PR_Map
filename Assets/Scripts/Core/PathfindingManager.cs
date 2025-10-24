using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Assets.Scripts.Plane.Road;
using Assets.Scripts.Plane;
using Assets.Scripts.Solid;

namespace Assets.Scripts.Core
{
    public class PathfindingManager : MonoBehaviour
    {
        [SerializeField] RoadNetworkBuilder roadNetwork;
        [SerializeField] UserLocationManager userLocation;
        [SerializeField] int IndoorCostMultiple = 3;

        [Header("UI")]
        [SerializeField] TMPro.TMP_Text no3DMappingUI;

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


        //public async Task<PathResult> FindOptimalPathAsync(int startNodeId, BuildingData buildingData, string roomKey, GameObject buildingInstance, NavMeshController navMeshController)
        public Task<PathResult> FindOptimalPathAsync(int startNodeId, BuildingData buildingData, string roomKey, GameObject buildingInstance, NavMeshController navMeshController)
        {
            // 目的の部屋情報を取得
            RoomInfo destinationRoom = buildingData.GetRoomByKey(roomKey);
            if (destinationRoom == null)
            {
                Debug.LogError($"部屋が見つかりません: {roomKey}");
                return null;
            }

            // buildingInstance が渡されていない場合は 3D 表示を要求していないと見なす
            // → このケースでは "3D未対応" UI を表示しない
            if (buildingInstance == null)
            {
#if UNITY_EDITOR
                Debug.LogError("建物オブジェクトのインスタンス化に失敗しました");
#endif
                return null;
            }

            // buildingInstance が存在する（＝3D案内が要求されている）が NavMeshController が無ければ
            // 3D案内未対応として no3DMappingUI を表示する
            if (navMeshController == null)
            {
                if (no3DMappingUI != null)
                {
                    no3DMappingUI.enabled = true;
                }
                return null;
            }

            // NavMeshController がある場合は 3D 表示のエラーメッセージを非表示にする
            if (no3DMappingUI != null)
            {
                no3DMappingUI.enabled = false;
            }

            // 建物インスタンスから参照管理コンポーネントを取得
            BuildingReferenceProvider referenceProvider = buildingInstance.GetComponent<BuildingReferenceProvider>();
            if (referenceProvider == null)
            {
#if UNITY_EDITOR
                Debug.LogError("BuildingReferenceProviderが建物プレハブに見つかりません");
#endif
                return null;
            }

            // 目標地点の座標を取得
            Transform roomTransform = referenceProvider.GetReference(destinationRoom.roomKey);
            if (roomTransform == null)
            {
                Debug.LogError($"建物プレハブ内に部屋オブジェクト '{destinationRoom.roomKey}' が見つかりません。");
                return null;
            }
            // NavMesh探索用にワールド座標を取得
            Vector3 roomWorldPos = roomTransform.position;


            // 各入り口までの合計コスト
            List<PathResult> results = new List<PathResult>();
            Transform buildingTransform = buildingInstance.transform;

            // Task.Run() をやめて、通常の foreach ループに変更します
            foreach (var entrance in buildingData.entrances)
            {
                // 屋外経路の探索
                List<RuntimeNode> outdoorPathNodes = AStarFinder.FindPath(roadNetwork, startNodeId, entrance.outdoorNodeId);
                if (outdoorPathNodes == null || outdoorPathNodes.Count == 0)
                {
                    continue; // この入り口はスキップ
                }
                float outdoorCost = (float)outdoorPathNodes.Last().gCost;

                // メインスレッドで実行
                Transform entranceTransform = referenceProvider.GetReference(entrance.entranceKey);
                if (entranceTransform == null)
                {
                    Debug.LogWarning($"建物プレハブ内にエントランス '{entrance.entranceKey}' が見つかりません。");
                    continue;
                }
                // これでエラーなく Transform の position を取得できます
                Vector3 entranceWorldPos = entranceTransform.position;

                // 屋内経路の探索
                (List<Vector3> indoorCoords, float indoorCost) = navMeshController.FindPathAndCost(entranceWorldPos, roomWorldPos);
                if (indoorCost < 0)
                {
                    continue;
                }

#if UNITY_EDITOR
                Debug.Log($"{entrance.entranceKey}: [Costs]: {{ Indoor: {indoorCost}, Outdoor: {outdoorCost}, Total: {indoorCost + outdoorCost}}}");
#endif

                // 結果をリストに追加
                results.Add(new PathResult
                {
                    Entrance = entrance,
                    OutdoorPath = outdoorPathNodes,
                    IndoorPathCoordinates = indoorCoords,
                    OutdoorCost = outdoorCost,
                    IndoorCost = indoorCost * IndoorCostMultiple
                });
            }

            // 最小コストを計算
            if (results.Count == 0)
            {
                Debug.Log("有効なルートが存在しません");
                return null;
            }

            var optimalResult = results.OrderBy(r => r.TotalCost).First();
            //return optimalResult;
            return Task.FromResult(optimalResult);
        }

        public PathResult FindOptimalPath(int startNodeId, BuildingData buildingData)
        {
            List<PathResult> results = new List<PathResult>();
            foreach (EntranceInfo entrance in buildingData.entrances)
            {
                List<RuntimeNode> outdoorPathNodes = AStarFinder.FindPath(roadNetwork, startNodeId, entrance.outdoorNodeId);
                if (outdoorPathNodes == null || outdoorPathNodes.Count == 0)
                {
                    continue;
                }

                float outdoorCost = (float)outdoorPathNodes.Last().gCost;

#if UNITY_EDITOR
                Debug.Log($"[Costs] Indoor: {0}, Outdoor: {outdoorCost}, Total: {outdoorCost}");
#endif

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
}