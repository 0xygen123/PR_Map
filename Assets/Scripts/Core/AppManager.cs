using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.AddressableAssets;
using System.Threading.Tasks;

using Assets.Scripts.Solid;

namespace Assets.Scripts.Core
{
    enum ViewType
    {
        Plane,
        Solid
    }

    public class AppManager : MonoBehaviour
    {
        [Header("マップオブジェクト")]
        [SerializeField] GameObject buildingObject;

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
        AsyncOperationHandle<GameObject> loadHandle;

        /// <summary>
        /// アプリ終了時にリソース解放
        /// </summary>
        void OnDestroy()
        {
            if (currentBuildingInstance != null)
            {
                Addressables.ReleaseInstance(currentBuildingInstance);
            }
        }

        /// <summary>
        /// カメラの切り替え
        /// </summary>
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

        /// <summary>
        /// Addressavkesを使用して建物をロードして表示
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
                loadingUI.SetActive(false);
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
                loadHandle = buildingReference.InstantiateAsync(parent: buildingObject.transform);

                // ロードとインスタンス化の完了を待つ
                currentBuildingInstance = await loadHandle.Task;

                // 3Dカメラのコントローラーに新しい
                var cameraController = solidCamera.GetComponent<CameraController>();
                if (cameraController != null)
                {
                    cameraController.SetMapObject(currentBuildingInstance);
                }
                else
                {
                    Debug.Log("solidCameraにCameraControllerが見つかりません。");
                }
            }
            catch (System.Exception e)
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
    }
}