using UnityEngine;

[CreateAssetMenu(fileName = "CameraSensitivityData")]
public class CameraSensitivityData : ScriptableObject
{
    [Tooltip("デバイス名")]
    
    public string deviceName;

    [Tooltip("2Dマップ設定")]
    public float moveSpeed2D;
    public float scrollZoomSpeed2D;
    public float pinchZoomSpeed2D;

    [Tooltip("3Dマップ設定")]
    public float rotationSpeed;
    public float zoomSpeed;
    public float pinchZoomSensitivity;
}