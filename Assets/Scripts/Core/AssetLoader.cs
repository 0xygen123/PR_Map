using UnityEngine;
using System;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.Collections.Generic;
using System.Threading.Tasks;
using Assets.Scripts.Solid;

namespace Assets.Scripts.Core
{
    public class BuildingLoader : MonoBehaviour
    {
        readonly List<AsyncOperationHandle> _loadedHandles = new List<AsyncOperationHandle>();

        public async Task<BuildingData> LoadDataAsync(AssetReferenceT<BuildingData> reference)
        {
            if (!reference.RuntimeKeyIsValid())
            {
                Debug.LogError("無効なAssetReferenceです。");
                return null;
            }
            var handle = Addressables.LoadAssetAsync<BuildingData>(reference);
            _loadedHandles.Add(handle);
            return await handle.Task;
        }

        public async Task<GameObject> LoadGameObjectAsync(AssetReferenceGameObject reference, Transform parent)
        {
            if (!reference.RuntimeKeyIsValid())
            {
                Debug.LogError("無効なAssetReferenceです。");
                return null;
            }
            string key = reference.RuntimeKeyIsValid() ? reference.RuntimeKey.ToString() : reference.ToString();
            var handle = reference.InstantiateAsync(parent);
            _loadedHandles.Add(handle);

            try
            {
                var instance = await handle.Task;

                // 詳細ログ: ステータスと例外情報を確認
                if (handle.Status != AsyncOperationStatus.Succeeded)
                {
                    Debug.LogError($"Addressables.InstantiateAsync failed for '{key}'. Status={handle.Status}. Exception={handle.OperationException}");
                    return null;
                }

                if (instance == null)
                {
                    Debug.LogError($"Addressables returned null instance for '{key}'. OperationException={handle.OperationException}");
                    return null;
                }

                // 親の下に配置された場合はローカル位置を初期化
                try
                {
                    instance.transform.localPosition = Vector3.zero;
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Failed to set localPosition on instantiated object '{key}': {ex.Message}");
                }

                Debug.Log($"Instantiated '{key}' -> {instance.name}");
                return instance;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Exception while instantiating Addressable '{key}': {ex}");
                return null;
            }
        }

        public void ReleaseGameObject(GameObject instance)
        {
            if (instance != null)
            {
                Addressables.ReleaseInstance(instance);
            }
        }

        void OnDestroy()
        {
            // シーン終了時にロードしたすべてのアセットを解放
            foreach (var handle in _loadedHandles)
            {
                if (handle.IsValid())
                {
                    Addressables.Release(handle);
                }
            }
            _loadedHandles.Clear();
        }
    }
}