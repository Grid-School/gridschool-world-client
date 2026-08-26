Shader "Unlit/PlanetarySkybox"
{
    Properties
    {
        _StarTex ("Star Texture", 2D) = "white" {}
        _StarDensity ("Star Density", Range(0, 1)) = 0.5
        _SkyColorDark ("Dark Side Color", Color) = (0, 0, 0.1, 1)
        _SkyColorLight ("Light Side Color", Color) = (0.5, 0.7, 1, 1)
        _SunDirection ("Sun Direction", Vector) = (0, 1, 0, 0)
        _TransitionSharpness ("Transition Sharpness", Range(0, 10)) = 5
    }
    SubShader
    {
        Tags { "RenderType"="Background" "Queue"="Background" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 viewDir : TEXCOORD1;
                float2 uv : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _SkyColorDark;
                float4 _SkyColorLight;
                float4 _SunDirection;
                float _TransitionSharpness;
                float _StarDensity;
                sampler2D _StarTex;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.viewDir = normalize(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // Sun influence based on view direction
                float sunDot = dot(normalize(IN.viewDir), normalize(_SunDirection.xyz));
                float sunFactor = pow(saturate(sunDot), _TransitionSharpness);

                // Blend between dark and light sky
                half4 skyColor = lerp(_SkyColorDark, _SkyColorLight, sunFactor);

                // Star visibility (only on dark side)
                half starIntensity = tex2D(_StarTex, IN.uv).r * (1 - sunFactor) * _StarDensity;
                skyColor.rgb += starIntensity;

                return skyColor;
            }
            ENDHLSL
        }
    }
}
