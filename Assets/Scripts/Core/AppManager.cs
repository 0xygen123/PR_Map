using UnityEngine;
using System.Collections;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;
using System;


enum ViewType
{
    Plane,
    Solid
}

public class AppManager : MonoBehaviour
{
    [Header("3Dカメラの中心")]
    [SerializeField] GameObject solidCameraTarget;

    [Header("AddressableDB")]
    [SerializeField] BuildingAddressMap buildingAddressMap;

    [Header("マップカメラ")]
    [SerializeField] GameObject planeCamera;
    [SerializeField] GameObject solidCamera;

    [Header("表示マップ")]
    [SerializeField] ViewType viewType = ViewType.Plane;

    [Header("UI")]
    [SerializeField] GameObject loadingUI;

    GameObject currentBuildingInstance;
    string currentBuildingName = null;
    AsyncOperationHandle<GameObject> loadHandle;


    void Awake()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        JSInterface.OnSwitchToPlaneView += SwitchToPlaneView;
        JSInterface.OnSwitchToSlidView += SwitchToSolidView;
        JSInterface.OnShowBuildingIn3D += ShowBuildingIn3D;
    }

    /// <summary>
    /// シーンのロード検出
    /// </summary>
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        JSInterface.SendToJS(JSInterface.JSFunctionNoArg.OnUnityLoaded);
    }

    /// <summary>
    /// カメラの切り替え
    /// </summary>
    [Obsolete("代わりにSwitchToPlaneView, SwitchToSolidViewを使用", false)]
    public void OnSwitchCamera()
    {
        if (viewType == ViewType.Plane)
        {
            LoadBuildingObject("DEV");
            planeCamera.SetActive(false);
            solidCamera.SetActive(true);

            viewType = ViewType.Solid;
        }
        else if (viewType == ViewType.Solid)
        {
            if (currentBuildingInstance != null)
            {
                Addressables.ReleaseInstance(currentBuildingInstance);
                currentBuildingInstance = null;
            }

            planeCamera.SetActive(true);
            solidCamera.SetActive(false);

            viewType = ViewType.Plane;
        }
    }

    void SwitchToPlaneView()
    {
        if (viewType == ViewType.Solid)
        {
            // 現在の建物を解放
            // if (currentBuildingInstance != null)
            // {
            //     Addressables.ReleaseInstance(currentBuildingInstance);
            //     currentBuildingInstance = null;
            // }

            // 現在の建物を非表示
            if (currentBuildingInstance != null)
            {
                currentBuildingInstance.SetActive(false);
            }

            // カメラを2Dに切り替え
            planeCamera.SetActive(true);
            solidCamera.SetActive(false);

            viewType = ViewType.Plane;
        }
    }

    void SwitchToSolidView()
    {
        if (viewType == ViewType.Plane)
        {
            // 建物がロード済みなら再表示
            if (currentBuildingInstance != null)
            {
                currentBuildingInstance.SetActive(true);
            }

            planeCamera.SetActive(false);
            solidCamera.SetActive(true);

            viewType = ViewType.Solid;
        }
    }

    void ShowBuildingIn3D(string buildingName)
    {
        // すでに3D表示だった場合は無視
        if (viewType == ViewType.Solid)
        {
            return;
        }

        if (currentBuildingInstance != null && currentBuildingName == buildingName)
        {
            SwitchToSolidView();
        }

        // 非同期で建物をロードし、完了後にカメラを切り替える
        LoadBuildingObject(buildingName);
    }

    /// <summary>
    /// Addressablesを使用して建物をロードして表示
    /// </summary>
    /// <param name="buildingName"></param>
    async void LoadBuildingObject(string buildingName)
    {
        // ロード中UI表示
        if (loadingUI != null)
        {
            loadingUI.SetActive(true);
        }

        AssetReference buildingReference = buildingAddressMap.GetAssetFromName(buildingName);

        if (buildingReference == null)
        {
            Debug.LogError($"ロード対象のアセット参照が見つかりません: {buildingName}");
            // ローディングUIを消すなどのエラー処理
            if (loadingUI != null)
            {
                loadingUI.SetActive(false);
            }
            return;
        }

        // すでに別の建物が読み込まれている場合解放
        if (currentBuildingInstance != null)
        {
            // Addresstables.ReleaseInstanceで解放
            Addressables.ReleaseInstance(currentBuildingInstance);
        }

        try
        {
            // Addressables.InstantiateAsyncでアセットロードとインスタンス化
            loadHandle = buildingReference.InstantiateAsync(parent: solidCameraTarget.transform);

            // ロードとインスタンス化の完了を待つ
            currentBuildingInstance = await loadHandle.Task;
            currentBuildingInstance.transform.localPosition = Vector3.zero;

            SwitchToSolidView();
        }
        catch (Exception e)
        {
            Debug.Log($"建物のロードに失敗しました: {buildingReference}\n{e.Message}");
            
        }
        finally
        {
            if (loadingUI != null)
            {
                loadingUI.SetActive(false);
            }
        }
    }

    void OnDestroy()
    {
        if (currentBuildingInstance != null)
        {
            Addressables.ReleaseInstance(currentBuildingInstance);
        }
    }
}
