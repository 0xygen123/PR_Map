using UnityEngine;
using System.ComponentModel;
using System;
using System.Threading.Tasks;

using Assets.Scripts.Plane;
using Assets.Scripts.Solid;
using Assets.Scripts.Plane.Road;

namespace Assets.Scripts.Core
{
    // 統括役：全体の非同期フローを管理
    public class AppManager : MonoBehaviour
    {
        [Header("Modules")]
        [SerializeField] BuildingLoader assetLoader;
        [SerializeField] PathfindingManager pathfindingManager;
        [SerializeField] PathRenderer pathRenderer;
        [SerializeField] ViewController viewController;
        [SerializeField] UserLocationManager userLocation;

        [Header("Data")]
        [SerializeField] BuildingAddressMap buildingAddressMap;

        [Header("UI")]
        [SerializeField] GameObject loadingUI;


        PathfindingManager.PathResult cachedPathResult = null;
        [ReadOnly(true)] [SerializeField] GameObject cachedBuildingInstance = null;
        [ReadOnly(true)] [SerializeField] string cachedBuildingKey = null;
        [ReadOnly(true)][SerializeField] BuildingData cachedBuildingData = null;

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
                if (buildingKey != cachedBuildingKey || cachedBuildingInstance == null)
                {
                    ClearCache();
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

                    await Task.Yield();

                    cachedBuildingInstance = buildingInstance;
                    cachedBuildingKey = buildingKey;
                    cachedBuildingData = buildingData;
                }
                else
                {
                    cachedBuildingInstance.SetActive(true);
                }

                RuntimeNode startNode = userLocation.GetStartNodeForPathfinding();
                if (startNode == null)
                {
                    Debug.LogError("現在地を取得できません");
                    return;
                }

                NavMeshController navMeshController = cachedBuildingInstance.GetComponent<NavMeshController>();

                // 3D案内に対応していない建物（NavMeshController が無い場合）は
                // 3D経路探索を中止して、可能なら2D経路を計算して表示し
                if (navMeshController == null)
                {
#if UNITY_WEBGL
                    JSInterface.SendToJS(JSInterface.JSFunction.OnShowError, "この建物は3D経路探索に対応していません。可能であれば2D経路を表示します。オブジェクトのみ表示される場合があります。");
#endif
                    // 2D経路を計算して表示する
                    PathfindingManager.PathResult planePath = pathfindingManager.FindOptimalPath(startNode.nodeId, cachedBuildingData);
                    if (planePath != null)
                    {
                        cachedPathResult = planePath;
                        cachedBuildingKey = buildingKey;

                        pathRenderer.DrawPath(planePath);
                        viewController.SwitchToSolidView(cachedBuildingInstance);
                    }
                    else
                    {
#if UNITY_WEBGL
                        JSInterface.SendToJS(JSInterface.JSFunction.OnShowError, "有効な経路が見つかりません");
#endif
                    }  return;
                }

                PathfindingManager.PathResult optimalPath = null;
                optimalPath = await pathfindingManager.FindOptimalPathAsync(
                    startNode.nodeId,
                    cachedBuildingData,
                    roomKey,
                    cachedBuildingInstance,
                    navMeshController
                );

                if (optimalPath != null)
                {
                    pathRenderer.DrawPath(optimalPath);
                    viewController.SwitchToSolidView(cachedBuildingInstance);
                }
                else
                {
#if UNITY_EDITOR
                    viewController.SwitchToSolidView(cachedBuildingInstance);
                    Debug.LogWarning("有効な経路が見つかりませんでした。");
#endif
#if UNITY_WEBGL
                    JSInterface.SendToJS(JSInterface.JSFunction.OnShowError, "有効な経路が見つかりませんでした");
#endif
                }
            }
            catch (Exception e)
            {
#if UNITY_EDITOR
                Debug.LogError($"経路探索プロセスでエラーが発生しました: {e.Message}");
#endif
#if UNITY_WEBGL
                JSInterface.SendToJS(JSInterface.JSFunction.OnShowError, $"経路探索プロセスでエラーが発生しました: {e.Message}");
#endif
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
                if (buildingKey != cachedBuildingKey)
                {
                    ClearCache();
                }

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
        void ClearCache()
        {
            Debug.Log("Cache Clear");
            // 違う建物を探索する場合のみ、古いインスタンスを解放する
            if (cachedBuildingInstance != null)
            {
                // Destroy(cachedBuildingInstance);
                assetLoader.ReleaseGameObject(cachedBuildingInstance);
                cachedBuildingInstance = null;
            }
            cachedPathResult = null;
            cachedBuildingKey = null;
            cachedBuildingData = null;
        }

        /// <summary>
        /// JSからの2Dビュー切り替えリクエストを処理します。
        /// </summary>
        void HandleSwitchToPlaneView()
        {
            viewController.SwitchToPlaneView();
        }

        /// <summary>
        /// JSからの3Dビュー切り替えリクエストを処理します。
        /// </summary>
        void HandleSwitchToSolidView()
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
}