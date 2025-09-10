using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Solid;

namespace Assets.Editor
{
    public class RoomAnchorGenerator : EditorWindow
    {
        BuildingData buildingData;
        GameObject buildingPrefab;
        string roomKeysText = "";
        Vector2 scrollPosition;


        [MenuItem("Tools/Room Anchor Generator")]
        public static void ShowWindow()
        {
            GetWindow<RoomAnchorGenerator>("Room Anchor Generator");
        }

        void OnGUI()
        {
            GUILayout.Label("部屋アンカー自動生成ツール", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("スプレッドシートからコピーした部屋キーを貼り付けて、一括でScriptableObjectとアンカーオブジェクトを生成します。", MessageType.Info);

            // ScriptableObjectとPrefabを受け取るフィールド
            buildingData = (BuildingData)EditorGUILayout.ObjectField("Building Data", buildingData, typeof(BuildingData), false);
            buildingPrefab = (GameObject)EditorGUILayout.ObjectField("Building Prefab", buildingPrefab, typeof(GameObject), false);

            // 部屋キーを入力するテキストエリア
            EditorGUILayout.LabelField("Room Keys (1行に1つ)");
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(200));
            roomKeysText = EditorGUILayout.TextArea(roomKeysText);
            EditorGUILayout.EndScrollView();

            // 必須項目が埋まっているかチェック
            if (buildingData == null || buildingPrefab == null || string.IsNullOrWhiteSpace(roomKeysText))
            {
                EditorGUILayout.HelpBox("Building Data, Building Prefab, Room Keysをすべて設定してください。", MessageType.Warning);
                GUI.enabled = false;
            }

            // 生成ボタン
            if (GUILayout.Button("Generate Anchors"))
            {
                GenerateAnchors();
            }
            GUI.enabled = true;
        }

        void GenerateAnchors()
        {
            // プレハブのパスを取得し、編集用にロード
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(buildingPrefab);
            instance.name = $"{buildingPrefab.name} (Editable)";

            Undo.RegisterCreatedObjectUndo(instance, "Generate Building Instance with Anchors");

            // "Rooms" オブジェクトを探す
            Transform roomsParent = FindOrCreateChild(instance.transform, "Rooms");
            FindOrCreateChild(instance.transform, "Entrances");

            // テキストエリアからキーをリストに変換（重複と空行は除く）
            List<string> roomKeys = roomKeysText.Split(new[] { '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries)
                    .Select(key => key.Trim())
                    .Distinct()
                    .ToList();


            int createdObjectCount = 0;
            int updatedDataCount = 0;
            foreach (string key in roomKeys)
            {
                // 既に同じキーが存在しないかチェック
                if (!buildingData.rooms.Any(r => r.roomKey == key))
                {
                    buildingData.rooms.Add(new RoomInfo { roomKey = key });
                    updatedDataCount++;
                }

                // 既に同名のオブジェクトが存在しないかチェック
                if (roomsParent.Find(key) == null)
                {
                    GameObject newAnchor = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    Undo.RegisterCreatedObjectUndo(newAnchor, "Create Anchor Cube");
                    newAnchor.name = key;
                    newAnchor.transform.SetParent(roomsParent);
                    newAnchor.transform.localPosition = Vector3.zero; // 初期位置は原点
                    newAnchor.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
                    createdObjectCount++;
                }
            }

            if (updatedDataCount > 0)
            {
                EditorUtility.SetDirty(buildingData); // 変更をマーク
                AssetDatabase.SaveAssets();
                Debug.Log($"<color=green>{updatedDataCount}件のRoomキーをBuildingData '{buildingData.name}' に追加しました。</color>");
            }

            if (createdObjectCount > 0)
            {
                Debug.Log($"<color=green>{createdObjectCount}個のアンカーオブジェクトをプレハブ '{buildingPrefab.name}' に追加しました。</color>");
            }

            // 後片付け
            Selection.activeGameObject = instance;
            SceneView.lastActiveSceneView.FrameSelected();

            Debug.Log("<color=cyan>自動生成が完了しました。</color>");
        }

        Transform FindOrCreateChild(Transform parent, string childName)
        {
            Transform childTransform = parent.Find(childName);
            if (childTransform == null)
            {
                GameObject newChild = new GameObject(childName);
                newChild.transform.SetParent(parent);
                newChild.transform.localPosition = Vector3.zero;
                newChild.transform.localRotation = Quaternion.identity;
                newChild.transform.localScale = Vector3.one;
                childTransform = newChild.transform;
            }
            return childTransform;
        }
    }
}