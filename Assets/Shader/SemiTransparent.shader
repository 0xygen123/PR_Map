Shader "Custom/ViewAngleTransparent"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (1,1,1,1)
        _BaseMap ("Base Map", 2D) = "white" {}
        _Transparency ("Transparency", Range(0,1)) = 0.5
        _FadeSharpness ("Fade Sharpness", Float) = 4.0
        
        // アウトライン用プロパティ
        //_OutlineWidth ("Outline Width", Range(0, 0.1)) = 0.03
        //_OutlineColor ("Outline Color", Color) = (0, 0, 0, 1)
        
        // AO用プロパティ
        _AOMap ("AO Map", 2D) = "white" {}
        _AOStrength ("AO Strength", Range(0, 2)) = 1.0
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        LOD 200

        /*
        // アウトライン描画パス
        Pass
        {
            Name "Outline"
            Tags { "LightMode" = "SRPDefaultUnlit" }
            Cull Front
            ZWrite Off
            ZTest LEqual
            
            HLSLPROGRAM
            #pragma vertex vertOutline
            #pragma fragment fragOutline
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float _Transparency;
                float _FadeSharpness;
                float _OutlineWidth;
                float4 _OutlineColor;
                float _AOStrength;
            CBUFFER_END

            struct AttributesOutline
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct VaryingsOutline
            {
                float4 positionHCS : SV_POSITION;
            };

            VaryingsOutline vertOutline(AttributesOutline IN)
            {
                VaryingsOutline OUT;
                
                // 法線方向にオブジェクトを拡大してアウトラインを作成
                float3 positionOS = IN.positionOS.xyz + IN.normalOS * _OutlineWidth;
                OUT.positionHCS = TransformObjectToHClip(positionOS);
                
                return OUT;
            }

            half4 fragOutline(VaryingsOutline IN) : SV_Target
            {
                return _OutlineColor;
            }
            ENDHLSL
        }
        */

        Pass
        {
            Name "ForwardLit"
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Back
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float3 viewDirWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float _Transparency;
                float _FadeSharpness;
                float _OutlineWidth;
                float4 _OutlineColor;
                float _AOStrength;
            CBUFFER_END

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            TEXTURE2D(_AOMap); SAMPLER(sampler_AOMap);

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS);
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                float3 worldPos = TransformObjectToWorld(IN.positionOS);
                OUT.viewDirWS = normalize(_WorldSpaceCameraPos.xyz - worldPos);
                OUT.uv = IN.uv;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 baseCol = _BaseColor * SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv);
                
                // AO（アンビエントオクルージョン）を適用
                float ao = SAMPLE_TEXTURE2D(_AOMap, sampler_AOMap, IN.uv).r;
                ao = lerp(1.0, ao, _AOStrength);
                baseCol.rgb *= ao;
                
                float ndotv = dot(IN.normalWS, IN.viewDirWS);

                // 面が裏を向いている場合（内側から見る場合）は、完全に不透明にする
                if (ndotv < 0)
                {
                    baseCol.a = 1.0;
                }
                // 面が表を向いている場合（外側から見る場合）は、角度に応じて半透明にする
                else
                {
                    // powの入力から "1.0 -" を削除し、より直感的に
                    float fade = pow(saturate(ndotv), _FadeSharpness);
                    // lerpのAとBを逆にし、角度が垂直に近いほど_Transparencyに近づける
                    float alpha = lerp(_Transparency, 1.0, fade);
                    baseCol.a *= alpha;
                }
                
                return baseCol;
            }
            ENDHLSL
        }
    }
    FallBack "Universal Forward"
}