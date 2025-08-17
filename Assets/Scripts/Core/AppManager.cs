using UnityEngine;
using UnityEngine.AddressableAssets;
using System;
using System.Threading.Tasks;

using Assets.Scripts.Core;
using Assets.Scripts.Plane;
using Assets.Scripts.Solid;
using Assets.Scripts.Plane.Road;
using Unity.VisualScripting;
using UnityEngine.AI;
using UnityEditor.PackageManager.Requests;

// --- 1. 統括役：全体の非同期フローを管理 ---
public class AppManager : MonoBehaviour
{
    [Header("Modules")]
    [SerializeField] private BuildingLoader assetLoader;
    [SerializeField] private PathfindingManager pathfindingManager;
    [SerializeField] private PathRenderer pathRenderer;
    [SerializeField] private ViewController viewController;
    [SerializeField] private UserLocationManager userLocation;

    [Header("Data")]
    [SerializeField] private BuildingAddressMap buildingAddressMap;

    [Header("UI")]
    [SerializeField] private GameObject loadingUI;

    PathfindingManager.PathResult cachedPathResult = null;
    GameObject cachedBuildingInstance = null;
    string cachedBuildingKey = null;

    void OnEnable()
    {
        JSInterface.OnPathfindingRequested2D += HandlePathfindingRequest;
        JSInterface.OnPathfindingRequested3D += HandlePathfindingRequest;
        JSInterface.OnSwitchToPlaneView += HandleSwitchToPlaneView;
        JSInterface.OnSwitchToSolidView += HandleSwitchToSolidView;
    }

    void OnDisable()
    {
        JSInterface.OnPathfindingRequested2D -= HandlePathfindingRequest;
        JSInterface.OnPathfindingRequested3D -= HandlePathfindingRequest;
        JSInterface.OnSwitchToPlaneView -= HandleSwitchToPlaneView;
        JSInterface.OnSwitchToSolidView -= HandleSwitchToSolidView;
    }


    public void TestPathfinding()
    {
        HandlePathfindingRequest("DEV");
    }
    public void TestPathfinding3D()
    {
        HandlePathfindingRequest("DEV", "DEVRoom");
    }

    /// <summary>
    /// 部屋を含めた探索を行う
    /// </summary>
    /// <param name="buildingKey"></param>
    /// <param name="roomKey"></param>
    public async void HandlePathfindingRequest(string buildingKey, string roomKey)
    {
        if (loadingUI != null) { loadingUI.SetActive(true); }

        try
        {
            ClearCache(buildingKey);

            BuildingInfo entry = buildingAddressMap.GetBuildingByKey(buildingKey);
            if (entry == null)
            {
                Debug.LogError($"BuildingAddressMapにキー '{buildingKey}' が見つかりません。");
                return;
            }

            // 必要なアセットを並行してロード
            Task<BuildingData> dataLoadTask = assetLoader.LoadDataAsync(entry.DataReference);
            Task<GameObject> instanceLoadTask = assetLoader.LoadGameObjectAsync(entry.GameObjectReference, viewController.SolidCameraTarget);

            await Task.WhenAll(dataLoadTask, instanceLoadTask);

            BuildingData buildingData = dataLoadTask.Result;
            GameObject buildingInstance = instanceLoadTask.Result;

            if (buildingData == null || buildingInstance == null)
            {
                Debug.LogError($"アセットのロードに失敗しました。Key: {buildingKey}");
                return;
            }

            RuntimeNode startNode = userLocation.GetStartNodeForPathfinding();
            if (startNode == null)
            {
                Debug.LogError("現在地を取得できません");
                return;
            }

            NavMeshController navMeshController = buildingInstance.GetComponent<NavMeshController>();
            PathfindingManager.PathResult optimalPath = await pathfindingManager.FindOptimalPathAsync(
                startNode.nodeId,
                buildingData,
                roomKey,
                buildingInstance,
                navMeshController
            );

            if (optimalPath != null)
            {
                // cache
                cachedPathResult = optimalPath;
                cachedBuildingKey = buildingKey;
                cachedBuildingInstance = buildingInstance;

                pathRenderer.ClearAllPaths();
                pathRenderer.DrawPath(optimalPath);
                viewController.SwitchToSolidView(buildingInstance);
            }
            else
            {
                // >> DEV
                viewController.SwitchToSolidView(buildingInstance);
                Debug.LogWarning("有効な経路が見つかりませんでした。");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"経路探索プロセスでエラーが発生しました: {e.Message}");
        }
        finally
        {
            if (loadingUI != null) { loadingUI.SetActive(false); }
        }
    }

    /// <summary>
    /// 2Dマップのみでの探索
    /// </summary>
    /// <param name="buildingKey"></param>
    public async void HandlePathfindingRequest(string buildingKey)
    {
        if (loadingUI != null) { loadingUI.SetActive(true); }

        try
        {
            ClearCache(buildingKey);

            BuildingInfo entry = buildingAddressMap.GetBuildingByKey(buildingKey);
            if (entry == null)
            {
                Debug.LogError($"BuildingAddressMapにキー '{buildingKey}' が見つかりません。");
                return;
            }

            Task<BuildingData> dataLoadTask = assetLoader.LoadDataAsync(entry.DataReference);
            BuildingData loadedData = await dataLoadTask;
            if (loadedData == null)
            {
                Debug.LogError($"アセットのロードに失敗しました。Key: {buildingKey}");
                return;
            }

            RuntimeNode startNode = userLocation.GetStartNodeForPathfinding();
            if (startNode == null)
            {
                Debug.LogError("現在地を取得できませんでした。");
                return;
            }

            // 経路探索を実行
            PathfindingManager.PathResult optimalPath = pathfindingManager.FindOptimalPath(startNode.nodeId, loadedData);
            if (optimalPath != null)
            {
                cachedPathResult = optimalPath;
                cachedBuildingKey = buildingKey;

                pathRenderer.ClearAllPaths();
                pathRenderer.DrawPath(optimalPath);
                viewController.SwitchToPlaneView();
            }
            else
            {
                Debug.LogError("有効な経路が見つかりません");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"経路探索プロセスでエラーが発生しました: {e.Message}");
            // エラー時のUI表示など
        }
        finally
        {
            if (loadingUI != null) loadingUI.SetActive(false);
        }
    }

    /// <summary>
    /// 探索済みの経路情報と建物インスタンスをクリアします。
    /// </summary>
    private void ClearCache(string newBuildingKey)
    {
        // 違う建物を探索する場合のみ、古いインスタンスを解放する
        if (cachedBuildingKey != null && cachedBuildingKey != newBuildingKey && cachedBuildingInstance != null)
        {
            assetLoader.ReleaseGameObject(cachedBuildingInstance);
        }
        cachedPathResult = null;
        cachedBuildingInstance = null;
        cachedBuildingKey = null;
    }
    
    /// <summary>
    /// JSからの2Dビュー切り替えリクエストを処理します。
    /// </summary>
    private void HandleSwitchToPlaneView()
    {
        viewController.SwitchToPlaneView();
    }

    /// <summary>
    /// JSからの3Dビュー切り替えリクエストを処理します。
    /// </summary>
    private void HandleSwitchToSolidView()
    {
        // キャッシュされた建物インスタンスがあれば、それを表示する
        if (cachedBuildingInstance != null)
        {
            viewController.SwitchToSolidView(cachedBuildingInstance);
        }
        else
        {
            Debug.LogWarning("3Dビューに切り替えようとしましたが、表示すべき建物がありません。先に経路探索を実行してください。");
            // 必要であれば、ここでJSにエラーを通知することもできます
            // JSInterface.SendToJS("showError", "3D表示する建物がありません");
        }
    }
}
