//阴影采样
#ifndef CUSTOM_SHADOW_INCLUDEED
#define CUSTOM_SHADOW_INCLUDEED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Shadow/ShadowSamplingTent.hlsl"
//如果用的是PCF 3x3
#if defined _DIRECTIONAL_PCF3
    //需要4个滤波样本
    #define DIRECTIONAL_FILTER_SAMPLES 4
    #define DIRECTIONAL_FILTER_SETUP SampleShadow_ComputeSamples_Tent_3x3
#elif defined _DIRECTIONAL_PCF5
    #define DIRECTIONAL_FILTER_SAMPLES 9
    #define DIRECTIONAL_FILTER_SETUP SampleShadow_ComputeSamples_Tent_5x5
#elif defined _DIRECTIONAL_PCF7
    #define DIRECTIONAL_FILTER_SAMPLES 16
    #define DIRECTIONAL_FILTER_SETUP SampleShadow_ComputeSamples_Tent_7x7
#endif


#define MAX_DIRECTIONAL_LIGHT_COUNT 4
#define MAX_CASCADE_COUNT 4
//阴影图集
TEXTURE2D(_DirectionalShadowAtlas);
#define SHADOW_SAMPLER sampler_linear_clamp_compare
SAMPLER_CMP(SHADOW_SAMPLER);

CBUFFER_START(_CustomShadows)
    //级联数量和包围球数据
    int _CascadeCount;
    float4 _CascadeCullingSpheres[MAX_CASCADE_COUNT];
    //级联数据
    float4 _CascadeData[MAX_CASCADE_COUNT];
    //阴影转换矩阵
    float4x4 _DirectionalShadowMatrices[MAX_DIRECTIONAL_LIGHT_COUNT * MAX_CASCADE_COUNT];
    //float _ShadowDistance;
    //阴影过度距离
    float4 _ShadowDistanceFade;
    //阴影贴图大小
    float4 _ShadowAtlasSize;
CBUFFER_END

//阴影的数据信息
struct DirectionalShadowData
{
	float strength;
    int tileIndex;
    //法线偏差
    float normalBias;
};

//烘培阴影数据
struct ShadowMask
{
    bool distance;
    float4 shadows;
};

//阴影数据
struct ShadowData
{
    int cascadeIndex;
    //是否采样阴影的标识
    float strength;
    //混合级联
    float cascadeBlend;
    ShadowMask shadowMask;
};

//公式计算阴影过度的强度
float FadeShadowStrength(float distance, float scale, float fade)
{
    return saturate((1.0 - distance * scale) * fade);
}

//得到世界空间的表面阴影数据
ShadowData GetShadowData(Surface surfaceWS)
{
    ShadowData data;
    data.cascadeBlend = 1.0;
    data.strength = FadeShadowStrength(surfaceWS.depth, _ShadowDistanceFade.x, _ShadowDistanceFade.y);
    //data.strength = surfaceWS.depth < _ShadowDistance ? 1.0 : 0.0;
    data.shadowMask.distance = false;
    data.shadowMask.shadows = 1.0;
    int i;
    for (i = 0; i < _CascadeCount; ++i)
    {
        float4 sphere = _CascadeCullingSpheres[i];
        float disSqr = DistanceSquared(surfaceWS.position, sphere.xyz);
        if (disSqr < sphere.w)
        {
            float fade = FadeShadowStrength(disSqr, _CascadeData[i].x, _ShadowDistanceFade.z);
            if (i == _CascadeCount - 1)
            {
                data.strength *= fade;
            }
            else
            {
                data.cascadeBlend = fade;
            }
            break;
        }
    }
    if(i == _CascadeCount)
    {
        data.strength = 0.0;
    }
#ifdef _CASCADE_BLEND_DITHER
    else if(data.cascadeBlend < surfaceWS.dither)
    {
        i += 1;
    }
#endif
#ifndef _CASCADE_BLEND_SOFT
    data.cascadeBlend = 1.0;
#endif
    data.cascadeIndex = i;
    return data;
}

//采样阴影图集
float SampleDirectionalShadowAtlas(float3 positionSTS)
{
	return SAMPLE_TEXTURE2D_SHADOW(_DirectionalShadowAtlas, SHADOW_SAMPLER, positionSTS);
}

float FilterDirectionalShadow(float3 positionSTS)
{
#ifdef DIRECTIONAL_FILTER_SETUP
    //权重样本
    float weights[DIRECTIONAL_FILTER_SAMPLES];
    //样本位置
    float2 positions[DIRECTIONAL_FILTER_SAMPLES];
    float4 size = _ShadowAtlasSize.yyxx;
    DIRECTIONAL_FILTER_SETUP(size, positionSTS.xy, weights, positions);
    float shadow = 0;
    for (int i = 0; i < DIRECTIONAL_FILTER_SAMPLES; ++i)
    {
        //遍历所有样本得到权重和
        shadow += weights[i] * SampleDirectionalShadowAtlas(float3(positions[i], positionSTS.z)); 
    }
    return shadow;
#else
    return SampleDirectionalShadowAtlas(positionSTS);
#endif
}

float GetCascadedShadow(DirectionalShadowData directional, ShadowData global, Surface surfaceWS)
{
    //计算法线偏移
    float3 normalBias = surfaceWS.normal * (directional.normalBias * _CascadeData[global.cascadeIndex].y);
	//通过阴影转换矩阵和表面位置得到在阴影纹理（图块）空间的位置，然后对图集进行采样
    float3 positionSTS = mul(_DirectionalShadowMatrices[directional.tileIndex], float4(surfaceWS.position + normalBias, 1.0)).xyz;
    float shadow = FilterDirectionalShadow(positionSTS);
    //如果级联混合小于1代表再级联层级过度区域中，必须从下一个级联中采样并在两个指之间进行插值
    if (global.cascadeBlend < 1.0)
    {
        normalBias = surfaceWS.normal * (directional.normalBias * _CascadeData[global.cascadeIndex + 1].y);
        positionSTS = mul(_DirectionalShadowMatrices[directional.tileIndex + 1], float4(surfaceWS.position + normalBias, 1.0)).xyz;
        shadow = lerp(FilterDirectionalShadow(positionSTS), shadow, global.cascadeBlend);
    }
    return shadow;
}

//得到烘培阴影的衰减值
float GetBakedShadow(ShadowMask mask)
{
    float shadow = 1.0;
    if (mask.distance)
    {
        shadow = mask.shadows.r;
    }
    return shadow;
}

float GetBakedShadow(ShadowMask mask, float strength)
{
    float shadow = 1.0;
    if (mask.distance)
    {
        return lerp(1.0, GetBakedShadow(mask), strength);
    }
    return 1.0;
}

//混合烘培和实时阴影
float MixBakedAndRealtimeShadows(ShadowData global, float shadow, float strength)
{
    float baked = GetBakedShadow(global.shadowMask);
    if(global.shadowMask.distance)
    {
        shadow = lerp(baked, shadow, global.strength);
        return lerp(1.0, shadow, strength);
    }
    return lerp(1.0, shadow, strength * global.strength);
}

//计算阴影衰减
float GetDirectionalShadowAttenuation(DirectionalShadowData directional, ShadowData global, Surface surfaceWS)
{
#ifndef _RECEIVE_SHADOWS
    return 1.0;
#endif
    float shadow;
    if (directional.strength * global.strength <= 0.0)
    {
        shadow = GetBakedShadow(global.shadowMask, abs(directional.strength));
    }
    else
    {
        shadow = GetCascadedShadow(directional, global, surfaceWS);
        //shadow = lerp(1.0, shadow, directional.strength);
        //阴影混合
        shadow = MixBakedAndRealtimeShadows(global, shadow, directional.strength);
    }
    return shadow;
}
#endif