Shader "Custom/Y_Range_Clip_Transparent"
{
    // 壁用: フレネル効果で角度によって透ける + Y座標クリッピング
    // ViewAngleTransparentShader.shadergraphの設定を再現
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        
        // フレネル効果のパラメータ
        _FresnelPower ("Fresnel Power", Range(0.1, 5.0)) = 3.0
        _FresnelScale ("Fresnel Scale", Range(0.0, 2.0)) = 1.0
        _MinAlpha ("Min Alpha (正面)", Range(0.0, 1.0)) = 0.2
        _MaxAlpha ("Max Alpha (側面)", Range(0.0, 1.0)) = 0.8
        
        // Y座標のクリッピング範囲
        _MinHeight ("Min Height (Y)", Float) = -10000
        _MaxHeight ("Max Height (Y)", Float) = 10000
    }
    SubShader
    {
        // Shader Graphと同じ設定: Transparent, 両面描画
        Tags 
        { 
            "Queue"="Transparent" 
            "RenderType"="Transparent" 
            "IgnoreProjector"="True" 
        }
        LOD 200
        
        // 透明度を有効にするためのブレンド設定
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        ZTest LEqual
        Cull Off // 両面描画

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float2 uv : TEXCOORD0;
                float fogFactor : TEXCOORD1;
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD2;
                float3 normalWS : TEXCOORD3;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4 _Color;
                half _FresnelPower;
                half _FresnelScale;
                half _MinAlpha;
                half _MaxAlpha;
                float _MinHeight;
                float _MaxHeight;
            CBUFFER_END

            Varyings vert (Attributes input)
            {
                Varyings output = (Varyings)0;
                
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS);
                
                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                output.normalWS = normalInput.normalWS;
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.fogFactor = ComputeFogFactor(vertexInput.positionCS.z);
                
                return output;
            }

            half4 frag (Varyings input) : SV_Target
            {
                // Y座標クリッピング処理
                float worldY = input.positionWS.y;
                if (worldY < _MinHeight || worldY > _MaxHeight)
                {
                    discard;
                }
                
                // フレネル効果の計算
                // カメラへの方向ベクトル
                float3 viewDirWS = normalize(GetCameraPositionWS() - input.positionWS);
                float3 normalWS = normalize(input.normalWS);
                
                // フレネル: 視線と法線の内積 (正面=1, 側面=0)
                float NdotV = saturate(dot(normalWS, viewDirWS));
                
                // フレネル項: 正面(NdotV=1)で最小、側面(NdotV=0)で最大
                float fresnel = pow(1.0 - NdotV, _FresnelPower) * _FresnelScale;
                fresnel = saturate(fresnel);
                
                // アルファ値を計算: 正面(_MinAlpha) → 側面(_MaxAlpha)
                half alpha = lerp(_MinAlpha, _MaxAlpha, fresnel);
                
                // テクスチャと色を適用
                half4 baseColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                half4 col = baseColor * _Color;
                col.a *= alpha;
                
                // フォグを適用
                col.rgb = MixFog(col.rgb, input.fogFactor);
                
                return col;
            }
            ENDHLSL
        }
    }
    
    // Built-in Render Pipeline用のフォールバック
    SubShader
    {
        Tags 
        { 
            "Queue"="Transparent" 
            "RenderType"="Transparent" 
            "IgnoreProjector"="True" 
        }
        LOD 100
        
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

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
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
                float3 worldPos : TEXCOORD2;
                float3 worldNormal : TEXCOORD3;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            half _FresnelPower;
            half _FresnelScale;
            half _MinAlpha;
            half _MaxAlpha;
            float _MinHeight;
            float _MaxHeight;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o, o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Y座標クリッピング処理
                if (i.worldPos.y < _MinHeight || i.worldPos.y > _MaxHeight)
                {
                    clip(-1);
                }
                
                // フレネル効果の計算
                float3 viewDir = normalize(_WorldSpaceCameraPos - i.worldPos);
                float3 normal = normalize(i.worldNormal);
                float NdotV = saturate(dot(normal, viewDir));
                float fresnel = pow(1.0 - NdotV, _FresnelPower) * _FresnelScale;
                fresnel = saturate(fresnel);
                
                // アルファ値を計算
                half alpha = lerp(_MinAlpha, _MaxAlpha, fresnel);
                
                // テクスチャと色を適用
                fixed4 col = tex2D(_MainTex, i.uv) * _Color;
                col.a *= alpha;
                
                // フォグを適用
                UNITY_APPLY_FOG(i.fogCoord, col);
                
                return col;
            }
            ENDCG
        }
    }
    
    FallBack "Universal Render Pipeline/Lit"
}
