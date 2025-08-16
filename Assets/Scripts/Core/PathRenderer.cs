using UnityEngine;
using Assets.Scripts.Plane.Road;

public class PathRenderer : MonoBehaviour
{
    // RoadNetworkBuilderを改名したものを参照
    [SerializeField] private RoadNetworkBuilder roadNetworkBuilder; 
    [SerializeField] private LineRenderer indoorPathRenderer;

    public void DrawPath(PathfindingManager.PathResult result)
    {
        // 1. 屋外経路の描画をRoadNetworkProviderに依頼する
        if (result.OutdoorPath != null && result.OutdoorPath.Count > 0)
        {
            roadNetworkBuilder.VisualizePathByChangingMaterial(result.OutdoorPath);
        }

        // 2. 屋内経路は自身で描画する
        if (result.IndoorPathCoordinates != null && result.IndoorPathCoordinates.Count > 1)
        {
            indoorPathRenderer.positionCount = result.IndoorPathCoordinates.Count;
            indoorPathRenderer.SetPositions(result.IndoorPathCoordinates.ToArray());
            indoorPathRenderer.enabled = true;
        }
    }

    public void ClearAllPaths()
    {
        // 屋外経路の表示リセットを依頼
        roadNetworkBuilder.ResetAllRoadMaterials();
        // 屋内経路を非表示に
        indoorPathRenderer.enabled = false;
    }
}