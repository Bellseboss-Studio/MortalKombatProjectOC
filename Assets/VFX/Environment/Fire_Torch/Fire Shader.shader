Shader "Custom/FireShader"
{
    Properties
    {
        _NoiseTex ("Noise Texture", 2D) = "white" {}
        _Speed ("Animation Speed", Float) = 0.5
        _EmissionIntensity ("Emission Intensity", Float) = 2.0
        _NoiseScale ("Noise Scale", Float) = 1.0
        _BaseColor ("Base Color (Bottom)", Color) = (1, 0, 0, 1)
        _TopColor ("Top Color", Color) = (1, 1, 0, 1)
        _GradientHeight ("Gradient Height", Float) = 1.0
        _MaxFireHeight ("Max Fire Height", Range(0.1, 1.0)) = 0.7
        _PulseSpeed ("Pulse Speed", Float) = 1.0
        _PulseAmplitude ("Pulse Amplitude", Range(0.0, 0.5)) = 0.2
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        LOD 100

        Pass
        {
            Name "Unlit"
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
                float fogCoord : TEXCOORD2;
            };

            // Propiedades
            TEXTURE2D(_NoiseTex);
            SAMPLER(sampler_NoiseTex);
            float _Speed;
            float _EmissionIntensity;
            float _NoiseScale;
            float4 _BaseColor;
            float4 _TopColor;
            float _GradientHeight;
            float _MaxFireHeight;
            float _PulseSpeed;
            float _PulseAmplitude;

            Varyings vert (Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.worldPos = TransformObjectToWorld(input.positionOS.xyz);
                output.fogCoord = ComputeFogFactor(output.positionCS.z);
                return output;
            }

            half4 frag (Varyings input) : SV_Target
            {
                // Animar UVs con desplazamiento basado en el tiempo
                float2 animatedUV = input.uv + float2(0, _Time.y * _Speed);
                animatedUV *= _NoiseScale;

                // Muestrear textura de ruido
                half noise = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, animatedUV).r;

                // Crear gradiente basado en la posición Y en el espacio del mundo
                float normalizedHeight = saturate(input.worldPos.y / _GradientHeight);

                // Limitar la altura del fuego
                float fireHeightLimit = _MaxFireHeight * (1.0 + _PulseAmplitude * sin(_Time.y * _PulseSpeed));
                float fireMask = smoothstep(fireHeightLimit, fireHeightLimit - 0.2, normalizedHeight);

                // Crear gradiente de color
                half4 fireColor = lerp(_BaseColor, _TopColor, normalizedHeight);

                // Combinar ruido, color y máscara de altura
                half4 finalColor = fireColor * noise * fireMask * _EmissionIntensity;

                // Asegurar transparencia suave
                finalColor.a = noise * fireColor.a * fireMask;

                // Aplicar niebla
                finalColor.rgb = MixFog(finalColor.rgb, input.fogCoord);

                return finalColor;
            }
            ENDHLSL
        }
    }
}