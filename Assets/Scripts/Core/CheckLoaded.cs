using Assets.Scripts.Core;
using UnityEngine;

namespace Assets.Scripts.Core
{
    public class CheckLoaded : MonoBehaviour
    {
        [SerializeField] GameObject initializingUI;
        [SerializeField] GameObject getLocationUI;

        void Start()
        {
            JSInterface.NotifyUnityLoaded();

            initializingUI.SetActive(false);
            getLocationUI.SetActive(true);

            enabled = false;
        }
    }
}
