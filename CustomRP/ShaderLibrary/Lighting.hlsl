//unity标准输入库
#ifndef CUSTOM_LIGHTING_INCLUDEED
#define CUSTOM_LIGHTING_INCLUDEED

float3 IncomingLighting(Surface surface, Light light)
{
	return saturate(dot(light.direction, surface.normal) * light.attenuation) * light.color;
}

float3 GetLighting(Surface surface, BRDF brdf, Light light)
{
	return IncomingLighting(surface, light) *DirectBRDF(surface, brdf, light);
}

float3 GetLighting(Surface surfaceWS, BRDF brdf, GI gi)
{
	//得到表面阴影数据
    ShadowData shadowData = GetShadowData(surfaceWS);
    shadowData.shadowMask = gi.shadowMask;
    //return shadowData.shadowMask.shadows.rgb;
    float3 color = gi.diffuse * brdf.diffuse;
	int dirctionalLightCount = GetDirectionalLightCount();
	for (int i = 0; i < dirctionalLightCount; ++i)
	{
        Light light = GetDirectionalLight(i, surfaceWS, shadowData);
        color += GetLighting(surfaceWS, brdf, light);
    }
	return color;
}

#endif