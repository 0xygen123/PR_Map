using UnityEngine;
using UnityEngine.AddressableAssets;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Assets.Scripts.Core
{
    public class BuildingLoader : MonoBehaviour
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
}