using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.AddressableAssets;
using Assets.Scripts.Solid;


[Serializable]
public class BuildingInfo
{
    [Tooltip("React側から受け取る建物キー")]
    [SerializeField] string buildigkey;
    public string BuildingKey => buildigkey;

    [Tooltip("建物オブジェクト")]
    [SerializeField] AssetReferenceGameObject gameObjectReference;
    public AssetReferenceGameObject GameObjectReference => gameObjectReference;

    [Tooltip("建物データのアセット参照")]
    [SerializeField] AssetReferenceT<BuildingData> dataReference;
    public AssetReferenceT<BuildingData> DataReference => dataReference;
}


[CreateAssetMenu(fileName = "BuildingAddressMap", menuName = "BuildingAddressMap")]
public class BuildingAddressMap : ScriptableObject
{
    public List<BuildingInfo> buildingsMappings;

    public BuildingInfo GetBuildingByKey(string key)
    {
        if (string.IsNullOrEmpty(key))
        {
            return null;
        }
        foreach (var e in buildingsMappings)
        {
            Debug.Log($"BuildingKey: {e.BuildingKey}");
        }
        return buildingsMappings.FirstOrDefault(e => e.BuildingKey == key);
    }
}