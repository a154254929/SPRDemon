//����
#ifndef CUSTOM_LIGHT_INCLUDEED
#define CUSTOM_LIGHT_INCLUDEED
#define MAX_DIRECTIONAL_LIGHT_COUNT 4
struct Light
{
	float3 color;
	float3 direction;
	float attenuation;
};

CBUFFER_START(_CustomLight)
	//float3 _DirectionalLightColor;
	//float3 _DirectionalLightDirection;
	int _DirectionalLightCount;
	float4 _DirectionalLightColors[MAX_DIRECTIONAL_LIGHT_COUNT];
	float3 _DirectionalLightDirections[MAX_DIRECTIONAL_LIGHT_COUNT];
	//��Ӱ����
	float4 _DirectionalLightShadowData[MAX_DIRECTIONAL_LIGHT_COUNT];
CBUFFER_END

//��ȡƽ�й�����
int GetDirectionalLightCount()
{
	return _DirectionalLightCount;
}

//��ȡƽ�й���Ӱ����
DirectionalShadowData GetDirectionalShadowData(int lightIndex, ShadowData shadowData)
{
	DirectionalShadowData data;
    //data.strength = _DirectionalLightShadowData[lightIndex].x * shadowData.strength;
    data.strength = _DirectionalLightShadowData[lightIndex].x;
    data.tileIndex = _DirectionalLightShadowData[lightIndex].y + shadowData.cascadeIndex;
	return data;
}

//��ȡָ���±�ƽ�й�����
Light GetDirectionalLight(int index, Surface surfaceWS, ShadowData shadowData)
{
	Light light;
	light.color = _DirectionalLightColors[index].rgb;
	light.direction = _DirectionalLightDirections[index].xyz;
	//�õ���Ӱ����
    DirectionalShadowData dirShadowData = GetDirectionalShadowData(index, shadowData);
	//�õ���Ӱ˥��
    //light.attenuation = dirShadowData.tileIndex / 4.0;
    dirShadowData.normalBias = _DirectionalLightShadowData[index].z;
    light.attenuation = GetDirectionalShadowAttenuation(dirShadowData, shadowData, surfaceWS);
	return light;
}

#endif