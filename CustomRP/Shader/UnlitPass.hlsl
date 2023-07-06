#ifndef CUSTOM_UNLIT_PASS_INCLUDED
#define CUSTOM_UNLIT_PASS_INCLUDED

//#include "../ShaderLibrary/Common.hlsl"

//CBUFFER_START(UnityPerMaterial)
//	float4 _BaseColor;
//CBUFFER_END

//UNITY_INSTANCING_BUFFER_START(UnityPerMaterial)
//UNITY_DEFINE_INSTANCED_PROP(float4, _BaseColor)
//UNITY_INSTANCING_BUFFER_END(UnityPerMaterial)

struct Attributes
{
	float3 positionOS : POSITION;
	#ifdef _FLIPBOOK_BLENDING
		float4 baseUV : TEXCOORD0;
		float flipbookBlend : TEXCOORD1;
	#else
		float2 baseUV : TEXCOORD0;
	#endif
	float4 color : COLOR;
	UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
	float4 positionCS_SS : SV_POSITION;
	float2 baseUV : VAR_BASE_UV;
	#ifdef _VERTEX_COLORS
		float4 color : VAR_COLOR;
	#endif
	#ifdef _FLIPBOOK_BLENDING
		float3 flipbookUVB : VAR_FLIPBOOK;
	#endif
	UNITY_VERTEX_INPUT_INSTANCE_ID
};

Varyings UnlitPassVertex(Attributes input)
{
	Varyings output;
	UNITY_SETUP_INSTANCE_ID(input);
	UNITY_TRANSFER_INSTANCE_ID(input, output);
	float3 positionWS = TransformObjectToWorld(input.positionOS);
	output.positionCS_SS = TransformWorldToHClip(positionWS);
	output.baseUV.xy = TransformBaseUV(input.baseUV.xy);
	#ifdef _VERTEX_COLORS
		output.color = input.color;
	#endif
	#ifdef _FLIPBOOK_BLENDING
		output.flipbookUVB.xy = TransformBaseUV(input.baseUV.zw);
		output.flipbookUVB.z =input.flipbookBlend;
	#endif
	return output;
}


float4 UnlitPassFragment(Varyings input) : SV_TARGET
{
	UNITY_SETUP_INSTANCE_ID(input);
	InputConfig inputconfig = GetInputConfig(input.positionCS_SS, input.baseUV);
	//return float4(inputconfig.fragment.bufferDepth.xxx / 20.0, 1.0);
	#ifdef _VERTEX_COLORS
		inputconfig.color = input.color;
	#endif
	#ifdef _FLIPBOOK_BLENDING
		inputconfig.flipbookUVB = input.flipbookUVB;
		inputconfig.flipbookBlending = true;
	#endif
	#ifdef _NEAR_FADE
		inputconfig.nearFade = true;
	#endif
	#ifdef _SOFT_PARTICLES
		inputconfig.softParticles = true;
	#endif
	float4 base = GetBase(inputconfig);
	#ifdef _CLIPPING
		clip(base.a - GetCutoff(inputconfig));
	#endif
	#ifdef _DISTORTION
		float2 distortion = GetDistortion(inputconfig) * base.a;
		base.rgb = lerp(GetBufferColor(inputconfig.fragment, distortion).rgb, base.rgb, saturate(base.a - GetDistortionBlend(inputconfig)));
	#endif
	return float4(base.rgb, GetFinalAlpha(base.a));
}

#endif
