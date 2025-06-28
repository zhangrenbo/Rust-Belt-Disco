Shader "Custom/SimpleUnlitSprite"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
        
        [Header(Texture Transform)]
        [Space(5)]
        _Scale("Scale", Vector) = (1, 1, 0, 0)
        _Offset("Offset", Vector) = (0, 0, 0, 0)
        
        [Space(10)]
        [Toggle] _UseCustomTransform("Use Custom Transform", Float) = 0
    }
    SubShader
    {
        // 透明对象，不写深度，不剔除面
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature _USECUSTOMTRANSFORM_ON
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv  : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4    _MainTex_ST;
            float4    _Scale;
            float4    _Offset;
            float     _UseCustomTransform;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                
                // 基础UV变换
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                
                #ifdef _USECUSTOMTRANSFORM_ON
                // 应用自定义缩放和位移
                // 先应用缩放，再应用位移
                o.uv = (o.uv - 0.5) * _Scale.xy + 0.5 + _Offset.xy;
                #endif
                
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                return tex2D(_MainTex, i.uv);
            }
            ENDHLSL
        }
    }
    
    // 为了在Inspector中更好的显示
    CustomEditor "SimpleUnlitSpriteShaderGUI"
}