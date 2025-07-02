using UnityEngine;
using Random = UnityEngine.Random;
using System.Collections;
using System.Linq;

#if UNITY_EDITOR
public class DevLocationGenerator : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] UserLocationManager userLocationManager;
    [SerializeField] RoadNetworkBuilder roadNetworkBuilder;

    [Header("Settings")]
    [Tooltip("このチェックボックスでGPS生成のON/OFFを切り替えます")]
    [SerializeField] private bool executeGeneration; // Inspectorで操作するフラグ
    [Tooltip("位置情報を更新する間隔（秒）")]
    [SerializeField] float updateInterval = 10.0f;
    [Tooltip("生成される座標に加える誤差の半径")]
    [SerializeField] float accuracyRadius = 5.0f;

    // 実行中のコルーチンを保存する変数
    private Coroutine _generatorCoroutine;
    private RuntimeNode currentNode;

    // UpdateメソッドでInspectorのチェック状態を監視する
    void Update()
    {
        // チェックがONにされた、かつコルーチンがまだ動いていない場合
        if (executeGeneration && _generatorCoroutine == null)
        {
            // コルーチンを開始し、その参照を保存
            _generatorCoroutine = StartCoroutine(GenerateLocationCoroutine());
        }
        // チェックがOFFにされた、かつコルーチンが動いている場合
        else if (!executeGeneration && _generatorCoroutine != null)
        {
            // 保存しておいた参照を使ってコルーチンを停止
            StopCoroutine(_generatorCoroutine);
            // 変数をリセット
            _generatorCoroutine = null;
            Debug.Log("Location generation stopped by user.");
        }
    }

    IEnumerator GenerateLocationCoroutine()
    {
        Debug.Log("Location generation started.");

        var nodes = roadNetworkBuilder.RuntimeNodes.Values.ToList();
        if (nodes.Count == 0)
        {
            Debug.LogError("No nodes found. Stopping coroutine.");
            // 実行フラグをOFFにして終了
            executeGeneration = false;
            yield break;
        }
        currentNode = nodes[Random.Range(0, nodes.Count)];

        while (true) // ループ条件をシンプルに
        {
            // (この中の処理は変更なし)
            Vector2 randomOffset = Random.insideUnitCircle * accuracyRadius;
            Vector2 generatedPosition = new Vector2(currentNode.position.x + randomOffset.x, currentNode.position.y + randomOffset.y);

            if (roadNetworkBuilder.MetersPerDegreeLon == 0)
            {
                Debug.Log(roadNetworkBuilder.MetersPerDegreeLon);
                break;
            }

            double lon = (generatedPosition.x / roadNetworkBuilder.MetersPerDegreeLon) + roadNetworkBuilder.centerLongitude;
            double lat = (generatedPosition.y / RoadNetworkBuilder.METERS_PER_DEGREE_LAT) + roadNetworkBuilder.centerLatitude;

            Debug.Log($"[Dev] Generating new location near Node {currentNode.nodeId}. Sending Lat: {lat:F8}, Lon: {lon:F8}");
            userLocationManager.SetLocation($"{lat},{lon}");

            if (currentNode.connectedEdges.Count == 0)
            {
                currentNode = nodes[Random.Range(0, nodes.Count)];
            }
            else
            {
                RuntimeEdge nextEdge = currentNode.connectedEdges[Random.Range(0, currentNode.connectedEdges.Count)];
                currentNode = nextEdge.toNode;
            }

            yield return new WaitForSeconds(updateInterval);
        }
    }
}
#endif