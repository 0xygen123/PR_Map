using UnityEngine;
using UnityEngine.InputSystem;

namespace Assets.Scripts.Solid
{
    /// <summary>
    /// 3Dマップ用のカメラコントローラー（平行投影版）。
    /// ICameraControlActionsインターフェースを実装して入力処理を行う。
    /// </summary>
    public class CameraController : MonoBehaviour, CameraControls.ICameraControlActions
    {
        [Header("ターゲット設定")]
        [Tooltip("カメラが周回する中心のオブジェクト")]
        [SerializeField] Transform target;

        [Header("周回・回転 設定")]
        [Tooltip("マウス/タッチでの回転速度")]
        [SerializeField] float rotationSpeed = 10.0f;
        [Tooltip("カメラの上下の角度制限（最小, 最大）")]
        [SerializeField] Vector2 pitchMinMax = new Vector2(-40f, 85f);
        [Tooltip("カメラがターゲットから離れる距離（回転半径）")]
        [SerializeField] float rotationRadius = 15.0f;

        [Header("ズーム設定 (平行投影)")]
        [Tooltip("初期のOrthographic Size")]
        [SerializeField] float initialOrthographicSize = 10.0f;
        [Tooltip("マウスホイールでのズーム速度")]
        [SerializeField] float zoomSpeed = 20.0f;
        [Tooltip("ズームできるOrthographic Sizeの範囲（最小, 最大）")]
        [SerializeField] Vector2 orthographicSizeMinMax = new Vector2(3f, 50f);

        CameraControls cameraControls;
        Camera mainCamera; // カメラコンポーネントへの参照
        bool isRotating = false;
        Vector2 rotationInput;

        float yaw = 0.0f;   // 水平方向の回転角度
        float pitch = 20.0f; // 垂直方向の回転角度

        void Awake()
        {
            // このオブジェクトにアタッチされているCameraコンポーネントを取得
            mainCamera = GetComponent<Camera>();
            
            // カメラを平行投影（Orthographic）に設定
            mainCamera.orthographic = true;
            // 初期Orthographic Sizeを設定
            mainCamera.orthographicSize = initialOrthographicSize;

            // Input System のコントロールクラスをインスタンス化
            cameraControls = new CameraControls();

            // このクラスのインターフェース実装をコールバックとして登録する
            cameraControls.CameraControl.SetCallbacks(this);
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
            // ターゲットの位置から、計算した回転と指定した半径分離れた位置にカメラを配置
            Vector3 targetOffset = new Vector3(0, 0, -rotationRadius);
            transform.position = target.position + rotation * targetOffset;

            // カメラが常にターゲットの方向を向くように設定
            transform.LookAt(target.position);
        }

        /// <summary>
        /// Orthographic Sizeを変更してズーム処理を行う
        /// </summary>
        /// <param name="scrollValue">マウスホイールのY軸スクロール量</param>
        void Zoom(float scrollValue)
        {
            // スクロール方向に応じてOrthographic Sizeを増減
            // Sizeが小さいほどズームイン（表示範囲が狭くなる）
            float newSize = mainCamera.orthographicSize - scrollValue * zoomSpeed * Time.deltaTime;
            
            // Orthographic Sizeが設定した範囲を超えないように制限
            mainCamera.orthographicSize = Mathf.Clamp(newSize, orthographicSizeMinMax.x, orthographicSizeMinMax.y);
        }

        #region ICameraControlActions Interface Implementations

        public void OnMove(InputAction.CallbackContext context)
        {
            rotationInput = context.ReadValue<Vector2>();
        }

        public void OnPrimaryContact(InputAction.CallbackContext context)
        {
            if (context.started)
            {
                isRotating = true;
            }
            else if (context.canceled)
            {
                isRotating = false;
            }
        }

        public void OnScroll(InputAction.CallbackContext context)
        {
            if (context.performed)
            {
                // Y軸のスクロール値（上下）をZoom関数に渡す
                Zoom(context.ReadValue<Vector2>().y);
            }
        }

        // --- 以下のアクションは今回使用しないため、中身は空のまま ---
        public void OnPoint(InputAction.CallbackContext context) { }
        public void OnSecondaryPoint(InputAction.CallbackContext context) { }
        public void OnSecondaryContact(InputAction.CallbackContext context) { }

        #endregion
    }
}