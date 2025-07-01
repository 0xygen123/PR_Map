using UnityEngine;
using Newtonsoft.Json; // Newtonsoft.Jsonを使用

// ノードのデータ構造
[System.Serializable]
public class NodeData
{
    [JsonProperty("node_id")]
    public int nodeId;

    [JsonProperty("coordinate")]
    public double[] coordinate; // JSONでは [経度, 緯度] の順

    [JsonProperty("name")]
    public string name;
    
    // Position プロパティは RoadNetworkBuilder で変換するため削除
}

// EdgeData, RuntimeNode, RuntimeEdge クラスは変更なし
// [System.Serializable]
// public class EdgeData { ... }
// public class RuntimeNode { ... }
// public class RuntimeEdge { ... }