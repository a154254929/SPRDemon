//unity标准输入库
#ifndef CUSTOM_SURFACE_INCLUDEED
#define CUSTOM_SURFACE_INCLUDEED

struct Surface
{
	float3 position;
	float3 normal;
	float3 color;
	float alpha;
	float metallic;
	float smoothness;
	float3 viewDirection;
	//表面深度
    float depth;
    float dither;
    float fresnelStrength;
};

#endif