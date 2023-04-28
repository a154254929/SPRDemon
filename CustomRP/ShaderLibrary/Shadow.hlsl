//��Ӱ����
#ifndef CUSTOM_SHADOW_INCLUDEED
#define CUSTOM_SHADOW_INCLUDEED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Shadow/ShadowSamplingTent.hlsl"
//����õ���PCF 3x3
#if defined _DIRECTIONAL_PCF3
    //��Ҫ4���˲�����
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
//��Ӱͼ��
TEXTURE2D(_DirectionalShadowAtlas);
#define SHADOW_SAMPLER sampler_linear_clamp_compare
SAMPLER_CMP(SHADOW_SAMPLER);

CBUFFER_START(_CustomShadows)
    //���������Ͱ�Χ������
    int _CascadeCount;
    float4 _CascadeCullingSpheres[MAX_CASCADE_COUNT];
    //��������
    float4 _CascadeData[MAX_CASCADE_COUNT];
    //��Ӱת������
    float4x4 _DirectionalShadowMatrices[MAX_DIRECTIONAL_LIGHT_COUNT * MAX_CASCADE_COUNT];
    //float _ShadowDistance;
    //��Ӱ���Ⱦ���
    float4 _ShadowDistanceFade;
    //��Ӱ��ͼ��С
    float4 _ShadowAtlasSize;
CBUFFER_END

//��Ӱ��������Ϣ
struct DirectionalShadowData
{
	float strength;
    int tileIndex;
    //����ƫ��
    float normalBias;
};

//������Ӱ����
struct ShadowMask
{
    bool distance;
    float4 shadows;
};

//��Ӱ����
struct ShadowData
{
    int cascadeIndex;
    //�Ƿ������Ӱ�ı�ʶ
    float strength;
    //��ϼ���
    float cascadeBlend;
    ShadowMask shadowMask;
};

//��ʽ������Ӱ���ȵ�ǿ��
float FadeShadowStrength(float distance, float scale, float fade)
{
    return saturate((1.0 - distance * scale) * fade);
}

//�õ�����ռ�ı�����Ӱ����
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

//������Ӱͼ��
float SampleDirectionalShadowAtlas(float3 positionSTS)
{
	return SAMPLE_TEXTURE2D_SHADOW(_DirectionalShadowAtlas, SHADOW_SAMPLER, positionSTS);
}

float FilterDirectionalShadow(float3 positionSTS)
{
#ifdef DIRECTIONAL_FILTER_SETUP
    //Ȩ������
    float weights[DIRECTIONAL_FILTER_SAMPLES];
    //����λ��
    float2 positions[DIRECTIONAL_FILTER_SAMPLES];
    float4 size = _ShadowAtlasSize.yyxx;
    DIRECTIONAL_FILTER_SETUP(size, positionSTS.xy, weights, positions);
    float shadow = 0;
    for (int i = 0; i < DIRECTIONAL_FILTER_SAMPLES; ++i)
    {
        //�������������õ�Ȩ�غ�
        shadow += weights[i] * SampleDirectionalShadowAtlas(float3(positions[i], positionSTS.z)); 
    }
    return shadow;
#else
    return SampleDirectionalShadowAtlas(positionSTS);
#endif
}

float GetCascadedShadow(DirectionalShadowData directional, ShadowData global, Surface surfaceWS)
{
    //���㷨��ƫ��
    float3 normalBias = surfaceWS.normal * (directional.normalBias * _CascadeData[global.cascadeIndex].y);
	//ͨ����Ӱת������ͱ���λ�õõ�����Ӱ����ͼ�飩�ռ��λ�ã�Ȼ���ͼ�����в���
    float3 positionSTS = mul(_DirectionalShadowMatrices[directional.tileIndex], float4(surfaceWS.position + normalBias, 1.0)).xyz;
    float shadow = FilterDirectionalShadow(positionSTS);
    //����������С��1�����ټ����㼶���������У��������һ�������в�����������ָ֮����в�ֵ
    if (global.cascadeBlend < 1.0)
    {
        normalBias = surfaceWS.normal * (directional.normalBias * _CascadeData[global.cascadeIndex + 1].y);
        positionSTS = mul(_DirectionalShadowMatrices[directional.tileIndex + 1], float4(surfaceWS.position + normalBias, 1.0)).xyz;
        shadow = lerp(FilterDirectionalShadow(positionSTS), shadow, global.cascadeBlend);
    }
    return shadow;
}

//�õ�������Ӱ��˥��ֵ
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

//��Ϻ����ʵʱ��Ӱ
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

//������Ӱ˥��
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
        //��Ӱ���
        shadow = MixBakedAndRealtimeShadows(global, shadow, directional.strength);
    }
    return shadow;
}
#endif