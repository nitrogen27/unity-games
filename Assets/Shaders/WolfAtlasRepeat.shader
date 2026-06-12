// Tiles one window of a texture atlas across a surface. TEXCOORD0 carries
// continuous module coordinates (world position / texture module), TEXCOORD1
// carries the window origin inside the atlas; frac() keeps the repeat inside
// the window without bleeding into neighboring atlas tiles, and tex2Dgrad
// keeps mip selection continuous across the repeat seam.
Shader "WolfMini/AtlasRepeat"
{
    Properties
    {
        _MainTex ("Atlas", 2D) = "white" {}
        _WindowScale ("Window Scale", Float) = 0.0625
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float _WindowScale;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float2 window : TEXCOORD1;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float2 window : TEXCOORD1;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.window = v.window;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.window + _WindowScale * frac(i.uv);
                float2 dx = ddx(i.uv) * _WindowScale;
                float2 dy = ddy(i.uv) * _WindowScale;
                return tex2Dgrad(_MainTex, uv, dx, dy);
            }
            ENDCG
        }
    }
    Fallback "Unlit/Texture"
}
