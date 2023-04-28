//���÷�����
#ifndef CUSTOM_COMMON_INCLUDEED
#define CUSTOM_COMMON_INCLUDEED
//include�ļ�UnityInpuy
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
#include "UnityInput.hlsl"
#define UNITY_MATRIX_M unity_ObjectToWorld
#define UNITY_MATRIX_I_M unity_WorldToObject
#define UNITY_MATRIX_V unity_MatrixV
#define UNITY_MATRIX_VP unity_MatrixVP
#define UNITY_MATRIX_P glstate_matrix_projection
#ifdef _SHADOW_MASK_DISTANCE
    #define SHADOWS_SHADOWMASK
#endif
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SpaceTransforms.hlsl"
////�����ģ�Ϳռ�ת������ռ�
//float3 TransformObjectToWorld(float3 positionOS)
//{
//	return mul(unity_ObjectToWorld, float4(positionOS, 1.0)).xyz;
//}
//
////���������ռ�ת���ü��ռ�
//float4 TransformWorldToHClip(float3 positionWS)
//{
//	return mul(unity_MatrixVP, float4(positionWS, 1.0));
//}

float Square(float v)
{
	return v * v;
}

//��������֮������ƽ��
float DistanceSquared(float3 pA, float3 pB)
{
    return dot(pA - pB, pA - pB);
}


#endif