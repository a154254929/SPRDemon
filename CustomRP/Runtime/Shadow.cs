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
    //级联数据
    static int cascadeDataId = Shader.PropertyToID("_CascadeData");
    static Vector4[] cascadeData = new Vector4[maxCascades];
    //可投射阴影的平行光数量
    const int maxShadowDirectionalLightCount = 4;
    //已储存的可投射阴影的平行光数量
    int shadowDirectionalLightCount;

    //可投射阴影的其他类型光源的最大数量
    const int maxShadowOtherLightCount = 16;
    //已储存的可投射阴影的其他类型光源数量
    int shadowOtherLightCount;

    //最大级联数量
    const int maxCascades = 4;

    //存储阴影转换矩阵
    static Matrix4x4[] dirShadowMatrices = new Matrix4x4[maxShadowDirectionalLightCount * maxCascades];

    //存储阴影转换矩阵
    static Matrix4x4[] otherShadowMatrices = new Matrix4x4[maxShadowOtherLightCount];

    //PCF过滤模式
    static string[] directionalFilterKeywords =
    {
        "_DIRECTIONAL_PCF3",
        "_DIRECTIONAL_PCF5",
        "_DIRECTIONAL_PCF7",
    };

    //其他类型光源的过滤模式
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
        //斜度比例偏差
        public float slopeScaleBias;
        //阴影视锥体近裁剪平面偏移
        public float nearPlaneOffset;
    }

    //储存可投射阴影的可见光索引
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

    //储存平行光的阴影数据
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
            //如果使用了ShadowMask
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

    //储存其他类型光的阴影数据
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

    //阴影渲染
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

        //是否使用阴影蒙版
        buffer.BeginSample(bufferName);
        SetKeyword(shadowNaskKeywords, useShadowMask ? QualitySettings.shadowmaskMode == ShadowmaskMode.Shadowmask ? 0 : 1 : -1);

        //将级联数量和包围球的数据发送到GPU
        buffer.SetGlobalInt(cascadeCountId, shadowSettings.directional.cascadeCount);

        //阴影距离过渡相关数据发送GPU
        float f = 1.0f - shadowSettings.directional.cascadeFade;
        buffer.SetGlobalVector(shadowDistanceFadeId, new Vector4(
            1.0f / shadowSettings.maxDistance,  //  1/m,用于(1 - （d / m)) / f
            1.0f / shadowSettings.distanceFade, //  1/f,用于(1 - （d / m)) / f
            1.0f / (1.0f - f * f)               //用于(1 - d^2 / r^2) / f
        ));

        //传递图集大小和文素大小
        buffer.SetGlobalVector(shadowAtlasSizeId, atlasSizes);
        buffer.EndSample(bufferName);
        ExcuteBuffer();
    }

    //渲染平行光
    public void RenderDirectionalShadows()
    {
        int tiles = shadowDirectionalLightCount * shadowSettings.directional.cascadeCount;
        //要分割的图块的大小和数量
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
        //遍历所有平行光渲染阴影
        for(int i = 0; i < shadowDirectionalLightCount; ++i)
        {
            RenderDirectionalShadow(i, split, tileSize);
        }
        //将级联数量和包围球的数据发送到GPU
        //buffer.SetGlobalInt(cascadeCountId, shadowSettings.directional.cascadeCount);

        buffer.SetGlobalVectorArray(cascadeCullingSpheresId, cascadeCullingSpheres);
        //将级联数据发送给GPU
        buffer.SetGlobalVectorArray(cascadeDataId, cascadeData);
        //阴影转换矩阵传入GPU
        buffer.SetGlobalMatrixArray(dirShadowMatricesId, dirShadowMatrices);
        //buffer.SetGlobalFloat(shadowDistanceId, shadowSettings.maxDistance);
        //阴影距离过渡相关数据发送GPU
        //float f = 1.0f - shadowSettings.directional.cascadeFade;
        //buffer.SetGlobalVector(shadowDistanceFadeId, new Vector4(
        //    1.0f / shadowSettings.maxDistance,  //  1/m,用于(1 - （d / m)) / f
        //    1.0f / shadowSettings.distanceFade, //  1/f,用于(1 - （d / m)) / f
        //    1.0f / (1.0f - f * f)               //用于(1 - d^2 / r^2) / f
        //));
        //设置PCF关键字
        SetKeyword(directionalFilterKeywords, (int)shadowSettings.directional.filter - 1);
        SetKeyword(cascadeBlendKeywords, (int)shadowSettings.directional.cascadeBlend - 1);

        //传递图集大小和文素大小
        //buffer.SetGlobalVector(shadowAtlasSizeId, new Vector4(atlasSize, 1.0f / atlasSize));

        buffer.EndSample(bufferName);
        ExcuteBuffer();
    }

    //渲染单个平行光阴影
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
            //计算视图合投影和裁剪空间的立方体
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
            //得到第一个光源的包围球数据
            if (index == 0)
            {
                //设置级联数据
                SetCascadeData(i, splitData.cullingSphere, tileSize);
            }
            splitData.shadowCascadeBlendCullingFactor = cullingFactor;
            shadowDrawingSettings.splitData = splitData;
            int tileIndex = tileOffset + i;

            //投影矩阵乘以视图矩阵，得到从世界空间到灯光空间的转换矩阵
            dirShadowMatrices[tileIndex] = ConvertToAtlasMatrix(
                projectionMatrix * viewMatrix,
                SetTileViewport(tileIndex, split, tileSize),
                split
            );
            buffer.SetViewProjectionMatrices(viewMatrix, projectionMatrix);
            //设置深度偏差
            //buffer.SetGlobalDepthBias(100000.0f, 0f);
            //设置斜度比例偏差
            buffer.SetGlobalDepthBias(0, light.slopeScaleBias);
            ExcuteBuffer();
            context.DrawShadows(ref shadowDrawingSettings);
            buffer.SetGlobalDepthBias(0.0f, 0.0f);
        }
    }

    //渲染其他类型光阴影
    public void RenderOtherShadows()
    {
        //创建renderTexture
        int atlasSize = (int)shadowSettings.other.atlasSize;
        atlasSizes.z = atlasSize;
        atlasSizes.w = 1.0f / atlasSize;

        buffer.GetTemporaryRT(otherShadowAtlasId, atlasSize, atlasSize, 32, FilterMode.Bilinear, RenderTextureFormat.Shadowmap);
        //指定渲染的阴影数据存储到RT中
        buffer.SetRenderTarget(otherShadowAtlasId, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
        //清除深度缓冲区
        buffer.ClearRenderTarget(true, false, Color.clear);

        buffer.BeginSample(bufferName);
        ExcuteBuffer();
        //要分割的图块数量和大小
        int tiles = shadowDirectionalLightCount;
        int split = tiles <= 1 ? 1 : tiles <= 4 ? 2 : 4;
        int tileSize = atlasSize / split;
        //遍历所有光源渲染阴影数据
        for(int i = 0; i < shadowOtherLightCount; ++i)
        {

        }

        //阴影转换矩阵传入GPU
        buffer.SetGlobalMatrixArray(otherShadowMatricesId, otherShadowMatrices);
        SetKeyword(otherFilterKeywords, (int)shadowSettings.other.filter - 1);

        buffer.EndSample(bufferName);
        ExcuteBuffer();
    }

    //调整渲染视口开渲染单个图块
    Vector2 SetTileViewport(int index, int split, float tileSize)
    {
        Vector2 offset = new Vector2(index % split, index / split);

        buffer.SetViewport(new Rect(offset.x * tileSize, offset.y * tileSize, tileSize, tileSize));
        return offset;
    }

    //返回一个从世界空间到阴影图块空间的转换矩阵
    Matrix4x4 ConvertToAtlasMatrix(Matrix4x4 m, Vector2 offset, int split)
    {
        //如果使用了反向ZBuffer
        if(SystemInfo.usesReversedZBuffer)
        {
            m.m20 = -m.m20;
            m.m21 = -m.m21;
            m.m22 = -m.m22;
            m.m23 = -m.m23;
        }
        //设置矩阵坐标
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

    //设置级联数据
    void SetCascadeData(int index, Vector4 cullingSphere, float tileSize)
    {
        //包围球直径除以阴影图块尺寸=文素大小
        float texelSize = 2.0f * cullingSphere.w / tileSize;
        float filterSize = texelSize * ((float)shadowSettings.directional.filter + 1.0f);

        cullingSphere.w -= filterSize;
        //得到半径的平方值
        cullingSphere.w *= cullingSphere.w;
        cascadeCullingSpheres[index] = cullingSphere;
        cascadeData[index] = new Vector4(1.0f / cullingSphere.w, filterSize * Mathf.Sqrt(2.0f));
    }

    //设置关键字开启哪种PCF模式
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

    //释放渲染纹理
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
