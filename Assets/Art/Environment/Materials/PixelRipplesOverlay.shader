Shader "URP/2D/PixelRipplesOverlay"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture (Mask)", 2D) = "white" {}
        [PerRendererData] _Color ("Tint", Color) = (1,1,1,1)

        _RippleTex ("Ripple Noise (tileable)", 2D) = "white" {}
        _RippleColor ("Ripple Color", Color) = (1,1,1,1)

        _Scroll ("Scroll (X,Y)", Vector) = (0.25, 0.0, 0, 0)
        _Scale ("UV Scale", Range(0.1, 64)) = 8

        _Threshold ("Threshold", Range(0,1)) = 0.55
        _BandCount ("Bands (hard steps)", Range(1,8)) = 3
        _Strength ("Alpha Strength", Range(0,1)) = 0.6

        _PixelSnap ("Pixel Snap Amount", Range(0,1)) = 1
        _PixelGrid ("Pixel Grid (X,Y)", Vector) = (320, 180, 0, 0) // your reference resolution
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
            Name "Ripples"
            Tags { "LightMode"="Universal2D" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);    SAMPLER(sampler_MainTex);
            TEXTURE2D(_RippleTex);  SAMPLER(sampler_RippleTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Color;

                float4 _RippleColor;
                float4 _Scroll;
                float  _Scale;

                float  _Threshold;
                float  _BandCount;
                float  _Strength;

                float  _PixelSnap;
                float4 _PixelGrid; // xy used
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
                OUT.screenPos = ComputeScreenPos(OUT.positionHCS);
                return OUT;
            }

            float2 PixelSnapUV(float2 screenUV)
            {
                // Snap in screen space to a reference pixel grid
               float2 grid = max(_PixelGrid.xy, float2(1.0, 1.0));
                float2 snapped = (floor(screenUV * grid) + 0.5) / grid;
                return lerp(screenUV, snapped, saturate(_PixelSnap));
            }

            half4 frag (Varyings IN) : SV_Target
            {
                // Use sprite alpha as the mask/shape for ripples
                half4 mask = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv) * IN.color;
                if (mask.a <= 0.001h) return 0;

                float2 screenUV = IN.screenPos.xy / max(IN.screenPos.w, 1e-6);
                screenUV = saturate(screenUV);

                // Pixel snap to avoid subpixel shimmer
                screenUV = PixelSnapUV(screenUV);

                // Tile + scroll ripples in screen space (so camera motion doesn't "slide" them oddly)
                float t = _Time.y;
                float2 ruv = screenUV * _Scale + _Scroll.xy * t;

                float n = SAMPLE_TEXTURE2D(_RippleTex, sampler_RippleTex, ruv).r;

                // Hard threshold + banding for pixel-art look
                // 1) shift by threshold
                float v = saturate(n - _Threshold);

                // 2) quantize into bands
                float bands = max(_BandCount, 1.0);
                float q = floor(v * bands) / bands;

                // Optional: make it binary-crisp (uncomment if desired)
                // q = step(0.001, q);

                float alpha = q * _Strength * mask.a;

                half3 rgb = _RippleColor.rgb;
                return half4(rgb, alpha);
            }
            ENDHLSL
        }
    }
}