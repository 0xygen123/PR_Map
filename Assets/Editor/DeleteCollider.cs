using UnityEngine;
using UnityEditor;


namespace Assets.Editor
{
    public class DeleteCollider : EditorWindow
    {
        [Header("Delete Collider from GameObjects")]
        GameObject targetGameObject;

        [MenuItem("Tools/Collider Deleter")]
        public static void ShowWindow()
        {
            GetWindow<DeleteCollider>("Collider Deleter");
        }

        public void OnGUI()
        {
            /// コライダ削除
            GUILayout.Space(20);
            GUILayout.Label("既存のオブジェクトのColliderを削除", EditorStyles.boldLabel);

            targetGameObject = (GameObject)EditorGUILayout.ObjectField("Target GameObject", targetGameObject, typeof(GameObject), true);

            if (targetGameObject == null)
            {
                GUI.enabled = false;
            }
            if (GUILayout.Button("Delete Colliders from Children"))
            {
                DeleteColliderAndApplyLayer();
            }
            GUI.enabled = true;
        }

        void DeleteColliderAndApplyLayer()
        {
            // "DEV" レイヤーのIDを取得
            int devLayer = LayerMask.NameToLayer("DEV");
            if (devLayer == -1)
            {
                Debug.LogError("The layer 'DEV' does not exist. Please create it in your Unity project.");
                return; // レイヤーが存在しない場合は処理を中断
            }

            if (targetGameObject == null)
            {
                Debug.LogError("TargetGameObjectが指定されていません");
                return;
            }
            int count = 0;
            foreach (Transform child in targetGameObject.transform)
            {
                Collider collider = child.GetComponent<Collider>();
                child.gameObject.layer = devLayer;
                if (collider != null)
                {
                    DestroyImmediate(collider);
                    count++;
                }
            }
            Debug.Log($"<color=green>{count}個の子オブジェクトからColliderを削除し、レイヤーを'DEV'に設定しました。</color>");
        }
    }
}