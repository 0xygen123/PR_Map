using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

using Assets.Scripts.Core;

namespace Assets.Scripts.Plane
{
    public class CameraController : MonoBehaviour, CameraControls.ICameraControlActions
    {
        [Header("Camera Settings")]
        [SerializeField, Tooltip("カメラの移動速度")]
        float moveSpeed = 0.005f;

        [SerializeField, Tooltip("マウスホイールでのズーム速度")]
        float scrollZoomSpeed = 0.5f;

        [SerializeField, Tooltip("ピンチ操作でのズーム速度")]
        float pinchZoomSpeed = 0.008f;

        [SerializeField, Tooltip("最小ズーム（Orthographic Size）")]
        float minZoom = 10f;

        [SerializeField, Tooltip("最大ズーム（Orthographic Size）")]
        float maxZoom = 300f;

        [Header("Map Boundaries (Optional)")]
        [SerializeField, Tooltip("カメラ移動範囲の最小座標")]
        Vector2 minBounds = new Vector2(-300, -300);

        [SerializeField, Tooltip("カメラ移動範囲の最大座標")]
        Vector2 maxBounds = new Vector2(300, 300);

        [Header("Target Object Settings")]
        [SerializeField, Tooltip("ユーザアイコンオブジェクト")]
        GameObject userObject;
        [SerializeField, Tooltip("オブジェクトの基本スケール")]
        float userBaseScale = 10.0f;
        [SerializeField, Tooltip("オブジェクトの最小スケール")]
        float minUserScale = 3f;
        [SerializeField, Tooltip("オブジェクトの最大スケール")]
        float maxUserScale = 10.0f;

        Camera mainCamera;
        CameraControls cameraControls;

        // --- 入力状態を保持する変数 ---
        Vector2 moveInput;
        bool isPrimaryContact = false;
        bool isPinching = false;
        float lastPinchDistance;

        // --- カメラの状態を管理する変数 ---
        [SerializeField] bool isFollowingUser = false; // ユーザー追従モードか
        bool isForceMoving = false;   // 強制移動中か

        void Awake()
        {
            mainCamera = GetComponent<Camera>();
            cameraControls = new CameraControls();
            cameraControls.CameraControl.SetCallbacks(this);

            JSInterface.OnUserFollow += CenterOnUserAndFollow;
        }

        void Start()
        {
            AdjustUserObjectScale();
        }

        void OnEnable()
        {
            cameraControls.CameraControl.Enable();
        }

        void OnDisable()
        {
            cameraControls.CameraControl.Disable();
        }

        void Update()
        {
            // 強制移動中は、ユーザーの操作を一切受け付けない
            if (isForceMoving) return;

            // ユーザーがカメラを操作したら、追従モードを解除する
            // if ((isPrimaryContact || isPinching) && isFollowingUser)
            if (isPrimaryContact && isFollowingUser)
            {
                isFollowingUser = false;
            }

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

        // オブジェクトの移動処理が全て終わった後でカメラを動かすためにLateUpdateを使う
        void LateUpdate()
        {
            // 追従モードが有効、かつ強制移動中でない場合
            if (isFollowingUser && !isForceMoving && userObject != null)
            {
                // カメラの位置をユーザアイコンの位置に合わせる（Z軸は維持）
                transform.position = new Vector3(userObject.transform.position.x, userObject.transform.position.y, transform.position.z);
                ClampCameraPosition(); // 追従後もマップ境界内に収める
            }
        }

        #region ICampusMapActions Interface Implementations
        public void OnMove(InputAction.CallbackContext context)
        {
            if (isForceMoving) return; // 強制移動中は無効
            moveInput = context.ReadValue<Vector2>();
        }

        public void OnPrimaryContact(InputAction.CallbackContext context)
        {
            if (isForceMoving) return; // 強制移動中は無効
            isPrimaryContact = context.ReadValueAsButton();
        }

        public void OnScroll(InputAction.CallbackContext context)
        {
            if (isForceMoving || isPinching) return; // 強制移動中、ピンチ操作中は無効

            float scrollValue = context.ReadValue<Vector2>().y;
            if (Mathf.Abs(scrollValue) > 0.1f)
            {
                // スクロール操作で追従を解除
                isFollowingUser = false;

                Zoom(Mathf.Sign(scrollValue) * -scrollZoomSpeed);
                ClampCameraPosition();
            }
        }

        public void OnSecondaryContact(InputAction.CallbackContext context)
        {
            if (isForceMoving) return; // 強制移動中は無効
            // if (context.started) isPinching = true;

            if (context.started)
            {
                isPinching = true;
                // ピンチ開始時の2点間の距離を記録する
                Vector2 pos1 = cameraControls.CameraControl.Point.ReadValue<Vector2>();
                Vector2 pos2 = cameraControls.CameraControl.SecondaryPoint.ReadValue<Vector2>();
                lastPinchDistance = Vector2.Distance(pos1, pos2);
            }
            else if (context.canceled)
            {
                isPinching = false;
            }
        }

        public void OnPoint(InputAction.CallbackContext context) { }
        public void OnSecondaryPoint(InputAction.CallbackContext context) { }
        #endregion

        #region Private Camera Control Methods
        void HandleMove()
        {
            transform.position -= new Vector3(moveInput.x, moveInput.y, 0) * moveSpeed * mainCamera.orthographicSize / 100;
            ClampCameraPosition();
        }

        void HandlePinchZoom()
        {
            // ピンチ操作で追従を解除
            if (isFollowingUser) isFollowingUser = false;

            Vector2 pos1 = cameraControls.CameraControl.Point.ReadValue<Vector2>();
            Vector2 pos2 = cameraControls.CameraControl.SecondaryPoint.ReadValue<Vector2>();

            // float previousDistance = Vector2.Distance(pos1 - moveInput, pos2 - moveInput);
            // float currentDistance = Vector2.Distance(pos1, pos2);

            // if (Mathf.Approximately(previousDistance, 0)) return;

            // float deltaDistance = currentDistance - previousDistance;
            // Zoom(deltaDistance * -pinchZoomSpeed);
            // ClampCameraPosition();
            // 現在の2点間の距離を計算
            float currentDistance = Vector2.Distance(pos1, pos2);

            // 前のフレームの距離との差分を計算
            float deltaDistance = currentDistance - lastPinchDistance;

            // 差分を使ってズーム処理
            Zoom(deltaDistance * -pinchZoomSpeed);
            ClampCameraPosition();

            // 次のフレームのために、現在の距離を保存しておく
            lastPinchDistance = currentDistance;
        }

        void Zoom(float delta)
        {
            mainCamera.orthographicSize = Mathf.Clamp(mainCamera.orthographicSize + delta, minZoom, maxZoom);
            AdjustUserObjectScale();
        }

        void ClampCameraPosition()
        {
            float camHeight = mainCamera.orthographicSize;
            float camWidth = mainCamera.orthographicSize * mainCamera.aspect; // アスペクト比を考慮

            float dynamicMinX = minBounds.x + camWidth;
            float dynamicMaxX = maxBounds.x - camWidth;
            float dynamicMinY = minBounds.y + camHeight;
            float dynamicMaxY = maxBounds.y - camHeight;

            Vector3 pos = transform.position;

            if (dynamicMinX > dynamicMaxX)
            {
                pos.x = (minBounds.x + maxBounds.x) / 2;
            }
            else
            {
                pos.x = Mathf.Clamp(pos.x, dynamicMinX, dynamicMaxX);
            }

            if (dynamicMinY > dynamicMaxY)
            {
                pos.y = (minBounds.y + maxBounds.y) / 2;
            }
            else
            {
                pos.y = Mathf.Clamp(pos.y, dynamicMinY, dynamicMaxY);
            }

            transform.position = pos;
        }
        #endregion

        #region User Object size Adjustment
        void AdjustUserObjectScale()
        {
            if (userObject != null)
            {
                float normalizedZoom = (mainCamera.orthographicSize - minZoom) / (maxZoom - minZoom);
                float newScale = userBaseScale * normalizedZoom;
                float clampedScale = Mathf.Lerp(minUserScale, maxUserScale, Mathf.Clamp01(normalizedZoom));
                userObject.transform.localScale = new Vector3(clampedScale, clampedScale, userObject.transform.localScale.z);
                // --- デバッグ用のログ出力 ---
                //     Debug.Log($"カメラ倍率: {mainCamera.orthographicSize}, " +
                //         $"計算スケール: {newScale}, " +
                //         $"最終スケール (クランプ後): {clampedScale}"
                //     );
            }
        }
        #endregion

        #region Public Control Methods

        /// <summary>
        /// カメラ操作のパラメータを設定する
        /// </summary>
        /// <param name="settings"></param>
        public void SetParameters(CameraSensitivityData settings)
        {
            moveSpeed = settings.moveSpeed2D;
            scrollZoomSpeed = settings.scrollZoomSpeed2D;
            pinchZoomSpeed = settings.pinchZoomSpeed2D;
        }

        /// <summary>
        /// JSの'現在地'ボタン押下時に呼び出されるメソッド
        /// </sumary>
        /// <param name="targetZoom"></param>
        public void OnUserFollow(string targetZoom)
        {
            if (userObject == null)
            {
                Debug.LogError("UserObject is not assigned or does not exist in the scene!");
                JSInterface.SendToJS(JSInterface.JSFunction.OnShowError, "予期しないエラーが発生しました (userObject is null.)");
                return;
            }
            if (!userObject.activeSelf)
            {
                Debug.Log("UserObject is not Enabled!");
                JSInterface.SendToJS(JSInterface.JSFunction.OnShowError, "位置情報が利用できません (can't use LocationAPI.)");
                return;
            }
            if (float.TryParse(targetZoom, out float zoomMultiple))
            {
                // 画面比率などを考慮できるようにtargetZoomを受け取れるが特に意味ないかも
                //float targetZoom = 70.0f;
                CenterOnUserAndFollow(zoomMultiple);
            }
            else
            {
                Debug.Log("OnUserFollow: Failed Try Parse");
            }
        }

        /// <summary>
        /// 現在地ボタンから呼び出すメソッド。
        /// ユーザーの位置にカメラを移動し、追従モードを開始します。
        /// </summary>
        /// <param name="targetZoom">移動完了後のズームレベル</param>
        public void CenterOnUserAndFollow(float targetZoom)
        {
            if (userObject == null)
            {
                Debug.LogError("UserObjectが設定されていません。");
                return;
            }

            if (!isForceMoving)
            {
                Vector3 targetPos = new Vector3(userObject.transform.position.x, userObject.transform.position.y, transform.position.z);
                // 以前のコルーチンの代わりに、新しいコルーチンを呼び出す
                StartCoroutine(MoveLikeGoogleEarthCoroutine(targetPos, targetZoom, true));
            }
        }

        /// <summary>
        /// ユーザー追従モードを外部から設定します。
        /// </summary>
        /// <param name="follow">追従させる場合はtrue</param>
        public void SetFollowUserMode(bool follow)
        {
            isFollowingUser = follow;
            // ここで追従モードのUI（ボタンのハイライトなど）を更新する処理を入れても良い
        }
        #endregion


        #region Coroutines for Smooth Movement
        /// <summary>
        /// Google Earthのように、ズームアウト・インをしながら目標地点へ移動するコルーチン
        /// </summary>
        /// <param name="targetPosition">目標座標</param>
        /// <param name="targetZoom">目標ズームレベル</param>
        /// <param name="followAfterMove">移動後に追従モードを開始するか</param>
        IEnumerator MoveLikeGoogleEarthCoroutine(Vector3 targetPosition, float targetZoom, bool followAfterMove)
        {
            isForceMoving = true;
            isFollowingUser = false;

            float startZoom = mainCamera.orthographicSize;
            Vector3 startPosition = transform.position;
            float journeyDistance = Vector3.Distance(startPosition, targetPosition);
            float timer = 0f;

            // --- 移動距離に応じて、最もズームアウトする際の高さを計算 ---
            // 移動距離の半分を基準に、大きすぎず小さすぎないように調整
            float peakZoom = Mathf.Clamp(journeyDistance * 0.5f, minZoom, maxZoom);
            // ただし、開始・終了時のズームよりは必ずズームアウトするようにする
            peakZoom = Mathf.Max(peakZoom, startZoom, targetZoom);

            // 移動にかける時間（距離が長いほど少し長くする）
            float duration = Mathf.Clamp(journeyDistance * 0.1f, 0.5f, 2.0f); // 0.5秒〜2.0秒の範囲

            while (timer < duration)
            {
                timer += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, timer / duration);

                // 位置を目的地まで滑らかに移動
                transform.position = Vector3.Lerp(startPosition, targetPosition, t);

                // ズームレベルを「山なり」に変化させる
                // 前半(t < 0.5) はスタートからピークへ、後半(t > 0.5)はピークからターゲットへ
                if (t < 0.5f)
                {
                    // 0から1の範囲に変換してLerp
                    mainCamera.orthographicSize = Mathf.Lerp(startZoom, peakZoom, t * 2f);
                }
                else
                {
                    // 0から1の範囲に変換してLerp
                    mainCamera.orthographicSize = Mathf.Lerp(peakZoom, targetZoom, (t - 0.5f) * 2f);
                }

                // 移動のたびに各種調整
                Zoom(0); // ユーザアイコンのスケール調整などを呼ぶ
                ClampCameraPosition();

                yield return null;
            }

            // 処理の最後に最終値をピッタリ合わせる
            transform.position = targetPosition;
            mainCamera.orthographicSize = targetZoom;
            Zoom(0);
            ClampCameraPosition();

            isForceMoving = false;
            if (followAfterMove)
            {
                isFollowingUser = true;
            }
        }
        #endregion
    }
}