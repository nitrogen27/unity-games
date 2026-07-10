Shader "Hidden/Wolf/CinematicPost"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _BloomTex ("Bloom", 2D) = "black" {}
    }

    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag_prefilter
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float _BloomThreshold;
            float _BloomKnee;

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

            float Max3(float3 value)
            {
                return max(max(value.r, value.g), value.b);
            }

            float4 frag_prefilter(v2f i) : SV_Target
            {
                float3 color = max(tex2D(_MainTex, i.uv).rgb, 0.0);
                float brightness = Max3(color);
                float knee = max(_BloomThreshold * _BloomKnee, 0.0001);
                float soft = brightness - _BloomThreshold + knee;
                soft = clamp(soft, 0.0, 2.0 * knee);
                soft = soft * soft / (4.0 * knee + 0.0001);
                float contribution = max(soft, brightness - _BloomThreshold);
                return float4(color * contribution / max(brightness, 0.0001), 1.0);
            }
            ENDCG
        }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag_blur
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float2 _BlurDirection;
            float _BloomScatter;

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

            float4 frag_blur(v2f i) : SV_Target
            {
                float2 stepUv = _MainTex_TexelSize.xy * _BlurDirection * _BloomScatter;
                float3 color = tex2D(_MainTex, i.uv).rgb * 0.227027;
                color += tex2D(_MainTex, i.uv + stepUv * 1.384615).rgb * 0.316216;
                color += tex2D(_MainTex, i.uv - stepUv * 1.384615).rgb * 0.316216;
                color += tex2D(_MainTex, i.uv + stepUv * 3.230769).rgb * 0.070270;
                color += tex2D(_MainTex, i.uv - stepUv * 3.230769).rgb * 0.070270;
                return float4(color, 1.0);
            }
            ENDCG
        }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag_composite
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            sampler2D _BloomTex;
            float4 _MainTex_TexelSize;
            float _Exposure;
            float _Contrast;
            float _BlackPoint;
            float _Saturation;
            float _LocalContrast;
            float _LocalRadius;
            float _ShadowLift;
            float _BloomIntensity;
            float _Vignette;
            float _WarmHighlights;
            float _CoolShadows;

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
                return dot(color, float3(0.2126, 0.7152, 0.0722));
            }

            float3 Aces(float3 color)
            {
                return saturate((color * (2.51 * color + 0.03)) / (color * (2.43 * color + 0.59) + 0.14));
            }

            float3 GradeSplitTone(float3 color)
            {
                float luma = saturate(Luma(color));
                float3 warm = float3(1.0, 0.77, 0.48);
                float3 cool = float3(0.62, 0.76, 1.0);
                color = lerp(color, color * cool, (1.0 - luma) * _CoolShadows);
                color = lerp(color, color * warm, smoothstep(0.45, 1.0, luma) * _WarmHighlights);
                return color;
            }

            float4 frag_composite(v2f i) : SV_Target
            {
                float2 uv = i.uv;
                float3 color = max(tex2D(_MainTex, uv).rgb, 0.0);

                float2 localStep = _MainTex_TexelSize.xy * _LocalRadius;
                float3 localAverage = color * 0.36;
                localAverage += tex2D(_MainTex, uv + float2(localStep.x, 0.0)).rgb * 0.16;
                localAverage += tex2D(_MainTex, uv - float2(localStep.x, 0.0)).rgb * 0.16;
                localAverage += tex2D(_MainTex, uv + float2(0.0, localStep.y)).rgb * 0.16;
                localAverage += tex2D(_MainTex, uv - float2(0.0, localStep.y)).rgb * 0.16;
                color += (color - localAverage) * _LocalContrast;

                color += tex2D(_BloomTex, uv).rgb * _BloomIntensity;
                color = GradeSplitTone(color * _Exposure);
                color = Aces(color);

                float liftLuma = Luma(color);
                float liftMask = (1.0 - smoothstep(0.22, 0.78, liftLuma)) * smoothstep(0.004, 0.05, liftLuma);
                color += (1.0 - color) * (_ShadowLift * liftMask);

                color = max(color - _BlackPoint, 0.0) / max(1.0 - _BlackPoint, 0.0001);
                color = (color - 0.5) * _Contrast + 0.5;

                float luma = Luma(color);
                color = lerp(float3(luma, luma, luma), color, _Saturation);

                float2 centered = uv * 2.0 - 1.0;
                float vignette = 1.0 - dot(centered, centered) * _Vignette;
                color *= saturate(vignette);

                return float4(saturate(color), 1.0);
            }
            ENDCG
        }
    }

    Fallback Off
}
