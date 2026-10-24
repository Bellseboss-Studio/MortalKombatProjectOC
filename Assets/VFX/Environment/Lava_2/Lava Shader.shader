Shader "Custom/LavaShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}          // Textura base para el color
        _NoiseTex ("Noise Texture", 2D) = "white" {}   // Textura de ruido para distorsión
        _DistortionStrength ("Distortion Strength", Range(0, 1)) = 0.1
        _FlowSpeed ("Flow Speed", Float) = 0.2
        _EmissionColor ("Emission Color", Color) = (1, 0.5, 0, 1)
        _EmissionIntensity ("Emission Intensity", Float) = 2.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" "RenderPipeline"="UniversalPipeline" }
        LOD 100

        Pass
        {
            Name "LavaPass"
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
                float fogCoord : TEXCOORD1;
            };

            // Declaración de propiedades
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_NoiseTex);
            SAMPLER(sampler_NoiseTex);
            float _DistortionStrength;
            float _FlowSpeed;
            float4 _EmissionColor;
            float _EmissionIntensity;

            Varyings vert (Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.fogCoord = ComputeFogFactor(output.positionCS.z);
                return output;
            }

            half4 frag (Varyings input) : SV_Target
            {
                // Animar las UV del ruido con el tiempo
                float2 noiseUV = input.uv + _Time.y * _FlowSpeed;
                half4 noise = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, noiseUV);

                // Crear vector de distorsión (rango [-1, 1])
                float2 distortion = (noise.rg * 2.0 - 1.0) * _DistortionStrength;

                // Aplicar distorsión a las UV originales
                float2 distortedUV = input.uv + distortion;

                // Muestrear la textura base con las UV distorsionadas
                half4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, distortedUV);

                // Añadir emisión para el brillo
                half4 emission = color * _EmissionColor * _EmissionIntensity;

                // Combinar color y emisión
                half4 finalColor = color + emission;

                // Aplicar niebla para integración con la escena
                finalColor.rgb = MixFog(finalColor.rgb, input.fogCoord);

                return finalColor;
            }
            ENDHLSL
        }
    }
}