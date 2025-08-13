using UnityEngine;
using Newtonsoft.Json; // Newtonsoft.Jsonを使用

namespace Assets.Scripts.Plane.Road
{
    // ノードのデータ構造
    [System.Serializable]
    public class NodeData
    {
        [JsonProperty("node_id")]
        public int nodeId;

        // JSONでは [経度, 緯度] の順
        [JsonProperty("coordinate")]
        public double[] coordinate;

        [JsonProperty("name")]
        public string name;
    }
}