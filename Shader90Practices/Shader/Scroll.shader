Shader "CustomRP/Scroll"
{
    Properties
    {
        _MainTex("Main Texture", 2D) = "white" {}
        _BaseColor("Color", Color) = (0.5, 0.5, 0.5, 1.0)
        _Progress("Progress", Range(0, 1)) = 0
        _HalfWidth("Half Width", float) = 1
        _MinRadius("Min Radius", float) = 0.01
        _MaxRadius("Max Radius", float) = 0.1
        _RollSmooth("Roll Smooth", Range(1, 10)) = 2
        _Angle("Angle", Range(0, 1080)) = 720
    }
    SubShader
    {
        Cull Off
        HLSLINCLUDE
        #include "../../CustomRP/ShaderLibrary/Common.hlsl"
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
                float positionOSX = abs(positionOS.x);
                float centerX = _HalfWidth - length;
                if (positionOSX > centerX)
                {
                    //方向
                    float dir = sign(positionOS.x);
                    //从哪里开始卷
                    //float rollMask = abs(positionOSX - centerX) / _HalfWidth;
                    float rollMask = positionOSX / _HalfWidth - 1 + rag;
                    float rollAngle = (rollMask * _Angle - 90) * acos(-1) / 180.0;
                    //半径
                    float rollRadius = _MaxRadius + rollMask * (_MinRadius - _MaxRadius)/ _RollSmooth;
                    float3 positionRoll = 0;
                    positionRoll.x = cos(rollAngle) * dir * rollRadius;
                    positionRoll.y = sin(rollAngle) * rollRadius;
                    positionRoll.z = positionOS.z;
                    half3 rollCenter = half3(centerX * dir, _MaxRadius, 0);
                    positionRoll += rollCenter;
                    positionOS = positionRoll;
                }
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

	            return color;
	            //return abs(input.positionOS.x) / 5.0;
            }
            ENDHLSL
        }
    }
}
