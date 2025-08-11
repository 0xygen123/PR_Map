using UnityEngine;
using System.Runtime.InteropServices;
using System;
using System.Globalization;


public class JSInterface : MonoBehaviour
{
    // JSの関数名
    public static string errorCalback = "";
    public static string errorCalbackNoArg = "";

    // 位置情報の更新時に発行されるイベント
    public static event Action<double, double> OnLocationReceived;
    // 方位情報の更新時に発行されるイベント
    public static event Action<float> OnDirectionReceived;

#if UNITY_WEBGL
    [DllImport("__Internal")]
    static extern void CallJavaScriptFunction(string functionName, string message);

    [DllImport("__Internal")]
    static extern void CallJavaScriptFunctionNoArg(string functionName);
#endif


    /// <summary>
    /// JSから緯度経度を受け取りパースする
    /// </summary>
    /// <param name="latLon"></param>
    public void SetLocation(string latLon)
    {
        if (string.IsNullOrEmpty(latLon))
        {
            Debug.Log("[JSInterface] ReceiveLocationFromJS: Received null or empty string.");
            SendToJS("SetLocation", "[JSInterface] ReceiveLocationFromJS: Received null or empty string.");
            return;
        }

        string[] coord = latLon.Split(',');
        if (coord.Length == 2 &&
            double.TryParse(coord[0], NumberStyles.Any, CultureInfo.InvariantCulture, out double latitude) &&
            double.TryParse(coord[1], NumberStyles.Any, CultureInfo.InvariantCulture, out double longitude)
        )
        {
            OnLocationReceived?.Invoke(latitude, longitude);
        }
        else
        {
            Debug.Log($"[JSInterface] Failed to parse location data: '{latLon}'");
        }
    }

    /// <summary>
    /// JSから方位を受け取る
    /// </summary>
    /// <param name="direction"></param>
    public void SetDirection(string direction)
    {
        if (string.IsNullOrEmpty(direction))
        {
            Debug.LogError("[JSInterface] ReceiveDirectionFromJS: Received null or empty string.");
            return;
        }

        if (float.TryParse(direction, NumberStyles.Any, CultureInfo.InvariantCulture, out float angle))
        {
            // パース成功。float型のデータをイベントで通知
            OnDirectionReceived?.Invoke(angle);
        }
        else
        {
            Debug.LogError($"[JSInterface] Failed to parse direction data: '{direction}'");
        }
    }


    /// <summary>
    /// JavaScriptの特定の関数を呼び出す
    /// </summary>
    /// <param name="functionName">呼び出すJavaScriptの関数名</param>
    /// <param name="message">JavaScript関数に渡す引数 (文字列)</param>
    public static void SendToJS(string functionName, string message)
    {
#if UNITY_WEBGL
        CallJavaScriptFunction(functionName, message);
#else
        Debug.LogWarning($"JSInterface: Not a WebGL build. Would call JS function '{functionName}' with message: '{message}'");
#endif
    }


    /// <summary>
    /// JavaScriptの特定の関数を引数なしで呼び出す
    /// </summary>
    /// <param name="functionName">呼び出すJavaScriptの関数名</param>
    public static void SendToJS(string functionName)
    {
#if UNITY_WEBGL
        CallJavaScriptFunctionNoArg(functionName);
#else
        Debug.LogWarning($"JSInterface: Not a WebGL build. Would call JS function '{functionName}' (no arguments).");
#endif
    }
}
