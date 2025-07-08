Shader "Hidden/RadialLightShader"
{
    Properties
    {
        _PlayerPos ("Player Position", Vector) = (0.5, 0.5, 0, 0)
        _Radius ("Light Radius", Float) = 0.3
        _Softness ("Edge Softness", Float) = 0.1
        _DarkColor ("Dark Color", Color) = (0, 0, 0, 0.6)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            ZTest Always Cull Off ZWrite Off

            HLSLPROGRAM
            #include "UnityCG.cginc"
            #pragma vertex vert
            #pragma fragment frag

            float4 _PlayerPos;
            float _Radius;
            float _Softness;
            float4 _DarkColor;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float2 uv : TEXCOORD0; float4 vertex : SV_POSITION; };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 diff = i.uv - _PlayerPos.xy;
                float dist = length(diff);
                float mask = smoothstep(_Radius, _Radius + _Softness, dist);
                return lerp(float4(1, 1, 1, 0), _DarkColor, mask);
            }
            ENDHLSL
        }
    }
}
