Shader "Hidden/UVSpaceNormalBaker"
{
    Properties
    {
        _SmoothingFactor ("Smoothing Factor", Range(0,1)) = 0.0
        _SphereCenter ("Sphere Center (Local)", Vector) = (0,0,0,0)
        _SphereScale ("Sphere Scale (Local)", Vector) = (1,1,1,1)
        _BakeMode ("Bake Mode (0=Object, 1=Tangent)", Float) = 0
        _UseVertexMask ("Use Vertex Mask (R)", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
                float4 tangent : TANGENT;
                float4 color : COLOR; // Read vertex color channels
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 normal : TEXCOORD0;
                float3 tangent : TEXCOORD1;
                float3 bitangent : TEXCOORD2;
                float3 localPos : TEXCOORD3;
                float4 vertexColor : COLOR;
            };

            float _SmoothingFactor;
            float4 _SphereCenter;
            float4 _SphereScale;
            float _BakeMode;
            float _UseVertexMask;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = float4(v.uv.x * 2.0 - 1.0, v.uv.y * 2.0 - 1.0, 0.0, 1.0);
                #if UNITY_UV_STARTS_AT_TOP
                o.vertex.y = -o.vertex.y;
                #endif

                o.localPos = v.vertex.xyz;
                o.normal = v.normal;
                o.vertexColor = v.color;
                
                o.tangent = v.tangent.xyz;
                o.bitangent = cross(v.normal, v.tangent.xyz) * v.tangent.w;

                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float3 baseNormal = normalize(i.normal);
                
                // Calculate directional vector relative to target center offset
                float3 offsetDir = i.localPos - _SphereCenter.xyz;
                
                // Upgraded Math: Scale vector by inverse squared components to create a true geometric Ellipsoid Normal Field
                float3 ellipsoidNormal = normalize(offsetDir / (_SphereScale.xyz * _SphereScale.xyz));
                
                // Check if vertex masking is enabled; falls back to 1.0 if disabled
                float mask = (_UseVertexMask > 0.5) ? i.vertexColor.r : 1.0;
                float finalSmoothWeight = _SmoothingFactor * mask;

                // Blend original normals with stylized ellipsoid vectors
                float3 finalNormal = normalize(lerp(baseNormal, ellipsoidNormal, finalSmoothWeight));

                if (_BakeMode > 0.5)
                {
                    float3x3 tbn = float3x3(normalize(i.tangent), normalize(i.bitangent), baseNormal);
                    finalNormal = mul(tbn, finalNormal);
                }

                float3 packedNormal = finalNormal * 0.5 + 0.5;
                return fixed4(packedNormal, 1.0);
            }
            ENDCG
        }
    }
}