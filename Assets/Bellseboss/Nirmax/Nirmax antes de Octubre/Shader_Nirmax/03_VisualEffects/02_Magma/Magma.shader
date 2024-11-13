Shader "Nirmax_Test/Magma"
{
    Properties
    {
        _RockTex ("Rock Texture", 2D) = "white"{}
        _MagmaTex ("Magma Texture", 2D) = "white"{}
        _DisTex ("Distortion Texture", 2D) = "white"{}
        [Space(10)]
        _DisValue ("Distortion Value", Range(2, 10)) = 3
        _DisSpeed ("Distortion Speed", Range(-0.4, 0.4)) = 0.1
        [Space(10)]
        _WaveSpeed ("Wave Speed", Range(0, 5)) = 1
        _WaveFrequency ("Wave Frequency", Range(0, 5)) = 1
        _WaveAmplitude ("Wave Amplitude", Range(0, 1)) = 0.2
    }
    SubShader
    {       
        // MAGMA PASS ----------------------------------
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
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MagmaTex;
            sampler2D _DisTex;
            float4 _MagmaTex_ST;

            float _DisValue;
            float _DisSpeed;

            float _WaveSpeed;
            float _WaveFrequency;
            float _WaveAmplitude;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);

                float3 worldPos = mul(unity_ObjectToWorld, v.vertex);
                o.vertex.y += sin((-worldPos.z + (_Time.y * _WaveSpeed)) * _WaveFrequency) * _WaveAmplitude;
                o.vertex.y += cos((-worldPos.x + (_Time.y * _WaveSpeed)) * _WaveFrequency) * _WaveAmplitude;

                o.uv = TRANSFORM_TEX(v.uv, _MagmaTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {       
                half distortion = tex2D(_DisTex, i.uv + (_Time * _DisSpeed)).r;
                i.uv.x += distortion / _DisValue;
                i.uv.y += distortion / _DisValue;

                fixed4 col = tex2D(_MagmaTex, i.uv);
                return col;
            }
            ENDCG
        }

        // ROCK PASS ----------------------------------
        Pass
        {
            Tags
            {
                "Queue"="Transparent"
            }

            Blend SrcAlpha OneMinusSrcAlpha

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
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _RockTex;
            float4 _RockTex_ST;

            float _WaveSpeed;
            float _WaveFrequency;
            float _WaveAmplitude;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);   

                float3 worldPos = mul(unity_ObjectToWorld, v.vertex);
                o.vertex.y += sin((-worldPos.z + (_Time.y * _WaveSpeed)) * _WaveFrequency) * _WaveAmplitude;
                o.vertex.y += cos((-worldPos.x + (_Time.y * _WaveSpeed)) * _WaveFrequency) * _WaveAmplitude;
                   
                o.uv = TRANSFORM_TEX(v.uv, _RockTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {               
                fixed4 col = tex2D(_RockTex, i.uv);
                return col;
            }
            ENDCG
        }
    }
}
