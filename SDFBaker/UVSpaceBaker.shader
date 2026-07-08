Shader "Hidden/UVSpaceBaker"
{
    Properties
    {
        _LightDir ("Light Direction", Vector) = (0,0,1,0)
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
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 worldNormal : TEXCOORD0;
            };

            float4 _LightDir;

            v2f vert (appdata v)
            {
                v2f o;
                // Flatten the mesh into UV space (mapping 0..1 to -1..1 clip space)
                o.vertex = float4(v.uv.x * 2.0 - 1.0, v.uv.y * 2.0 - 1.0, 0.0, 1.0);
                
                // Flip Y if necessary based on graphics API (DirectX vs OpenGL)
                #if UNITY_UV_STARTS_AT_TOP
                o.vertex.y = -o.vertex.y;
                #endif

                // We need the raw normal to calculate lighting
                o.worldNormal = normalize(v.normal);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float3 N = normalize(i.worldNormal);
                float3 L = normalize(_LightDir.xyz);
                
                // Simple dot product for lighting
                float NdotL = dot(N, L);
                
                // Hard threshold: 1 if lit, 0 if in shadow
                float lit = NdotL > 0.0 ? 1.0 : 0.0;
                return fixed4(lit, lit, lit, 1.0);
            }
            ENDCG
        }
    }
}