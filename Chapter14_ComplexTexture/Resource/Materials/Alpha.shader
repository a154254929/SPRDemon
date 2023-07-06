Shader "CustomRP/Alpha"
{
    Properties
    {
        _MainTex("Main Texture", 2D) = "white" {}
    }
    SubShader
    {
        Cull Off
        HLSLINCLUDE
        #include "../../../CustomRP/ShaderLibrary/Common.hlsl"
        ENDHLSL
        Pass
        {
            HLSLPROGRAM
            #pragma target 3.5
            #pragma multi_compile_instancing 
            #pragma vertex ScrollVertex
            #pragma fragment ScrollFragment
            struct Attributes
            {
	            float3 positionOS : POSITION;
	            float2 baseUV : TEXCOORD0;
	            float3 normalOS : NORMAL;
	            UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
	            float4 positionCS : SV_POSITION;
	            float3 positionWS : VAR_POSITION;
	            float2 baseUV : VAR_BASE_UV;
                float3 normalWS : VAR_NORMAL;
                float3 positionOS : TEXCOORD0;
	            UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            sampler2D _MainTex;
            float _Progress;
            float _HalfWidth;
            float _MinRadius;
            float _MaxRadius;
            float _RollSmooth;
            float _Angle;

            Varyings ScrollVertex(Attributes input)
            {
	            Varyings output;
	            UNITY_SETUP_INSTANCE_ID(input);
	            UNITY_TRANSFER_INSTANCE_ID(input, output);
                float maxRange = 1 - (_MaxRadius + (270 / _Angle) * (_MinRadius - _MaxRadius)/ _RollSmooth) / _HalfWidth;
                float rag = clamp(_Progress, 0, maxRange);
                float length = rag * _HalfWidth;
                float3 positionOS = input.positionOS;
	            output.positionOS = positionOS;
	            output.positionWS = TransformObjectToWorld(positionOS);
	            output.positionCS = TransformWorldToHClip(output.positionWS);
                output.baseUV = input.baseUV;
	            output.normalWS = TransformObjectToWorldNormal(input.normalOS);
	            return output;
            }


            float4 ScrollFragment(Varyings input) : SV_TARGET
            {
	            UNITY_SETUP_INSTANCE_ID(input);
                float4 color = tex2D(_MainTex, input.baseUV);

	            return color.a;
	            //return abs(input.positionOS.x) / 5.0;
            }
            ENDHLSL
        }
    }
}
