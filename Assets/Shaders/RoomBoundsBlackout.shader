Shader "Custom/RoomBoundsBlackout"
{
    Properties {}
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Overlay" }
        ZTest Always Cull Off ZWrite Off
        Pass
        {
            Name "Blackout"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            sampler2D _CameraDepthTexture;
            float4 _MainTex_TexelSize;
            float3 _RoomMin;
            float3 _RoomMax;
            float  _SoftMargin; // fade distance outside bounds

            struct Attributes { uint vertexID : SV_VertexID; };
            struct Varyings { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };

            Varyings Vert(Attributes IN)
            {
                Varyings OUT;
                float2 uv = float2((IN.vertexID << 1) & 2, IN.vertexID & 2);
                OUT.uv = uv;
                OUT.pos = float4(uv * 2 - 1, 0, 1);
                return OUT;
            }

            float3 ReconstructWorld(float2 uv, out float rawDepth)
            {
                rawDepth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, uv);
                // Convert depth to clip space (-1..1)
                float4 clip = float4(uv * 2 - 1, rawDepth * 2 - 1, 1);
                // View space
                float4 view = mul(unity_CameraInvProjection, clip);
                view /= view.w;
                float3 world = mul(unity_CameraToWorld, float4(view.xyz, 1)).xyz;
                return world;
            }

            fixed4 Frag(Varyings IN) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, IN.uv);

                float rawDepth;
                float3 wpos = ReconstructWorld(IN.uv, rawDepth);

                // If depth is at far plane (eg skybox/clear), treat as outside and black out.
                if (rawDepth >= 0.999f)
                    return fixed4(0,0,0,1);

                // Inside AABB test
                bool inside = (wpos.x >= _RoomMin.x && wpos.x <= _RoomMax.x) &&
                              (wpos.y >= _RoomMin.y && wpos.y <= _RoomMax.y) &&
                              (wpos.z >= _RoomMin.z && wpos.z <= _RoomMax.z);

                if (inside) return col;

                // Hard edge if margin <= 0
                if (_SoftMargin <= 0.0001f)
                    return fixed4(0,0,0,1);

                // Distance outside box
                float3 outsideVec = float3(0,0,0);
                if (wpos.x < _RoomMin.x) outsideVec.x = _RoomMin.x - wpos.x; else if (wpos.x > _RoomMax.x) outsideVec.x = wpos.x - _RoomMax.x;
                if (wpos.y < _RoomMin.y) outsideVec.y = _RoomMin.y - wpos.y; else if (wpos.y > _RoomMax.y) outsideVec.y = wpos.y - _RoomMax.y;
                if (wpos.z < _RoomMin.z) outsideVec.z = _RoomMin.z - wpos.z; else if (wpos.z > _RoomMax.z) outsideVec.z = wpos.z - _RoomMax.z;
                float distOutside = length(outsideVec);
                float fade = saturate(distOutside / max(_SoftMargin, 1e-5));
                return lerp(col, fixed4(0,0,0,1), fade);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
