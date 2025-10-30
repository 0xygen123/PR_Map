using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 建物の階層表示を制御する。
/// 指定した階層以外のCollider内のオブジェクトを透明マテリアルに差し替えることで、
/// 特定の階層のみを表示する。
/// </summary>
public class FloorController : MonoBehaviour
{
    [Header("各階層の範囲を定義するCollider")]
    [Tooltip("各階層をぴったり覆うBoxColliderを（1階から順に）登録する。Is Trigger推奨")]
    public List<Collider> floorBoundsColliders = new List<Collider>();

    [Header("透明化用マテリアル")]
    [Tooltip("階層外のオブジェクトに適用する透明マテリアル")]
    public Material transparentMaterial;

    [Header("設定（オプション）")]
    [Tooltip("自動検出対象のレイヤー。空の場合はすべてのレイヤーを対象")]
    public LayerMask targetLayers = -1;

    [Tooltip("特定のタグを持つオブジェクトのみを対象にする（空の場合は全て）")]
    public string targetTag = "";

    // 各階層内のレンダラーとその元のマテリアルを保持
    private class RendererData
    {
        public Renderer renderer;
        public Material[] originalMaterials;
        public int floorIndex; // どの階層に属しているか
    }

    private List<RendererData> allRenderers = new List<RendererData>();
    private int currentDisplayFloor = -1; // -1 = 全階表示

    void Start()
    {
        if (transparentMaterial == null)
        {
            Debug.LogError("透明化用マテリアルが設定されていません!");
            return;
        }

        // 各階層内のレンダラーを自動検出
        DetectRenderersInFloors();
        
        Debug.Log($"FloorController初期化完了: {allRenderers.Count}個のRendererを検出");
    }

    void Update()
    {
        // 数字キー 1-9 で階層切り替え
        for (int i = 0; i < Mathf.Min(9, floorBoundsColliders.Count); i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
            {
                ShowFloor(i);
            }
        }

        // 数字キー 0 で全階表示
        if (Input.GetKeyDown(KeyCode.Alpha0))
        {
            ShowAllFloors();
        }

        // テンキーにも対応
        for (int i = 0; i < Mathf.Min(9, floorBoundsColliders.Count); i++)
        {
            if (Input.GetKeyDown(KeyCode.Keypad1 + i))
            {
                ShowFloor(i);
            }
        }

        if (Input.GetKeyDown(KeyCode.Keypad0))
        {
            ShowAllFloors();
        }
    }

    /// <summary>
    /// 各階層のCollider内にあるレンダラーを自動検出し、元のマテリアルを保存
    /// </summary>
    private void DetectRenderersInFloors()
    {
        allRenderers.Clear();

        for (int floorIndex = 0; floorIndex < floorBoundsColliders.Count; floorIndex++)
        {
            Collider floorCollider = floorBoundsColliders[floorIndex];
            if (floorCollider == null)
            {
                Debug.LogWarning($"階層 {floorIndex} のColliderが設定されていません。");
                continue;
            }

            // Collider内のすべてのRendererを検出
            Bounds bounds = floorCollider.bounds;
            Renderer[] allSceneRenderers = FindObjectsByType<Renderer>(FindObjectsSortMode.None);

            foreach (Renderer renderer in allSceneRenderers)
            {
                // レイヤーチェック
                if (targetLayers != -1 && ((1 << renderer.gameObject.layer) & targetLayers) == 0)
                    continue;

                // タグチェック
                if (!string.IsNullOrEmpty(targetTag) && !renderer.gameObject.CompareTag(targetTag))
                    continue;

                // Rendererの位置がこの階層のBounds内にあるかチェック
                if (bounds.Contains(renderer.bounds.center))
                {
                    // 既に登録済みでないかチェック
                    if (!allRenderers.Any(r => r.renderer == renderer))
                    {
                        RendererData data = new RendererData
                        {
                            renderer = renderer,
                            originalMaterials = renderer.sharedMaterials.ToArray(),
                            floorIndex = floorIndex
                        };
                        allRenderers.Add(data);
                    }
                }
            }
        }
    }

    /// <summary>
    /// 指定した階層のみを表示する（それ以外の階層を透明化）
    /// </summary>
    /// <param name="floorIndex">表示したい階層のインデックス（0 = 1階, 1 = 2階, ...）</param>
    public void ShowFloor(int floorIndex)
    {
        if (floorIndex < 0 || floorIndex >= floorBoundsColliders.Count)
        {
            Debug.LogError($"無効な階層インデックスです: {floorIndex}");
            return;
        }

        if (transparentMaterial == null)
        {
            Debug.LogError("透明化用マテリアルが設定されていません!");
            return;
        }

        currentDisplayFloor = floorIndex;

        foreach (RendererData data in allRenderers)
        {
            if (data.renderer == null) continue;

            if (data.floorIndex == floorIndex)
            {
                // 表示する階層: 元のマテリアルに戻す
                data.renderer.sharedMaterials = data.originalMaterials;
            }
            else
            {
                // 非表示にする階層: 透明マテリアルに差し替え
                Material[] transparentMats = new Material[data.renderer.sharedMaterials.Length];
                for (int i = 0; i < transparentMats.Length; i++)
                {
                    transparentMats[i] = transparentMaterial;
                }
                data.renderer.sharedMaterials = transparentMats;
            }
        }
        
        Debug.Log($"階層 {floorIndex + 1} を表示しました。({allRenderers.Count(r => r.floorIndex != floorIndex)}個のRendererを透明化)");
    }

    /// <summary>
    /// すべての階層を表示する（すべてを元のマテリアルに戻す）
    /// </summary>
    public void ShowAllFloors()
    {
        currentDisplayFloor = -1;

        foreach (RendererData data in allRenderers)
        {
            if (data.renderer == null) continue;
            
            // すべて元のマテリアルに戻す
            data.renderer.sharedMaterials = data.originalMaterials;
        }
        
        Debug.Log("全階層を表示しました。");
    }

    /// <summary>
    /// 現在の表示状態を取得
    /// </summary>
    public int GetCurrentDisplayFloor()
    {
        return currentDisplayFloor;
    }

    /// <summary>
    /// レンダラーの再検出（シーン変更時など）
    /// </summary>
    public void RefreshRenderers()
    {
        int previousFloor = currentDisplayFloor;
        DetectRenderersInFloors();
        
        if (previousFloor >= 0)
        {
            ShowFloor(previousFloor);
        }
        
        Debug.Log("レンダラーを再検出しました。");
    }

    // （参考）UIボタンなどから呼び出すためのpublic関数
    // UnityEvent（OnClickなど）に設定する場合
    public void ShowFloor1() { ShowFloor(0); }
    public void ShowFloor2() { ShowFloor(1); }
    public void ShowFloor3() { ShowFloor(2); }
    public void ShowFloor4() { ShowFloor(3); }
    public void ShowFloor5() { ShowFloor(4); }
    public void ShowFloor6() { ShowFloor(5); }
}