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

#if defined _OTHER_PCF3
    //��Ҫ4���˲�����
    #define OTHER_FILTER_SAMPLES 4
    #define OTHER_FILTER_SETUP SampleShadow_ComputeSamples_Tent_3x3
#elif defined _OTHER_PCF5
    #define OTHER_FILTER_SAMPLES 9
    #define OTHER_FILTER_SETUP SampleShadow_ComputeSamples_Tent_5x5
#elif defined _OTHER_PCF7
    #define OTHER_FILTER_SAMPLES 16
    #define OTHER_FILTER_SETUP SampleShadow_ComputeSamples_Tent_7x7
#endif


#define MAX_SHADOWED_DIRECTIONAL_LIGHT_COUNT 4
#define MAX_CASCADE_COUNT 4
#define MAX_SHADOWED_OTHER_LIGHT_COUNT 16
//��Ӱͼ��
TEXTURE2D(_DirectionalShadowAtlas);
#define SHADOW_SAMPLER sampler_linear_clamp_compare
SAMPLER_CMP(SHADOW_SAMPLER);
TEXTURE2D(_OtherShadowAtlas);

CBUFFER_START(_CustomShadows)
    //���������Ͱ�Χ������
    int _CascadeCount;
    float4 _CascadeCullingSpheres[MAX_CASCADE_COUNT];
    //��������
    float4 _CascadeData[MAX_CASCADE_COUNT];
    //��Ӱת������
    float4x4 _DirectionalShadowMatrices[MAX_SHADOWED_DIRECTIONAL_LIGHT_COUNT * MAX_CASCADE_COUNT];
    //float _ShadowDistance;
    //��Ӱ���Ⱦ���
    float4 _ShadowDistanceFade;
    //��Ӱ��ͼ��С
    float4 _ShadowAtlasSize;
    float4x4 _OtherShadowMatrices[MAX_SHADOWED_OTHER_LIGHT_COUNT];
    float4 _OtherShadowTiles[MAX_SHADOWED_OTHER_LIGHT_COUNT];
CBUFFER_END

//��Ӱ��������Ϣ
struct DirectionalShadowData
{
	float strength;
    int tileIndex;
    //����ƫ��
    float normalBias;
    int shadowMaskChannel;
};

//������Ӱ����
struct ShadowMask
{
    bool distance;
    float4 shadows;
    bool always;
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

struct OtherShadowData
{
    float strength;
    int tileIndex;
    int shadowMaskChannel;
    float3 lightPositionWS;
    float3 spotDirectionWS;
    bool isPoint;
    float3 lightDirectionWS;
};

static const float3 pointShadowPlanes[6] = 
{
    float3(-1.0, 0.0, 0.0),
    float3(1.0, 0.0, 0.0),
    float3(0.0, -1.0, 0.0),
    float3(0.0, 1.0, 0.0),
    float3(0.0, 0.0, -1.0),
    float3(0.0, 0.0, 1.0),
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
    data.shadowMask.always = false;
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
    //��������������Χ�Ҽ�����������0����ȫ����Ӱǿ����Ϊ0����������Ӱ������
    if(i == _CascadeCount && _CascadeCount > 0)
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
float GetBakedShadow(ShadowMask mask, int channel)
{
    float shadow = 1.0;
    if (mask.distance || mask.always)
    {
        shadow = mask.shadows[channel];
    }
    return shadow;
}

float GetBakedShadow(ShadowMask mask, int channel, float strength)
{
    float shadow = 1.0;
    if (mask.distance || mask.always)
    {
        return lerp(1.0, GetBakedShadow(mask, channel), strength);
    }
    return 1.0;
}

//��Ϻ����ʵʱ��Ӱ
float MixBakedAndRealtimeShadows(ShadowData global, float shadow, int shadowMaskChannel, float strength)
{
    float baked = GetBakedShadow(global.shadowMask, shadowMaskChannel);
    if(global.shadowMask.always)
    {
        shadow = lerp(1.0, shadow, global.strength);
        shadow = min(baked, shadow);
        return lerp(1.0, shadow, strength);
    }
    if(global.shadowMask.distance)
    {
        shadow = lerp(baked, shadow, global.strength);
        return 0;
    }
    return lerp(1.0, shadow, strength * global.strength);
}

//����ƽ�й���Ӱ˥��
float GetDirectionalShadowAttenuation(DirectionalShadowData directional, ShadowData global, Surface surfaceWS)
{
    #ifndef _RECEIVE_SHADOWS
        return 1.0;
    #endif
    float shadow;
    if (directional.strength * global.strength <= 0.0)
    {
        shadow = GetBakedShadow(global.shadowMask, directional.shadowMaskChannel, abs(directional.strength));
    }
    else
    {
        shadow = GetCascadedShadow(directional, global, surfaceWS);
        //shadow = lerp(1.0, shadow, directional.strength);
        //��Ӱ���
        shadow = MixBakedAndRealtimeShadows(global, shadow, directional.shadowMaskChannel, directional.strength);
    }
    return shadow;
}

float SampleOtherShadowAtlas(float3 positionSTS, float3 bounds)
{
    positionSTS.xy = clamp(positionSTS.xy, bounds.xy, bounds.xy + bounds.z);
	return SAMPLE_TEXTURE2D_SHADOW(_OtherShadowAtlas, SHADOW_SAMPLER, positionSTS);
}

float FilterOtherShadow(float3 positionSTS, float3 bounds)
{
    #ifdef OTHER_FILTER_SETUP
        //Ȩ������
        real weights[OTHER_FILTER_SAMPLES];
        //����λ��
        real2 positions[OTHER_FILTER_SAMPLES];
        float4 size = _ShadowAtlasSize.wwzz;
        OTHER_FILTER_SETUP(size, positionSTS.xy, weights, positions);
        float shadow = 0;
        for (int i = 0; i < OTHER_FILTER_SAMPLES; ++i)
        {
            //�������������õ�Ȩ�غ�
            shadow += weights[i] * SampleOtherShadowAtlas(float3(positions[i], positionSTS.z), bounds); 
        }
        return shadow;
    #else
        return SampleOtherShadowAtlas(positionSTS, bounds);
    #endif
}

//�õ��������͹�Դ
float GetOtherShadow(OtherShadowData other, ShadowData global, Surface surfaceWS)
{
    float tileIndex = other.tileIndex;
    float3 lightPlane = other.spotDirectionWS;
    if (other.isPoint)
    {
        float faceOffset = CubeMapFaceID(-other.lightDirectionWS);
        tileIndex += faceOffset;
        lightPlane = pointShadowPlanes[faceOffset];
    }
    float4 tileData = _OtherShadowTiles[tileIndex];
    float3 surfaceToLight = other.lightPositionWS - surfaceWS.position;
    float distanceToLLightPlane = dot(surfaceToLight, lightPlane);
    float3 normalBias = surfaceWS.normal * (distanceToLLightPlane * tileData.w);
    float4 positionSTS = mul(_OtherShadowMatrices[tileIndex], float4(surfaceWS.position + normalBias, 1.0));
    //͸��ͶӰ,�任λ�õ�XYZ/2
    return FilterOtherShadow(positionSTS.xyz / positionSTS.w, tileData.xyz);
}

//�����������͹���Ӱ˥��
float GetOtherShadowAttenuation(OtherShadowData other, ShadowData global, Surface surfaceWS)
{
    #ifndef _RECEIVE_SHADOWS
        return 1.0;
    #endif
    float shadow;
    if (other.strength * global.strength <= 0.0)
    {
        shadow = GetBakedShadow(global.shadowMask, other.shadowMaskChannel, other.strength);
    }
    else
    {
        shadow = GetOtherShadow(other, global, surfaceWS);
        shadow = MixBakedAndRealtimeShadows(global, shadow, other.shadowMaskChannel, other.strength);
    }
    return shadow;
}
#endif