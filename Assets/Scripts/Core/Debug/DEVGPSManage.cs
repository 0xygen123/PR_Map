using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using TMPro;
#if UNITY_ANDROID
using UnityEngine.Android; // Androidのパーミッション機能を使うために必要
#endif

public class DEVGPSManager : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI latitudeText;
    [SerializeField] TextMeshProUGUI longitudeText;
    [SerializeField] TextMeshProUGUI altitudeText;

    [SerializeField] UserLocationManager userLocationManager;

    void Start()
    {
        StartCoroutine(GetLocation());
    }

    IEnumerator GetLocation()
    {
        #if UNITY_ANDROID
            if (!Permission.HasUserAuthorizedPermission(Permission.FineLocation))
            {
                Permission.RequestUserPermission(Permission.FineLocation);

                // ユーザーが応答するまで待つ
                while (!Permission.HasUserAuthorizedPermission(Permission.FineLocation))
                {
                    yield return new WaitForSeconds(1);
                }
            }
        #endif
        // まず、ユーザーが位置情報サービスを有効にしているかチェック
        if (!Input.location.isEnabledByUser)
        {
            Debug.LogWarning("Location services are not enabled by the user.");
            yield break; // 処理を中断
        }

        // 位置情報サービスの開始
        Input.location.Start();

        // サービスが初期化されるまで待つ（最大20秒）
        int maxWait = 20;
        while (Input.location.status == LocationServiceStatus.Initializing && maxWait > 0)
        {
            yield return new WaitForSeconds(1);
            maxWait--;
        }

        // タイムアウトした場合
        if (maxWait < 1)
        {
            Debug.LogWarning("Timed out");
            yield break;
        }

        // 接続に失敗した場合
        if (Input.location.status == LocationServiceStatus.Failed)
        {
            Debug.LogError("Unable to determine device location");
            yield break;
        }
        else
        {
            // 接続に成功した場合
            Debug.Log("Location service started successfully.");

            // 無限ループで位置情報を更新し続ける
            while (true)
            {
                // 最新の位置情報を取得
                LocationInfo locationData = Input.location.lastData;

                // UIに表示
                latitudeText.text = "緯度: " + locationData.latitude.ToString();
                longitudeText.text = "経度: " + locationData.longitude.ToString();
                altitudeText.text = "高度: " + locationData.altitude.ToString();

                userLocationManager.SetLocation($"{locationData.latitude}, {locationData.longitude}");

                // 5秒待ってから次の更新へ
                yield return new WaitForSeconds(5);
            }
        }
    }

    // アプリケーションが終了するときにGPSを停止
    void OnDestroy()
    {
        if (Input.location.status == LocationServiceStatus.Running)
        {
            Input.location.Stop();
            Debug.Log("Location service stopped.");
        }
    }
}