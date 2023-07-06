using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

public class Lighting
{
    const string bufferName = "Lighting";
    static string lighsPerObjectKeyword = "_LIGHTS_PER_OBJECT";

    CommandBuffer buffer = new CommandBuffer
    {
        name = bufferName
    };

    CullingResults cullingResults;

    const int maxDirLightCount = 4;
    const int maxOtherLightCount = 64;

    //static int dirLightColorId = Shader.PropertyToID("_DirectionalLightColor");
    //static int dirLightDirectionId = Shader.PropertyToID("_DirectionalLightDirection");
    static int dirLightCountId = Shader.PropertyToID("_DirectionalLightCount");
    static int dirLightColorsId = Shader.PropertyToID("_DirectionalLightColors");
    static int dirLightDirectionsAndMasksId = Shader.PropertyToID("_DirectionalLightDirectionsAndMasks");
    static int dirLightShadowDataId = Shader.PropertyToID("_DirectionalLightShadowData");
    static int otherLightCountId = Shader.PropertyToID("_OtherLightCount");
    static int otherLightColorsId = Shader.PropertyToID("_OtherLightColors");
    static int otherLightPositionsId = Shader.PropertyToID("_OtherLightPositions");
    static int otherLightDirectionsAndMasksId = Shader.PropertyToID("_OtherLightDirectionsAndMasks");
    static int otherLightSpotAnglesId = Shader.PropertyToID("_OtherLightSpotAngles");
    static int otherLightShadowDataId = Shader.PropertyToID("_OtherLightShadowData");

    //存储其它类型光源的颜色和位置数据
    static Vector4[] otherLightColors = new Vector4[maxOtherLightCount];
    static Vector4[] otherLightPositions = new Vector4[maxOtherLightCount];
    static Vector4[] otherLightDirectionsAndMasks = new Vector4[maxOtherLightCount];
    static Vector4[] otherLightSpotAngles = new Vector4[maxOtherLightCount];
    static Vector4[] otherLightShadowData = new Vector4[maxOtherLightCount];

    //存储阴影数据
    static Vector4[] dirLightShadowData = new Vector4[maxDirLightCount];

    //储存可见光方向和颜色
    static Vector4[] dirLightColors = new Vector4[maxDirLightCount];
    static Vector4[] dirLightDirectionsAndMasks = new Vector4[maxDirLightCount];

    Shadow shadow = new Shadow();

    public void SetUp(
        ScriptableRenderContext context,
        CullingResults cullingResults,
        ShadowSettings shadowSettings,
        bool useLightsPerObject,
        int renderingLayerMask
    )
    {
        this.cullingResults = cullingResults;
        buffer.BeginSample(bufferName);
        //设置光源数据
        //SetupDirectionalLight();

        //传递阴影数据
        shadow.Setup(context, cullingResults, shadowSettings);
        SetupLights(useLightsPerObject, renderingLayerMask);
        shadow.Render();
        buffer.EndSample(bufferName);
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();
    }

    void SetupLights(bool useLightsPerObject, int renderingLayerMask)
    {
        //拿到光源索引
        NativeArray<int> indexMap = useLightsPerObject ? cullingResults.GetLightIndexMap(Allocator.Temp) : default;
        //得到所有可见光
        NativeArray<VisibleLight> visibleLights = cullingResults.visibleLights;

        int dirLightCount = 0, otherLightCount = 0;
        int i;
        for (i = 0; i < visibleLights.Length; ++i)
        {
            int newIndex = -1;
            VisibleLight visibleLight = visibleLights[i];
            Light light = visibleLight.light;
            if ((renderingLayerMask & light.renderingLayerMask) != 0)
            {
                switch (visibleLight.lightType)
                {
                    case LightType.Directional:
                        if (dirLightCount < maxDirLightCount)
                        {
                            SetupDirectionalLight(dirLightCount++, i, ref visibleLight, light);
                        }
                        break;
                    case LightType.Point:
                        if (otherLightCount < maxOtherLightCount)
                        {
                            newIndex = otherLightCount;
                            SetupPointLight(otherLightCount++, i, ref visibleLight, light);
                        }
                        break;
                    case LightType.Spot:
                        if (otherLightCount < maxOtherLightCount)
                        {
                            newIndex = otherLightCount;
                            SetupSpotLight(otherLightCount++, i, ref visibleLight, light);
                        }
                        break;
                }
            }
            if(useLightsPerObject)
            {
                indexMap[i] = newIndex;
            }
        }
        if (useLightsPerObject)
        {
            for (; i < visibleLights.Length; ++i)
            {
                indexMap[i] = -1;
            }
            cullingResults.SetLightIndexMap(indexMap);
            indexMap.Dispose();
            Shader.EnableKeyword(lighsPerObjectKeyword);
        }
        else
        {
            Shader.DisableKeyword(lighsPerObjectKeyword);
        }
        buffer.SetGlobalInt(dirLightCountId, dirLightCount);
        if(dirLightCount > 0)
        {
            buffer.SetGlobalVectorArray(dirLightColorsId, dirLightColors);
            buffer.SetGlobalVectorArray(dirLightDirectionsAndMasksId, dirLightDirectionsAndMasks);
            buffer.SetGlobalVectorArray(dirLightShadowDataId, dirLightShadowData);
        }
        buffer.SetGlobalInt(otherLightCountId, otherLightCount);
        if(otherLightCount > 0)
        {
            buffer.SetGlobalVectorArray(otherLightColorsId, otherLightColors);
            buffer.SetGlobalVectorArray(otherLightPositionsId, otherLightPositions);
            buffer.SetGlobalVectorArray(otherLightDirectionsAndMasksId, otherLightDirectionsAndMasks);
            buffer.SetGlobalVectorArray(otherLightSpotAnglesId, otherLightSpotAngles);
            buffer.SetGlobalVectorArray(otherLightShadowDataId, otherLightShadowData);
        }
    }

    void SetupDirectionalLight(int index, int visibleIndex, ref VisibleLight visibleLight, Light light)
    {
        dirLightColors[index] = visibleLight.finalColor;
        //Light light = RenderSettings.sun;
        //buffer.SetGlobalVector(dirLightColorId, light.color.linear * light.intensity);
        //buffer.SetGlobalVector(dirLightDirectionId, -light.transform.forward);
        Vector4 dirAndMask = -visibleLight.localToWorldMatrix.GetColumn(2);
        dirAndMask.w = light.renderingLayerMask.ReinterpretAsFloat();
        dirLightDirectionsAndMasks[index] = dirAndMask;
        //存储阴影数据
        dirLightShadowData[index] = shadow.ReserveDirectionalLightShadows(light, visibleIndex);
    }

    //将点光源的颜色和位置信息储存到数据
    void SetupPointLight(int index, int visibleIndex, ref VisibleLight visibleLight, Light light)
    {
        otherLightColors[index] = visibleLight.finalColor;
        //位置信息在本地到世界的转换矩阵的最后一列
        Vector4 position = visibleLight.localToWorldMatrix.GetColumn(3);
        position.w = 1.0f / Mathf.Max(visibleLight.range, 0.00001f);
        otherLightPositions[index] = position;
        otherLightSpotAngles[index] = new Vector4(0.0f, 1.0f);
        Vector4 dirAndMask = Vector4.zero;
        dirAndMask.w = light.renderingLayerMask.ReinterpretAsFloat();
        otherLightDirectionsAndMasks[index] = dirAndMask;
        otherLightShadowData[index] = shadow.ReserveOtherLightShadows(light, visibleIndex);
    }

    //将聚光灯的颜色和位置信息储存到数据
    void SetupSpotLight(int index, int visibleIndex, ref VisibleLight visibleLight, Light light)
    {
        otherLightColors[index] = visibleLight.finalColor;
        //位置信息在本地到世界的转换矩阵的最后一列
        Vector4 position = visibleLight.localToWorldMatrix.GetColumn(3);
        position.w = 1.0f / Mathf.Max(visibleLight.range, 0.00001f);
        otherLightPositions[index] = position;
        //本地到世界的转换矩阵的第三列求反得到光照方向
        Vector4 dirAndMask = -visibleLight.localToWorldMatrix.GetColumn(2);
        dirAndMask.w = light.renderingLayerMask.ReinterpretAsFloat();
        otherLightDirectionsAndMasks[index] = dirAndMask;
        float innerCos = Mathf.Cos(Mathf.Deg2Rad * 0.5f * light.innerSpotAngle);
        float outerCos = Mathf.Cos(Mathf.Deg2Rad * 0.5f * light.spotAngle);
        float angleRangeInv = 1.0f / (Mathf.Max(innerCos - outerCos, 0.001f));
        otherLightSpotAngles[index] = new Vector4(angleRangeInv, -outerCos * angleRangeInv);
        otherLightShadowData[index] = shadow.ReserveOtherLightShadows(light, visibleIndex);
    }

    public void Cleanup()
    {
        shadow.Cleanup();
    }
}
