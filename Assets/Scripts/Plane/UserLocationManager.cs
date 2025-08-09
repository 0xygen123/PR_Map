using UnityEngine;
using System.Linq;

using Assets.Scripts.Plane.Road;

namespace Assets.Scripts.Plane
{
    public class UserLocationManager : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] RoadNetworkBuilder roadNetworkBuilder; // InspectorでRoadNetworkBuilderを持つオブジェクトをアサイン
        [SerializeField] Transform userTransform; // Inspectorでユーザーを表すオブジェクトをアサイン

        [Header("Movement Settings")]
        [SerializeField] float moveSpeed = 1.5f; // スムーズ移動の速度
        RuntimeEdge currentEdge;
        public RuntimeEdge CurrentEdge => currentEdge;


        Vector3 targetPosition; // 移動目標位置
        bool hasInitialPosition = false;

        void Start()
        {
            if (roadNetworkBuilder == null || userTransform == null)
            {
                Debug.LogError("Dependencies are not assigned in UserLocationManager!");
                enabled = false;
                return;
            }
            // 初期位置を現在のtransformの位置に設定
            targetPosition = userTransform.position;
        }

        void Update()
        {
            // targetPositionに向かってスムーズに移動
            userTransform.position = Vector3.Lerp(
                userTransform.position,
                targetPosition,
                Time.deltaTime * moveSpeed
            );
        }

        void OnEnable()
        {
            JSInterface.OnLocationReceived += UpdateLocation;
            JSInterface.OnDirectionReceived += UpdateDirection;
        }

        void OnDisable()
        {
            JSInterface.OnLocationReceived -= UpdateLocation;
            JSInterface.OnDirectionReceived -= UpdateDirection;       
        }

        /// <summary>
        /// 位置情報の更新をするメソッド
        /// </summary>
        /// <param name="latitude">緯度</param>
        /// <param name="longitude">経度</param>
        void UpdateLocation(double latitude, double longitude)
        {
            // 緯度経度をUnityのワールド座標に変換
            Vector3 rawUnityPosition = roadNetworkBuilder.ConvertLatLonToUnityPosition(latitude, longitude);

            // 最も近い道路上の点（スナップする座標）を見つける
            // Vector3 snappedPosition = FindNearestPointOnRoadNetwork(rawUnityPosition);
            var (snappedPosition, nearestEdge) = FindNearestPointOnRoadNetwork(rawUnityPosition);

            // 移動目標位置を更新
            targetPosition = snappedPosition;
            currentEdge = nearestEdge;

            // 最初の位置情報を受け取った場合、ワープ感をなくすために即座に位置を反映
            if (!hasInitialPosition)
            {
                userTransform.position = targetPosition;
                hasInitialPosition = true;
            }
        }

        /// <summary>
        /// 向きを更新をするメソッド
        /// </summary>
        /// <param name="angle">方位(度数法)</param>
        void UpdateDirection(float angle)
        {
            // オイラー角ver
            // Vector3 currentRotation = transform.eulerAngles;
            // currentRotation.z = angle;
            // transform.eulerAngles = currentRotation;

            // クオータニオンver
            userTransform.transform.rotation = Quaternion.Euler(0, 0, angle);
        }

        /// <summary>
        /// 経路探索の開始ノードを決定します。
        /// 現在スナップしているエッジの両端のうち、ユーザーの現在位置に近い方のノードを返します。
        /// </summary>
        /// <returns>経路探索の開始ノード。見つからない場合はnull。</returns>
        public RuntimeNode GetStartNodeForPathfinding()
        {
            if (CurrentEdge == null)
            {
                Debug.LogWarning("Cannot determine start node because user is not snapped to any edge.");
                return null;
            }

            // ユーザーの現在位置とエッジの両端ノードとの距離を比較
            float distToFromNode = Vector3.Distance(userTransform.position, CurrentEdge.fromNode.position);
            float distToToNode = Vector3.Distance(userTransform.position, CurrentEdge.toNode.position);

            // より近い方のノードを返す
            if (distToFromNode < distToToNode)
            {
                return CurrentEdge.fromNode;
            }
            else
            {
                return CurrentEdge.toNode;
            }
        }

        (Vector3, RuntimeEdge) FindNearestPointOnRoadNetwork(Vector3 point)
        {
            var allEdges = roadNetworkBuilder.RuntimeEdges;
            if (allEdges == null || !allEdges.Any())
            {
                Debug.LogWarning("No road network edges found.");
                return (point, null);
            }

            Vector3 nearestPoint = Vector3.zero;
            RuntimeEdge nearestEdge = null;
            float minDistanceSquared = float.MaxValue;

            // 全てのエッジをチェックして、最も近い点を探す
            // >> DEV REFACTORING すべてのエッジは激重にならんか?.
            foreach (var edge in allEdges)
            {
                Vector3 closestPointOnEdge = FindNearestPointOnLineSegment(edge.fromNode.position, edge.toNode.position, point);
                float distanceSquared = (point - closestPointOnEdge).sqrMagnitude;

                if (distanceSquared < minDistanceSquared)
                {
                    minDistanceSquared = distanceSquared;
                    nearestPoint = closestPointOnEdge;
                    nearestEdge = edge;
                }
            }

            // Z座標をuserTransformの現在のZ座標に合わせる（必要に応じて）
            nearestPoint.z = userTransform.position.z;

            return (nearestPoint, nearestEdge);
        }

        /// <summary>
        /// 線分 AB 上で、点 P に最も近い点を計算します。
        /// </summary>
        /// <param name="a">線分の始点</param>
        /// <param name="b">線分の終点</param>
        /// <param name="p">点</param>
        /// <returns>線分上の最近接点</returns>
        Vector3 FindNearestPointOnLineSegment(Vector3 a, Vector3 b, Vector3 p)
        {
            Vector3 ab = b - a;
            Vector3 ap = p - a;

            float projection = Vector3.Dot(ap, ab);
            float abLengthSquared = ab.sqrMagnitude;

            // ab の長さがほぼ0の場合は、始点 a を返す
            if (abLengthSquared < 0.0001f)
            {
                return a;
            }

            // 射影の比率を計算
            float t = projection / abLengthSquared;

            // 比率 t を 0 から 1 の範囲にクランプ（丸め込み）する
            // t < 0 なら最近接点は a
            // t > 1 なら最近接点は b
            // 0 <= t <= 1 なら射影点が線分上にある
            if (t < 0f)
            {
                return a;
            }
            else if (t > 1f)
            {
                return b;
            }
            else
            {
                return a + ab * t;
            }
        }
    }
}