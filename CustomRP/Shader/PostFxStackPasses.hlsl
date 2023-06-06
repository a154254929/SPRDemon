#ifndef CUSTOM_POST_FX_PASS_INCLUDED
#define CUSTOM_POST_FX_PASS_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Filtering.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
struct Varyings
{
	float4 positionCS : SV_POSITION;
	float2 screenUV : VAR_SCREEN_UV;
};

TEXTURE2D(_PostFXSource);
SAMPLER(sampler_linear_clamp);
SAMPLER(sampler_point_clamp);
float4 _PostFXSource_TexelSize;
TEXTURE2D(_PostFXSource2);

float _BloomIntensity;
float4 _BloomThreshold;

float4 _ColorAdjustments;
float4 _ColorFilter;

float4 _WhiteBalance;

float4 _SplitToningShadows;
float4 _SplitToningHighlights;

float4 _ChannelMixerRed;
float4 _ChannelMixerGreen;
float4 _ChannelMixerBlue;

float4 _SMHShadows;
float4 _SMHMidtones;
float4 _SMHHighlights;
float4 _SMHRange;

float4 _ColorGradingLUTParameters;
float _ColorGradingLUTInLohC;
TEXTURE2D(_ColorGradingLUT);

float Luminance(float3 color, bool useACES)
{
	return useACES ? AcesLuminance(color) : Luminance(color);
}

float3 ApplyBloomThreshold(float3 color)
{
	//float brightness = Max3(color.r, color.g, color.b);
	//float brightness = 0.2125 * color.r + 0.7154 * color.g + 0.0721 * color.b;
	float brightness = Luminance(color);
	float soft = brightness + _BloomThreshold.y;
	soft = clamp(soft, 0.0, _BloomThreshold.z);
	soft = soft * soft * _BloomThreshold.w;
	float contribute = max(soft, brightness - _BloomThreshold.x);
	contribute /= max(brightness, 0.00001);
	return color * contribute;
}

float3 ColorGradePostExposure(float3 color)
{
	return color * _ColorAdjustments.x;
}

float3 ColorGradeConstrast(float3 color, bool useACES)
{
	color = useACES ? ACES_to_ACEScc(unity_to_ACES(color)) : LinearToLogC(color);
	color = (color - ACEScc_MIDGRAY) * _ColorAdjustments.y + ACEScc_MIDGRAY;
	return useACES ? ACES_to_ACEScg(ACEScc_to_ACES(color)) : LogCToLinear(color);
}

float3 ColorGradeColorFilter(float3 color)
{
	return color * _ColorFilter.rgb;
}

float3 ColorGradeHueShift(float3 color)
{
	color = RgbToHsv(color);
	float hue = color.x + _ColorAdjustments.z;
	color.x = RotateHue(hue, 0.0, 1.0);
	return HsvToRgb(color);
}

float3 ColorGradeSaturation(float3 color, bool useACES)
{
	float luminance = Luminance(color, useACES);
	return (color - luminance) * _ColorAdjustments.w + luminance;
}

float3 ColorGradeWhiteBalance(float3 color)
{
	//color = LinearToLMS(color);
	//color *= _WhiteBalance.rgb;
	//return LMSToLinear(color);
	color *= LMSToLinear( _WhiteBalance.rgb);
	return color;
}

//色调分离
float3 ColorGradeSplitToning(float3 color, bool useACES)
{
	color = PositivePow(color, 0.45);
	float t = saturate(Luminance(saturate(color), useACES) + _SplitToningShadows.w);
	float3 shadows = lerp(0.5, _SplitToningShadows.rgb, 1.0 - t);
	float3 highlights = lerp(0.5, _SplitToningHighlights.rgb, t);
	color = SoftLight(color, shadows);
	color = SoftLight(color, highlights);
	return PositivePow(color, 2.2);
}

float3 ColorGradeChannelMixer(float3 color)
{
	return mul(float3x3(_ChannelMixerRed.rgb, _ChannelMixerGreen.rgb, _ChannelMixerBlue.rgb), color);
}

float3 ColorGradeShadowsMidtonesHighlights(float3 color, bool useACES)
{
	float luminance = Luminance(color, useACES);
	float shadowsWeight = 1.0 - smoothstep(_SMHRange.x, _SMHRange.y, luminance);
	float highlightsWeight = smoothstep(_SMHRange.z, _SMHRange.w, luminance);
	float midtonesWeight = 1.0 - shadowsWeight - highlightsWeight;
	return color * (
		_SMHShadows.rgb * shadowsWeight +
		_SMHMidtones.rgb * midtonesWeight +
		_SMHHighlights.rgb * highlightsWeight 
	);
}

//颜色分级
float3 ColorGrade(float3 color, bool useACES = false)
{
	//color = min(color, 60.0);
	color = ColorGradePostExposure(color);
	color = ColorGradeWhiteBalance(color);
	color = ColorGradeConstrast(color, useACES);
	color = ColorGradeColorFilter(color);
	color = min(color, 60.0);
	color = ColorGradeSplitToning(color, useACES);
	color = ColorGradeChannelMixer(color);
	color = min(color, 60.0);
	color = ColorGradeShadowsMidtonesHighlights(color, useACES);
	color = ColorGradeHueShift(color);
	color = ColorGradeSaturation(color, useACES);
	//消除负值
	color = max(color, 0.0);
	color = ColorGradeShadowsMidtonesHighlights(color, useACES);
	return max(useACES ? ACEScg_to_ACES(color) : color, 0.0);
}

float3 GetColorGraderLUT(float2 uv, bool useACES = false)
{
	float3 color = GetLutStripValue(uv, _ColorGradingLUTParameters);
	return ColorGrade(_ColorGradingLUTInLohC ? LogCToLinear(color) : color, useACES);
}

float3 ApplyColorGradingLUT(float3 color)
{
	return ApplyLut2D(
		TEXTURE2D_ARGS(
			_ColorGradingLUT,
			sampler_linear_clamp
			//sampler_point_clamp
		),
		saturate(_ColorGradingLUTInLohC ? LinearToLogC(color) : color),
		_ColorGradingLUTParameters.xyz
	);
}

Varyings DefaultPassVertex (uint vertexID : SV_VertexID)
{
	Varyings output;
	output.positionCS = float4(
		vertexID == 2 ? 3.0 : -1.0,
		vertexID == 1 ? 3.0 : -1.0,
		0,
		1
	);
	output.screenUV = float2(
		vertexID == 2 ? 2.0 : 0.0,
		vertexID == 1 ? 2.0 : 0.0
	);
	if (_ProjectionParams.x < 0.0)
	{
		output.screenUV.y = 1.0 - output.screenUV.y;
	}
	return output;
}

float4 GetSource(float2 screenUV)
{
	return SAMPLE_TEXTURE2D_LOD(_PostFXSource, sampler_linear_clamp, screenUV, 0);
}

float4 GetSource2(float2 screenUV)
{
	return SAMPLE_TEXTURE2D_LOD(_PostFXSource2, sampler_linear_clamp, screenUV, 0);
}

float4 GetSourceBicubic(float2 screenUV)
{
	return SampleTexture2DBicubic(
		TEXTURE2D_ARGS(_PostFXSource, sampler_linear_clamp),
		screenUV,
		_PostFXSource_TexelSize.zwxy,
		1.0,
		0.0
	);
}

float4 CpoyPassFragment(Varyings input) : SV_TARGET
{
	return GetSource(input.screenUV);
}

float4 GetSourceTexelSize()
{
	return _PostFXSource_TexelSize;
}

//float4 BloomHorizontalPassFragment(Varyings input) : SV_TARGET
//{
//	float3 color = 0.0;
//	float offsets[] = {-4.0, -3.0, -2.0, -1.0, 0.0, 1.0, 2.0, 3.0, 4.0};
//	float weights[] = 
//	{
//		0.01621622, 0.05405405, 0.12162162, 0.19459459, 0.22702703,
//		0.19459459, 0.12162162, 0.05405405, 0.01621622
//	};
//	for(int i = 0; i < 9; ++i)
//	{
//		float offset = offsets[i] * 2.0 * GetSourceTexelSize().x;
//		color += GetSource(input.screenUV + float2(offset, 0.0)).rgb * weights[i];
//	}
//	return float4(color, 1.0);
//}

float4 BloomHorizontalPassFragment(Varyings input) : SV_TARGET
{
	float3 color = 0.0;
	float offsets[] = { 
		-3.23076923, -1.38461538, 0.0, 1.38461538, 3.23076923
	};
	float weights[] = 
	{
		0.07027027, 0.31621622, 0.22702703, 0.31621622, 0.07027027
	};
	for(int i = 0; i < 5; ++i)
	{
		float offset = offsets[i] * 2.0 * GetSourceTexelSize().x;
		color += GetSource(input.screenUV + float2(offset, 0.0)).rgb * weights[i];
	}
	return float4(color, 1.0);
}

//float4 BloomVerticalPassFragment(Varyings input) : SV_TARGET
//{
//	float3 color = 0.0;
//	float offsets[] = {-4.0, -3.0, -2.0, -1.0, 0.0, 1.0, 2.0, 3.0, 4.0};
//	float weights[] = 
//	{
//		0.01621622, 0.05405405, 0.12162162, 0.19459459, 0.22702703,
//		0.19459459, 0.12162162, 0.05405405, 0.01621622
//	};
//	for(int i = 0; i < 9; ++i)
//	{
//		float offset = offsets[i] * GetSourceTexelSize().y;
//		color += GetSource(input.screenUV + float2(0.0, offset)).rgb * weights[i];
//	}
//	return float4(color, 1.0);
//}

float4 BloomVerticalPassFragment(Varyings input) : SV_TARGET
{
	float3 color = 0.0;
	float offsets[] = { 
		-3.23076923, -1.38461538, 0.0, 1.38461538, 3.23076923
	};
	float weights[] = 
	{
		0.07027027, 0.31621622, 0.22702703, 0.31621622, 0.07027027
	};
	for(int i = 0; i < 5; ++i)
	{
		float offset = offsets[i] * GetSourceTexelSize().y;
		color += GetSource(input.screenUV + float2(0.0, offset)).rgb * weights[i];
	}
	return float4(color, 1.0);
}

bool _BloomBicubicUpsampling;
float4 BloomAddPassFragment(Varyings input) : SV_TARGET
{
	float3 lowRes = _BloomBicubicUpsampling ? GetSourceBicubic(input.screenUV).rgb : GetSource(input.screenUV).rgb;
	float4 highRes = GetSource2(input.screenUV);
	return float4(lowRes * _BloomIntensity + highRes, highRes.a);
}

float4 BloomScatterPassFragment(Varyings input) : SV_TARGET
{
	float3 lowRes = _BloomBicubicUpsampling ? GetSourceBicubic(input.screenUV).rgb : GetSource(input.screenUV).rgb;
	float3 highRes = GetSource2(input.screenUV);
	return float4(lerp(highRes, lowRes, _BloomIntensity), 1.0);
}

float4 BloomScatterFinalPassFragment(Varyings input) : SV_TARGET
{
	float3 lowRes = _BloomBicubicUpsampling ? GetSourceBicubic(input.screenUV).rgb : GetSource(input.screenUV).rgb;
	float4 highRes = GetSource2(input.screenUV);
	lowRes += highRes.rgb - ApplyBloomThreshold(highRes.rgb);
	return float4(lerp(highRes, lowRes, _BloomIntensity), highRes.a);
}

float4 BloomPrefilterPassFragment(Varyings input) : SV_TARGET
{
	float3 color = ApplyBloomThreshold(GetSource(input.screenUV).rgb);
	return float4(color, 1.0);
}

float4 BloomPrefilterFirefilesPassFragment(Varyings input) : SV_TARGET
{
	float3 color = 0.0;
	float weightSum = 0.0;
	float2 offsets[] = 
	{
		float2(-1, -1),					float2(1, -1),
						float2(0, 0),
		float2(-1, 1),					float2(1, 1),
	};
	for(int i = 0; i < 5; ++i)
	{
		float3 c = ApplyBloomThreshold(GetSource(input.screenUV + offsets[i] * GetSourceTexelSize().xy * 2.0).rgb);

		float w = 1.0 / (Luminance(c) + 1.0);
		color += c * w;
		weightSum += w;

	}
	color /= weightSum;
	return float4(color, 1.0);
	//return 1;
}

float4 ToneMappingNonePassFragment(Varyings input) : SV_TARGET
{
	float4 color = GetSource(input.screenUV);
	color.rgb = ColorGrade(color.rgb);
	return color;
}

float4 ToneMappingACESPassFragment(Varyings input) : SV_TARGET
{
	float4 color = GetSource(input.screenUV);
	color.rgb = ColorGrade(color.rgb, true);
	color.rgb = AcesTonemap(color.rgb);
	return color;
}

float4 ToneMappingNeutralPassFragment(Varyings input) : SV_TARGET
{
	float4 color = GetSource(input.screenUV);
	color.rgb = ColorGrade(color.rgb);
	color.rgb = NeutralTonemap(color.rgb);
	return color;
}

float4 ToneMappingReinhardPassFragment(Varyings input) : SV_TARGET
{
	float4 color = GetSource(input.screenUV);
	color.rgb = ColorGrade(color.rgb);
	color.rgb /= 1.0 + color.rgb;
	return color;
}

float4 ColorGradingNonePassFragment(Varyings input) : SV_TARGET
{
	float3 color = GetColorGraderLUT(input.screenUV);
	return float4(color ,1.0);
}

float4 ColorGradingReinhardPassFragment(Varyings input) : SV_TARGET
{
	float3 color = GetColorGraderLUT(input.screenUV);
	color /= color + 1.0;
	return float4(color ,1.0);
}

float4 ColorGradingNeutralPassFragment(Varyings input) : SV_TARGET
{
	float3 color = GetColorGraderLUT(input.screenUV);
	color = NeutralTonemap(color);
	return float4(color ,1.0);
}

float4 ColorGradingACESPassFragment(Varyings input) : SV_TARGET
{
	float3 color = GetColorGraderLUT(input.screenUV, true);
	color = AcesTonemap(color);
	return float4(color ,1.0);
}

float4 FinalPassFragment(Varyings input) : SV_TARGET
{
	float4 color = GetSource(input.screenUV);
	color.rgb = ApplyColorGradingLUT(color.rgb);
	return color;
}
#endif
