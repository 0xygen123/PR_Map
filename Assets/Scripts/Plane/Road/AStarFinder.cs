using UnityEngine;
using System.Collections.Generic;


namespace Assets.Scripts.Plane.Road
{
    /// <summary>
    /// A*アルゴリズムによる経路探索機能を提供します。
    /// </summary>
    public static class AStarFinder
    {
        /// <summary>
        /// 指定されたノード間の最短経路を探索します。
        /// </summary>
        /// <returns>見つかった経路（RuntimeNodeのリスト）見つからない場合はnull。</returns>
        public static List<RuntimeNode> FindPath(RoadNetworkBuilder network, int startNodeId, int goalNodeId)
        {
            RuntimeNode startNode = network.GetNodeById(startNodeId);
            RuntimeNode goalNode = network.GetNodeById(goalNodeId);

            if (startNode == null || goalNode == null)
            {
                Debug.LogError("Start or Goal node ID is not valid.");
                return null;
            }

            List<RuntimeNode> openSet = new List<RuntimeNode>();
            HashSet<RuntimeNode> closedSet = new HashSet<RuntimeNode>();
            openSet.Add(startNode);

            // 全ノードのコストを初期化
            foreach (var node in network.RuntimeNodes.Values)
            {
                node.gCost = double.PositiveInfinity;
                node.parent = null;
            }

            startNode.gCost = 0;
            startNode.hCost = Heuristic(startNode, goalNode);

            while (openSet.Count > 0)
            {
                // openSetの中でfCostが最も低いノードを取得（Linqを使わずにループで実装）
                RuntimeNode currentNode = openSet[0];
                for (int i = 1; i < openSet.Count; i++)
                {
                    if (openSet[i].FCost < currentNode.FCost ||
                    (openSet[i].FCost == currentNode.FCost && openSet[i].hCost < currentNode.hCost))
                    {
                        currentNode = openSet[i];
                    }
                }

                // ゴールに到達
                if (currentNode == goalNode)
                {
                    return ReconstructPath(currentNode); // RuntimeNodeのリストを返す
                }

                openSet.Remove(currentNode);
                closedSet.Add(currentNode);

                foreach (var neighborNode in GetNeighbors(currentNode))
                {
                    if (closedSet.Contains(neighborNode)) continue;

                    double tentativeGCost = currentNode.gCost + (double)Cost(currentNode, neighborNode);

                    if (tentativeGCost < neighborNode.gCost)
                    {
                        neighborNode.parent = currentNode;
                        neighborNode.gCost = tentativeGCost;
                        neighborNode.hCost = Heuristic(neighborNode, goalNode);

                        if (!openSet.Contains(neighborNode))
                        {
                            openSet.Add(neighborNode);
                        }
                    }
                }
            }

            return null; // 経路が見つからなかった
        }

        // ヒューリスティック関数（ゴールまでの推定コスト）
        static double Heuristic(RuntimeNode nodeA, RuntimeNode nodeB)
        {
            return Vector3.Distance(nodeA.position, nodeB.position);
        }

        // ２ノード間の移動コスト
        static float Cost(RuntimeNode fromNode, RuntimeNode toNode)
        {
            foreach (var edge in fromNode.connectedEdges)
            {
                if (edge.toNode == toNode || edge.fromNode == toNode)
                {
                    return edge.cost;
                }
            }
            return float.PositiveInfinity;
        }

        // 隣接ノードを取得
        static IEnumerable<RuntimeNode> GetNeighbors(RuntimeNode node)
        {
            foreach (var edge in node.connectedEdges)
            {
                yield return edge.fromNode == node ? edge.toNode : edge.fromNode;
            }
        }

        // ゴールから親をたどって経路を復元
        static List<RuntimeNode> ReconstructPath(RuntimeNode endNode)
        {
            List<RuntimeNode> path = new List<RuntimeNode>();
            RuntimeNode currentNode = endNode;
            while (currentNode != null)
            {
                path.Add(currentNode);
                currentNode = currentNode.parent;
            }
            path.Reverse(); // スタートからゴールの順にする
            return path;
        }
    }
}