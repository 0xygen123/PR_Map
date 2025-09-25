using UnityEngine;
using System;
using System.Globalization;
using System.Runtime.InteropServices;

namespace Assets.Scripts.Core
{
    public class JSInterface : MonoBehaviour
    {
        float zoomLevel = 70;

        public static class JSFunction
        {
            public const string OnShowError = "showError";
        }
        public static class JSFunctionNoArg
        {
            public const string OnUnityLoaded = "onUnityLoaded";
        }

        // ユーザにフェードインするイベント
        public static event Action<float> OnUserFollow;

        // ユーザデバイス名受信時に発行されるイベント
        public static event Action<string> OnDeviceNameReceived;

        // JSのGeoLocationのステータスコード受信時に発行されるイベント
        public static event Action<int> OnGeolocationStatusReceived;

        // 位置情報の更新時に発行されるイベント
        public static event Action<double, double> OnLocationReceived;
        // 方位情報の更新時に発行されるイベント
        public static event Action<float> OnDirectionReceived;
        // 建物オブジェクトのロードリクエスト
        public static event Action<string> OnPathfindingRequested2D;
        public static event Action<string, string> OnPathfindingRequested3D;
        // 2Dマップへの切り替え
        public static event Action OnSwitchToPlaneView;
        public static event Action OnSwitchToSolidView;


#if UNITY_WEBGL
        [DllImport("__Internal")]
        static extern void CallJavaScriptFunction(string functionName, string message);

        [DllImport("__Internal")]
        static extern void CallJavaScriptFunctionNoArg(string functionName);
#endif


        #region JS -> CSharp

        public void SetUserDevice(string deviceName)
        {
            if (string.IsNullOrEmpty(deviceName))
            {
                Debug.Log("[JSinterface] SetUserDevice: Received null or empty string.");
                SendToJS(JSFunction.OnShowError, "[JSinterface] SetUserDevice: Received null or empty string.");
                return;
            }

            OnDeviceNameReceived?.Invoke(deviceName);
        }

        /// <summary>
        /// 
        /// </summary>
        public void FollowToUser()
        {
            OnUserFollow?.Invoke(zoomLevel);
        }


        /// <summary>
        /// 2Dマップへカメラを切り替えるメソッド
        /// </summary>
        public void SwitchToPlaneView()
        {
            OnSwitchToPlaneView?.Invoke();
        }

        /// <summary>
        /// 3Dマップへカメラを切り替えるメソッド
        /// </summary>
        public void SwitchToSolidView()
        {
            OnSwitchToSolidView?.Invoke();
        }

        /// <summary>
        /// GeoLocationAPIのステータスコードを受け取る
        /// </summary>
        /// <param name="buildingAndRoom"></param>
        public void SetGPSSTate(string gpsState)
        {
            if (string.IsNullOrEmpty(gpsState))
            {
                Debug.Log("[JSInterface] ReceiveLocationFromJS: Received null or empty string.");
                SendToJS(JSFunction.OnShowError, "[JSInterface] ReceiveLocationFromJS: Received null or empty string.");
                return;
            }

            if (int.TryParse(gpsState, NumberStyles.Any, CultureInfo.InvariantCulture, out int state))
            {
                // パース成功。int型のデータをイベントで通知
                OnGeolocationStatusReceived?.Invoke(state);
            }
            else
            {
                Debug.LogError($"[JSInterface] Failed to parse direction data: '{gpsState}'");
            }
        }


        //public void PathfindingRequested(string buildingKey, string roomKey)
        public void PathfindingRequested(string buildingAndRoom)
        {
            if (string.IsNullOrEmpty(buildingAndRoom))
            {
                Debug.Log("[JSInterface] ReceiveLocationFromJS: Received null or empty string.");
                SendToJS(JSFunction.OnShowError, "[JSInterface] ReceiveLocationFromJS: Received null or empty string.");
                return;
            }

            string[] keys = buildingAndRoom.Split('-');
            if (keys.Length == 1)
            {
                OnPathfindingRequested2D?.Invoke(keys[0]);
            }
            else if (keys.Length == 2)
            {
                OnPathfindingRequested3D?.Invoke(keys[0], keys[1]);
            }
        }

        /// <summary>
        /// JSから緯度経度を受け取りパースする
        /// </summary>
        /// <param name="latLon"></param>
        public void SetLocation(string latLon)
        {
            if (string.IsNullOrEmpty(latLon))
            {
                Debug.Log("[JSInterface] ReceiveLocationFromJS: Received null or empty string.");
                SendToJS(JSFunction.OnShowError, "[JSInterface] ReceiveLocationFromJS: Received null or empty string.");
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
        #endregion



        #region CSharp -> JS
        /// <summary>
        /// JavaScriptの特定の関数を呼び出す
        /// </summary>
        /// <param name="functionName">呼び出すJavaScriptの関数名</param>
        /// <param name="message">JavaScript関数に渡す引数 (文字列)</param>
        public static void SendToJS(string functionName, string message)
        {
#if UNITY_EDITOR
            Debug.Log($"[JSInterface] '{functionName}', message'{message}'");
#elif UNITY_WEBGL
            // CallJavaScriptFunction(functionName, message);
            Debug.Log($"[JSInterface] '{functionName}', message'{message}'");
#endif
        }


        /// <summary>
        /// JavaScriptの特定の関数を引数なしで呼び出す
        /// </summary>
        /// <param name="functionName">呼び出すJavaScriptの関数名</param>
        public static void SendToJS(string functionName)
        {
#if UNITY_EDITOR
            Debug.Log($"[JSInterface] '{functionName}'");
#elif UNITY_WEBGL
            Debug.Log($"[JSInterface] '{functionName}'");
            // CallJavaScriptFunctionNoArg(functionName);
#endif
        }
    }
    #endregion
}