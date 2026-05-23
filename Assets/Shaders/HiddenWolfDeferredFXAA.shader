Shader "Hidden/Wolf/DeferredFXAA"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }

    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float _SubpixelBlending;
            float _EdgeThreshold;
            float _EdgeThresholdMin;

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

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float Luma(float3 color)
            {
                return dot(color, float3(0.299, 0.587, 0.114));
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 texel = _MainTex_TexelSize.xy;
                float2 uv = i.uv;

                float3 rgbM = tex2D(_MainTex, uv).rgb;
                float3 rgbNW = tex2D(_MainTex, uv + texel * float2(-1.0, -1.0)).rgb;
                float3 rgbNE = tex2D(_MainTex, uv + texel * float2(1.0, -1.0)).rgb;
                float3 rgbSW = tex2D(_MainTex, uv + texel * float2(-1.0, 1.0)).rgb;
                float3 rgbSE = tex2D(_MainTex, uv + texel * float2(1.0, 1.0)).rgb;

                float lumaM = Luma(rgbM);
                float lumaNW = Luma(rgbNW);
                float lumaNE = Luma(rgbNE);
                float lumaSW = Luma(rgbSW);
                float lumaSE = Luma(rgbSE);

                float lumaMin = min(lumaM, min(min(lumaNW, lumaNE), min(lumaSW, lumaSE)));
                float lumaMax = max(lumaM, max(max(lumaNW, lumaNE), max(lumaSW, lumaSE)));
                float contrast = lumaMax - lumaMin;

                if (contrast < max(_EdgeThresholdMin, lumaMax * _EdgeThreshold))
                {
                    return float4(rgbM, 1.0);
                }

                float2 dir;
                dir.x = -((lumaNW + lumaNE) - (lumaSW + lumaSE));
                dir.y = ((lumaNW + lumaSW) - (lumaNE + lumaSE));

                float dirReduce = max((lumaNW + lumaNE + lumaSW + lumaSE) * 0.0078125, 0.0009765625);
                float rcpDirMin = rcp(min(abs(dir.x), abs(dir.y)) + dirReduce);
                dir = clamp(dir * rcpDirMin, -8.0, 8.0) * texel;

                float3 rgbA =
                    0.5 * (
                        tex2D(_MainTex, uv + dir * (1.0 / 3.0 - 0.5)).rgb +
                        tex2D(_MainTex, uv + dir * (2.0 / 3.0 - 0.5)).rgb);

                float3 rgbB =
                    rgbA * 0.5 +
                    0.25 * (
                        tex2D(_MainTex, uv + dir * -0.5).rgb +
                        tex2D(_MainTex, uv + dir * 0.5).rgb);

                float lumaB = Luma(rgbB);
                float3 filtered = (lumaB < lumaMin || lumaB > lumaMax) ? rgbA : rgbB;
                return float4(lerp(rgbM, filtered, saturate(_SubpixelBlending)), 1.0);
            }
            ENDCG
        }
    }
    Fallback Off
}
