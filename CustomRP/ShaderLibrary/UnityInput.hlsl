//unity��׼�����
#ifndef CUSTOM_UNITY_INPUT_INCLUDEED
#define CUSTOM_UNITY_INPUT_INCLUDEED

CBUFFER_START(UnityPerDraw)
	//����ģ�Ϳռ�->����ռ�ľ���
	float4x4 unity_ObjectToWorld;

	//��������ռ�->ģ�Ϳռ�ľ���
	float4x4 unity_WorldToObject;

	float4 unity_LODFade;

	//һЩд���벻��Ҫ����Ϣ����
	real4 unity_WorldTransformParams;

	float4 unity_ProbesOcclusion;
	float4 unity_LightmapST;
	float4 unity_DynamicLightmapST;

	//����̽�����
	float4 unity_SHAr;
	float4 unity_SHAg;
	float4 unity_SHAb;
	float4 unity_SHBr;
	float4 unity_SHBg;
	float4 unity_SHBb;
	float4 unity_SHC;

	//����̽����������
	float4 unity_ProbeVolumeParams;
	float4x4 unity_ProbeVolumeWorldToObject;
	float4 unity_ProbeVolumeSizeInv;
	float4 unity_ProbeVolumeMin;
CBUFFER_END

//���λ��
float3 _WorldSpaceCameraPos;

//�۲�ռ�ľ���
float4x4 unity_MatrixV;

//��������ռ�->�ü��ռ�ľ���
float4x4 unity_MatrixVP;

//͸�ӿռ�ľ���
float4x4 glstate_matrix_projection;

#endif