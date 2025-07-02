// UserLocationManager.cs

using UnityEngine;
using System.Linq;

public class UserLocationManager : MonoBehaviour
{
    [Header("Dependencies")]
    public RoadNetworkBuilder roadNetworkBuilder; // InspectorでRoadNetworkBuilderを持つオブジェクトをアサイン
    public Transform userTransform; // Inspectorでユーザーを表すオブジェクトをアサイン

    [Header("Movement Settings")]
    public float moveSpeed = 1.5f; // スムーズ移動の速度

    private Vector3 targetPosition; // 移動目標位置
    private bool hasInitialPosition = false;

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

    /// <summary>
    /// 外部（GeoLocation APIのラッパーなど）から緯度・経度を受け取るメソッド
    /// </summary>
    /// <param name="latLon"></param>
    public void SetLocation(string latLon)
    {
        string[] coords = latLon.Split(',');
        if (coords.Length == 2)
        {
            if (double.TryParse(coords[0], out double latitude) && double.TryParse(coords[1], out double longitude))
            {
                UpdateLocation(latitude, longitude);
            }
        }
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
        Vector3 snappedPosition = FindNearestPointOnRoadNetwork(rawUnityPosition);

        // 移動目標位置を更新
        targetPosition = snappedPosition;

        // 最初の位置情報を受け取った場合、ワープ感をなくすために即座に位置を反映
        if (!hasInitialPosition)
        {
            userTransform.position = targetPosition;
            hasInitialPosition = true;
        }
    }
    
    Vector3 FindNearestPointOnRoadNetwork(Vector3 point)
    {
        var allEdges = roadNetworkBuilder.RuntimeEdges;
        if (allEdges == null || !allEdges.Any())
        {
            Debug.LogWarning("No road network edges found.");
            return point;
        }

        Vector3 nearestPoint = Vector3.zero;
        float minDistanceSquared = float.MaxValue;

        // 全てのエッジをチェックして、最も近い点を探す
        foreach (var edge in allEdges)
        {
            Vector3 closestPointOnEdge = FindNearestPointOnLineSegment(edge.fromNode.position, edge.toNode.position, point);
            float distanceSquared = (point - closestPointOnEdge).sqrMagnitude;

            if (distanceSquared < minDistanceSquared)
            {
                minDistanceSquared = distanceSquared;
                nearestPoint = closestPointOnEdge;
            }
        }

        // Z座標をuserTransformの現在のZ座標に合わせる（必要に応じて）
        nearestPoint.z = userTransform.position.z;

        return nearestPoint;
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