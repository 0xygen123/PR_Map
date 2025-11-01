using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 建物の階層表示を透明マテリアル差し替えで制御する。
/// Colliderの範囲内のオブジェクトは元のまま、範囲外は完全透明に。
/// </summary>
public class FloorController_Y : MonoBehaviour
{
    [Header("各階層の範囲を定義するCollider")]
    [Tooltip("各階層をぴったり覆うBoxColliderを（1階から順に）登録する")]
    public List<Collider> floorBoundsColliders = new List<Collider>();

    [Header("透明化用マテリアル")]
    [Tooltip("階層外のオブジェクトに適用する完全透明マテリアル")]
    public Material transparentMaterial;

    [Header("制御対象")]
    [Tooltip("自動検出: シーン内のすべてのRendererを対象")]
    public bool autoDetectRenderers = true;

    [Tooltip("手動指定: 制御対象のRenderer")]
    public List<Renderer> targetRenderers = new List<Renderer>();

    [Header("フィルター設定")]
    [Tooltip("対象レイヤー（-1で全レイヤー）")]
    public LayerMask targetLayers = -1;

    [Tooltip("対象タグ（空で全タグ）")]
    public string targetTag = "";

    [Header("階層判定方式")]
    [Tooltip("オブジェクトの中心点で判定（false: Boundsの交差で判定）")]
    public bool useCenterPoint = false;

    // 各Rendererのデータ
    private class RendererData
    {
        public Renderer renderer;
        public Material[] originalMaterials;      // 元のマテリアル（Asset参照）
        public List<int> belongsToFloors = new List<int>(); // 所属する階層リスト
    }

    private List<RendererData> allRendererData = new List<RendererData>();
    private int currentDisplayFloor = -1;

    void Start()
    {
        if (transparentMaterial == null)
        {
            Debug.LogError("[FloorController] 透明マテリアルが設定されていません!");
            return;
        }

        InitializeRenderers();
        Debug.Log($"[FloorController] 初期化完了: {allRendererData.Count}個のRendererを制御");
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

        // テンキー対応
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
    /// Rendererを検出し、どの階層に属するかを判定
    /// </summary>
    private void InitializeRenderers()
    {
        // 既存データから元のマテリアルを保持
        Dictionary<Renderer, Material[]> existingOriginalMaterials = new Dictionary<Renderer, Material[]>();
        foreach (var data in allRendererData)
        {
            if (data.renderer != null && data.originalMaterials != null)
            {
                existingOriginalMaterials[data.renderer] = data.originalMaterials;
            }
        }

        allRendererData.Clear();

        // 自動検出
        if (autoDetectRenderers)
        {
            targetRenderers.Clear();
            Renderer[] allRenderers = FindObjectsByType<Renderer>(FindObjectsSortMode.None);

            foreach (Renderer renderer in allRenderers)
            {
                // レイヤーフィルター
                if (targetLayers != -1 && ((1 << renderer.gameObject.layer) & targetLayers) == 0)
                    continue;

                // タグフィルター
                if (!string.IsNullOrEmpty(targetTag) && !renderer.CompareTag(targetTag))
                    continue;

                targetRenderers.Add(renderer);
            }
        }

        Debug.Log($"[FloorController] {targetRenderers.Count}個のRendererを検出");

        // 各Rendererがどの階層に属するかを判定
        Dictionary<Renderer, List<int>> rendererToFloors = new Dictionary<Renderer, List<int>>();

        for (int floorIndex = 0; floorIndex < floorBoundsColliders.Count; floorIndex++)
        {
            Collider floorCollider = floorBoundsColliders[floorIndex];
            if (floorCollider == null)
            {
                Debug.LogWarning($"[FloorController] 階層 {floorIndex + 1} のColliderが未設定");
                continue;
            }

            Bounds floorBounds = floorCollider.bounds;

            foreach (Renderer renderer in targetRenderers)
            {
                if (renderer == null) continue;

                bool isInFloor = false;

                if (useCenterPoint)
                {
                    // 中心点で判定
                    isInFloor = floorBounds.Contains(renderer.bounds.center);
                }
                else
                {
                    // Boundsの交差で判定
                    isInFloor = floorBounds.Intersects(renderer.bounds);
                }

                if (isInFloor)
                {
                    if (!rendererToFloors.ContainsKey(renderer))
                    {
                        rendererToFloors[renderer] = new List<int>();
                    }
                    rendererToFloors[renderer].Add(floorIndex);
                }
            }
        }

        // RendererDataを作成
        foreach (var kvp in rendererToFloors)
        {
            Renderer renderer = kvp.Key;
            List<int> floors = kvp.Value;

            RendererData data = new RendererData
            {
                renderer = renderer,
                originalMaterials = existingOriginalMaterials.ContainsKey(renderer)
                    ? existingOriginalMaterials[renderer]
                    : renderer.sharedMaterials,
                belongsToFloors = floors
            };

            allRendererData.Add(data);
        }

        Debug.Log($"[FloorController] {allRendererData.Count}個のRendererを登録");
        Debug.Log($"[FloorController] うち{allRendererData.Count(r => r.belongsToFloors.Count > 1)}個が複数階層に所属");
    }

    /// <summary>
    /// 指定した階層のみを表示（透明マテリアル差し替え）
    /// </summary>
    public void ShowFloor(int floorIndex)
    {
        if (floorIndex < 0 || floorIndex >= floorBoundsColliders.Count)
        {
            Debug.LogError($"[FloorController] 無効な階層インデックス: {floorIndex}");
            return;
        }

        currentDisplayFloor = floorIndex;

        int visibleCount = 0;
        int hiddenCount = 0;

        foreach (var data in allRendererData)
        {
            if (data.renderer == null) continue;

            // この階層に属しているかチェック
            bool shouldShow = data.belongsToFloors.Contains(floorIndex);

            if (shouldShow)
            {
                // 表示: 元のマテリアルに戻す
                data.renderer.sharedMaterials = data.originalMaterials;
                visibleCount++;
            }
            else
            {
                // 非表示: 透明マテリアルに差し替え
                Material[] transparentMats = new Material[data.originalMaterials.Length];
                for (int i = 0; i < transparentMats.Length; i++)
                {
                    transparentMats[i] = transparentMaterial;
                }
                data.renderer.sharedMaterials = transparentMats;
                hiddenCount++;
            }
        }

        Debug.Log($"[FloorController] {floorIndex + 1}階を表示: 表示={visibleCount}, 非表示={hiddenCount}");
    }

    /// <summary>
    /// すべての階層を表示（元のマテリアルに戻す）
    /// </summary>
    public void ShowAllFloors()
    {
        currentDisplayFloor = -1;

        foreach (var data in allRendererData)
        {
            if (data.renderer == null) continue;

            // すべて元のマテリアルに戻す
            data.renderer.sharedMaterials = data.originalMaterials;
        }

        Debug.Log($"[FloorController] 全階層を表示");
    }

    public int GetCurrentDisplayFloor() => currentDisplayFloor;

    public void RefreshRenderers()
    {
        int previousFloor = currentDisplayFloor;
        InitializeRenderers();

        if (previousFloor >= 0)
        {
            ShowFloor(previousFloor);
        }

        Debug.Log("[FloorController] Rendererを再検出");
    }

    void OnDrawGizmos()
    {
        // 各階層のColliderを可視化
        for (int i = 0; i < floorBoundsColliders.Count; i++)
        {
            Collider col = floorBoundsColliders[i];
            if (col == null) continue;

            Bounds bounds = col.bounds;

            // 選択中の階層は緑、それ以外は黄色
            Gizmos.color = (i == currentDisplayFloor) ? Color.green : Color.yellow;
            Gizmos.DrawWireCube(bounds.center, bounds.size);
        }
    }

    // UIボタン用
    public void ShowFloor1() { ShowFloor(0); }
    public void ShowFloor2() { ShowFloor(1); }
    public void ShowFloor3() { ShowFloor(2); }
    public void ShowFloor4() { ShowFloor(3); }
    public void ShowFloor5() { ShowFloor(4); }
}