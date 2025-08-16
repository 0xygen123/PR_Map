using UnityEngine;

public class ViewController : MonoBehaviour
{
    [Header("Cameras")]
    [SerializeField] private GameObject planeCamera;
    [SerializeField] private GameObject solidCamera;

    [Header("Targets")]
    [SerializeField] private Transform solidCameraTarget;
    public Transform SolidCameraTarget => solidCameraTarget;

    private GameObject _currentBuilding;

    public void SwitchToPlaneView()
    {
        if (_currentBuilding != null) _currentBuilding.SetActive(false);
        planeCamera.SetActive(true);
        solidCamera.SetActive(false);
    }

    public void SwitchToSolidView(GameObject buildingInstance)
    {
        _currentBuilding = buildingInstance;
        if (_currentBuilding != null) _currentBuilding.SetActive(true);
        planeCamera.SetActive(false);
        solidCamera.SetActive(true);
    }
}