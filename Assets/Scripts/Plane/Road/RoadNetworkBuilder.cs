using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace Assets.Scripts.Plane.Road
{
    public class RoadNetworkBuilder : MonoBehaviour
    {
        [Header("JSON Data Files")]
        [SerializeField] TextAsset nodesJsonFile;
        [SerializeField] TextAsset edgesJsonFile;

        [Header("Road Visualization Settings")]
        [SerializeField] Material roadMaterial;
        [SerializeField] Material pathMaterial;
        [SerializeField] float roadWidth = 0.5f;

        // --- データ管理用の変数 ---
        Dictionary<int, RuntimeNode> runtimeNodes = new Dictionary<int, RuntimeNode>();
        List<RuntimeEdge> runtimeEdges = new List<RuntimeEdge>();
        Dictionary<int, LineRenderer> roadRenderers = new Dictionary<int, LineRenderer>();

        public IReadOnlyDictionary<int, RuntimeNode> RuntimeNodes => runtimeNodes;
        public IReadOnlyList<RuntimeEdge> RuntimeEdges => runtimeEdges;

        [Header("Coordinate Transformation (Lat/Lon to Unity)")]
        public double centerLatitude = 34.964962019263758;
        public double centerLongitude = 135.940185503739031;
        public const double METERS_PER_DEGREE_LAT = 111320.0;
        double metersPerDegreeLon;
        public double MetersPerDegreeLon => metersPerDegreeLon;

        [Header("Object Layer Settings")]
        [SerializeField] string roadLayerName = "2DMap";

        GameObject roadContainer;

        void Awake()
        {
            metersPerDegreeLon = 40075000.0 * System.Math.Cos(centerLatitude * System.Math.PI / 180.0) / 360.0;
            LoadRoadNetwork();
            VisualizeRoads();
        }

        public void FindPath(int startNodeId, int goalNodeId)
        {
            List<RuntimeNode> path = AStarFinder.FindPath(this, startNodeId, goalNodeId);

            if (path != null && path.Count > 0)
            {
                var pathIds = path.Select(p => p.nodeId);
                Debug.Log($"Path found: {string.Join(" -> ", pathIds)}");
                VisualizePathByChangingMaterial(path);
            }
            else
            {
                Debug.LogWarning($"Path not found from {startNodeId} to {goalNodeId}.");
                ResetAllRoadMaterials();
            }
        }

        public RuntimeNode GetNodeById(int nodeId)
        {
            runtimeNodes.TryGetValue(nodeId, out RuntimeNode node);
            return node;
        }

        public void VisualizePathByChangingMaterial(List<RuntimeNode> path)
        {
            // まず、全ての道路をデフォルトのマテリアルに戻す
            ResetAllRoadMaterials();

            if (pathMaterial == null)
            {
                Debug.LogWarning("Path Material is not set. Cannot visualize path.");
                return;
            }

            // 経路上のエッジのマテリアルをpathMaterialに変更する
            for (int i = 0; i < path.Count - 1; i++)
            {
                RuntimeNode fromNode = path[i];
                RuntimeNode toNode = path[i + 1];

                // 2つのノードを繋ぐエッジを探す
                RuntimeEdge edge = fromNode.connectedEdges.FirstOrDefault(e =>
                    (e.fromNode == fromNode && e.toNode == toNode) ||
                    (e.toNode == fromNode && e.fromNode == toNode)
                );

                if (edge != null)
                {
                    // 対応するLineRendererを取得してマテリアルを変える
                    if (roadRenderers.TryGetValue(edge.edgeId, out LineRenderer renderer))
                    {
                        renderer.material = pathMaterial;
                    }
                }
            }
        }

        public void ResetAllRoadMaterials()
        {
            if (roadMaterial == null) return;

            foreach (var renderer in roadRenderers.Values)
            {
                renderer.material = roadMaterial;
            }
        }

        public Vector3 ConvertLatLonToUnityPosition(double lat, double lon)
        {
            return new Vector3(
                (float)((lon - centerLongitude) * metersPerDegreeLon),
                (float)((lat - centerLatitude) * METERS_PER_DEGREE_LAT),
                0f
            );
        }

        void LoadRoadNetwork()
        {
            if (nodesJsonFile == null || edgesJsonFile == null) return;
            List<NodeData> rawNodes = JsonConvert.DeserializeObject<List<NodeData>>(nodesJsonFile.text);
            foreach (var nodeData in rawNodes)
            {
                Vector3 unityPosition = ConvertLatLonToUnityPosition(nodeData.coordinate[1], nodeData.coordinate[0]);
                runtimeNodes.Add(nodeData.nodeId, new RuntimeNode { nodeId = nodeData.nodeId, position = unityPosition, name = nodeData.name });
            }
            List<EdgeData> rawEdges = JsonConvert.DeserializeObject<List<EdgeData>>(edgesJsonFile.text);
            if (rawEdges == null) return;
            foreach (var edgeData in rawEdges)
            {
                if (runtimeNodes.TryGetValue(edgeData.fromNodeId, out RuntimeNode fromNode) && runtimeNodes.TryGetValue(edgeData.toNodeId, out RuntimeNode toNode))
                {
                    RuntimeEdge newEdge = new RuntimeEdge { edgeId = edgeData.edgeId, fromNode = fromNode, toNode = toNode, cost = edgeData.cost };
                    runtimeEdges.Add(newEdge);
                    fromNode.connectedEdges.Add(newEdge);
                    toNode.connectedEdges.Add(newEdge);
                }
            }
        }

        void VisualizeRoads()
        {
            if (roadContainer != null) Destroy(roadContainer);

            roadContainer = new GameObject("Roads");
            roadContainer.transform.SetParent(transform);

            if (roadMaterial == null)
            {
                Debug.LogError("Road Material is not set!");
                return;
            }

            int roadLayer = LayerMask.NameToLayer(roadLayerName);
            if (roadLayer == -1) // レイヤーが存在しない場合のエラーチェック
            {
                Debug.LogError($"Layer '{roadLayerName}' not found. Please create it in the Tag and Layers settings.");
                roadLayer = 0;
                return;
            }

            foreach (var edge in runtimeEdges)
            {
                GameObject roadSegment = new GameObject($"Edge_{edge.edgeId}");
                roadSegment.transform.SetParent(roadContainer.transform);
                roadSegment.layer = roadLayer;
                LineRenderer lineRenderer = roadSegment.AddComponent<LineRenderer>();

                lineRenderer.material = roadMaterial;
                lineRenderer.startWidth = roadWidth;
                lineRenderer.endWidth = roadWidth;

                lineRenderer.SetPositions(new Vector3[] { edge.fromNode.position, edge.toNode.position });

                // Sorting Orderを設定して、他のオブジェクトに隠れないようにする
                lineRenderer.sortingOrder = 1;

                roadRenderers.Add(edge.edgeId, lineRenderer);
            }
            // Debug.Log($"Roads visualized. {roadRenderers.Count} renderers stored.");
        }

#if UNITY_EDITOR
        void OnDrawGizmos()
        {
            // OnDrawGizmosはエディタ上での表示なので、色は固定で問題ない
            if (runtimeNodes == null || runtimeEdges == null) return;

            foreach (var nodeEntry in runtimeNodes)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawSphere(nodeEntry.Value.position, 0.1f);
                string labelText = $"{nodeEntry.Value.name ?? "NoName"} (ID: {nodeEntry.Value.nodeId})";
                UnityEditor.Handles.Label(nodeEntry.Value.position + Vector3.up * 1f, labelText);
            }

            Gizmos.color = Color.gray;
            foreach (var edge in runtimeEdges)
            {
                Gizmos.DrawLine(edge.fromNode.position, edge.toNode.position);
                Vector3 center = (edge.fromNode.position + edge.toNode.position) / 2f;
                UnityEditor.Handles.Label(center + Vector3.up * 0.5f, edge.cost.ToString("F1"));
            }
        }
#endif
    }
}