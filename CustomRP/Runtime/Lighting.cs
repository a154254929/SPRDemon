using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

public class Lighting
{
    const string bufferName = "Lighting";

    CommandBuffer buffer = new CommandBuffer
    {
        name = bufferName
    };

    CullingResults cullingResults;

    const int maxDirLightCount = 4;

    //static int dirLightColorId = Shader.PropertyToID("_DirectionalLightColor");
    //static int dirLightDirectionId = Shader.PropertyToID("_DirectionalLightDirection");
    static int dirLightCountId = Shader.PropertyToID("_DirectionalLightCount");
    static int dirLightColorsId = Shader.PropertyToID("_DirectionalLightColors");
    static int dirLightDirectionsId = Shader.PropertyToID("_DirectionalLightDirections");
    static int dirLightShadowDataId = Shader.PropertyToID("_DirectionalLightShadowData");

    //�洢��Ӱ����
    static Vector4[] dirLightShadowData = new Vector4[maxDirLightCount];

    //����ɼ��ⷽ�����ɫ
    static Vector4[] dirLightColors = new Vector4[maxDirLightCount];
    static Vector4[] dirLightDirections = new Vector4[maxDirLightCount];

    Shadow shadow = new Shadow();

    public void SetUp(ScriptableRenderContext context, CullingResults cullingResults, ShadowSettings shadowSettings)
    {
        this.cullingResults = cullingResults;
        buffer.BeginSample(bufferName);
        //���ù�Դ����
        //SetupDirectionalLight();

        //������Ӱ����
        shadow.Setup(context, cullingResults, shadowSettings);

        SetupLights();
        shadow.Render();
        buffer.EndSample(bufferName);
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();
    }

    void SetupLights()
    {
        //�õ����пɼ���
        NativeArray<VisibleLight> visibleLights = cullingResults.visibleLights;

        int dirLightCount = 0;
        for(int i = 0; i < visibleLights.Length; ++i)
        {
            VisibleLight visibleLight = visibleLights[i];
            if(visibleLight.lightType == LightType.Directional)
            {
                SetupDirectionalLight(dirLightCount++, visibleLight);
                if (dirLightCount >= maxDirLightCount) break;
            }
        }
        buffer.SetGlobalInt(dirLightCountId, dirLightCount);
        buffer.SetGlobalVectorArray(dirLightColorsId, dirLightColors);
        buffer.SetGlobalVectorArray(dirLightDirectionsId, dirLightDirections);
        buffer.SetGlobalVectorArray(dirLightShadowDataId, dirLightShadowData);
    }

    void SetupDirectionalLight(int index, VisibleLight visibleLight)
    {
        //Light light = RenderSettings.sun;
        //buffer.SetGlobalVector(dirLightColorId, light.color.linear * light.intensity);
        //buffer.SetGlobalVector(dirLightDirectionId, -light.transform.forward);

        dirLightColors[index] = visibleLight.finalColor;
        dirLightDirections[index] = -visibleLight.localToWorldMatrix.GetColumn(2);
        //�洢��Ӱ����
        dirLightShadowData[index] = shadow.ReserveDirectionalLightShadows(visibleLight.light, index);
    }

    public void Cleanup()
    {
        shadow.Cleanup();
    }
}
