using UnityEngine;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using System.Linq;

public class RoadNetworkBuilder : MonoBehaviour
{
    [Header("JSON Data Files")]
    public TextAsset nodesJsonFile; // campus_nodes.jsonをアサイン
    public TextAsset edgesJsonFile; // campus_edges.jsonをアサイン

    [Header("Road Visualization Settings")]
    public Material roadMaterial; // 道路表示用のマテリアル (Inspectorで設定)
    public float roadWidth = 0.5f; // 道路の幅
    public Color lineColor = Color.green; // 道路の色

    // 構築されたロードネットワーク
    private Dictionary<int, RuntimeNode> runtimeNodes = new Dictionary<int, RuntimeNode>();
    private List<RuntimeEdge> runtimeEdges = new List<RuntimeEdge>();

    // 外部からアクセスするためのプロパティ
    public IReadOnlyDictionary<int, RuntimeNode> RuntimeNodes => runtimeNodes;
    public IReadOnlyList<RuntimeEdge> RuntimeEdges => runtimeEdges;

    // 緯度経度からUnity座標への変換設定
    [Header("Coordinate Transformation (Lat/Lon to Unity)")]
    // double型に変更
    public double centerLatitude = 34.964962019263758;   // 噴水の緯度 (GPS N)
    public double centerLongitude = 135.940185503739031; // 噴水の経度 (GPS E)
    
    // double型に変更
    public const double METERS_PER_DEGREE_LAT = 111320.0; 
    private double metersPerDegreeLon;
    public double MetersPerDegreeLon{ get{ return metersPerDegreeLon; } }

    void Awake()
    {
        // double型で計算
        metersPerDegreeLon = 40075000.0 * System.Math.Cos(centerLatitude * System.Math.PI / 180.0) / 360.0;
        Debug.Log($"Calculated meters per degree longitude at {centerLatitude}N: {metersPerDegreeLon:F2}m");
        LoadRoadNetwork();
    }

    private void LoadRoadNetwork()
    {
        if (nodesJsonFile == null || edgesJsonFile == null)
        {
            Debug.LogError("JSON files are not assigned in the inspector!");
            return;
        }

        Debug.Log("Loading Road Network...");

        List<NodeData> rawNodes = JsonConvert.DeserializeObject<List<NodeData>>(nodesJsonFile.text);
        if (rawNodes == null) { Debug.LogError("Failed to deserialize nodes JSON."); return; }

        foreach (var nodeData in rawNodes)
        {
            // double型で取得
            double lon = nodeData.coordinate[0]; 
            double lat = nodeData.coordinate[1]; 

            // double型で計算し、Vector3に変換する際にfloatにキャスト
            Vector3 unityPosition = new Vector3(
                (float)((lon - centerLongitude) * metersPerDegreeLon), 
                (float)((lat - centerLatitude) * METERS_PER_DEGREE_LAT), 
                0f                                           
            );

            RuntimeNode newNode = new RuntimeNode
            {
                nodeId = nodeData.nodeId,
                position = unityPosition,
                name = nodeData.name
            };
            runtimeNodes.Add(newNode.nodeId, newNode);
        }
        Debug.Log($"Loaded {runtimeNodes.Count} nodes.");

        List<EdgeData> rawEdges = JsonConvert.DeserializeObject<List<EdgeData>>(edgesJsonFile.text);
        if (rawEdges == null) { Debug.LogError("Failed to deserialize edges JSON."); return; }

        foreach (var edgeData in rawEdges)
        {
            RuntimeNode fromNode;
            RuntimeNode toNode;

            if (runtimeNodes.TryGetValue(edgeData.fromNodeId, out fromNode) &&
                runtimeNodes.TryGetValue(edgeData.toNodeId, out toNode))
            {
                RuntimeEdge newEdge = new RuntimeEdge
                {
                    edgeId = edgeData.edgeId,
                    fromNode = fromNode,
                    toNode = toNode,
                    cost = edgeData.cost
                };
                runtimeEdges.Add(newEdge);

                fromNode.connectedEdges.Add(newEdge); 
            }
            else
            {
                Debug.LogWarning($"Edge {edgeData.edgeId} refers to non-existent nodes (from:{edgeData.fromNodeId}, to:{edgeData.toNodeId}). Skipping.");
            }
        }
        Debug.Log($"Loaded {runtimeEdges.Count} edges.");

        VisualizeRoads();
    }

    private void VisualizeRoads()
    {
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }

        if (roadMaterial == null)
        {
            Debug.LogWarning("Road Material is not assigned. Cannot visualize roads.");
            return;
        }

        foreach (var edge in runtimeEdges)
        {
            GameObject roadSegment = new GameObject($"Edge_{edge.edgeId}");
            roadSegment.transform.SetParent(this.transform);

            LineRenderer lineRenderer = roadSegment.AddComponent<LineRenderer>();
            lineRenderer.material = roadMaterial;
            lineRenderer.startWidth = roadWidth;
            lineRenderer.endWidth = roadWidth;
            lineRenderer.positionCount = 2;
            lineRenderer.SetPosition(0, edge.fromNode.position);
            lineRenderer.SetPosition(1, edge.toNode.position);
            lineRenderer.useWorldSpace = true;
            lineRenderer.startColor = lineColor;
            lineRenderer.endColor = lineColor;
        }
        Debug.Log("Roads visualized with Line Renderers.");
    }

    // RoadNetworkBuilder.cs に追加
    public Vector3 ConvertLatLonToUnityPosition(double lat, double lon)
    {
        // metersPerDegreeLonがAwakeで計算されていることを確認
        // 必要であればAwakeの計算結果をprivateフィールドに保存し、そのフィールドを使用
        double currentMetersPerDegreeLon = 40075000.0 * System.Math.Cos(centerLatitude * System.Math.PI / 180.0) / 360.0; // または保存されたフィールドを使用

        return new Vector3(
            (float)((lon - centerLongitude) * currentMetersPerDegreeLon),
            (float)((lat - centerLatitude) * METERS_PER_DEGREE_LAT),
            0f
        );
    }

#if UNITY_EDITOR
    // デバッグ用のGizmos (Editorのみで表示)
    void OnDrawGizmos()
    {
        if (runtimeNodes == null || runtimeEdges == null) return;

        // ノードの描画 (Gizmos)
        Gizmos.color = Color.blue;
        foreach (var nodeEntry in runtimeNodes)
        {
            // ノードを球で表示
            Gizmos.DrawSphere(nodeEntry.Value.position, 0.1f);

            // UnityEditor.Handles.LabelはUNITY_EDITORディレクティブ内でなければエラーになるため、ここに追加
            #if UNITY_EDITOR
            // ノードの名前とIDを表示
            string labelText = $"{nodeEntry.Value.name ?? "NoName"} (ID: {nodeEntry.Value.nodeId})";
            UnityEditor.Handles.Label(nodeEntry.Value.position + Vector3.up * 1f, labelText);
            #endif
        }

        // エッジの描画 (Gizmos)
        Gizmos.color = lineColor; 
        foreach (var edge in runtimeEdges)
        {
            Gizmos.DrawLine(edge.fromNode.position, edge.toNode.position);
            #if UNITY_EDITOR
            // エッジの中心にコストを表示 (Editor only)
            Vector3 center = (edge.fromNode.position + edge.toNode.position) / 2f;
            UnityEditor.Handles.Label(center + Vector3.up * 0.5f, edge.cost.ToString("F1"));
            #endif
        }
    }
#endif
}