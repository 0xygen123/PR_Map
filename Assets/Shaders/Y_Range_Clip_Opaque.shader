Shader "Custom/Y_Range_Clip_Opaque"
{
    // 床・その他用: 不透明 + Y座標クリッピング
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        
        // Y座標のクリッピング範囲
        _MinHeight ("Min Height (Y)", Float) = -10000
        _MaxHeight ("Max Height (Y)", Float) = 10000
    }
    SubShader
    {
        // AlphaTest（clip）を使うため、QueueをAlphaTestに設定
        Tags { "Queue"="AlphaTest" "RenderType"="TransparentCutout" }
        LOD 100
        Cull Back // 通常の片面描画

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
                float4 worldPos : TEXCOORD2;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            
            // C#から受け取る高さ
            float _MinHeight;
            float _MaxHeight;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o, o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // ピクセルのワールドY座標を取得
                float worldY = i.worldPos.y;

                // Y座標が指定された範囲の「外側」なら、ピクセルを破棄(clip)する
                if (worldY < _MinHeight || worldY > _MaxHeight)
                {
                    clip(-1);
                }
                
                // 範囲内なら通常通りテクスチャと色を描画
                fixed4 col = tex2D(_MainTex, i.uv) * _Color;
                
                // フォグを適用
                UNITY_APPLY_FOG(i.fogCoord, col);
                
                return col;
            }
            ENDCG
        }
    }
}
