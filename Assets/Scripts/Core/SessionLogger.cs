using System;
using UnityEngine;

namespace Assets.Scripts.Core
{
    /// <summary>
    /// デバッグ用: アプリ起動時のタイムスタンプを保持し、一定間隔で稼働時間を Unity コンソールに出力します。
    /// - 起動時に開始時刻を記録
    /// - スペック: 出力間隔は Inspector で設定可能（デフォルト 30 秒）
    /// - 他クラスから稼働時間を取得できる静的メソッド GetUptime() を提供
    /// </summary>
    public class SessionLogger : MonoBehaviour
    {
        [Tooltip("稼働時間ログを出力する間隔（秒）。0 以下にすると周期ログは無効になります。")]
        [SerializeField]
        float logIntervalSeconds = 30f;

        static DateTime sessionStartUtc;
        float timer = 0f;

        void Awake()
        {
            sessionStartUtc = DateTime.UtcNow;
            Debug.Log($"[SessionLogger] Session started (UTC): {sessionStartUtc:O}");
            timer = 0f;
        }

        void Update()
        {
            if (logIntervalSeconds <= 0f) return;

            timer += Time.deltaTime;
            if (timer >= logIntervalSeconds)
            {
                timer = 0f;
                TimeSpan uptime = GetUptime();
                // hh:mm:ss 形式で出力
                string hhmmss = string.Format("{0:D2}:{1:D2}:{2:D2}",
                    (int)uptime.TotalHours,
                    uptime.Minutes,
                    uptime.Seconds);
                Debug.Log($"[SessionLogger] Uptime: {hhmmss} (hh:mm:ss) — TotalSeconds={uptime.TotalSeconds:F1}");
            }
        }

        /// <summary>
        /// 起動からの稼働時間を返します。
        /// </summary>
        public static TimeSpan GetUptime()
        {
            return DateTime.UtcNow - sessionStartUtc;
        }
    }
}
