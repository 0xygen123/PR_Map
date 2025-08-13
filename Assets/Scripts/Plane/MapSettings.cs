using UnityEngine;

namespace Assets.Scripts.Plane
{
    [CreateAssetMenu(menuName = "2Dマップ設定")]
    public class MapSettings : ScriptableObject
    {
        [Header("2DMap Settings")]
        [Tooltip("マップサイズ")]
        public Vector2 minBounds = new Vector2(-300, -300);
        public Vector2 maxBounds = new Vector2(300, 300);

        [Tooltip("Unity座標への変換設定")]
        public double centerLatitude = 34.964962019263758;   // 噴水の緯度 (GPS N)
        public double centerLongitude = 135.940185503739031; // 噴水の経度 (GPS E)
    }
}