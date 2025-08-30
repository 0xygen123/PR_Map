using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.Collections.Generic;
using System.Linq;
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
            var handle = reference.InstantiateAsync(parent);
            _loadedHandles.Add(handle);
            var instance = await handle.Task;
            if (instance != null)
            {
                instance.transform.localPosition = Vector3.zero;
            }
            return instance;
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