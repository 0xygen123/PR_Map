using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Assets.Scripts.Solid
{
    public class BuildingReferenceProvider : MonoBehaviour
    {
        // Inspectorでわかりやすくするために親オブジェクトを指定
        [SerializeField] private Transform entrancesParent;
        [SerializeField] private Transform roomsParent;

        private Dictionary<string, Transform> references = new Dictionary<string, Transform>();

        void Awake()
        {
            if (entrancesParent != null)
            {
                foreach (Transform entrance in entrancesParent)
                {
                    references[entrance.name] = entrance;
                }
            }

            if (roomsParent != null)
            {
                foreach (Transform room in roomsParent)
                {
                    references[room.name] = room;
                }
            }
            DEVwriteline();
        }

        public Transform GetReference(string key)
        {
            references.TryGetValue(key, out Transform value);
            return value;
        }

        void DEVwriteline()
        {
            foreach (KeyValuePair<string, Transform> kv in references)
            {
                Debug.Log($"{kv.Key}: {kv.Value}");
            }
        }
    }
}