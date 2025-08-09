using UnityEngine;
using System.Collections.Generic;

namespace Assets.Scripts.Plane.Road
{
    // ランタイムでノードとエッジの関連付けを行うためのクラス
    public class RuntimeNode
    {
        public int nodeId;
        public Vector3 position;
        public string name;
        // このノードから伸びるエッジ
        public List<RuntimeEdge> connectedEdges = new List<RuntimeEdge>();

        // A*探索アルゴリズム
        public double gCost;
        public double hCost;
        public double FCost => gCost + hCost;
        public RuntimeNode parent;
    }
}