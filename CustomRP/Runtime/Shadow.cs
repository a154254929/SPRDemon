using UnityEngine;
using UnityEngine.Rendering;

public class Shadow
{
    const string bufferName = "Shadow";

    static int dirShadowAtlasId = Shader.PropertyToID("_DirectionalShadowAtlas");
    static int dirShadowMatricesId = Shader.PropertyToID("_DirectionalShadowMatrices");
    static int otherShadowAtlasId = Shader.PropertyToID("_OtherShadowAtlas");
    static int otherShadowMatricesId = Shader.PropertyToID("_OtherShadowMatrices");
    static int cascadeCountId = Shader.PropertyToID("_CascadeCount");
    static int cascadeCullingSpheresId = Shader.PropertyToID("_CascadeCullingSpheres");
    //static int shadowDistanceId = Shader.PropertyToID("_ShadowDistance");
    static int shadowDistanceFadeId = Shader.PropertyToID("_ShadowDistanceFade");
    static int shadowAtlasSizeId = Shader.PropertyToID("_ShadowAtlasSize");
    static Vector4[] cascadeCullingSpheres = new Vector4[maxCascades];
    //��������
    static int cascadeDataId = Shader.PropertyToID("_CascadeData");
    static Vector4[] cascadeData = new Vector4[maxCascades];
    //��Ͷ����Ӱ��ƽ�й�����
    const int maxShadowDirectionalLightCount = 4;
    //�Ѵ���Ŀ�Ͷ����Ӱ��ƽ�й�����
    int shadowDirectionalLightCount;

    //��Ͷ����Ӱ���������͹�Դ���������
    const int maxShadowOtherLightCount = 16;
    //�Ѵ���Ŀ�Ͷ����Ӱ���������͹�Դ����
    int shadowOtherLightCount;

    //���������
    const int maxCascades = 4;

    //�洢��Ӱת������
    static Matrix4x4[] dirShadowMatrices = new Matrix4x4[maxShadowDirectionalLightCount * maxCascades];

    //�洢��Ӱת������
    static Matrix4x4[] otherShadowMatrices = new Matrix4x4[maxShadowOtherLightCount];

    //PCF����ģʽ
    static string[] directionalFilterKeywords =
    {
        "_DIRECTIONAL_PCF3",
        "_DIRECTIONAL_PCF5",
        "_DIRECTIONAL_PCF7",
    };

    //�������͹�Դ�Ĺ���ģʽ
    static string[] otherFilterKeywords =
    {
        "_OTHER_PCF3",
        "_OTHER_PCF5",
        "_OTHER_PCF7",
    };

    static string[] cascadeBlendKeywords =
    {
        "_CASCADE_BLEND_SOFT",
        "_CASCADE_BLEND_DITHER"
    };

    static string[] shadowNaskKeywords =
    {
        "_SHADOW_MASK_ALWAYS",
        "_SHADOW_MASK_DISTANCE",
    };

    CommandBuffer buffer = new CommandBuffer
    {
        name = bufferName
    };

    ScriptableRenderContext context;

    CullingResults cullingResults;

    ShadowSettings shadowSettings;

    bool useShadowMask;

    Vector4 atlasSizes;

    struct ShadowDirectionalLight
    {
        public int visibleLightIndex;
        //б�ȱ���ƫ��
        public float slopeScaleBias;
        //��Ӱ��׶����ü�ƽ��ƫ��
        public float nearPlaneOffset;
    }

    //�����Ͷ����Ӱ�Ŀɼ�������
    ShadowDirectionalLight[] shadowDirectionalLights = new ShadowDirectionalLight[maxShadowDirectionalLightCount];

    public void Setup(
        ScriptableRenderContext context,
        CullingResults cullingResults,
        ShadowSettings shadowSettings
    )
    {
        this.context = context;
        this.cullingResults = cullingResults;
        this.shadowSettings = shadowSettings;

        shadowDirectionalLightCount = 0;
        shadowOtherLightCount = 0;
        useShadowMask = false;
    }

    void ExcuteBuffer()
    {
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();
    }

    //����ƽ�й����Ӱ����
    public Vector4 ReserveDirectionalLightShadows(Light light, int visibleLightIndex)
    {
        if(
            shadowDirectionalLightCount < maxShadowDirectionalLightCount &&
            light.shadows != LightShadows.None &&
            light.shadowStrength > 0.0f &&
            cullingResults.GetShadowCasterBounds(visibleLightIndex, out Bounds bounds)
        )
        {
            float maskChannel = -1;
            //���ʹ����ShadowMask
            LightBakingOutput lightBaking = light.bakingOutput;
            if(lightBaking.lightmapBakeType == LightmapBakeType.Mixed && lightBaking.mixedLightingMode == MixedLightingMode.Shadowmask)
            {
                useShadowMask = true;
                maskChannel = lightBaking.occlusionMaskChannel;
            }
            if(!cullingResults.GetShadowCasterBounds(visibleLightIndex, out Bounds b))
            {
                return new Vector4(-light.shadowStrength, 0.0f, 0.0f, maskChannel);
            }
            shadowDirectionalLights[shadowDirectionalLightCount] = new ShadowDirectionalLight
            {
                visibleLightIndex = visibleLightIndex,
                slopeScaleBias = light.shadowBias,
                nearPlaneOffset = light.shadowNearPlane
            };
            return new Vector4(
                light.shadowStrength,
                shadowSettings.directional.cascadeCount * shadowDirectionalLightCount++,
                light.shadowBias,
                maskChannel
            );
        }
        return new Vector4(0.0f, 0.0f, 0.0f, -1f);
    }

    //�����������͹����Ӱ����
    public Vector4 ReserveOtherLightShadows(Light light, int visibleLightIndex)
    {
        if(light.shadows == LightShadows.None || light.shadowStrength <= 0.0f)
        {
            return new Vector4(0.0f, 0.0f, 0.0f, -1f);
        }
        float maskChannel = -1f;
        LightBakingOutput lightBaking = light.bakingOutput;
        if(lightBaking.lightmapBakeType == LightmapBakeType.Mixed && lightBaking.mixedLightingMode == MixedLightingMode.Shadowmask)
        {
            useShadowMask = true;
            maskChannel = lightBaking.occlusionMaskChannel;
        }
        if(shadowOtherLightCount >= maxShadowOtherLightCount || !cullingResults.GetShadowCasterBounds(visibleLightIndex, out Bounds b))
        {
            return new Vector4(light.shadowStrength, shadowOtherLightCount++, 0.0f, lightBaking.occlusionMaskChannel);
        }
        return new Vector4(light.shadowStrength, 0.0f, 0.0f, lightBaking.occlusionMaskChannel);
    }

    //��Ӱ��Ⱦ
    public void Render()
    {
        if(shadowDirectionalLightCount > 0)
        {
            RenderDirectionalShadows();
        }
        else
        {
            buffer.GetTemporaryRT(dirShadowAtlasId, 1, 1, 32, FilterMode.Bilinear, RenderTextureFormat.Shadowmap);
        }

        if(shadowOtherLightCount > 0)
        {
            RenderOtherShadows();
        }
        else
        {
            buffer.SetGlobalTexture(otherShadowAtlasId, dirShadowAtlasId);
        }

        //�Ƿ�ʹ����Ӱ�ɰ�
        buffer.BeginSample(bufferName);
        SetKeyword(shadowNaskKeywords, useShadowMask ? QualitySettings.shadowmaskMode == ShadowmaskMode.Shadowmask ? 0 : 1 : -1);

        //�����������Ͱ�Χ������ݷ��͵�GPU
        buffer.SetGlobalInt(cascadeCountId, shadowSettings.directional.cascadeCount);

        //��Ӱ�������������ݷ���GPU
        float f = 1.0f - shadowSettings.directional.cascadeFade;
        buffer.SetGlobalVector(shadowDistanceFadeId, new Vector4(
            1.0f / shadowSettings.maxDistance,  //  1/m,����(1 - ��d / m)) / f
            1.0f / shadowSettings.distanceFade, //  1/f,����(1 - ��d / m)) / f
            1.0f / (1.0f - f * f)               //����(1 - d^2 / r^2) / f
        ));

        //����ͼ����С�����ش�С
        buffer.SetGlobalVector(shadowAtlasSizeId, atlasSizes);
        buffer.EndSample(bufferName);
        ExcuteBuffer();
    }

    //��Ⱦƽ�й�
    public void RenderDirectionalShadows()
    {
        int tiles = shadowDirectionalLightCount * shadowSettings.directional.cascadeCount;
        //Ҫ�ָ��ͼ��Ĵ�С������
        int split = tiles <= 1 ? 1 : tiles <= 4 ? 2 : 4;
        int atlasSize = (int)shadowSettings.directional.atlasSize;
        atlasSizes.x = atlasSize;
        atlasSizes.y = 1.0f / atlasSize;
        int tileSize = atlasSize / split;
        buffer.GetTemporaryRT(dirShadowAtlasId, atlasSize, atlasSize, 32, FilterMode.Bilinear, RenderTextureFormat.Shadowmap);
        buffer.SetRenderTarget(dirShadowAtlasId, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);

        buffer.ClearRenderTarget(true, false, Color.clear);

        buffer.BeginSample(bufferName);
        ExcuteBuffer();
        //��������ƽ�й���Ⱦ��Ӱ
        for(int i = 0; i < shadowDirectionalLightCount; ++i)
        {
            RenderDirectionalShadow(i, split, tileSize);
        }
        //�����������Ͱ�Χ������ݷ��͵�GPU
        //buffer.SetGlobalInt(cascadeCountId, shadowSettings.directional.cascadeCount);

        buffer.SetGlobalVectorArray(cascadeCullingSpheresId, cascadeCullingSpheres);
        //���������ݷ��͸�GPU
        buffer.SetGlobalVectorArray(cascadeDataId, cascadeData);
        //��Ӱת��������GPU
        buffer.SetGlobalMatrixArray(dirShadowMatricesId, dirShadowMatrices);
        //buffer.SetGlobalFloat(shadowDistanceId, shadowSettings.maxDistance);
        //��Ӱ�������������ݷ���GPU
        //float f = 1.0f - shadowSettings.directional.cascadeFade;
        //buffer.SetGlobalVector(shadowDistanceFadeId, new Vector4(
        //    1.0f / shadowSettings.maxDistance,  //  1/m,����(1 - ��d / m)) / f
        //    1.0f / shadowSettings.distanceFade, //  1/f,����(1 - ��d / m)) / f
        //    1.0f / (1.0f - f * f)               //����(1 - d^2 / r^2) / f
        //));
        //����PCF�ؼ���
        SetKeyword(directionalFilterKeywords, (int)shadowSettings.directional.filter - 1);
        SetKeyword(cascadeBlendKeywords, (int)shadowSettings.directional.cascadeBlend - 1);

        //����ͼ����С�����ش�С
        //buffer.SetGlobalVector(shadowAtlasSizeId, new Vector4(atlasSize, 1.0f / atlasSize));

        buffer.EndSample(bufferName);
        ExcuteBuffer();
    }

    //��Ⱦ����ƽ�й���Ӱ
    public void RenderDirectionalShadow(int index, int split, int tileSize)
    {
        ShadowDirectionalLight light = shadowDirectionalLights[index];
        ShadowDrawingSettings shadowDrawingSettings = new ShadowDrawingSettings(cullingResults, light.visibleLightIndex);

        int cascadeCount = shadowSettings.directional.cascadeCount;
        int tileOffset = index * cascadeCount;
        Vector3 ratios = shadowSettings.directional.CascadeRadios;
        float cullingFactor = Mathf.Max(0f, 0.8f - shadowSettings.directional.cascadeFade);
        for (int i = 0; i < cascadeCount; ++i)
        {
            //������ͼ��ͶӰ�Ͳü��ռ��������
            cullingResults.ComputeDirectionalShadowMatricesAndCullingPrimitives(
                light.visibleLightIndex,
                i,
                cascadeCount,
                ratios,
                tileSize,
                light.nearPlaneOffset,
                out Matrix4x4 viewMatrix,
                out Matrix4x4 projectionMatrix,
                out ShadowSplitData splitData
            );
            //�õ���һ����Դ�İ�Χ������
            if (index == 0)
            {
                //���ü�������
                SetCascadeData(i, splitData.cullingSphere, tileSize);
            }
            splitData.shadowCascadeBlendCullingFactor = cullingFactor;
            shadowDrawingSettings.splitData = splitData;
            int tileIndex = tileOffset + i;

            //ͶӰ���������ͼ���󣬵õ�������ռ䵽�ƹ�ռ��ת������
            dirShadowMatrices[tileIndex] = ConvertToAtlasMatrix(
                projectionMatrix * viewMatrix,
                SetTileViewport(tileIndex, split, tileSize),
                split
            );
            buffer.SetViewProjectionMatrices(viewMatrix, projectionMatrix);
            //�������ƫ��
            //buffer.SetGlobalDepthBias(100000.0f, 0f);
            //����б�ȱ���ƫ��
            buffer.SetGlobalDepthBias(0, light.slopeScaleBias);
            ExcuteBuffer();
            context.DrawShadows(ref shadowDrawingSettings);
            buffer.SetGlobalDepthBias(0.0f, 0.0f);
        }
    }

    //��Ⱦ�������͹���Ӱ
    public void RenderOtherShadows()
    {
        //����renderTexture
        int atlasSize = (int)shadowSettings.other.atlasSize;
        atlasSizes.z = atlasSize;
        atlasSizes.w = 1.0f / atlasSize;

        buffer.GetTemporaryRT(otherShadowAtlasId, atlasSize, atlasSize, 32, FilterMode.Bilinear, RenderTextureFormat.Shadowmap);
        //ָ����Ⱦ����Ӱ���ݴ洢��RT��
        buffer.SetRenderTarget(otherShadowAtlasId, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
        //�����Ȼ�����
        buffer.ClearRenderTarget(true, false, Color.clear);

        buffer.BeginSample(bufferName);
        ExcuteBuffer();
        //Ҫ�ָ��ͼ�������ʹ�С
        int tiles = shadowDirectionalLightCount;
        int split = tiles <= 1 ? 1 : tiles <= 4 ? 2 : 4;
        int tileSize = atlasSize / split;
        //�������й�Դ��Ⱦ��Ӱ����
        for(int i = 0; i < shadowOtherLightCount; ++i)
        {

        }

        //��Ӱת��������GPU
        buffer.SetGlobalMatrixArray(otherShadowMatricesId, otherShadowMatrices);
        SetKeyword(otherFilterKeywords, (int)shadowSettings.other.filter - 1);

        buffer.EndSample(bufferName);
        ExcuteBuffer();
    }

    //������Ⱦ�ӿڿ���Ⱦ����ͼ��
    Vector2 SetTileViewport(int index, int split, float tileSize)
    {
        Vector2 offset = new Vector2(index % split, index / split);

        buffer.SetViewport(new Rect(offset.x * tileSize, offset.y * tileSize, tileSize, tileSize));
        return offset;
    }

    //����һ��������ռ䵽��Ӱͼ��ռ��ת������
    Matrix4x4 ConvertToAtlasMatrix(Matrix4x4 m, Vector2 offset, int split)
    {
        //���ʹ���˷���ZBuffer
        if(SystemInfo.usesReversedZBuffer)
        {
            m.m20 = -m.m20;
            m.m21 = -m.m21;
            m.m22 = -m.m22;
            m.m23 = -m.m23;
        }
        //���þ�������
        float scale = 1.0f / split;
        m.m00 = (0.5f * (m.m00 + m.m30) + offset.x * m.m30) * scale;
        m.m01 = (0.5f * (m.m01 + m.m31) + offset.x * m.m31) *scale;
        m.m02 = (0.5f * (m.m02 + m.m32) + offset.x * m.m32) *scale;
        m.m03 = (0.5f * (m.m03 + m.m33) + offset.x * m.m33) *scale;
        m.m10 = (0.5f * (m.m10 + m.m30) + offset.y * m.m30) *scale;
        m.m11 = (0.5f * (m.m11 + m.m31) + offset.y * m.m31) *scale;
        m.m12 = (0.5f * (m.m12 + m.m32) + offset.y * m.m32) *scale;
        m.m13 = (0.5f * (m.m13 + m.m33) + offset.y * m.m33) *scale;
        m.m20 = 0.5f * (m.m20 + m.m30);
        m.m21 = 0.5f * (m.m21 + m.m31);
        m.m22 = 0.5f * (m.m22 + m.m32);
        m.m23 = 0.5f * (m.m23 + m.m33);
        return m;
    }

    //���ü�������
    void SetCascadeData(int index, Vector4 cullingSphere, float tileSize)
    {
        //��Χ��ֱ��������Ӱͼ��ߴ�=���ش�С
        float texelSize = 2.0f * cullingSphere.w / tileSize;
        float filterSize = texelSize * ((float)shadowSettings.directional.filter + 1.0f);

        cullingSphere.w -= filterSize;
        //�õ��뾶��ƽ��ֵ
        cullingSphere.w *= cullingSphere.w;
        cascadeCullingSpheres[index] = cullingSphere;
        cascadeData[index] = new Vector4(1.0f / cullingSphere.w, filterSize * Mathf.Sqrt(2.0f));
    }

    //���ùؼ��ֿ�������PCFģʽ
    void SetKeyword(string[] keywords, int enabledIndex)
    {
        //int enabledIndex = (int)shadowSettings.directional.filter - 1;
        for(int i = 0; i < keywords.Length; ++i)
        {
            if (i == enabledIndex)
            {
                buffer.EnableShaderKeyword(keywords[i]);
            }
            else
            {
                buffer.DisableShaderKeyword(keywords[i]);
            }
        }
    }

    //�ͷ���Ⱦ����
    public void Cleanup()
    {
        buffer.ReleaseTemporaryRT(dirShadowAtlasId);
        if(shadowOtherLightCount > 0)
        {
            buffer.ReleaseTemporaryRT(otherShadowAtlasId);
        }
        ExcuteBuffer();
    }
}
