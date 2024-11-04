Shader "Nirmax_Test/Coin"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _GoldTex ("Gold Texture", 2D) = "white"{}
        [Space(10)]
        _Range ("Gold Range", Range(1, 5)) = 1
        _Speed ("Gold Speed", Range(-1, 1)) = 0
        _Brightness ("Gold Brightness", Range(0.0, 0.5)) = 0.1
        _Saturation ("Gold Saturation", Range(0.5, 1)) = 0.5
        [Space(10)]
        _Color ("Rim Color", Color) = (1, 1, 1, 1)
        _Rim ("Rim Effect", Range(0, 1)) = 1
    }
    SubShader
    {
        // TEXTURE PASS
        Pass
        {
            Tags
            {
                "Queue"= "Geometry"
            }            

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float4 uvv : TEXCOORD1;
            };

            sampler2D _MainTex;
            sampler2D _GoldTex;
            float4 _MainTex_ST;
            float _Range;
            float _Speed;
            float _Brightness;
            float _Saturation;

            v2f vert (appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.uvv = ComputeScreenPos(o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {                
                float2 coords = i.uvv.xy / i.uvv.w;
                coords.x += _Time * _Speed;

                fixed4 gol = tex2D(_GoldTex, coords * _Range);
                fixed4 col = tex2D(_MainTex, i.uv);

                col *= gol / _Saturation;

                return col + _Brightness;
            }
            ENDCG
        }

        // RIM PASS
        Pass
        {
            Tags
            {
                "Queue"= "Transparent"
            }

            ZWrite Off
            Blend One One

            CGPROGRAM
            #pragma vertex vertexShader
            #pragma fragment fragmentShader
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"

            struct vertexInput
            {
                float4 vertex : POSITION;
                float4 normal : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct vertexOutput
            {
                float4 pos : SV_POSITION;
                float3 normal : NORMAL;
                float3 uv : TEXCOORD0;
            };

            float4 _Color;
            float _Rim;

            vertexOutput vertexShader(vertexInput i)
            {
                vertexOutput o;
                UNITY_SETUP_INSTANCE_ID(i);
                o.pos = UnityObjectToClipPos(i.vertex);
                o.normal = normalize(mul((float3x3)unity_ObjectToWorld, i.normal.xyz));
                o.uv = normalize(_WorldSpaceCameraPos - mul((float3x3)unity_ObjectToWorld, i.vertex.xyz));
                return o;
            }

            float rimEffect(float3 uv, float3 normal)
            {
                float rim = 1 - abs(dot(uv, normal)) * _Rim;
                return rim;
            }

            fixed4 fragmentShader(vertexOutput o) : Color
            {
                fixed rimColor = rimEffect(o.uv, o.normal.xyz);
                return _Color * rimColor * rimColor;
            }
            ENDCG

        }
    }
}
