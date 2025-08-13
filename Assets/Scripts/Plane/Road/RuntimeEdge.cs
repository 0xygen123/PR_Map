using UnityEngine;

namespace Assets.Scripts.Plane.Road
{
    public class RuntimeEdge
    {
        public int edgeId;
        public RuntimeNode fromNode;
        public RuntimeNode toNode;
        public float cost;
    }
}