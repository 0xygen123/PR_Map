using UnityEngine;
using UnityEngine.InputSystem;

// 1. 生成されたC#クラスのインターフェースを実装する
public class CameraController : MonoBehaviour, CameraControls.ICameraControlActions
{
    [Header("Camera Settings")]
    [SerializeField, Tooltip("カメラの移動速度")]
    private float moveSpeed = 0.005f;

    [SerializeField, Tooltip("マウスホイールでのズーム速度")]
    private float scrollZoomSpeed = 0.5f;

    [SerializeField, Tooltip("ピンチ操作でのズーム速度")]
    private float pinchZoomSpeed = 0.008f;

    [SerializeField, Tooltip("最小ズーム（Orthographic Size）")]
    private float minZoom = 3f;

    [SerializeField, Tooltip("最大ズーム（Orthographic Size）")]
    private float maxZoom = 15f;

    [Header("Map Boundaries (Optional)")]
    [SerializeField, Tooltip("カメラ移動範囲の最小座標")]
    private Vector2 minBounds = new Vector2(-20, -15);

    [SerializeField, Tooltip("カメラ移動範囲の最大座標")]
    private Vector2 maxBounds = new Vector2(20, 15);

    private Camera mainCamera;

    // 2. 生成されたC#クラスのインスタンスを保持
    private CameraControls controls;

    // 入力の状態を保持する変数
    private Vector2 moveInput;
    private bool isPrimaryContact = false;
    private bool isPinching = false;

    private void Awake()
    {
        mainCamera = GetComponent<Camera>();

        // 3. クラスをインスタンス化
        controls = new CameraControls();

        // 4. このスクリプト自体をコールバックの受け手として登録
        controls.CameraControl.SetCallbacks(this);
    }

    private void OnEnable()
    {
        // 5. ActionMapを有効化
        controls.CameraControl.Enable();
    }

    private void OnDisable()
    {
        controls.CameraControl.Disable();
    }

    private void Update()
    {
        // Updateでは、コールバックで更新された状態を見て、実際の処理を実行する

        // ピンチ操作中でなく、ドラッグ/スワイプ中なら移動
        if (!isPinching && isPrimaryContact)
        {
            HandleMove();
        }

        // ピンチ操作中ならズーム
        if (isPinching)
        {
            HandlePinchZoom();
        }
    }

    #region ICampusMapActions Interface Implementations
    // --- 以下は、ICampusMapActionsインターフェースによって実装が要求されるメソッド ---

    public void OnMove(InputAction.CallbackContext context)
    {
        // マウスや指の移動量を読み取り、変数に保持
        moveInput = context.ReadValue<Vector2>();
    }

    public void OnPrimaryContact(InputAction.CallbackContext context)
    {
        // context.ReadValueAsButton() は、押されている間trueを返す
        isPrimaryContact = context.ReadValueAsButton();
    }

    public void OnScroll(InputAction.CallbackContext context)
    {
        // ピンチ操作中はホイールズームを無効化
        if (isPinching) return;

        float scrollValue = context.ReadValue<Vector2>().y;
        if (Mathf.Abs(scrollValue) > 0.1f)
        {
            // スクロール量に応じてズーム
            Zoom(Mathf.Sign(scrollValue) * -scrollZoomSpeed);
            ClampCameraPosition(); // ズーム後にはみ出さないように位置を再調整
        }
    }

    public void OnSecondaryContact(InputAction.CallbackContext context)
    {
        // 2本目の指が触れた/離れたタイミングで isPinching フラグを切り替える
        if (context.started)
        {
            isPinching = true;
        }
        else if (context.canceled)
        {
            isPinching = false;
        }
    }

    // 今回は使用しないが、インターフェースの一部として定義が必要なメソッド
    public void OnPoint(InputAction.CallbackContext context) { }
    public void OnSecondaryPoint(InputAction.CallbackContext context) { }

    #endregion

    #region Private Methods
    // --- 実際のカメラ操作を行うメソッド ---

    private void HandleMove()
    {
        // moveInput（OnMoveコールバックで更新）を使ってカメラを移動
        transform.position -= new Vector3(moveInput.x, moveInput.y, 0) * moveSpeed * mainCamera.orthographicSize;
        //Debug.Log($"{moveInput.x}, {moveInput.y}");
        ClampCameraPosition();
    }

    private void HandlePinchZoom()
    {
        // controls.[ActionMap名].[Action名] で直接Actionにアクセスできる
        Vector2 pos1 = controls.CameraControl.Point.ReadValue<Vector2>();
        Vector2 pos2 = controls.CameraControl.SecondaryPoint.ReadValue<Vector2>();

        // 前のフレームでの2点間距離を、現在の座標と移動量から計算
        float previousDistance = Vector2.Distance(pos1 - moveInput, pos2 - moveInput);
        float currentDistance = Vector2.Distance(pos1, pos2);

        // ゼロ除算を防止
        if (Mathf.Approximately(previousDistance, 0)) return;

        // 距離の変化量に応じてズーム
        float deltaDistance = currentDistance - previousDistance;
        Zoom(deltaDistance * -pinchZoomSpeed);
        ClampCameraPosition();
    }

    private void Zoom(float delta)
    {
        mainCamera.orthographicSize = Mathf.Clamp(mainCamera.orthographicSize + delta, minZoom, maxZoom);
    }

    private void ClampCameraPosition()
    {
        float camHeight = mainCamera.orthographicSize;
        float camWidth = mainCamera.orthographicSize;
        //float camWidth = mainCamera.orthographicSize * mainCamera.aspect;

        // カメラの表示領域を考慮した、移動可能な境界を動的に計算
        float dynamicMinX = minBounds.x + camWidth;
        float dynamicMaxX = maxBounds.x - camWidth;
        float dynamicMinY = minBounds.y + camHeight;
        float dynamicMaxY = maxBounds.y - camHeight;

        Vector3 pos = transform.position;

        //Debug.Log($"{dynamicMinX}, {dynamicMinY}, {dynamicMaxX}, {dynamicMaxY}");

        // X軸のクランプ処理
        // マップの幅より、カメラの表示幅が広いか？
        if (dynamicMinX > dynamicMaxX)
        {
            // 広い場合は、カメラ位置をマップのX軸中央に固定
            pos.x = (minBounds.x + maxBounds.x) / 2;
        }
        else
        {
            // 狭い（通常）場合は、計算した移動範囲内で位置を制限
            pos.x = Mathf.Clamp(pos.x, dynamicMinX, dynamicMaxX);
        }

        // Y軸のクランプ処理
        // マップの高さより、カメラの表示高さが広いか？
        if (dynamicMinY > dynamicMaxY)
        {
            // 広い場合は、カメラ位置をマップのY軸中央に固定
            pos.y = (minBounds.y + maxBounds.y) / 2;
        }
        else
        {
            // 狭い（通常）場合は、計算した移動範囲内で位置を制限
            pos.y = Mathf.Clamp(pos.y, dynamicMinY, dynamicMaxY);
        }

        transform.position = pos;
    }
    #endregion
}