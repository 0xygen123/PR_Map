using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.AddressableAssets;


[Serializable]
public class BuildingInfo
{
    [Tooltip("React側から受け取る建物ID")]
    [SerializeField] string buildigID;
    public string BuildingID => buildigID;

    [Tooltip("Addressablesに設定したキー")]
    [SerializeField] AssetReference assetReference;
    public AssetReference AssetReference => assetReference;
}


[CreateAssetMenu(fileName = "BuildingAddressMap", menuName = "BuildingAddressMap")]
public class BuildingAddressMap : ScriptableObject
{
    public List<BuildingInfo> buildingsMappings;

    public AssetReference GetAssetFromName(string name)
    {
        BuildingInfo info = buildingsMappings.FirstOrDefault(b => b.BuildingID == name);

        if (info != null && info.AssetReference.RuntimeKeyIsValid())
        {
            return info.AssetReference;
        }
        else
        {
            Debug.LogWarning($"対応するAddressableキーが存在しません: {name}");
            return null;
        }
    }
}