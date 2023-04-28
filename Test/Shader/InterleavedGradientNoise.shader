Shader "Unlit/InterleavedGradientNoise"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _BaseColor("Color", color) = (1, 1, 1, 1)
        _X("X", Range(0, 1)) = 0
        _Y("Y", Range(0, 1)) = 0
        _Z("Z", Range(0, 1)) = 0
    }
    SubShader
    {
        Tags {
            "RenderType" = "Opaque"
            "Queue" = "Transparent"
        }
        LOD 100

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment

            #include "../../CustomRP/ShaderLibrary/Common.hlsl"

            struct Attributes
            {
                float3 positionOS : POSITION;
                float2 baseUV : TEXCOORD0;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : VAR_POSITION;
                float2 baseUV : VAR_BASE_UV;
                float3 normalWS : VAR_NORMAL;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            half4 _BaseColor;
            float _X, _Y, _Z;

            Varyings Vertex(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                float4 mainTexST = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _MainTex_ST);
                output.baseUV = input.baseUV * mainTexST.xy + mainTexST.zw;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }


            float4 Fragment(Varyings input) : SV_TARGET
            {
                //return float4(_BaseColor.rgb, InterleavedGradientNoise(input.positionCS.xy, 0));
                return float4(_BaseColor.rgb, InterleavedGradientNoise(input.positionCS.xy / float2(_X, _Y), _Z));
            }
            ENDHLSL
        }
    }
}
