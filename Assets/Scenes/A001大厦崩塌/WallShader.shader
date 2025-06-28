Shader "Unlit/BrickWallShader_WithOutline"
{
    Properties
    {
        _MainTex ("Brick Texture", 2D) = "white" {}
        _NormalMap ("Normal Map", 2D) = "bump" {}
        _Tiling ("Tiling", Float) = 1.0
        _Offset ("Offset", Vector) = (0, 0, 0, 0)
        _Color ("Color", Color) = (1,1,1,1)
        _Rotation ("Rotation", Float) = 0.0 // ��ת�Ƕȣ���λ�Ƕ�
        _OutlineColor ("Outline Color", Color) = (0, 0, 0, 1)
        _OutlineThickness ("Outline Thickness", Float) = 1.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

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
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float4 screenPos : TEXCOORD1;
                float3 worldNormal : TEXCOORD2;
            };

            sampler2D _MainTex;
            sampler2D _NormalMap;
            float _Tiling;
            float4 _Offset;
            float4 _Color;
            float _Rotation; // ��ת�Ƕ�
            float4 _OutlineColor;
            float _OutlineThickness;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);

                // ������ת����
                float angle = _Rotation * 3.14159 / 180.0; // ת��Ϊ����
                float cosA = cos(angle);
                float sinA = sin(angle);

                // Apply tiling and offset, and then rotate the UV coordinates
                float2 uv_rotated;
                uv_rotated.x = (v.uv.x + _Offset.x) * _Tiling;
                uv_rotated.y = (v.uv.y + _Offset.y) * _Tiling;

                // Ӧ����ת
                float x_rot = cosA * uv_rotated.x - sinA * uv_rotated.y;
                float y_rot = sinA * uv_rotated.x + cosA * uv_rotated.y;

                o.uv = float2(x_rot, y_rot); // ����ת��� UV ���긳ֵ�� o.uv

                o.screenPos = ComputeScreenPos(o.vertex); // ������Ļ�ռ����
                o.worldNormal = UnityObjectToWorldNormal(v.normal); // ת��������ռ䷨��

                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Sample the main brick texture
                fixed4 col = tex2D(_MainTex, i.uv) * _Color;

                // ��ȡ��Ļ�ռ���������
                float2 screenUV = i.screenPos.xy / i.screenPos.w;
                float depth = i.screenPos.z / i.screenPos.w;

                // ������Ļ�ռ����ȱ仯
                float depthDiff = abs(ddx(depth)) + abs(ddy(depth));

                // ���㷨�ߵı仯
                float3 normal = normalize(i.worldNormal);
                float3 normalDiff = abs(ddx(normal)) + abs(ddy(normal));
                float normalEdge = length(normalDiff);

                // ��Ե����߼�
                float edge = 0.0;
                edge += step(0.1, depthDiff); // ��ȱ仯������ֵʱ��Ϊ�Ǳ�Ե
                edge += step(0.1, normalEdge); // ���߱仯������ֵʱ��Ϊ�Ǳ�Ե

                // ���Ʊ�Ե��Χ
                edge = saturate(edge);

                // ����Ǳ�Ե��Ӧ�������ɫ
                if (edge > 0.5)
                {
                    col = lerp(col, _OutlineColor, edge);
                }

                return col;
            }
            ENDCG
        }
    }
}
