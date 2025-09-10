using UnityEngine;
using UnityEngine.InputSystem;

public class DEVUIController : MonoBehaviour, @DEVUIControls.IDEVUIControllerActions
{
    // 自動生成されたInputActionクラスのインスタンス
    @DEVUIControls devUIControls;

    // デバッグUIのGameObject（Inspectorから設定）
    public GameObject debugUIPanel;
    bool isDebugUIEnabled = false;

    void Awake()
    {
        // 新しいインスタンスを作成
        devUIControls = new @DEVUIControls();
    }

    void OnEnable()
    {
        // コールバックを登録
        devUIControls.DEVUIController.SetCallbacks(this);
        // アクションマップを有効化
        devUIControls.DEVUIController.Enable();
    }

    void OnDisable()
    {
        // アクションマップを無効化
        devUIControls.DEVUIController.Disable();
        // コールバックを解除
        devUIControls.DEVUIController.RemoveCallbacks(this);
    }

    void OnDestroy()
    {
        // アセットを破棄
        devUIControls.Dispose();
    }

    // IDEVUIControllerActionsインターフェースの実装
    // "ToggleUI"アクションが実行されたときに呼び出される
    public void OnToggleUI(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            // デバッグUIの表示/非表示を切り替える
            if (debugUIPanel != null)
            {
                isDebugUIEnabled = !isDebugUIEnabled;
                debugUIPanel.SetActive(isDebugUIEnabled);
                Debug.Log($"デバッグモード: {isDebugUIEnabled}");
            }
        }
    }

    public void OnDebugKey(InputAction.CallbackContext context)
    {
        // if (context.performed)
        // {
        //     Debug.Log("Dキーが押された");
        // }
    }
}
