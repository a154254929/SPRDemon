//光照
#ifndef CUSTOM_LIGHT_INCLUDEED
#define CUSTOM_LIGHT_INCLUDEED
#define MAX_DIRECTIONAL_LIGHT_COUNT 4
#define MAX_OTHER_LIGHT_COUNT 64
struct Light
{
	float3 color;
	float3 direction;
	float attenuation;
	uint renderingLayerMask;
};

CBUFFER_START(_CustomLight)
	//float3 _DirectionalLightColor;
	//float3 _DirectionalLightDirection;
	int _DirectionalLightCount;
	float4 _DirectionalLightColors[MAX_DIRECTIONAL_LIGHT_COUNT];
	float4 _DirectionalLightDirectionsAndMasks[MAX_DIRECTIONAL_LIGHT_COUNT];
	//阴影数据
	float4 _DirectionalLightShadowData[MAX_DIRECTIONAL_LIGHT_COUNT];
	//非平行光源属性
	int _OtherLightCount;
	float4 _OtherLightColors[MAX_OTHER_LIGHT_COUNT];
	float4 _OtherLightPositions[MAX_OTHER_LIGHT_COUNT];
	float4 _OtherLightDirectionsAndMasks[MAX_OTHER_LIGHT_COUNT];
	float4 _OtherLightSpotAngles[MAX_OTHER_LIGHT_COUNT];
	float4 _OtherLightShadowData[MAX_OTHER_LIGHT_COUNT];

CBUFFER_END

//获取平行光数量
int GetDirectionalLightCount()
{
	return _DirectionalLightCount;
}

//获取非平行光源数量
int GetOtherLightCount()
{
    return _OtherLightCount;
}

//获取平行光阴影数据
DirectionalShadowData GetDirectionalShadowData(int lightIndex, ShadowData shadowData)
{
	DirectionalShadowData data;
    //data.strength = _DirectionalLightShadowData[lightIndex].x * shadowData.strength;
    data.strength = _DirectionalLightShadowData[lightIndex].x;
    data.tileIndex = _DirectionalLightShadowData[lightIndex].y + shadowData.cascadeIndex;
    data.shadowMaskChannel = _DirectionalLightShadowData[lightIndex].w;
	return data;
}

//获取其他类型光阴影数据
OtherShadowData GetOtherShadowData(int lightIndex)
{
    OtherShadowData data;
    data.strength = _OtherLightShadowData[lightIndex].x;
    data.tileIndex = _OtherLightShadowData[lightIndex].y;
    data.shadowMaskChannel = _OtherLightShadowData[lightIndex].w;
    data.lightPositionWS = 0.0;
    data.isPoint = _OtherLightShadowData[lightIndex].z == 1.0;
    data.spotDirectionWS = 0.0;
	return data;
}

//获取指定下标平行光数据
Light GetDirectionalLight(int index, Surface surfaceWS, ShadowData shadowData)
{
	Light light;
	light.color = _DirectionalLightColors[index].rgb;
	light.direction = _DirectionalLightDirectionsAndMasks[index].xyz;
	light.renderingLayerMask = asuint(_DirectionalLightDirectionsAndMasks[index].w);
	//得到阴影数据
    DirectionalShadowData dirShadowData = GetDirectionalShadowData(index, shadowData);
	//得到阴影衰减
    //light.attenuation = dirShadowData.tileIndex / 4.0;
    dirShadowData.normalBias = _DirectionalLightShadowData[index].z;
    light.attenuation = GetDirectionalShadowAttenuation(dirShadowData, shadowData, surfaceWS);
	return light;
}

//获取指定下标非平行光数据
Light GetOtherLight(int index, Surface surfaceWS, ShadowData shadowData)
{
	Light light;
    light.color = _OtherLightColors[index].rgb;
	float3 lightPosition = _OtherLightPositions[index].xyz;
	float3 ray = lightPosition - surfaceWS.position;
    light.direction = normalize(ray);
    //light.attenuation = 1.0;
	//光照随距离衰减
    float distanceSqr = max(dot(ray, ray), 0.00001);
    //light.attenuation = 1.0 / distanceSqr;
    float rangeAttenuation = Square(saturate(1.0 - Square(distanceSqr * _OtherLightPositions[index].w)));
	float3 spotDirectionWS = _OtherLightDirectionsAndMasks[index].xyz;
	light.renderingLayerMask = asuint(_OtherLightDirectionsAndMasks[index].w);
	//得到聚光灯衰减
    float4 spotAngles = _OtherLightSpotAngles[index];
    float spotAttenuation = Square(saturate(dot(spotDirectionWS, light.direction) * spotAngles.x + spotAngles.y));
    OtherShadowData otherShadowData = GetOtherShadowData(index);
	otherShadowData.lightPositionWS = lightPosition;
	otherShadowData.lightDirectionWS = light.direction;
	otherShadowData.spotDirectionWS = spotDirectionWS;
    light.attenuation = GetOtherShadowAttenuation(otherShadowData, shadowData, surfaceWS) * spotAttenuation * rangeAttenuation / distanceSqr;
	return light;
}

#endif