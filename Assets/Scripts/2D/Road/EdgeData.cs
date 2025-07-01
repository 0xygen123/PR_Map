using UnityEngine;
using Newtonsoft.Json; // Newtonsoft.Jsonを使用

// エッジのデータ構造
[System.Serializable]
public class EdgeData
{
    [JsonProperty("edge_id")]
    public int edgeId;

    [JsonProperty("from_node_id")]
    public int fromNodeId;

    [JsonProperty("to_node_id")]
    public int toNodeId;

    [JsonProperty("cost")]
    public float cost;
}