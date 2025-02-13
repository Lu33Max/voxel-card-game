Shader "Unlit/DottedLine"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _DashSize ("Dash Size", Range(0, 1)) = 0.5
        _Spacing ("Spacing", Range(0, 1)) = 0.5
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            float _DashSize;
            float _Spacing;
            fixed4 _Color;

            v2f vert (appdata_t v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Berechnung des Streifenmusters entlang der X-Achse
                float pattern = step(_DashSize, fmod(i.uv.x, _DashSize + _Spacing));
                return fixed4(_Color.rgb, 1.0 - pattern);
            }
            ENDCG
        }
    }
}
