#ifndef CUSTOM_SHADOW_CASTER_PASS_INCLUDED
#define CUSTOM_SHADOW_CASTER_PASS_INCLUDED

//#include "../ShaderLibrary/Common.hlsl"

//TEXTURE2D(_MainTex);
//SAMPLER(sampler_MainTex);

//UNITY_INSTANCING_BUFFER_START(UnityPerMaterial)
//	UNITY_DEFINE_INSTANCED_PROP(float4, _MainTex_ST)
//	UNITY_DEFINE_INSTANCED_PROP(float4, _BaseColor)
//	UNITY_DEFINE_INSTANCED_PROP(float, _Cutoff)
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

bool _ShadowPancaking;

Varyings ShadowCasterPassVertex(Attributes input)
{
	Varyings output;
	UNITY_SETUP_INSTANCE_ID(input);
	UNITY_TRANSFER_INSTANCE_ID(input, output);
	output.positionCS = TransformWorldToHClip(TransformObjectToWorld(input.positionOS));
	if (_ShadowPancaking)
	{
		#if UNITY_REVERSED_Z
			output.positionCS.z = min(output.positionCS.z, output.positionCS.w * UNITY_NEAR_CLIP_VALUE);
		#else
			output.positionCS.z = max(output.positionCS.z, output.positionCS.w * UNITY_NEAR_CLIP_VALUE);
		#endif
	}
	//float4 mainTexST = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _MainTex_ST);
	//output.baseUV = input.baseUV * mainTexST.xy + mainTexST.zw;
    output.baseUV = TransformBaseUV(input.baseUV);
	return output;
}


void ShadowCasterPassFragment(Varyings input)// : SV_TARGET
{
    UNITY_SETUP_INSTANCE_ID(input);
    InputConfig inputConfig = GetInputConfig(input.positionCS, input.baseUV);
    ClipLOD(inputConfig.fragment, unity_LODFade.x);

	//float4 baseColor = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _BaseColor);
	//float4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.baseUV);
	//float4 finalColor = baseColor * texColor;
    float4 base = GetBase(inputConfig);

#if defined(_SHADOWS_CLIP)
	//clip(finalColor.a - UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Cutoff));
	clip(base.a - GetCutoff());
#elif defined(_SHADOWS_DITHER)
	float dither = InterleavedGradientNoise(input.positionCS.xy, 0);
	//clip(finalColor.a - dither);
	clip(base.a - dither);
#endif

	//return finalColor;
}

#endif
