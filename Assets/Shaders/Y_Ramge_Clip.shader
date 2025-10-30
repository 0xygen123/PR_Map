Shader "Custom/Y_Range_Clip"
{
    // C#スクリプトから制御するプロパティ
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
        // これにより、半透明オブジェクトより手前に描画され、
        // 不透明オブジェクト（"Geometry"）の後に描画される
        Tags { "Queue"="AlphaTest" "RenderType"="TransparentCutout" }
        LOD 100
        Cull Off // メッシュの裏側も描画する（建物内部から見た場合など）

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float4 worldPos : TEXCOORD1; // ワールド座標を渡す
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
                // 頂点のワールド座標を計算
                o.worldPos = mul(unity_ObjectToWorld, v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
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
                return col;
            }
            ENDCG
        }
    }
}