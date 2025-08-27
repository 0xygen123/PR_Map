using UnityEngine;
using Assets.Scripts.Plane.Road;

public class PathRenderer : MonoBehaviour
{
    // RoadNetworkBuilderを改名したものを参照
    [SerializeField] private RoadNetworkBuilder roadNetworkBuilder; 
    [SerializeField] private LineRenderer indoorPathRenderer;

    public void DrawPath(PathfindingManager.PathResult result)
    {
        ClearAllPaths();

        // 屋外経路の描画
        if (result.OutdoorPath != null && result.OutdoorPath.Count > 0)
        {
            roadNetworkBuilder.VisualizePathByChangingMaterial(result.OutdoorPath);
        }

        // 屋内経路の描画
        if (result.IndoorPathCoordinates != null && result.IndoorPathCoordinates.Count > 1)
        {
            if (indoorPathRenderer == null)
            {
                Debug.Log("IndoorPathRenderer がアタッチされていません");
                return;
            }

            // LineRenderer
            indoorPathRenderer.gameObject.SetActive(true);
            indoorPathRenderer.positionCount = result.IndoorPathCoordinates.Count;
            indoorPathRenderer.SetPositions(result.IndoorPathCoordinates.ToArray());
            indoorPathRenderer.enabled = true;
        }
    }

    public void ClearAllPaths()
    {
        // 屋外経路の表示リセット
        roadNetworkBuilder.ResetAllRoadMaterials();
        // 屋内経路を非表示に
        if (indoorPathRenderer != null)
        {
            indoorPathRenderer.positionCount = 0;
            indoorPathRenderer.enabled = false;
        }
    }
}