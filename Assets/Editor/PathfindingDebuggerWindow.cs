#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using Assets.Scripts.Solid;
using Assets.Scripts.Core; // PathRenderer を見つけるために必要

/// <summary>
/// 3D経路探索をデバッグするためのUnityエディタ拡張ウィンドウです。
/// このスクリプトは "Assets" フォルダ内の "Editor" フォルダに配置する必要があります。
/// 使用方法：
/// 1. Unityメニューの [Window] > [Pathfinding Debugger] からウィンドウを開きます。
/// 2. "Building Data" にテストしたい建物の BuildingData アセットをアサインします。
/// 3. "Building Instance" にシーン内に配置されている建物のGameObjectをアサインします。
/// 4. ウィンドウ内のボタンをクリックして、経路を一つずつテストします。
/// </summary>
public class PathfindingDebuggerWindow : EditorWindow
{
    // --- Private Fields ---
    private BuildingData buildingToTest;
    private GameObject buildingInstance;

    private int currentEntranceIndex = 0;
    private int currentRoomIndex = 0;

    // --- Component References ---
    private NavMeshController navMeshController;
    private BuildingReferenceProvider referenceProvider;
    private PathRenderer pathRenderer;

    private Vector2 scrollPosition;

    [MenuItem("Tools/Pathfinding Debugger")]
    public static void ShowWindow()
    {
        // ウィンドウを表示します
        GetWindow<PathfindingDebuggerWindow>("Pathfinding Debugger");
    }

    /// <summary>
    /// エディタウィンドウのGUIを描画します。
    /// </summary>
    void OnGUI()
    {
        // スクロールビューを開始
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        GUILayout.Label("Pathfinding Test Setup", EditorStyles.boldLabel);

        // ScriptableObjectとSceneオブジェクトをGUIから設定できるようにする
        buildingToTest = (BuildingData)EditorGUILayout.ObjectField("Building Data", buildingToTest, typeof(BuildingData), false);
        buildingInstance = (GameObject)EditorGUILayout.ObjectField("Building Instance (In Scene)", buildingInstance, typeof(GameObject), true);
        pathRenderer = (PathRenderer)EditorGUILayout.ObjectField("Path Renderer (In Scene)", pathRenderer, typeof(PathRenderer), true);

        // 必須項目が設定されていない場合は警告を表示して処理を中断
        if (buildingToTest == null || buildingInstance == null || pathRenderer == null)
        {
            EditorGUILayout.HelpBox("Please assign the Building Data, Building Instance, and Path Renderer.", MessageType.Warning);
            EditorGUILayout.EndScrollView();
            return;
        }

        // 参照が設定されたら、関連コンポーネントを自動で取得
        if (!GetComponentsFromInstance())
        {
            EditorGUILayout.HelpBox("The building instance is missing a NavMeshController or BuildingReferenceProvider.", MessageType.Error);
            EditorGUILayout.EndScrollView();
            return;
        }

        // 参照が設定されたら、関連コンポーネントを自動で取得
        if (!GetComponentsFromInstance())
        {
            EditorGUILayout.HelpBox("The building instance is missing a NavMeshController or BuildingReferenceProvider, or there is no PathRenderer in the scene.", MessageType.Error);
            EditorGUILayout.EndScrollView();
            return;
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Test Controls", EditorStyles.boldLabel);

        // 現在テスト中の組み合わせを表示
        string entranceName = (buildingToTest.entrances.Count > 0) ? buildingToTest.entrances[currentEntranceIndex].entranceKey : "N/A";
        string roomName = (buildingToTest.rooms.Count > 0) ? buildingToTest.rooms[currentRoomIndex].roomKey : "N/A";
        EditorGUILayout.LabelField("Current Test:", $"{entranceName}  ->  {roomName}");

        EditorGUILayout.Space();

        // Entrance (入り口) のコントロールボタン
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("<< Prev Entrance"))
        {
            currentEntranceIndex--;
            if (currentEntranceIndex < 0) currentEntranceIndex = buildingToTest.entrances.Count - 1;
            TestCurrentPath();
        }
        if (GUILayout.Button("Next Entrance >>"))
        {
            if (buildingToTest.entrances.Count > 0)
            {
                currentEntranceIndex = (currentEntranceIndex + 1) % buildingToTest.entrances.Count;
            }
            TestCurrentPath();
        }
        EditorGUILayout.EndHorizontal();

        // Room (部屋) のコントロールボタン
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("<< Prev Room"))
        {
            currentRoomIndex--;
            if (currentRoomIndex < 0) currentRoomIndex = buildingToTest.rooms.Count - 1;
            TestCurrentPath();
        }
        if (GUILayout.Button("Next Room >>"))
        {
             if (buildingToTest.rooms.Count > 0)
            {
                currentRoomIndex = (currentRoomIndex + 1) % buildingToTest.rooms.Count;
            }
            TestCurrentPath();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        // 現在の組み合わせでテストを再実行するボタン
        if (GUILayout.Button("Run Current Test"))
        {
            TestCurrentPath();
        }
        
        EditorGUILayout.EndScrollView();
    }
    
    /// <summary>
    /// 指定された建物インスタンスから必要なコンポーネントを取得します。
    /// </summary>
    private bool GetComponentsFromInstance()
    {
        // 建物インスタンスが変更された場合に備えて、毎回参照を確認
        navMeshController = buildingInstance.GetComponent<NavMeshController>();
        referenceProvider = buildingInstance.GetComponent<BuildingReferenceProvider>();
        
        // PathRenderer は GUI から直接アサインされるように変更しました
        return navMeshController != null && referenceProvider != null && pathRenderer != null;
    }

    /// <summary>
    /// 現在選択されているEntranceとRoomの組み合わせで経路探索を実行します。
    /// </summary>
    private void TestCurrentPath()
    {
        if (buildingToTest.entrances.Count == 0 || buildingToTest.rooms.Count == 0) return;

        // インデックスが範囲外になるのを防ぐ
        currentEntranceIndex = Mathf.Clamp(currentEntranceIndex, 0, buildingToTest.entrances.Count - 1);
        currentRoomIndex = Mathf.Clamp(currentRoomIndex, 0, buildingToTest.rooms.Count - 1);

        EntranceInfo currentEntrance = buildingToTest.entrances[currentEntranceIndex];
        RoomInfo currentRoom = buildingToTest.rooms[currentRoomIndex];

        // 参照プロバイダーから開始地点と目標地点のTransformを取得
        Transform startTransform = referenceProvider.GetReference(currentEntrance.entranceKey);
        Transform endTransform = referenceProvider.GetReference(currentRoom.roomKey);

        // Transformが見つからない場合はエラーログを出して終了
        if (startTransform == null || endTransform == null)
        {
            Debug.LogError($"Transform not found for Entrance '{currentEntrance.entranceKey}' or Room '{currentRoom.roomKey}'.");
            pathRenderer.ClearAllPaths();
            return;
        }

        // NavMeshControllerを使って経路を計算
        var (path, cost) = navMeshController.FindPathAndCost(startTransform.position, endTransform.position);

        // 結果に応じてログ出力と経路描画を行う
        if (cost >= 0)
        {
            Debug.Log($"<color=green>SUCCESS:</color> Path found from {currentEntrance.entranceKey} to {currentRoom.roomKey}. Cost: {cost:F2}");
            // PathRendererが描画できるようにPathResultオブジェクトを作成
            var result = new PathfindingManager.PathResult { IndoorPathCoordinates = path, OutdoorPath = null };
            pathRenderer.DrawPath(result);
        }
        else
        {
            Debug.LogWarning($"<color=red>FAILED:</color> No path found from {currentEntrance.entranceKey} to {currentRoom.roomKey}.");
            pathRenderer.ClearAllPaths();
        }
    }
}
#endif

