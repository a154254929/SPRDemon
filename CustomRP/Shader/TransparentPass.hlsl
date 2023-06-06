#ifndef CUSTOM_TRANSPARENT_PASS_INCLUDED
#define CUSTOM_TRANSPARENT_PASS_INCLUDED

//#include "../ShaderLibrary/Common.hlsl"

//CBUFFER_START(UnityPerMaterial)
//	float4 _BaseColor;
//CBUFFER_END
 
//TEXTURE2D(_MainTex);
//SAMPLER(sampler_MainTex);

//UNITY_INSTANCING_BUFFER_START(UnityPerMaterial)
//UNITY_DEFINE_INSTANCED_PROP(float4, _MainTex_ST)
//UNITY_DEFINE_INSTANCED_PROP(float4, _BaseColor)
//UNITY_DEFINE_INSTANCED_PROP(float, _Cutoff)
//UNITY_INSTANCING_BUFFER_END(UnityPerMaterial)

struct Attributes
{
	float3 positionOS : POSITION;
	float2 baseUV : TEXCOORD0;
	UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
	float4 positionCS : SV_POSITION;
	float2 baseUV : VAR_BASE_UV;
	UNITY_VERTEX_INPUT_INSTANCE_ID
};

Varyings TransparentPassVertex(Attributes input)
{
	Varyings output;
	UNITY_SETUP_INSTANCE_ID(input);
	UNITY_TRANSFER_INSTANCE_ID(input, output);
	float3 positionWS = TransformObjectToWorld(input.positionOS);
	output.positionCS = TransformWorldToHClip(positionWS);
	float4 mainTexST = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _MainTex_ST);
	output.baseUV = input.baseUV * mainTexST.xy + mainTexST.zw;
	return output;
}


float4 TransparentPassFragment(Varyings input) : SV_TARGET
{
	UNITY_SETUP_INSTANCE_ID(input);
	float4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.baseUV);
	float4 baseColor = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _BaseColor);
	float4 finalColor = texColor * baseColor;
	#ifdef _CLIPPING
		clip(finalColor.a - UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Cutoff));
	#endif
	return float4(finalColor.rgb, GetFinalAlpha(finalColor.a));
}

#endif
