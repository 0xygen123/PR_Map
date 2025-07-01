using UnityEngine;
using System.Collections.Generic;

// ランタイムでノードとエッジの関連付けを行うためのクラス
public class RuntimeNode
{
    public int nodeId;
    public Vector3 position;
    public string name;
    public List<RuntimeEdge> connectedEdges = new List<RuntimeEdge>(); // このノードから伸びるエッジ
}