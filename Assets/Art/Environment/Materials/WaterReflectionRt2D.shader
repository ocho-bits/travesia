Shader "URP/2D/WaterReflectionRT2D"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        [PerRendererData] _Color ("Tint", Color) = (1,1,1,1)

        _ReflectionTex ("Reflection RT", 2D) = "black" {}
        _WaterTint ("Water Tint", Color) = (0.12,0.28,0.35,1)
        _ReflectionStrength ("Reflection Strength", Range(0,1)) = 0.75

        _FadePower ("Fade Power", Range(0.5,8)) = 2.0

        _NoiseTex ("Noise", 2D) = "gray" {}
        _NoiseScale ("Noise Scale", Range(0.1, 50)) = 8
        _NoiseSpeed ("Noise Speed", Range(0, 10)) = 1
        _DistortStrength ("Distort Strength", Range(0, 0.05)) = 0.01
        _DistortAniso ("Distort Aniso (X,Y)", Vector) = (1, 0.25, 0, 0)
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline"
            "Queue"="Transparent"
            "RenderType"="Transparent"
        }

        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "Water"
            Tags { "LightMode"="Universal2D" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            TEXTURE2D(_ReflectionTex); SAMPLER(sampler_ReflectionTex);
            TEXTURE2D(_NoiseTex); SAMPLER(sampler_NoiseTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Color;

                float4 _WaterTint;
                float _ReflectionStrength;
                float _FadePower;

                float _NoiseScale;
                float _NoiseSpeed;
                float _DistortStrength;
                float4 _DistortAniso;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                float4 color      : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float4 color       : COLOR;
                float4 screenPos   : TEXCOORD1;
            };

            Varyings vert (Attributes IN)
            {
                Varyings OUT;

                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.color = IN.color * _Color;

                // Needed for proper screen-space sampling
                OUT.screenPos = ComputeScreenPos(OUT.positionHCS);

                return OUT;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                // River sprite as mask/shape
                half4 river = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv) * IN.color;
                if (river.a <= 0.001h)
                    return 0;

                // ----- SCREEN SPACE UV -----
                float2 screenUV = IN.screenPos.xy / max(IN.screenPos.w, 1e-6);

                // Clamp to avoid sampling outside RT
                screenUV = saturate(screenUV);

                // Flip Y
                screenUV.y = 1.0 - screenUV.y;

                // ----- DISTORTION (in screen space) -----
                float t = _Time.y;

                float2 noiseUV = screenUV * _NoiseScale
                               + float2(t * _NoiseSpeed, t * _NoiseSpeed * 0.73);

                float2 n = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, noiseUV).rg;
                n = n * 2.0 - 1.0;

                float2 distortedUV =
                    screenUV + (n * _DistortAniso.xy) * _DistortStrength;

                distortedUV = saturate(distortedUV);

                // ----- SAMPLE REFLECTION -----
                half3 refl =
                    SAMPLE_TEXTURE2D(_ReflectionTex, sampler_ReflectionTex, distortedUV).rgb;

                // ----- FADE (based on river sprite vertical UV) -----
                float fade = pow(saturate(IN.uv.y), _FadePower);

                float blend = river.a * _ReflectionStrength * fade;

                half3 outRgb = lerp(_WaterTint.rgb, refl, blend);

                return half4(outRgb, river.a);
            }

            ENDHLSL
        }
    }
}