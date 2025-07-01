using UnityEngine;
using System.Collections.Generic;
using Newtonsoft.Json; // Newtonsoft.Jsonを使用

// グラフ全体を保持するシンプルなコンテナクラス (オプション)
public class GraphData
{
    public List<NodeData> nodes;
    public List<EdgeData> edges;
}
