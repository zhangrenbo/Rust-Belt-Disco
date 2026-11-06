Shader "Unlit/BrickWallShader_WithOutline"
{
    Properties
    {
        _MainTex ("Brick Texture", 2D) = "white" {}
        _NormalMap ("Normal Map", 2D) = "bump" {}
        _Tiling ("Tiling", Float) = 1.0
        _Offset ("Offset", Vector) = (0, 0, 0, 0)
        _Color ("Color", Color) = (1,1,1,1)
        _Rotation ("Rotation", Float) = 0.0 // 旋转角度，单位是度
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
            float _Rotation; // 旋转角度
            float4 _OutlineColor;
            float _OutlineThickness;

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

                o.screenPos = ComputeScreenPos(o.vertex); // 用于屏幕空间计算
                o.worldNormal = UnityObjectToWorldNormal(v.normal); // 转换到世界空间法线

                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Sample the main brick texture
                fixed4 col = tex2D(_MainTex, i.uv) * _Color;

                // 获取屏幕空间坐标和深度
                float2 screenUV = i.screenPos.xy / i.screenPos.w;
                float depth = i.screenPos.z / i.screenPos.w;

                // 计算屏幕空间的深度变化
                float depthDiff = abs(ddx(depth)) + abs(ddy(depth));

                // 计算法线的变化
                float3 normal = normalize(i.worldNormal);
                float3 normalDiff = abs(ddx(normal)) + abs(ddy(normal));
                float normalEdge = length(normalDiff);

                // 边缘检测逻辑
                float edge = 0.0;
                edge += step(0.1, depthDiff); // 深度变化大于阈值时认为是边缘
                edge += step(0.1, normalEdge); // 法线变化大于阈值时认为是边缘

                // 限制边缘范围
                edge = saturate(edge);

                // 如果是边缘，应用描边颜色
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
