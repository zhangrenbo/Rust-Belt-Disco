Shader "UI/Halftone/GradientDotMultiply_RotateScale"
{
    Properties
    {
        // UI 专用贴图属性，支持 Sprite Atlas 的缩放/偏移
        [PerRendererData] _MainTex        ("Sprite Texture", 2D)    = "white" {}
        _Color        ("Tint Color",      Color)   = (1,1,1,1)
        _CellCount    ("Cell Count (X,Y)",Vector)  = (64,64,0,0)
        _PatternScale ("Pattern Scale",   Float)   = 1.0
        _Rotation     ("Rotation (rad)",  Float)   = 0.0
    }
    SubShader
    {
        Tags
        {
            "Queue"            = "Transparent"
            "IgnoreProjector"  = "True"
            "RenderType"       = "Transparent"
            "PreviewType"      = "Plane"
            "CanUseSpriteAtlas"= "True"
        }
        Cull Off
        ZWrite Off
        Blend One OneMinusSrcAlpha

        Pass
        {
            Name "FORWARD"

            // 内置管线和 URP 均支持 CGPROGRAM/ENDCG
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"    // UI 专用宏：TRANSFORM_TEX

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
            float    _PatternScale;
            float    _Rotation;

            v2f vert(appdata_t IN)
            {
                v2f OUT;
                // 顶点坐标
                OUT.vertex = UnityObjectToClipPos(IN.vertex);
                // UV 采样时自动乘以 _MainTex_ST（Sprite Atlas 缩放/偏移）
                OUT.uv     = TRANSFORM_TEX(IN.texcoord, _MainTex);
                OUT.color  = IN.color * _Color;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                // 1. 先采样原图
                fixed4 src = tex2D(_MainTex, IN.uv);

                // 2. 计算渐变因子（从上到下，点由小到大）
                float radiusFac = 1.0 - IN.uv.y;

                // 3. 对 UV 做中心缩放 & 旋转
                float2 uv0 = IN.uv - 0.5;
                uv0 *= _PatternScale;
                float s = sin(_Rotation);
                float c = cos(_Rotation);
                float2 uvR;
                uvR.x = uv0.x * c - uv0.y * s;
                uvR.y = uv0.x * s + uv0.y * c;
                float2 uv = uvR + 0.5;

                // 4. 计算网格坐标 & 单元内偏移（-0.5~+0.5）
                float2 cell = uv * _CellCount.xy;
                float2 f    = frac(cell) - 0.5;

                // 5. 到单元中心距离
                float d = length(f);

                // 6. 最大半径 √(0.5²+0.5²)≈0.707
                float threshold = radiusFac * 0.70710678;

                // 7. d<threshold 时画点，否则空白
                float m = step(d, threshold);
                float pattern = 1.0 - m;

                // 8. 正片叠底：原图 * pattern
                src.rgb *= pattern;
                return fixed4(src.rgb, src.a * IN.color.a);
            }
            ENDCG
        }
    }

    FallBack "UI/Default"
}
