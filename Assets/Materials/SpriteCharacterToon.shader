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
        // ͸�����󣬲�д��ȣ����޳���
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
                
                // ����UV�任
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                
                #ifdef _USECUSTOMTRANSFORM_ON
                // Ӧ���Զ������ź�λ��
                // ��Ӧ�����ţ���Ӧ��λ��
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
    
    // Ϊ����Inspector�и��õ���ʾ
    CustomEditor "SimpleUnlitSpriteShaderGUI"
}