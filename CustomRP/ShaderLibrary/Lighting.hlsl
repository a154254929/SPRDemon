//unity标准输入库
#ifndef CUSTOM_LIGHTING_INCLUDEED
#define CUSTOM_LIGHTING_INCLUDEED

float3 IncomingLighting(Surface surface, Light light)
{
	return saturate(dot(light.direction, surface.normal) * light.attenuation) * light.color;
}

//检测表面掩码和灯光掩码是否重叠
bool RenderingLayersOverlap(Surface surface, Light light)
{
    return (surface.renderingLayerMask & light.renderingLayerMask) != 0;
}

float3 GetLighting(Surface surface, BRDF brdf, Light light)
{
	return IncomingLighting(surface, light) * DirectBRDF(surface, brdf, light);
}

float3 GetLighting(Surface surfaceWS, BRDF brdf, GI gi)
{
	//得到表面阴影数据
    ShadowData shadowData = GetShadowData(surfaceWS);
    shadowData.shadowMask = gi.shadowMask;
    //return shadowData.shadowMask.shadows.rgb;
    //float3 color = gi.diffuse * brdf.diffuse;
    float3 color = IndirectBRDF(surfaceWS, brdf, gi.diffuse, gi.specular);
    int dirctionalLightCount = GetDirectionalLightCount();
    for (int i = 0; i < dirctionalLightCount; ++i)
    {
        Light light = GetDirectionalLight(i, surfaceWS, shadowData);
        if (RenderingLayersOverlap(surfaceWS, light))
        {
            color += GetLighting(surfaceWS, brdf, light);
        }
    }
    #ifdef _LIGHTS_PER_OBJECT
        for (int i = 0; i < min(unity_LightData.y, 8); ++i)
        {
            int lightIndex = unity_LightIndices[(uint)i / 4][(uint)i % 4];
            Light light = GetOtherLight(lightIndex, surfaceWS, shadowData);
            if (RenderingLayersOverlap(surfaceWS, light))
            {
                color += GetLighting(surfaceWS, brdf, light);
            }
        }
    #else
        for (int i = 0; i < GetOtherLightCount(); ++i)
        {
            Light light = GetOtherLight(i, surfaceWS, shadowData);
            if (RenderingLayersOverlap(surfaceWS, light))
            {
                color += GetLighting(surfaceWS, brdf, light);
            }
        }
    #endif
	return color;
}

#endif