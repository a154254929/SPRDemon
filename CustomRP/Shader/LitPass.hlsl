#ifndef CUSTOM_LIT_PASS_INCLUDED
#define CUSTOM_LIT_PASS_INCLUDED

//#include "../ShaderLibrary/Common.hlsl"
#include "../ShaderLibrary/Surface.hlsl"
#include "../ShaderLibrary/Shadow.hlsl"
#include "../ShaderLibrary/Light.hlsl"
#include "../ShaderLibrary/BRDF.hlsl"
#include "../ShaderLibrary/GI.hlsl"
#include "../ShaderLibrary/Lighting.hlsl"

//CBUFFER_START(UnityPerMaterial)
//	float4 _BaseColor;
//CBUFFER_END
 
//TEXTURE2D(_MainTex);
//SAMPLER(sampler_MainTex);

//UNITY_INSTANCING_BUFFER_START(UnityPerMaterial)
//	UNITY_DEFINE_INSTANCED_PROP(float4, _MainTex_ST)
//	UNITY_DEFINE_INSTANCED_PROP(float4, _BaseColor)
//	UNITY_DEFINE_INSTANCED_PROP(float, _Cutoff)
//	UNITY_DEFINE_INSTANCED_PROP(float, _Metallic)
//	UNITY_DEFINE_INSTANCED_PROP(float, _Smoothness)
//UNITY_INSTANCING_BUFFER_END(UnityPerMaterial)

struct Attributes
{
	float3 positionOS : POSITION;
	float2 baseUV : TEXCOORD0;
	float3 normalOS : NORMAL;
	float4 tangentOS : TANGENT;
	GI_ATTRIBUTE_DATA
	UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
	float4 positionCS_SS : SV_POSITION;
	float3 positionWS : VAR_POSITION;
	float2 baseUV : VAR_BASE_UV;
	#ifdef _DETAIL_MAP 
		float2 detailUV : VAR_DETAIL_UV;
	#endif
    float3 normalWS : VAR_NORMAL;
	#ifdef _NORMAL_MAP
		float4 tangentWS : VAR_TANGENT;
	#endif
    GI_VARYINGS_DATA
	UNITY_VERTEX_INPUT_INSTANCE_ID
};

Varyings LitPassVertex(Attributes input)
{
	Varyings output;
	UNITY_SETUP_INSTANCE_ID(input);
	UNITY_TRANSFER_INSTANCE_ID(input, output);
    TRANSFER_GI_DATA(input, output);
	output.positionWS = TransformObjectToWorld(input.positionOS);
	output.positionCS_SS = TransformWorldToHClip(output.positionWS);
	//float4 mainTexST = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _MainTex_ST);
	//output.baseUV = input.baseUV * mainTexST.xy + mainTexST.zw;
    output.baseUV = TransformBaseUV(input.baseUV);
	#ifdef _DETAIL_MAP 
		output.detailUV = TransformDetailUV(input.baseUV);
	#endif
	output.normalWS = TransformObjectToWorldNormal(input.normalOS);
	#ifdef _NORMAL_MAP
		output.tangentWS = float4(TransformObjectToWorldDir(input.tangentOS.xyz), input.tangentOS.w);
	#endif
	return output;
}


float4 LitPassFragment(Varyings input) : SV_TARGET
{
	UNITY_SETUP_INSTANCE_ID(input);

	//#ifdef LOD_FADE_CROSSFADE
	//	return unity_LODFade.x;
	//#endif
	InputConfig inputconfig = GetInputConfig(input.positionCS_SS, input.baseUV);
    ClipLOD(inputconfig.fragment, unity_LODFade.x);
	#ifdef _MASK_MAP 
		inputconfig.useMask = true;
	#endif
	#ifdef _DETAIL_MAP 
		inputconfig.useDetail = true;
		inputconfig.detailUV = input.detailUV;
	#endif
	
	//float4 baseColor = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _BaseColor);
	//baseColor *= SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.baseUV);
    float4 base = GetBase(inputconfig);

	#ifdef _CLIPPING
		//clip(baseColor.alpha - UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Cutoff));
		clip(base.a - GetCutoff());
	#endif
	
	Surface surface;
	surface.position = input.positionWS;
	//surface.normal = normalize(input.normalWS);
	#ifdef _NORMAL_MAP
		surface.normal = NormalTangentToWorld(GetNormalTS(inputconfig), input.normalWS, input.tangentWS);
		surface.interpolateNormal = input.normalWS;
	#else
		surface.normal = normalize(input.normalWS);
		surface.interpolateNormal = surface.normal;
	#endif
	//surface.color = baseColor.rgb;
	//surface.alpha = baseColor.a;
    surface.color = base.rgb;
    surface.alpha = base.a;
    surface.metallic = GetMetallic(inputconfig);
    surface.smoothness = GetSmoothness(inputconfig);
	surface.viewDirection = normalize(_WorldSpaceCameraPos - input.positionWS);
    surface.depth = -TransformWorldToView(input.positionWS).z;
    surface.dither = InterleavedGradientNoise(input.positionCS_SS.xy, 0);
	surface.renderingLayerMask = asuint(unity_RenderingLayer.x);
    surface.fresnelStrength = GetFresnel();
    surface.occlusion = GetOcclusion(inputconfig);
#ifdef _PREMULTIPLY_ALPHA
	BRDF brdf = GetBRDF(surface, true);
#else
	BRDF brdf = GetBRDF(surface);
#endif
	//获取全局照明数据
    GI gi = GetGI(GI_FRAGMENT_DATA(input), surface, brdf);
    float3 finalColor = GetLighting(surface, brdf, gi);
    finalColor += GetEmission(input.baseUV);

	//return float4(surface.normal, GetFinalAlpha(surface.alpha));
	return float4(finalColor, GetFinalAlpha(surface.alpha));
}

#endif
