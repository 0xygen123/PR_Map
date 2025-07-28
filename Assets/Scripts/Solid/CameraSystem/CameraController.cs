using UnityEngine;
using UnityEngine.InputSystem;

namespace Assets.Scripts.Solid
{
    /// <summary>
    /// 3Dマップ用のカメラコントローラー。
    /// ICameraControlActionsインターフェースを実装して入力処理を行う。
    /// </summary>
    public class CameraController3D : MonoBehaviour, CameraControls.ICameraControlActions
    {
        [Header("ターゲット設定")]
        [Tooltip("カメラが周回する中心のオブジェクト")]
        [SerializeField] Transform target;

        [Header("周回・回転 設定")]
        [Tooltip("マウス/タッチでの回転速度")]
        [SerializeField] float rotationSpeed = 10.0f;
        [Tooltip("カメラの上下の角度制限（最小, 最大）")]
        [SerializeField] Vector2 pitchMinMax = new Vector2(-40f, 85f);

        [Header("ズーム設定")]
        [Tooltip("ターゲットからの初期距離")]
        [SerializeField] float initialDistance = 10.0f;
        [Tooltip("マウスホイールでのズーム速度")]
        [SerializeField] float zoomSpeed = 5.0f;
        [Tooltip("ズームできる距離の範囲（最小, 最大）")]
        [SerializeField] Vector2 distanceMinMax = new Vector2(3f, 50f);

        // --- プライベート変数 ---
        CameraControls cameraControls;
        bool isRotating = false;
        Vector2 rotationInput;

        float currentDistance;
        float yaw = 0.0f;   // 水平方向の回転角度
        float pitch = 20.0f; // 垂直方向の回転角度

        void Awake()
        {
            // Input System のコントロールクラスをインスタンス化
            cameraControls = new CameraControls();

            // このクラスのインターフェース実装をコールバックとして登録する
            cameraControls.CameraControl.SetCallbacks(this);

            // 初期距離を設定
            currentDistance = initialDistance;
        }

        void OnEnable()
        {
            // このスクリプトが有効になったときに入力を有効化
            cameraControls.CameraControl.Enable();
        }

        void OnDisable()
        {
            // このスクリプトが無効になったときに入力を無効化
            cameraControls.CameraControl.Disable();
        }

        void LateUpdate()
        {
            if (target == null)
            {
                Debug.LogWarning("ターゲットが設定されていません。");
                return;
            }

            // isRotatingフラグがtrueの時だけカメラを回転させる
            if (isRotating)
            {
                // 入力値と時間経過、回転速度を元に回転角度を更新
                yaw += rotationInput.x * rotationSpeed * Time.deltaTime;
                pitch -= rotationInput.y * rotationSpeed * Time.deltaTime;

                // pitch（上下の角度）が設定した範囲を超えないように制限
                pitch = Mathf.Clamp(pitch, pitchMinMax.x, pitchMinMax.y);
            }

            // オイラー角からQuaternion（回転）を計算
            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0);

            // カメラの位置を計算
            // ターゲットの位置から、計算した回転と現在の距離分離れた位置にカメラを配置
            Vector3 targetOffset = new Vector3(0, 0, -currentDistance);
            transform.position = target.position + rotation * targetOffset;

            // カメラが常にターゲットの方向を向くように設定
            transform.LookAt(target.position);
        }

        void Zoom(float scrollValue)
        {
            // スクロール方向に応じて距離を増減
            currentDistance -= scrollValue * zoomSpeed * Time.deltaTime;
            // 距離が設定した範囲を超えないように制限
            currentDistance = Mathf.Clamp(currentDistance, distanceMinMax.x, distanceMinMax.y);
        }

        #region ICameraControlActions Interface Implementations

        public void OnMove(InputAction.CallbackContext context)
        {
            // Moveアクションが実行中（performed）またはキャンセル（canceled）されたときに入力値を取得
            // 入力が止まると自動的にVector2.zeroが読み込まれる
            rotationInput = context.ReadValue<Vector2>();
        }

        public void OnPrimaryContact(InputAction.CallbackContext context)
        {
            // PrimaryContactアクションが開始（started）されたら回転フラグを立てる
            if (context.started)
            {
                isRotating = true;
            }
            // アクションがキャンセル（canceled）されたら回転フラグを下ろす
            else if (context.canceled)
            {
                isRotating = false;
            }
        }

        public void OnScroll(InputAction.CallbackContext context)
        {
            // Scrollアクションが実行（performed）されたときだけZoom処理を呼び出す
            if (context.performed)
            {
                Zoom(context.ReadValue<Vector2>().y);
            }
        }

        // --- 以下のアクションは今回使用しないため、中身は空のまま ---

        public void OnPoint(InputAction.CallbackContext context) { }

        public void OnSecondaryPoint(InputAction.CallbackContext context) { }

        public void OnSecondaryContact(InputAction.CallbackContext context) { }

        #endregion

        #region 3DmapObject
        public void SetMapObject(GameObject mapObject)
        {
            target = mapObject.transform;
        }
        #endregion
    }
}