using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Assets.Scripts.Core
{
    public enum ViewType
    {
        Plane,
        Solid
    }

    public class AppManager : MonoBehaviour
    {
        [Header("マップオブジェクト")]
        [SerializeField] GameObject planeMapObjects;
        [SerializeField] GameObject solidMapObjects;

        [Header("マップカメラ")]
        [SerializeField] GameObject planeCamera;
        [SerializeField] GameObject solidCamera;

        [Header("表示マップ")]
        [SerializeField] ViewType viewType = ViewType.Plane;


        public void OnSwitchCamera()
        {
            if (viewType == ViewType.Plane)
            {
                // planeCamera.enabled = false;
                // solidCamera.enabled = true;
                planeCamera.SetActive(false);
                solidCamera.SetActive(true);

                viewType = ViewType.Solid;
            }
            else if(viewType == ViewType.Solid)
            {
                // planeCamera.enabled = true;
                // solidCamera.enabled = false;
                planeCamera.SetActive(true);
                solidCamera.SetActive(false);
                
                viewType = ViewType.Plane;
            }
        }
    }
}