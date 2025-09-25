using UnityEngine;
using UnityEngine.InputSystem;

namespace Assets.Scripts.Solid
{
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
        [Tooltip("ピンチ操作でのズーム感度")]
        [SerializeField] float pinchZoomSensitivity = 0.05f;
        [Tooltip("ズームできるOrthographic Sizeの範囲（最小, 最大）")]
        [SerializeField] Vector2 orthographicSizeMinMax = new Vector2(3f, 50f);

        CameraControls cameraControls;
        Camera mainCamera;
        Vector2 rotationInput;

        // --- 状態を管理する変数 ---
        bool isRotating = false;
        bool isPinching = false;
        Vector2 touchPosition1;
        Vector2 touchPosition2;
        float previousPinchDistance;

        float yaw = 0.0f;
        float pitch = 20.0f;

        void Awake()
        {
            mainCamera = GetComponent<Camera>();
            mainCamera.orthographic = true;
            mainCamera.orthographicSize = initialOrthographicSize;

            cameraControls = new CameraControls();
            cameraControls.CameraControl.SetCallbacks(this);
        }

        void OnEnable()
        {
            cameraControls.CameraControl.Enable();
        }

        void OnDisable()
        {
            cameraControls.CameraControl.Disable();
        }

        void LateUpdate()
        {
            if (target == null)
            {
                Debug.LogWarning("ターゲットが設定されていません。");
                return;
            }

            // 回転処理：isRotatingがtrueで、かつピンチ中でない時に実行
            if (isRotating && !isPinching)
            {
                yaw += rotationInput.x * rotationSpeed * Time.deltaTime;
                pitch -= rotationInput.y * rotationSpeed * Time.deltaTime;
                pitch = Mathf.Clamp(pitch, pitchMinMax.x, pitchMinMax.y);
            }

            // ピンチズーム処理：isPinchingがtrueの時に実行
            if (isPinching)
            {
                HandlePinchZoom();
            }

            // カメラの位置と向きを更新
            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0);
            Vector3 targetOffset = new Vector3(0, 0, -rotationRadius);
            transform.position = target.position + rotation * targetOffset;
            transform.LookAt(target.position);
        }

        /// <summary>
        /// カメラ操作のパラメータを設定する
        /// </summary>
        /// <param name="settings"></param>
        public void SetParameters(CameraSensitivityData settings)
        {
            rotationSpeed = settings.rotationSpeed;
            zoomSpeed = settings.zoomSpeed;
            pinchZoomSensitivity = settings.pinchZoomSensitivity;
        }

        /// <summary>
        /// マウスホイールによるズーム処理
        /// </summary>
        void Zoom(float scrollValue)
        {
            float newSize = mainCamera.orthographicSize - scrollValue; // Time.deltaTimeはOnScrollでは不要な場合が多い
            mainCamera.orthographicSize = Mathf.Clamp(newSize, orthographicSizeMinMax.x, orthographicSizeMinMax.y);
        }

        /// <summary>
        /// ピンチ操作によるズーム処理
        /// </summary>
        void HandlePinchZoom()
        {
            // 2つのタッチ座標間の現在の距離を計算
            float currentDistance = Vector2.Distance(touchPosition1, touchPosition2);

            // ピンチ開始フレームでは previousPinchDistance が 0 なので何もしない
            // 2フレーム目以降、距離の変化を計算する
            if (previousPinchDistance > 0)
            {
                float deltaDistance = currentDistance - previousPinchDistance;
                float newSize = mainCamera.orthographicSize - deltaDistance * pinchZoomSensitivity;
                mainCamera.orthographicSize = Mathf.Clamp(newSize, orthographicSizeMinMax.x, orthographicSizeMinMax.y);
            }

            // 現在の距離を次のフレームのために保存
            previousPinchDistance = currentDistance;
        }


        #region ICameraControlActions Interface Implementations

        public void OnMove(InputAction.CallbackContext context)
        {
            rotationInput = context.ReadValue<Vector2>();
        }

        public void OnPrimaryContact(InputAction.CallbackContext context)
        {
            // 2本目の指が触れていない（ピンチ中でない）場合のみ回転を開始/終了
            if (context.started && !isPinching)
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
            // ピンチ操作中はマウスホイールでのズームを無効にする
            if (isPinching) return;

            if (context.performed)
            {
                // Y軸のスクロール値に速度を掛けてZoom関数に渡す
                Zoom(context.ReadValue<Vector2>().y * zoomSpeed * Time.deltaTime);
            }
        }

        // 1本目の指の座標を更新し続ける
        public void OnPoint(InputAction.CallbackContext context)
        {
            touchPosition1 = context.ReadValue<Vector2>();
        }

        // 2本目の指の座標を更新し続ける
        public void OnSecondaryPoint(InputAction.CallbackContext context)
        {
            touchPosition2 = context.ReadValue<Vector2>();
        }

        // 2本目の指のタッチ開始/終了を検知する
        public void OnSecondaryContact(InputAction.CallbackContext context)
        {
            if (context.started)
            {
                isPinching = true;
                isRotating = false; // ピンチ開始と同時に回転は停止する
            }
            else if (context.canceled)
            {
                isPinching = false;
                // ピンチ終了時にリセット
                previousPinchDistance = 0f;
            }
        }

        #endregion
    }
}