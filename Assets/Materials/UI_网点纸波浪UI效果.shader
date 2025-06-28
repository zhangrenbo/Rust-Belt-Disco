Shader "UI/Halftone/WavyGradientDotMultiply"
{
    Properties
    {
        [PerRendererData] _MainTex      ("Sprite Texture", 2D)  = "white" {}
        _Color        ("Tint Color",      Color)   = (1,1,1,1)
        _CellCount    ("Cell Count (X,Y)",Vector)  = (64,64,0,0)
        _WaveFreq     ("Wave Frequency",  Float)   = 10.0
        _WavePhase    ("Wave Phase",      Float)   = 0.0
        _WaveAmp      ("Wave Amplitude",  Float)   = 0.5
        _WaveOff      ("Wave Offset",     Float)   = 0.5
    }
    SubShader
    {
        Tags
        {
            "Queue"             = "Transparent"
            "IgnoreProjector"   = "True"
            "RenderType"        = "Transparent"
            "PreviewType"       = "Plane"
            "CanUseSpriteAtlas" = "True"
        }
        Cull Off
        ZWrite Off
        Blend One OneMinusSrcAlpha

        Pass
        {
            Name "FORWARD"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float2 texcoord : TEXCOORD0;
                float4 color    : COLOR;
            };
            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv     : TEXCOORD0;
                float4 color  : COLOR;
            };

            sampler2D _MainTex;
            float4   _MainTex_ST;
            fixed4   _Color;
            float4   _CellCount;
            float    _WaveFreq;
            float    _WavePhase;
            float    _WaveAmp;
            float    _WaveOff;

            v2f vert(appdata_t IN)
            {
                v2f OUT;
                OUT.vertex = UnityObjectToClipPos(IN.vertex);
                OUT.uv     = TRANSFORM_TEX(IN.texcoord, _MainTex);
                OUT.color  = IN.color * _Color;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                // 采样原图 & 混入 Tint
                fixed4 src = tex2D(_MainTex, IN.uv) * IN.color;

                // 垂直渐变因子
                float vertGrad = 1.0 - IN.uv.y;

                // 横向波浪：sin -> [-1,1] -> [0,1]
                float wave = sin(IN.uv.x * _WaveFreq + _WavePhase);
                float waveNorm = wave * _WaveAmp + _WaveOff;

                // 计算最终半径比例
                float radiusFac = saturate(vertGrad * waveNorm);

                // 网格坐标与单元内偏移（-0.5~+0.5）
                float2 cell = IN.uv * _CellCount.xy;
                float2 f    = frac(cell) - 0.5;

                // 到中心距离 & 阈值
                float d = length(f);
                float threshold = radiusFac * 0.70710678;

                // 内部为点（1），外部为背景（0）
                float m = step(d, threshold);
                float pattern = 1.0 - m;

                // 正片叠底：原图 * pattern
                src.rgb *= pattern;
                return fixed4(src.rgb, src.a);
            }
            ENDCG
        }
    }
    FallBack "UI/Default"
}
