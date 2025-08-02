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
                // RoadBuildingObject("");

                planeCamera.SetActive(false);
                solidCamera.SetActive(true);

                viewType = ViewType.Solid;
            }
            else if (viewType == ViewType.Solid)
            {
                planeCamera.SetActive(true);
                solidCamera.SetActive(false);

                viewType = ViewType.Plane;
            }
        }

        /// <summary>
        /// Addressavkesを使用して建物をロードして表示
        /// </summary>
        /// <param name="buildingName"></param>
        async void RoadBuildingObject(string buildingName)
        {
            // ロード中UI表示
            if (loadingUI != null)
            {
                loadingUI.SetActive(true);
            }

            string buildingAddress = TranslateNameToKey(buildingName);

            // すでに別の建物が読み込まれている場合解放
            if (currentBuildingInstance != null)
            {
                // Addresstables.ReleaseInstanceで解放
                Addressables.ReleaseInstance(currentBuildingInstance);
            }

            try
            {
                // Addressables.InstantiateAsyncでアセットロードとインスタンス化
                loadHandle = Addressables.InstantiateAsync(buildingAddress, parent: buildingObject.transform);

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
                Debug.Log($"建物のロードに失敗しました: {buildingAddress}\n{e.Message}");
            }
            finally
            {
                if (loadingUI != null)
                {
                    loadingUI.SetActive(false);
                }
            }
        }

        /// <summary>
        /// DBとAddressablesのキーの翻訳
        /// </summary>
        /// <param name="buildingName"></param>
        /// <returns></returns>
        string TranslateNameToKey(string buildingName)
        {
            string buildingAddressableKey = "DEV";
            return buildingAddressableKey;
        }
    }
}