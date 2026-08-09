Shader "CrammingHamster/TitleLightSweep"
{
    Properties
    {
        _MainTex ("Title", 2D) = "white" {}
        _SweepPosition ("Sweep Position", Range(-0.3, 1.3)) = -0.3
        _SweepWidth ("Sweep Width", Range(0.01, 0.3)) = 0.11
        _SweepStrength ("Sweep Strength", Range(0, 1)) = 0.72
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

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
                float2 uv : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _SweepPosition;
            float _SweepWidth;
            float _SweepStrength;

            v2f vert(appdata input)
            {
                v2f output;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                return output;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                fixed4 color = tex2D(_MainTex, input.uv);
                float diagonalCenter = _SweepPosition + (input.uv.y - 0.5) * 0.18;
                float distanceFromSweep = abs(input.uv.x - diagonalCenter);
                float sweep = 1.0 - smoothstep(0.0, _SweepWidth, distanceFromSweep);
                float sweepAlpha = sweep * color.a * _SweepStrength;
                return fixed4(1.0, 1.0, 1.0, sweepAlpha);
            }
            ENDCG
        }
    }
}
