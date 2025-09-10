using UnityEngine;
using System.Collections.Generic; // Listを使うために必要
using Assets.Scripts.Plane.Road;

namespace Assets.Scripts.Core
{
    // MeshFilterとMeshRendererがアタッチされていることを保証する
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class PathRenderer : MonoBehaviour
    {
        [SerializeField] RoadNetworkBuilder roadNetworkBuilder;
        [SerializeField] float pathWidth = 0.5f; // 経路の帯の幅

        MeshFilter pathMeshFilter;
        MeshRenderer pathMeshRenderer;

        void Awake()
        {
            // アタッチされているコンポーネントを自動で取得
            pathMeshFilter = GetComponent<MeshFilter>();
            pathMeshRenderer = GetComponent<MeshRenderer>();

            // 最初は非表示にしておく
            pathMeshRenderer.enabled = false;
        }

        public void DrawPath(PathfindingManager.PathResult result)
        {
            ClearAllPaths();

            // 屋外経路の描画
            if (result.OutdoorPath != null && result.OutdoorPath.Count > 0)
            {
                roadNetworkBuilder.VisualizePathByChangingMaterial(result.OutdoorPath);
            }

            // 屋内経路の描画（メッシュ生成）
            if (result.IndoorPathCoordinates != null && result.IndoorPathCoordinates.Count > 1)
            {
                GeneratePathMesh(result.IndoorPathCoordinates);
                pathMeshRenderer.enabled = true; // メッシュができたら表示
            }
        }

        public void ClearAllPaths()
        {
            roadNetworkBuilder.ResetAllRoadMaterials();
            
            // メッシュをクリアして非表示に
            if (pathMeshFilter != null)
            {
                pathMeshFilter.mesh = null; 
            }
            if(pathMeshRenderer != null)
            {
                pathMeshRenderer.enabled = false;
            }
        }

        /// <summary>
        /// 経路座標リストから帯状のメッシュを生成する
        /// </summary>
        void GeneratePathMesh(List<Vector3> pathPoints)
        {
            if (pathPoints.Count < 2) return;

            Mesh mesh = new Mesh();
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            var uvs = new List<Vector2>();

            float distanceAlongPath = 0f;

            for (int i = 0; i < pathPoints.Count; i++)
            {
                // 経路の進行方向を計算（コーナーで滑らかになるように前後のセグメントから平均を取る）
                Vector3 forward = Vector3.zero;
                if (i < pathPoints.Count - 1) forward += (pathPoints[i + 1] - pathPoints[i]).normalized;
                if (i > 0) forward += (pathPoints[i] - pathPoints[i - 1]).normalized;
                forward.Normalize();

                // 進行方向に対して水平な右方向ベクトルを計算
                Vector3 right = Vector3.Cross(Vector3.up, forward).normalized * (pathWidth / 2f);

                // 左右の頂点を生成
                Vector3 vertLeft = pathPoints[i] - right;
                Vector3 vertRight = pathPoints[i] + right;

                // (任意) Zファイティング対策で少しだけ頂点を持ち上げる
                // vertLeft += Vector3.up * 0.05f;
                // vertRight += Vector3.up * 0.05f;

                vertices.Add(vertLeft);
                vertices.Add(vertRight);

                // UV座標を設定（U:幅方向, V:進行方向）
                // 進行方向のV座標には、経路に沿った実距離を使うとテクスチャの伸び縮みを防げる
                if (i > 0) distanceAlongPath += Vector3.Distance(pathPoints[i], pathPoints[i - 1]);
                uvs.Add(new Vector2(0, distanceAlongPath)); // 左端 (U=0)
                uvs.Add(new Vector2(1, distanceAlongPath)); // 右端 (U=1)

                // 2つの頂点ペア（4頂点）から2つの三角形（ポリゴン）を生成
                if (i > 0)
                {
                    int baseIndex = (i - 1) * 2;
                    triangles.Add(baseIndex);       // 前の左
                    triangles.Add(baseIndex + 2);   // 今の左
                    triangles.Add(baseIndex + 1);   // 前の右

                    triangles.Add(baseIndex + 1);   // 前の右
                    triangles.Add(baseIndex + 2);   // 今の左
                    triangles.Add(baseIndex + 3);   // 今の右
                }
            }

            // 生成した頂点リストなどをメッシュに設定
            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.uv = uvs.ToArray();
            mesh.RecalculateNormals(); // 法線を再計算してライティングを正しくする

            pathMeshFilter.mesh = mesh; // MeshFilterに完成したメッシュを渡す

            Debug.Log($"3. メッシュを生成完了。頂点数: {mesh.vertexCount}, 三角形インデックス数: {mesh.triangles.Length}");
            
            // 三角形が1つでも作られているかチェック
            if (mesh.triangles.Length == 0)
            {
                Debug.LogError("エラー: 三角形が一つも生成されていません！");
            }
        }
    }
}