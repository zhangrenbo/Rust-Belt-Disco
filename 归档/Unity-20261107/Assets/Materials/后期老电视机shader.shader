Shader "Hidden/CRT/ScreenEffect"
{
    Properties
    {
        _MainTex            ("_MainTex",            2D)    = "white" {}
        _Distortion         ("畸变强度",            Range(0,1)) = 0.1
        _ChromAber          ("色散强度",            Range(0,0.1)) = 0.02
        _ScanlineCount      ("扫描线数",            Float)       = 480
        _ScanlineIntensity  ("扫描线强度",          Range(0,1))  = 0.2
        _NoiseIntensity     ("噪点强度",            Range(0,1))  = 0.1
        _VignetteIntensity  ("暗角强度",            Range(0,1))  = 0.5
        _VignetteSoftness   ("暗角柔和度",          Range(0.1,1))= 0.5
    }
    SubShader
    {
        Cull Off
        ZWrite Off
        ZTest Always

        Pass
        {
            Name "CRT_POST"

            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float    _Distortion;
            float    _ChromAber;
            float    _ScanlineCount;
            float    _ScanlineIntensity;
            float    _NoiseIntensity;
            float    _VignetteIntensity;
            float    _VignetteSoftness;

            // 简单伪随机
            float rand(in float2 co) 
            { 
                return frac(sin(dot(co, float2(12.9898,78.233))) * 43758.5453); 
            }

            fixed4 frag(v2f_img i) : SV_Target
            {
                // 1. UV -> [-1,1]
                float2 uv = i.uv * 2 - 1;

                // 2. 桶形/枕形失真
                float r2 = dot(uv, uv);
                uv *= (1 + _Distortion * r2);

                // 3. 回到 [0,1]
                uv = uv * 0.5 + 0.5;

                // 4. 色散：R/G/B 三次采样
                float3 col;
                col.r = tex2D(_MainTex, uv + _ChromAber).r;
                col.g = tex2D(_MainTex, uv).g;
                col.b = tex2D(_MainTex, uv - _ChromAber).b;

                // 5. 扫描线（sin 波动）
                float scan = sin(i.uv.y * _ScanlineCount) * _ScanlineIntensity;
                col *= (1 - scan);

                // 6. 噪点叠加
                float n = (rand(i.uv) - 0.5) * _NoiseIntensity;
                col += n;

                // 7. 暗角（中心到边缘距离越大越暗）
                float dist = length(i.uv - 0.5);
                float vig = smoothstep(_VignetteSoftness, 0.0, dist);
                col *= lerp(1, vig, _VignetteIntensity);

                return float4(col, 1);
            }
            ENDCG
        }
    }
    Fallback Off
}
