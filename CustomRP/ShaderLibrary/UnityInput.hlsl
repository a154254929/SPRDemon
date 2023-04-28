//unity标准输入库
#ifndef CUSTOM_UNITY_INPUT_INCLUDEED
#define CUSTOM_UNITY_INPUT_INCLUDEED

CBUFFER_START(UnityPerDraw)
	//定义模型空间->世界空间的矩阵
	float4x4 unity_ObjectToWorld;

	//定义世界空间->模型空间的矩阵
	float4x4 unity_WorldToObject;

	float4 unity_LODFade;

	//一些写代码不需要的信息矩阵
	real4 unity_WorldTransformParams;

	float4 unity_ProbesOcclusion;
	float4 unity_LightmapST;
	float4 unity_DynamicLightmapST;

	//光照探针参数
	float4 unity_SHAr;
	float4 unity_SHAg;
	float4 unity_SHAb;
	float4 unity_SHBr;
	float4 unity_SHBg;
	float4 unity_SHBb;
	float4 unity_SHC;

	//光照探针代理体参数
	float4 unity_ProbeVolumeParams;
	float4x4 unity_ProbeVolumeWorldToObject;
	float4 unity_ProbeVolumeSizeInv;
	float4 unity_ProbeVolumeMin;
CBUFFER_END

//相机位置
float3 _WorldSpaceCameraPos;

//观察空间的矩阵
float4x4 unity_MatrixV;

//定义世界空间->裁剪空间的矩阵
float4x4 unity_MatrixVP;

//透视空间的矩阵
float4x4 glstate_matrix_projection;

#endif