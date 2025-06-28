Shader "Universal Render Pipeline/ToonLitWithOutline"
{
    Properties
    {
        _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        _Cutoff("Shadow Threshold", Range(0, 1)) = 0.5
        _OutlineColor("Outline Color", Color) = (0, 0, 0, 1)
        _OutlineThickness("Outline Thickness", Range(0.001, 0.05)) = 0.02
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" }

        // 🔲 Pass 1：描边
        Pass
        {
            Name "Outline"
            Tags { "LightMode" = "SRPDefaultUnlit" }

            Cull Front

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
            };

            float _OutlineThickness;

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                float3 normOS = normalize(IN.normalOS);
                float3 pos = IN.positionOS.xyz + normOS * _OutlineThickness;
                float3 posWS = TransformObjectToWorld(pos);
                OUT.positionHCS = TransformWorldToHClip(posWS);
                return OUT;
            }

            half4 _OutlineColor;

            half4 frag(Varyings IN) : SV_Target
            {
                return _OutlineColor;
            }
            ENDHLSL
        }

        // 🟦 Pass 2：正常 Toon 渲染
        Pass
        {
            Name "ToonLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 normalWS    : TEXCOORD0;
                float3 posWS       : TEXCOORD1;
            };

            float4 _BaseColor;
            float _Cutoff;

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                float3 posWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.positionHCS = TransformWorldToHClip(posWS);
                OUT.posWS = posWS;
                OUT.normalWS = normalize(TransformObjectToWorldNormal(IN.normalOS));
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                Light light = GetMainLight();
                float NdotL = saturate(dot(IN.normalWS, normalize(light.direction)));
                float shadowMask = step(_Cutoff, NdotL);
                float3 color = _BaseColor.rgb * shadowMask;
                return float4(color, 1.0);
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
