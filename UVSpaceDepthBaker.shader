Shader "Hidden/UVSpaceDepthBaker"
{
    Properties
    {
        _ProjAxis ("Projection Axis", Vector) = (0,1,0,0)
        _MinDepth ("Min Depth", Float) = 0.0
        _MaxDepth ("Max Depth", Float) = 1.0
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
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float depthVal : TEXCOORD0;
            };

            float4 _ProjAxis;
            float _MinDepth;
            float _MaxDepth;

            v2f vert (appdata v)
            {
                v2f o;
                // Flatten mesh into UV space layout
                o.vertex = float4(v.uv.x * 2.0 - 1.0, v.uv.y * 2.0 - 1.0, 0.0, 1.0);
                #if UNITY_UV_STARTS_AT_TOP
                o.vertex.y = -o.vertex.y;
                #endif

                // Project local vertex position onto our tracking axis vector
                float dist = dot(v.vertex.xyz, normalize(_ProjAxis.xyz));
                
                // Convert raw distance into a normalized 0 to 1 range between planes
                o.depthVal = (dist - _MinDepth) / (_MaxDepth - _MinDepth);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Clamp values strictly to prevent out-of-bounds color corruption
                float d = clamp(i.depthVal, 0.0, 1.0);
                return fixed4(d, d, d, 1.0);
            }
            ENDCG
        }
    }
}