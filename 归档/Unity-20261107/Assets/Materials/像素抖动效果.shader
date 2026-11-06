Shader "Custom/DitheringScaleControl"
{
    Properties
    {
        _MainTex ("Main Texture", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (1, 1, 1, 1)
        
        // 抖动贴图控制
        _DitherPattern ("Dither Pattern", 2D) = "white" {}
        _DitherPixelSize ("Dither Pixel Size", Range(1, 200)) = 20
        _DitherIntensity ("Dither Intensity", Range(0, 1)) = 0.8
        _DitherContrast ("Dither Contrast", Range(0.1, 5)) = 2.0
        
        // 缩放动画控制
        _ScaleAnimSpeed ("Scale Animation Speed", Range(0, 10)) = 1.0
        _ScaleRange ("Scale Range", Range(0.1, 5)) = 2.0
        _ScaleOffset ("Scale Offset", Range(0.5, 3)) = 1.0
        
        // 高级控制
        _ScreenSpaceMapping ("Screen Space Mapping", Range(0, 1)) = 1
        _PatternRotation ("Pattern Rotation", Range(0, 360)) = 0
        _PatternOffset ("Pattern Offset", Vector) = (0, 0, 0, 0)
        
        // Frank方法相关
        _FrankIntensity ("Frank Method Intensity", Range(0, 2)) = 1.0
        _FrankThreshold ("Frank Method Threshold", Range(0, 1)) = 0.5
        
        // 像素精确控制
        _ExactPixelControl ("Exact Pixel Control", Range(0, 1)) = 1
        _PixelPerfectScale ("Pixel Perfect Scale", Range(0.1, 10)) = 1.0
    }
    
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 100

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float2 screenPos : TEXCOORD1;
                float4 screenPosNorm : TEXCOORD2;
            };

            // 纹理和采样器
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_DitherPattern);
            SAMPLER(sampler_DitherPattern);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _DitherPattern_ST;
                float4 _BaseColor;
                float4 _PatternOffset;
                float _DitherPixelSize;
                float _DitherIntensity;
                float _DitherContrast;
                float _ScaleAnimSpeed;
                float _ScaleRange;
                float _ScaleOffset;
                float _ScreenSpaceMapping;
                float _PatternRotation;
                float _FrankIntensity;
                float _FrankThreshold;
                float _ExactPixelControl;
                float _PixelPerfectScale;
            CBUFFER_END

            // UV旋转函数
            float2 RotateUV(float2 uv, float rotation)
            {
                float rad = radians(rotation);
                float cosAngle = cos(rad);
                float sinAngle = sin(rad);
                
                uv -= 0.5;
                float2 rotatedUV;
                rotatedUV.x = uv.x * cosAngle - uv.y * sinAngle;
                rotatedUV.y = uv.x * sinAngle + uv.y * cosAngle;
                return rotatedUV + 0.5;
            }

            // Frank方法 - 重要采样处理
            float FrankMethod(float2 uv, float ditherValue, float threshold)
            {
                // 基于Frank方法的抖动处理
                float frankValue = step(threshold, ditherValue);
                
                // 添加基于UV位置的变化
                float2 frankUV = uv * 10.0;
                float frankNoise = frac(sin(dot(frankUV, float2(12.9898, 78.233))) * 43758.5453);
                
                // 结合阈值和噪声
                frankValue = lerp(frankValue, frankNoise, _FrankIntensity * 0.3);
                
                return saturate(frankValue);
            }

            // 精确像素控制函数
            float2 PixelPerfectUV(float2 screenPos, float pixelSize)
            {
                if (_ExactPixelControl > 0.5)
                {
                    // 像素完美对齐
                    float2 pixelPos = floor(screenPos / pixelSize) * pixelSize;
                    return pixelPos / _ScreenParams.xy;
                }
                else
                {
                    // 标准缩放
                    return screenPos / _ScreenParams.xy;
                }
            }

            Varyings vert (Attributes input)
            {
                Varyings output;
                
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                
                // 计算屏幕空间坐标
                output.screenPosNorm = ComputeScreenPos(output.positionHCS);
                output.screenPos = output.screenPosNorm.xy / output.screenPosNorm.w;
                
                return output;
            }

            half4 frag (Varyings input) : SV_Target
            {
                // 采样主纹理
                half4 mainColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                mainColor *= _BaseColor;
                
                // 计算动态缩放
                float timeScale = sin(_Time.y * _ScaleAnimSpeed) * _ScaleRange + _ScaleOffset;
                float currentPixelSize = _DitherPixelSize / timeScale;
                
                // 应用像素完美缩放
                currentPixelSize *= _PixelPerfectScale;
                
                // 计算抖动图案的UV坐标
                float2 ditherUV;
                
                if (_ScreenSpaceMapping > 0.5)
                {
                    // 屏幕空间映射 - 满屏效果
                    float2 screenPixelPos = input.screenPos * _ScreenParams.xy;
                    ditherUV = PixelPerfectUV(screenPixelPos, currentPixelSize);
                    ditherUV = ditherUV * (_ScreenParams.xy / currentPixelSize);
                }
                else
                {
                    // 对象空间映射
                    ditherUV = input.uv * (_ScreenParams.xy / currentPixelSize);
                }
                
                // 应用图案偏移
                ditherUV += _PatternOffset.xy;
                
                // 应用旋转
                if (_PatternRotation > 0.1)
                {
                    ditherUV = RotateUV(frac(ditherUV), _PatternRotation);
                }
                
                // 采样抖动图案
                half4 ditherSample = SAMPLE_TEXTURE2D(_DitherPattern, sampler_DitherPattern, frac(ditherUV));
                
                // 增强对比度
                float ditherValue = pow(abs(ditherSample.r), _DitherContrast);
                
                // 应用Frank方法处理
                ditherValue = FrankMethod(input.uv, ditherValue, _FrankThreshold);
                
                // 将抖动效果应用到最终颜色
                float3 finalColor = lerp(mainColor.rgb * 0.3, mainColor.rgb, ditherValue);
                finalColor = lerp(mainColor.rgb, finalColor, _DitherIntensity);
                
                // 确保颜色在合理范围内
                finalColor = saturate(finalColor);
                
                return half4(finalColor, mainColor.a);
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}