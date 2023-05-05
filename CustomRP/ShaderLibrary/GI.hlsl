//unity标准输入库
#ifndef CUSTOM_GI_INCLUDEED
#define CUSTOM_GI_INCLUDEED
    
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/EntityLighting.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/ImageBasedLighting.hlsl"

TEXTURE2D(unity_Lightmap);
SAMPLER(samplerunity_Lightmap);
TEXTURE2D(unity_ShadowMask);
SAMPLER(samplerunity_ShadowMask);
TEXTURE3D_FLOAT(unity_ProbeVolumeSH);
SAMPLER(samplerunity_ProbeVolumeSH);
TEXTURECUBE(unity_SpecCube0);
SAMPLER(samplerunity_SpecCube0);

//当需要渲染光照贴图对象时
#ifdef LIGHTMAP_ON
#define GI_ATTRIBUTE_DATA float2 lightMapUV : TEXCOORD1;
#define GI_VARYINGS_DATA float2 lightMapUV : VAR_LIGHT_MAP_UV;
#define TRANSFER_GI_DATA(input, output) output.lightMapUV = input.lightMapUV * unity_LightmapST.xy + unity_LightmapST.zw;
#define GI_FRAGMENT_DATA(input) input.lightMapUV
#else
#define GI_ATTRIBUTE_DATA
#define GI_VARYINGS_DATA
#define TRANSFER_GI_DATA(input, output)
#define GI_FRAGMENT_DATA(input) 0.0
#endif

struct GI
{
    //漫反射颜色
    float3 diffuse;
    ShadowMask shadowMask;
    //镜面反射颜色
    float3 specular;
};

//采样光照贴图
float3 SamplLightMap(float2 lightMapUV)
{
    #ifdef LIGHTMAP_ON
        return SampleSingleLightmap(
            TEXTURE2D_ARGS(unity_Lightmap, samplerunity_Lightmap),
            lightMapUV,
            float4(1.0, 1.0, 0.0, 0.0),
        #ifdef UNITY_LIGHTMAP_FULL_HDR
            false,
        #else
            true,
        #endif
            float4(LIGHTMAP_HDR_MULTIPLIER, LIGHTMAP_HDR_EXPONENT, 0.0, 0.0)
        );
    #else
        return 0.0;
    #endif
}

//光照探针采样
float3 SampleLightProbe(Surface surfaceWS)
{
#ifdef LIGHTMAP_ON
        return 0.0;
#else
    //判断是否使用LPPV或插值光照探针
    if(unity_ProbeVolumeParams.x)
    {
        return SampleProbeVolumeSH4(
            TEXTURE3D_ARGS(unity_ProbeVolumeSH, samplerunity_ProbeVolumeSH),
            surfaceWS.position,
            surfaceWS.normal,
            unity_ProbeVolumeWorldToObject,
            unity_ProbeVolumeParams.y,
            unity_ProbeVolumeParams.z,
            unity_ProbeVolumeMin.xyz,
            unity_ProbeVolumeSizeInv.xyz
        );
    }
    else
    {
        float4 coefficients[7];
        coefficients[0] = unity_SHAr;
        coefficients[1] = unity_SHAg;
        coefficients[2] = unity_SHAb;
        coefficients[3] = unity_SHBr;
        coefficients[4] = unity_SHBg;
        coefficients[5] = unity_SHBb;
        coefficients[6] = unity_SHC;
        return max(0.0, SampleSH9(coefficients, surfaceWS.normal));
    }
#endif

}

//采样shadowMask得到烘培阴影数据
float4 SampleBakeShadows(float2 lightMapUV, Surface surfaceWS)
{
    #ifdef LIGHTMAP_ON
    return SAMPLE_TEXTURE2D(unity_ShadowMask, samplerunity_ShadowMask, lightMapUV);
    #else
    if (unity_ProbeVolumeParams.x)
    {
        return SampleProbeOcclusion(
            TEXTURE3D_ARGS(unity_ProbeVolumeSH, samplerunity_ProbeVolumeSH),
            surfaceWS.position,
            unity_ProbeVolumeWorldToObject,
            unity_ProbeVolumeParams.y,
            unity_ProbeVolumeParams.z,
            unity_ProbeVolumeMin.xyz,
            unity_ProbeVolumeSizeInv.xyz
        );

    }
    return unity_ProbesOcclusion;
    #endif
}

//采样环境立方体纹理
float3 SampleEnvironment(Surface surfaceWS, BRDF brdf)
{
    float3 uvw = reflect(-surfaceWS.viewDirection, surfaceWS.normal);
    float mip = PerceptualRoughnessToMipmapLevel(brdf.perceptualRoughness);
    float4 environment = SAMPLE_TEXTURECUBE_LOD(unity_SpecCube0, samplerunity_SpecCube0, uvw, mip);
    return DecodeHDREnvironment(environment, unity_SpecCube0_HDR);
    //return environment.rgb;
}

GI GetGI(float2 lightMapUV, Surface surfaceWS, BRDF brdf)
{
    GI gi;
    gi.diffuse = SamplLightMap(lightMapUV) + SampleLightProbe(surfaceWS);
    gi.specular = SampleEnvironment(surfaceWS, brdf);
    gi.shadowMask.distance = false;
    gi.shadowMask.shadows = float4(lightMapUV, 0, 0);
    gi.shadowMask.always = false;
    
#if defined(_SHADOW_MASK_ALWAYS)
    gi.shadowMask.shadows = SampleBakeShadows(lightMapUV, surfaceWS);
    gi.shadowMask.always = true;
#elif defined(_SHDAOW_MASK_DISTANCE)
    gi.shadowMask.distance = true;
    gi.shadowMask.shadows = SampleBakeShadows(lightMapUV, surfaceWS);
#endif
    return gi;
}
#endif