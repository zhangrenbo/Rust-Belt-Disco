Shader "Universal Render Pipeline/ToonLitWithEnhancedOutline"
{
    Properties
    {
        // 主纹理
        _MainTex("Main Texture", 2D) = "white" {}
        _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        _TextureRotation("Texture Rotation", Range(0, 360)) = 0
        _Cutoff("Shadow Threshold", Range(0, 1)) = 0.5
        
        // 渐变阴影属性
        _ShadowColor("Shadow Color", Color) = (0.15, 0.15, 0.15, 1)
        _ShadowDistance("Shadow Distance", Range(0.1, 5)) = 1.0
        _ShadowFalloff("Shadow Falloff", Range(0.1, 3)) = 1.5
        _ShadowIntensity("Shadow Intensity", Range(0, 1)) = 0.9
        _ShadowSoftness("Shadow Edge Softness", Range(0, 0.2)) = 0.05
        _ShadowDarkening("Shadow Darkening", Range(1, 3)) = 1.5
        
        // 阴影边缘线条
        _ShadowEdgeLineColor("Shadow Edge Line Color", Color) = (0.1, 0.1, 0.1, 1)
        _ShadowEdgeLineWidth("Shadow Edge Line Width", Range(0, 0.1)) = 0.02
        _ShadowEdgeLineIntensity("Shadow Edge Line Intensity", Range(0, 1)) = 0.8
        
        // 阴影半色调抖动效果
        _ShadowHalftoneSize("Shadow Halftone Base Size", Range(1, 100)) = 20
        _ShadowHalftoneSpacing("Shadow Halftone Spacing", Range(0.1, 5.0)) = 1.0
        _ShadowHalftoneIntensity("Shadow Halftone Intensity", Range(0, 1)) = 0.8
        _ShadowHalftoneThreshold("Shadow Halftone Threshold", Range(0, 1)) = 0.3
        _ShadowHalftoneAnimSpeed("Shadow Halftone Animation Speed", Range(0, 5)) = 1.0
        _ShadowHalftoneNoiseScale("Shadow Halftone Noise Scale", Range(0.1, 10)) = 1.0
        
        // 密度控制参数
        _DitherDensityMap("Dither Density Map", 2D) = "white" {}
        _DensityScale("Density Scale", Range(0.1, 10)) = 1.0
        _DensityContrast("Density Contrast", Range(0.1, 5)) = 2.0
        _MinDensity("Minimum Density", Range(0.1, 1)) = 0.3
        _MaxDensity("Maximum Density", Range(1, 5)) = 3.0
        _DensityBasedOnLighting("Density Based On Lighting", Range(0, 1)) = 1.0
        
        // 高光区域
        _HighlightColor("Highlight Color", Color) = (1.2, 1.2, 1.2, 1)
        _HighlightThreshold("Highlight Threshold", Range(0.5, 1)) = 0.8
        _HighlightSoftness("Highlight Softness", Range(0, 0.2)) = 0.05
        
        // 明暗交界线增强
        _TerminatorIntensity("Terminator Line Intensity", Range(0, 2)) = 1.0
        _TerminatorWidth("Terminator Line Width", Range(0.01, 0.2)) = 0.05
        _TerminatorDensityBoost("Terminator Density Boost", Range(1, 5)) = 2.0
        
        // 反射光效果
        _RimLightColor("Rim Light Color", Color) = (0.8, 0.8, 1.0, 1)
        _RimLightPower("Rim Light Power", Range(0.1, 8)) = 2.0
        _RimLightIntensity("Rim Light Intensity", Range(0, 2)) = 0.5
        _RimHalftoneDensity("Rim Halftone Density", Range(0.1, 2)) = 0.6
        
        // 增强描边属性
        _OutlineColor("Outline Base Color", Color) = (0.2, 0.2, 0.2, 1)
        _OutlineThickness("Outline Thickness", Range(0.001, 0.05)) = 0.02
        _OutlineLightInfluence("Light Influence on Outline", Range(0, 1)) = 0.3
        _OutlineShadowColor("Outline Shadow Color", Color) = (0.05, 0.05, 0.05, 1)
        _OutlineNoiseScale("Outline Noise Scale", Range(1, 100)) = 50
        _OutlineNoiseIntensity("Outline Noise Intensity", Range(0, 0.1)) = 0.02
        
        // 法线贴图
        _NormalMap("Normal Map", 2D) = "bump" {}
        _NormalStrength("Normal Strength", Range(0, 2)) = 1.0
        
        // 细节贴图
        _DetailTex("Detail Texture", 2D) = "gray" {}
        _DetailScale("Detail Scale", Range(0.1, 10)) = 1.0
        _DetailIntensity("Detail Intensity", Range(0, 1)) = 0.3
        
        // 高光贴图
        _SpecularMap("Specular Map", 2D) = "white" {}
        _SpecularColor("Specular Color", Color) = (1, 1, 1, 1)
        _SpecularPower("Specular Power", Range(1, 100)) = 20
        _SpecularIntensity("Specular Intensity", Range(0, 2)) = 1.0
        
        // 纸张纹理效果
        _PaperTexture("Paper Texture", 2D) = "white" {}
        _PaperTextureScale("Paper Texture Scale", Range(0.1, 10)) = 1
        _PaperTextureIntensity("Paper Texture Intensity", Range(0, 1)) = 0.3
        _PaperTextureContrast("Paper Texture Contrast", Range(0.5, 2)) = 1.2
        
        // 透明度控制
        _AlphaClip("Alpha Clipping", Range(0, 1)) = 0.5
        _AlphaToOpaque("Alpha To Opaque", Range(0, 1)) = 0
        [Toggle] _UseAlphaClip("Use Alpha Clipping", Float) = 0
        [Toggle] _DebugAlpha("Debug Alpha Channel", Float) = 0
        [Toggle(_ALPHATEST_ON)] _AlphaTest("Alpha Test", Float) = 1.0
    }
    
    SubShader
    {
        Tags { 
            "RenderType" = "TransparentCutout" 
            "Queue" = "AlphaTest+1" 
            "RenderPipeline" = "UniversalPipeline"
            "DisableBatching" = "True"
            "IgnoreProjector" = "True"
        }
        
        // Pass 1：增强描边
        Pass
        {
            Name "EnhancedOutline"
            Tags { "LightMode" = "SRPDefaultUnlit" }
            Cull Front
            ZWrite On
            ZTest LEqual
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #pragma shader_feature_local _ALPHATEST_ON
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };
            
            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 normalWS    : TEXCOORD0;
                float3 posWS       : TEXCOORD1;
                float2 uv          : TEXCOORD2;
                float2 screenUV    : TEXCOORD3;
            };
            
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_PaperTexture);
            SAMPLER(sampler_PaperTexture);
            
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _OutlineColor;
                float4 _OutlineShadowColor;
                float _OutlineThickness;
                float _OutlineLightInfluence;
                float _PaperTextureScale;
                float _PaperTextureIntensity;
                float _PaperTextureContrast;
                float _OutlineNoiseScale;
                float _OutlineNoiseIntensity;
                float _AlphaClip;
                float _UseAlphaClip;
            CBUFFER_END
            
            float noise(float2 uv)
            {
                return frac(sin(dot(uv, float2(12.9898, 78.233))) * 43758.5453);
            }
            
            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                
                float3 normOS = normalize(IN.normalOS);
                float noiseValue = noise(OUT.uv * _OutlineNoiseScale) * 2.0 - 1.0;
                normOS += normalize(IN.normalOS) * noiseValue * _OutlineNoiseIntensity;
                
                float3 pos = IN.positionOS.xyz + normOS * _OutlineThickness;
                float3 posWS = TransformObjectToWorld(pos);
                OUT.positionHCS = TransformWorldToHClip(posWS);
                OUT.posWS = posWS;
                OUT.normalWS = normalize(TransformObjectToWorldNormal(IN.normalOS));
                
                OUT.screenUV = OUT.positionHCS.xy / OUT.positionHCS.w * 0.5 + 0.5;
                
                return OUT;
            }
            
            half4 frag(Varyings IN) : SV_Target
            {
                float4 mainTexSample = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                
                float alphaValue = mainTexSample.a;
                
                #ifdef _ALPHATEST_ON
                    clip(alphaValue - _AlphaClip);
                #endif
                
                if(_UseAlphaClip > 0.5)
                {
                    clip(alphaValue - _AlphaClip);
                    if(alphaValue < _AlphaClip) {
                        discard;
                    }
                }
                
                Light light = GetMainLight();
                float3 lightDir = normalize(light.direction);
                
                float NdotL = saturate(dot(IN.normalWS, lightDir));
                
                float lightInfluence = lerp(1.0, NdotL, _OutlineLightInfluence);
                
                float3 outlineColor = lerp(_OutlineShadowColor.rgb, _OutlineColor.rgb, lightInfluence);
                
                float2 paperUV = IN.screenUV * _PaperTextureScale;
                float4 paperSample = SAMPLE_TEXTURE2D(_PaperTexture, sampler_PaperTexture, paperUV);
                
                // 修正：使用abs和max确保值为正
                float paperTexture = pow(max(paperSample.r, 0.001), max(_PaperTextureContrast, 0.001));
                
                outlineColor = lerp(outlineColor, outlineColor * paperTexture, _PaperTextureIntensity);
                
                float randomVariation = noise(IN.uv * 50.0) * 0.1 - 0.05;
                outlineColor += randomVariation;
                
                outlineColor = saturate(outlineColor);
                
                return float4(outlineColor, mainTexSample.a);
            }
            ENDHLSL
        }
        
        // Pass 2：正常 Toon 渲染
        Pass
        {
            Name "ToonLit"
            Tags { "LightMode" = "UniversalForward" }
            
            ZWrite On
            ZTest LEqual
            Cull Back
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            
            #pragma shader_feature_local _ALPHATEST_ON
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 tangentOS  : TANGENT;
                float2 uv         : TEXCOORD0;
            };
            
            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 normalWS    : TEXCOORD0;
                float3 posWS       : TEXCOORD1;
                float4 shadowCoord : TEXCOORD2;
                float2 uv          : TEXCOORD3;
                float3 tangentWS   : TEXCOORD4;
                float3 bitangentWS : TEXCOORD5;
                float3 viewDirWS   : TEXCOORD6;
            };
            
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_NormalMap);
            SAMPLER(sampler_NormalMap);
            TEXTURE2D(_DetailTex);
            SAMPLER(sampler_DetailTex);
            TEXTURE2D(_SpecularMap);
            SAMPLER(sampler_SpecularMap);
            TEXTURE2D(_DitherDensityMap);
            SAMPLER(sampler_DitherDensityMap);
            
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _DitherDensityMap_ST;
                float4 _BaseColor;
                float4 _ShadowColor;
                float4 _ShadowEdgeLineColor;
                float4 _SpecularColor;
                float4 _HighlightColor;
                float4 _RimLightColor;
                float _TextureRotation;
                float _Cutoff;
                float _ShadowDistance;
                float _ShadowFalloff;
                float _ShadowIntensity;
                float _ShadowSoftness;
                float _ShadowDarkening;
                float _ShadowEdgeLineWidth;
                float _ShadowEdgeLineIntensity;
                float _ShadowHalftoneSize;
                float _ShadowHalftoneSpacing;
                float _ShadowHalftoneIntensity;
                float _ShadowHalftoneThreshold;
                float _ShadowHalftoneAnimSpeed;
                float _ShadowHalftoneNoiseScale;
                float _DensityScale;
                float _DensityContrast;
                float _MinDensity;
                float _MaxDensity;
                float _DensityBasedOnLighting;
                float _HighlightThreshold;
                float _HighlightSoftness;
                float _TerminatorIntensity;
                float _TerminatorWidth;
                float _TerminatorDensityBoost;
                float _RimLightPower;
                float _RimLightIntensity;
                float _RimHalftoneDensity;
                float _NormalStrength;
                float _DetailScale;
                float _DetailIntensity;
                float _SpecularPower;
                float _SpecularIntensity;
                float _AlphaClip;
                float _AlphaToOpaque;
                float _UseAlphaClip;
                float _DebugAlpha;
            CBUFFER_END
            
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
            
            float CalculateDistanceBasedShadow(float3 worldPos, float4 shadowCoord, float shadowAttenuation)
            {
                float shadowFactor = shadowAttenuation;
                float2 shadowUV = shadowCoord.xy;
                float centerDistance = length(shadowUV - 0.5) * 2.0;
                float distanceFactor = saturate(1.0 - centerDistance / _ShadowDistance);
                // 修正：确保falloff值为正
                distanceFactor = pow(max(distanceFactor, 0.001), max(_ShadowFalloff, 0.001));
                float finalShadow = lerp(distanceFactor * 0.3, 1.0, shadowFactor);
                return finalShadow;
            }
            
            float noise2D(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                
                float a = frac(sin(dot(i, float2(12.9898, 78.233))) * 43758.5453);
                float b = frac(sin(dot(i + float2(1.0, 0.0), float2(12.9898, 78.233))) * 43758.5453);
                float c = frac(sin(dot(i + float2(0.0, 1.0), float2(12.9898, 78.233))) * 43758.5453);
                float d = frac(sin(dot(i + float2(1.0, 1.0), float2(12.9898, 78.233))) * 43758.5453);
                
                float2 u = f * f * (3.0 - 2.0 * f);
                
                return lerp(a, b, u.x) + (c - a) * u.y * (1.0 - u.x) + (d - b) * u.x * u.y;
            }
            
            float CalculateDensityBasedHalftone(float2 screenPos, float2 uv, float shadowIntensity, float densityMultiplier, float rimInfluence, float lightingValue)
            {
                float2 densityUV = TRANSFORM_TEX(uv, _DitherDensityMap) * _DensityScale;
                float4 densityMapSample = SAMPLE_TEXTURE2D(_DitherDensityMap, sampler_DitherDensityMap, densityUV);
                // 修正：确保参数为正
                float densityFromMap = pow(max(densityMapSample.r, 0.001), max(_DensityContrast, 0.001));
                
                float lightingBasedDensity = lerp(1.0, 1.0 - lightingValue, _DensityBasedOnLighting);
                
                float finalDensity = densityFromMap * lightingBasedDensity * densityMultiplier;
                finalDensity = lerp(_MinDensity, _MaxDensity, finalDensity);
                
                finalDensity *= lerp(1.0, _RimHalftoneDensity, rimInfluence);
                
                float halftoneResult = 0.0;
                float totalWeight = 0.0;
                
                for(int layer = 0; layer < 3; layer++)
                {
                    float layerDensity = finalDensity * (1.0 + layer * 0.7);
                    // 修正：防止除零错误
                    float layerSize = _ShadowHalftoneSize / max(layerDensity, 0.001);
                    
                    float2 gridPos = screenPos / max(layerSize, 0.001) * _ShadowHalftoneSpacing;
                    float2 gridCell = floor(gridPos);
                    float2 gridUV = frac(gridPos);
                    
                    float timeOffset = _Time.y * _ShadowHalftoneAnimSpeed + layer * 123.456;
                    float2 noiseOffset = float2(
                        noise2D(gridCell * _ShadowHalftoneNoiseScale + timeOffset),
                        noise2D(gridCell * _ShadowHalftoneNoiseScale + timeOffset + 100.0 + layer * 50.0)
                    ) * 0.2 - 0.1;
                    
                    gridUV += noiseOffset;
                    
                    float2 centerOffset = gridUV - 0.5;
                    float distToCenter = length(centerOffset);
                    
                    float dotRadius = 0.35 + _ShadowHalftoneThreshold * 0.2;
                    
                    float dotMask = 1.0 - smoothstep(dotRadius - 0.1, dotRadius + 0.1, distToCenter);
                    
                    float randomFactor = noise2D(gridCell + timeOffset) * 0.15;
                    dotMask *= (1.0 + randomFactor);
                    dotMask = saturate(dotMask);
                    
                    float layerWeight = saturate(shadowIntensity * 3.0 - layer);
                    
                    halftoneResult += dotMask * layerWeight;
                    totalWeight += layerWeight;
                }
                
                if(totalWeight > 0.001)
                {
                    halftoneResult /= totalWeight;
                }
                
                return saturate(halftoneResult);
            }
            
            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs normalInput = GetVertexNormalInputs(IN.normalOS, IN.tangentOS);
                
                OUT.positionHCS = vertexInput.positionCS;
                OUT.posWS = vertexInput.positionWS;
                OUT.normalWS = normalInput.normalWS;
                OUT.tangentWS = normalInput.tangentWS;
                OUT.bitangentWS = normalInput.bitangentWS;
                OUT.viewDirWS = normalize(_WorldSpaceCameraPos - vertexInput.positionWS);
                
                OUT.shadowCoord = GetShadowCoord(vertexInput);
                
                float2 baseUV = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.uv = RotateUV(baseUV, _TextureRotation);
                
                return OUT;
            }
            
            half4 frag(Varyings IN) : SV_Target
            {
                float4 mainTexColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                
                float testAlpha = mainTexColor.a;
                
                clip(testAlpha - _AlphaClip);
                
                if(testAlpha < _AlphaClip)
                {
                    discard;
                }
                
                float4 normalSample = SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, IN.uv);
                float3 normalTS = UnpackNormalScale(normalSample, _NormalStrength);
                
                float3x3 TBN = float3x3(IN.tangentWS, IN.bitangentWS, IN.normalWS);
                float3 normalWS = normalize(mul(normalTS, TBN));
                
                float2 detailUV = IN.uv * _DetailScale;
                float4 detailSample = SAMPLE_TEXTURE2D(_DetailTex, sampler_DetailTex, detailUV);
                
                float4 specularSample = SAMPLE_TEXTURE2D(_SpecularMap, sampler_SpecularMap, IN.uv);
                
                float4 baseColor = mainTexColor * _BaseColor;
                baseColor.rgb = lerp(baseColor.rgb, baseColor.rgb * detailSample.rgb, _DetailIntensity);
                
                float4 shadowCoord = TransformWorldToShadowCoord(IN.posWS);
                Light light = GetMainLight(shadowCoord);
                
                float NdotL = saturate(dot(normalWS, normalize(light.direction)));
                
                float NdotV = saturate(dot(normalWS, IN.viewDirWS));
                
                float rimLight = 1.0 - NdotV;
                // 修正：确保power值为正
                rimLight = pow(max(rimLight, 0.001), max(_RimLightPower, 0.001));
                rimLight *= _RimLightIntensity;
                
                float distanceShadow = CalculateDistanceBasedShadow(IN.posWS, shadowCoord, light.shadowAttenuation);
                
                float combinedLighting = NdotL * distanceShadow;
                
                float shadowMask = smoothstep(_Cutoff - _ShadowSoftness, _Cutoff + _ShadowSoftness, combinedLighting);
                
                float highlightMask = smoothstep(_HighlightThreshold - _HighlightSoftness, _HighlightThreshold + _HighlightSoftness, combinedLighting);
                
                float terminator = abs(combinedLighting - _Cutoff);
                float terminatorMask = 1.0 - smoothstep(0, _TerminatorWidth, terminator);
                float densityMultiplier = lerp(1.0, _TerminatorDensityBoost, terminatorMask * _TerminatorIntensity);
                
                float2 screenPos = IN.positionHCS.xy;
                
                float shadowStrength = 1.0 - shadowMask;
                float halftoneDither = CalculateDensityBasedHalftone(screenPos, IN.uv, shadowStrength, densityMultiplier, rimLight, combinedLighting);
                
                float finalShadowMask = shadowMask;
                if (shadowStrength > 0.1)
                {
                    float halftoneBlend = halftoneDither * _ShadowHalftoneIntensity * shadowStrength;
                    finalShadowMask = lerp(shadowMask * 0.3, shadowMask, halftoneBlend);
                }
                
                float shadowEdge = abs(ddx(finalShadowMask)) + abs(ddy(finalShadowMask));
                shadowEdge = smoothstep(0, _ShadowEdgeLineWidth, shadowEdge);
                
                float3 lightColor = baseColor.rgb;
                float3 shadowColor = _ShadowColor.rgb * baseColor.rgb;
                
                // 修正：确保shadow darkening值为正
                shadowColor = pow(max(shadowColor, 0.001), max(_ShadowDarkening, 1.0));
                
                float3 highlightColor = _HighlightColor.rgb * baseColor.rgb;
                
                float shadowBlend = lerp(_ShadowIntensity, 0.0, distanceShadow);
                float3 finalShadowColor = lerp(shadowColor, lightColor, shadowBlend);
                
                float3 color = lerp(finalShadowColor, lightColor, finalShadowMask);
                
                color = lerp(color, highlightColor, highlightMask);
                
                float3 halfDir = normalize(light.direction + IN.viewDirWS);
                float NdotH = saturate(dot(normalWS, halfDir));
                // 修正：确保specular power值为正
                float specular = pow(max(NdotH, 0.001), max(_SpecularPower, 1.0)) * specularSample.r * _SpecularIntensity;
                color += specular * _SpecularColor.rgb * light.color;
                
                color += rimLight * _RimLightColor.rgb * baseColor.rgb;
                
                color = lerp(color, _ShadowEdgeLineColor.rgb, shadowEdge * _ShadowEdgeLineIntensity);
                
                float3 ambient = SampleSH(normalWS) * 0.15 * baseColor.rgb;
                color += ambient;
                
                if(_DebugAlpha > 0.5)
                {
                    color = float3(mainTexColor.a, mainTexColor.a, mainTexColor.a);
                }
                
                float finalAlpha = mainTexColor.a;
                if(_UseAlphaClip > 0.5)
                {
                    finalAlpha = step(_AlphaClip, mainTexColor.a);
                }
                else
                {
                    finalAlpha = lerp(mainTexColor.a, 1.0, _AlphaToOpaque);
                }
                
                return float4(color, finalAlpha);
            }
            ENDHLSL
        }
        
        // Shadow Caster Pass
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            
            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back
            
            HLSLPROGRAM
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment
            
            #pragma shader_feature_local _ALPHATEST_ON
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
            };
            
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float _AlphaClip;
                float _UseAlphaClip;
            CBUFFER_END
            
            float3 _LightDirection;
            float3 _LightPosition;
            
            Varyings ShadowPassVertex(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                
            #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                float3 lightDirectionWS = normalize(_LightPosition - positionWS);
            #else
                float3 lightDirectionWS = _LightDirection;
            #endif
                
                positionWS = ApplyShadowBias(positionWS, normalWS, lightDirectionWS);
                output.positionCS = TransformWorldToHClip(positionWS);
                
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                
            #if UNITY_REVERSED_Z
                output.positionCS.z = min(output.positionCS.z, UNITY_NEAR_CLIP_VALUE);
            #else
                output.positionCS.z = max(output.positionCS.z, UNITY_NEAR_CLIP_VALUE);
            #endif
                
                return output;
            }
            
            half4 ShadowPassFragment(Varyings input) : SV_TARGET
            {
                float4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                
                float alpha = texColor.a;
                
                clip(alpha - _AlphaClip);
                
                if (alpha < _AlphaClip)
                    discard;
                
                if (alpha < 0.001)
                    discard;
                
                return 0;
            }
            
            ENDHLSL
        }
        
        // Depth Only Pass
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            
            ZWrite On
            ColorMask 0
            Cull Back
            
            HLSLPROGRAM
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment
            
            #pragma shader_feature_local _ALPHATEST_ON
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
            struct Attributes
            {
                float4 position : POSITION;
                float2 uv       : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };
            
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float _AlphaClip;
                float _UseAlphaClip;
            CBUFFER_END
            
            Varyings DepthOnlyVertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                
                output.positionCS = TransformObjectToHClip(input.position.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                return output;
            }
            
            half4 DepthOnlyFragment(Varyings input) : SV_TARGET
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                
                float4 mainTexColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                float testAlpha = mainTexColor.a;
                
                clip(testAlpha - _AlphaClip);
                
                if(testAlpha < _AlphaClip)
                {
                    discard;
                }
                
                return 0;
            }
            
            ENDHLSL
        }
        
        // Depth Normals Pass
        Pass
        {
            Name "DepthNormals"
            Tags{"LightMode" = "DepthNormals"}

            ZWrite On
            Cull Back

            HLSLPROGRAM
            #pragma vertex DepthNormalsVertex
            #pragma fragment DepthNormalsFragment

            #pragma shader_feature_local _ALPHATEST_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/UnityInput.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float _AlphaClip;
                float _UseAlphaClip;
            CBUFFER_END

            struct Attributes
            {
                float4 position     : POSITION;
                float4 tangent      : TANGENT;
                float3 normal       : NORMAL;
                float2 texcoord     : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float2 uv           : TEXCOORD1;
                float3 normalWS     : TEXCOORD2;

                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings DepthNormalsVertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                output.uv         = TRANSFORM_TEX(input.texcoord, _MainTex);
                output.positionCS = TransformObjectToHClip(input.position.xyz);

                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normal, input.tangent);
                output.normalWS = normalInput.normalWS;

                return output;
            }

            half4 DepthNormalsFragment(Varyings input) : SV_TARGET
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float4 mainTexColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                float testAlpha = mainTexColor.a;

                #ifdef _ALPHATEST_ON
                    clip(testAlpha - _AlphaClip);
                #endif

                if(_UseAlphaClip > 0.5)
                {
                    clip(testAlpha - _AlphaClip);
                }

                return float4(PackNormalOctRectEncode(TransformWorldToViewDir(input.normalWS, true)), 0.0, 0.0);
            }
            ENDHLSL
        }
        
        // Meta Pass (for Lightmapping)
        Pass
        {
            Name "Meta"
            Tags{"LightMode" = "Meta"}

            Cull Off

            HLSLPROGRAM
            #pragma vertex UniversalVertexMeta
            #pragma fragment UniversalFragmentMeta

            #pragma shader_feature_local _ALPHATEST_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/MetaInput.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _BaseColor;
                float _AlphaClip;
                float _UseAlphaClip;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                float2 uv0          : TEXCOORD0;
                float2 uv1          : TEXCOORD1;
                float2 uv2          : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float2 uv           : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings UniversalVertexMeta(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                
                output.positionCS = MetaVertexPosition(input.positionOS, input.uv1, input.uv2,
                    unity_LightmapST, unity_DynamicLightmapST);
                output.uv = TRANSFORM_TEX(input.uv0, _MainTex);
                return output;
            }

            half4 UniversalFragmentMeta(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                
                float4 mainTexColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                float4 baseColor = mainTexColor * _BaseColor;

                #ifdef _ALPHATEST_ON
                    clip(baseColor.a - _AlphaClip);
                #endif

                if(_UseAlphaClip > 0.5)
                {
                    clip(baseColor.a - _AlphaClip);
                }

                MetaInput metaInput;
                metaInput.Albedo = baseColor.rgb;
                metaInput.Emission = 0;
                metaInput.SpecularColor = half3(0, 0, 0);

                return MetaFragment(metaInput);
            }

            ENDHLSL
        }
        
        // Universal2D Pass
        Pass
        {
            Name "Universal2D"
            Tags{ "LightMode" = "Universal2D" }

            Blend One OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature_local _ALPHATEST_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _BaseColor;
                float _AlphaClip;
                float _UseAlphaClip;
            CBUFFER_END

            struct Attributes
            {
                float3 positionOS   : POSITION;
                float4 color        : COLOR;
                float2 uv           : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4  positionCS  : SV_POSITION;
                float4  color       : COLOR;
                float2  uv          : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes v)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.positionCS = TransformObjectToHClip(v.positionOS);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color * _BaseColor;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                
                float4 mainTexColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                float4 color = mainTexColor * i.color;

                #ifdef _ALPHATEST_ON
                    clip(color.a - _AlphaClip);
                #endif

                if(_UseAlphaClip > 0.5)
                {
                    clip(color.a - _AlphaClip);
                }

                return color;
            }
            ENDHLSL
        }
    }
    
    // Fallback and Custom Editor
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}

/*
自定义编辑器类（可选）
可以在单独的C#文件中创建以下编辑器类：

using UnityEngine;
using UnityEditor;

public class ToonShaderGUI : ShaderGUI
{
    public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
    {
        Material material = materialEditor.target as Material;
        
        EditorGUILayout.LabelField("Toon Lit with Enhanced Outline", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        
        // 主要属性
        EditorGUILayout.LabelField("Main Properties", EditorStyles.boldLabel);
        MaterialProperty mainTex = FindProperty("_MainTex", properties);
        MaterialProperty baseColor = FindProperty("_BaseColor", properties);
        materialEditor.TexturePropertySingleLine(new GUIContent("Main Texture"), mainTex, baseColor);
        
        MaterialProperty cutoff = FindProperty("_Cutoff", properties);
        materialEditor.ShaderProperty(cutoff, "Shadow Threshold");
        
        EditorGUILayout.Space();
        
        // 阴影属性
        EditorGUILayout.LabelField("Shadow Properties", EditorStyles.boldLabel);
        MaterialProperty shadowColor = FindProperty("_ShadowColor", properties);
        materialEditor.ShaderProperty(shadowColor, "Shadow Color");
        
        MaterialProperty shadowIntensity = FindProperty("_ShadowIntensity", properties);
        materialEditor.ShaderProperty(shadowIntensity, "Shadow Intensity");
        
        EditorGUILayout.Space();
        
        // 描边属性
        EditorGUILayout.LabelField("Outline Properties", EditorStyles.boldLabel);
        MaterialProperty outlineColor = FindProperty("_OutlineColor", properties);
        materialEditor.ShaderProperty(outlineColor, "Outline Color");
        
        MaterialProperty outlineThickness = FindProperty("_OutlineThickness", properties);
        materialEditor.ShaderProperty(outlineThickness, "Outline Thickness");
        
        EditorGUILayout.Space();
        
        // Alpha 设置
        EditorGUILayout.LabelField("Alpha Settings", EditorStyles.boldLabel);
        MaterialProperty alphaClip = FindProperty("_AlphaClip", properties);
        materialEditor.ShaderProperty(alphaClip, "Alpha Clipping");
        
        MaterialProperty useAlphaClip = FindProperty("_UseAlphaClip", properties);
        materialEditor.ShaderProperty(useAlphaClip, "Use Alpha Clipping");
        
        // 其余属性
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Advanced Properties", EditorStyles.boldLabel);
        
        // 显示剩余的属性
        foreach (MaterialProperty prop in properties)
        {
            if ((prop.flags & MaterialProperty.PropFlags.HideInInspector) == 0)
            {
                if (prop.name != "_MainTex" && prop.name != "_BaseColor" && 
                    prop.name != "_Cutoff" && prop.name != "_ShadowColor" && 
                    prop.name != "_ShadowIntensity" && prop.name != "_OutlineColor" && 
                    prop.name != "_OutlineThickness" && prop.name != "_AlphaClip" && 
                    prop.name != "_UseAlphaClip")
                {
                    materialEditor.ShaderProperty(prop, prop.displayName);
                }
            }
        }
    }
}
*/