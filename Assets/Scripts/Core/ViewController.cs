using UnityEngine;
using System.Collections.Generic;

namespace Assets.Scripts.Core
{
    public class ViewController : MonoBehaviour
    {
        [Header("Cameras")]
        [SerializeField] GameObject planeCamera;
        [SerializeField] GameObject solidCamera;

        [Header("Targets")]
        [SerializeField] Transform solidCameraTarget;
        public Transform SolidCameraTarget => solidCameraTarget;

        [Header("Camera Settings")]
        [SerializeField] List<CameraSensitivityData> cameraSensitivityDatas;

        [Header("Canvas")]
        [SerializeField] GameObject canvas;

        GameObject _currentBuilding;

        void Awake()
        {
            JSInterface.OnDeviceNameReceived += ApplyCameraSettings;
        }

        void ApplyCameraSettings(string deviceName)
        {
            var settings = cameraSensitivityDatas.Find(s => s.deviceName == deviceName);
            if (settings == null)
            {
                Debug.Log($"No preset found for device: {deviceName}. user default");
                return;
            }

            planeCamera.GetComponent<Plane.CameraController>().SetParameters(settings);
            solidCamera.GetComponent<Solid.CameraController>().SetParameters(settings);
        }

        public void SwitchToPlaneView()
        {
            if (_currentBuilding != null) _currentBuilding.SetActive(false);
            planeCamera.SetActive(true);
            solidCamera.SetActive(false);
            canvas.SetActive(false);
        }

        public void SwitchToSolidView(GameObject buildingInstance)
        {
            _currentBuilding = buildingInstance;
            if (_currentBuilding != null) _currentBuilding.SetActive(true);
            planeCamera.SetActive(false);
            solidCamera.SetActive(true);
            canvas.SetActive(true);
        }
    }
}