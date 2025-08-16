using UnityEngine;
using UnityEngine.AddressableAssets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Assets.Scripts.Core;
using Assets.Scripts.Plane;
using Assets.Scripts.Solid;

// --- 1. 統括役：全体の非同期フローを管理 ---
public class AppManager : MonoBehaviour
{
    [Header("Modules")]
    [SerializeField] private BuildingLoader assetLoader;
    [SerializeField] private PathfindingManager pathfindingManager;
    [SerializeField] private PathRenderer pathRenderer;
    [SerializeField] private ViewController viewController;
    [SerializeField] private UserLocationManager userLocation;

    [Header("UI")]
    [SerializeField] private GameObject loadingUI;

    public void TestPathfinding()
    {
        // Reactからのイベントをリッスンする想定
        // JSInterface.OnStartPathfinding += HandlePathfindingRequest;
        
        // --- テスト用の呼び出し ---
        HandlePathfindingRequest("BuildingA", "Room101");
    }

    private async void HandlePathfindingRequest(string buildingKey, string roomKey)
    {
        if (loadingUI != null) loadingUI.SetActive(true);

        try
        {
            // 1. 必要なアセット（建物データと3Dモデル）を並行してロード
            var buildingDataTask = assetLoader.LoadDataAsync<BuildingData>(buildingKey);
            var buildingInstanceTask = assetLoader.LoadGameObjectAsync(buildingKey, viewController.SolidCameraTarget);
            
            await Task.WhenAll(buildingDataTask, buildingInstanceTask);

            BuildingData buildingData = buildingDataTask.Result;
            GameObject buildingInstance = buildingInstanceTask.Result;

            if (buildingData == null || buildingInstance == null)
            {
                Debug.LogError("アセットのロードに失敗しました。");
                return;
            }

            // 2. 現在地を取得
            var startNode = userLocation.GetStartNodeForPathfinding();
            if (startNode == null)
            {
                Debug.LogError("現在地を取得できませんでした。");
                return;
            }

            // 3. 経路探索を実行
            var navMeshController = buildingInstance.GetComponent<NavMeshController>();
            PathfindingManager.PathResult optimalPath = await pathfindingManager.FindOptimalPathAsync(startNode.nodeId, buildingData, roomKey, navMeshController);

            if (optimalPath == null)
            {
                Debug.LogWarning("有効な経路が見つかりませんでした。");
                return;
            }
            
            // 4. 経路を描画
            pathRenderer.ClearAllPaths();
            pathRenderer.DrawPath(optimalPath);

            // 5. 3Dビューに切り替え
            viewController.SwitchToSolidView(buildingInstance);
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
}

public class AssetLoader : MonoBehaviour
{
    private readonly Dictionary<string, GameObject> _loadedInstances = new Dictionary<string, GameObject>();
    private readonly Dictionary<string, ScriptableObject> _loadedData = new Dictionary<string, ScriptableObject>();

    public async Task<T> LoadDataAsync<T>(string key) where T : ScriptableObject
    {
        if (_loadedData.TryGetValue(key, out var data))
        {
            return data as T;
        }

        var handle = Addressables.LoadAssetAsync<T>(key);
        var loadedData = await handle.Task;
        if (loadedData != null)
        {
            _loadedData[key] = loadedData;
        }
        return loadedData;
    }

    public async Task<GameObject> LoadGameObjectAsync(string key, Transform parent)
    {
        // 既存のインスタンスがあれば解放
        if (_loadedInstances.ContainsKey(key))
        {
            ReleaseGameObject(key);
        }
        
        var handle = Addressables.InstantiateAsync(key, parent);
        var instance = await handle.Task;
        instance.transform.localPosition = Vector3.zero;
        _loadedInstances[key] = instance;
        return instance;
    }

    public void ReleaseGameObject(string key)
    {
        if (_loadedInstances.TryGetValue(key, out var instance))
        {
            Addressables.ReleaseInstance(instance);
            _loadedInstances.Remove(key);
        }
    }

    private void OnDestroy()
    {
        // シーン終了時にすべて解放
        foreach (var key in _loadedInstances.Keys.ToList())
        {
            ReleaseGameObject(key);
        }
        foreach (var data in _loadedData.Values)
        {
            Addressables.Release(data);
        }
        _loadedData.Clear();
    }
}