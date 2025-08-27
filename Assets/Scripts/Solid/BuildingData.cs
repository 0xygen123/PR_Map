using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;


namespace Assets.Scripts.Solid
{
    // 部屋情報を表すクラス
    [Serializable]
    public class RoomInfo
    {
        public string roomKey;
    }

    // 建物の入り口を表すクラス
    [Serializable]
    public class EntranceInfo
    {
        public string entranceKey;
        [Tooltip("屋外の2D道路ネットワークにおけるノードID")]
        public int outdoorNodeId;
    }

    [CreateAssetMenu(menuName = "BuildingData")]
    public class BuildingData : ScriptableObject
    {
        public List<RoomInfo> rooms;
        public List<EntranceInfo> entrances;

        public RoomInfo GetRoomByKey(string roomKey)
        {
            // Debug.Log(roomKey);
            // DEVWriteline();
            return rooms.FirstOrDefault(r => r.roomKey == roomKey);
        }

        void DEVWriteline()
        {
            foreach (RoomInfo room in rooms)
            {
                Debug.Log($"{room.roomKey}");
            }
        }
    }
}
