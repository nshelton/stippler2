Shader "Custom/InstancedSphere"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _DotSize ("Dot Size", Range(0, 1)) = 0.1
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
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 normal : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            StructuredBuffer<float2> _PointBuffer;
            float _DotSize;
            float _ImageWidth;
            float _ImageHeight;
            float4 _Color;

            v2f vert (appdata v, uint instanceID : SV_InstanceID)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);

                float2 pos = _PointBuffer[instanceID];
                float3 worldPos = float3(
                    pos.x ,
                    pos.y ,
                    0
                );
                
                float3 vertexPos = worldPos + v.vertex.xyz * _DotSize;
                o.vertex = UnityObjectToClipPos(float4(vertexPos, 1.0));
                o.normal = UnityObjectToWorldNormal(v.normal);
                
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                float3 lightDir = normalize(float3(1,1,1));
                float ndotl = max(0, dot(i.normal, lightDir));
                return _Color * (0.5 + 0.5 * ndotl);
            }
            ENDCG
        }
    }
} 