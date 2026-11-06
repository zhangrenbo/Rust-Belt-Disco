Shader "Unlit/BrickWallShader"
{
    Properties
    {
        _MainTex ("Brick Texture", 2D) = "white" {}
        _NormalMap ("Normal Map", 2D) = "bump" {}
        _Tiling ("Tiling", Float) = 1.0
        _Offset ("Offset", Vector) = (0, 0, 0, 0)
        _Color ("Color", Color) = (1,1,1,1)
        _Rotation ("Rotation", Float) = 0.0 // 旋转角度，单位是度
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
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            sampler2D _NormalMap;
            float _Tiling;
            float4 _Offset;
            float4 _Color;
            float _Rotation; // 旋转角度

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);

                // 计算旋转矩阵
                float angle = _Rotation * 3.14159 / 180.0; // 转换为弧度
                float cosA = cos(angle);
                float sinA = sin(angle);

                // Apply tiling and offset, and then rotate the UV coordinates
                float2 uv_rotated;
                uv_rotated.x = (v.uv.x + _Offset.x) * _Tiling;
                uv_rotated.y = (v.uv.y + _Offset.y) * _Tiling;

                // 应用旋转
                float x_rot = cosA * uv_rotated.x - sinA * uv_rotated.y;
                float y_rot = sinA * uv_rotated.x + cosA * uv_rotated.y;

                o.uv = float2(x_rot, y_rot); // 将旋转后的 UV 坐标赋值给 o.uv

                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Sample the main brick texture
                fixed4 col = tex2D(_MainTex, i.uv) * _Color;

                // Apply the normal map (optional for additional details like bumpiness)
                fixed3 normal = tex2D(_NormalMap, i.uv).rgb;
                normal = normalize(normal * 2.0 - 1.0); // Normalize the normal map

                return col;
            }
            ENDCG
        }
    }
}
