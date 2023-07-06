using UnityEngine;
using UnityEngine.Rendering;

public partial class CameraRenderer
{

    ScriptableRenderContext context;

    Camera camera;

    CullingResults cullResults;

    const string bufferName = "Render Camera";

    bool useHDR;

    static ShaderTagId unlitShaderTagId = new ShaderTagId("SRPDefaultUnlit");
    static ShaderTagId litShaderTagId = new ShaderTagId("CustomLit");

    //static int frameBufferId = Shader.PropertyToID("_CameraFrameBuffer");
    static int colorAttachmentId = Shader.PropertyToID("_CameraColorAttachment");
    static int depthAttachmentId = Shader.PropertyToID("_CameraDepthAttachment");

    static int depthTextureId = Shader.PropertyToID("_CameraDepthTexture");
    //是否在使用深度纹理
    bool useDepthTexture;

    static int colorTextureId = Shader.PropertyToID("_CameraColorTexture");
    bool useColorTexture;

    //是否使用中间帧缓冲
    bool useIntermediateBuffer;

    static int sourceTextureId = Shader.PropertyToID("_SourceTexture");

    static CameraSettings defaultCameraSettings = new CameraSettings();

    Lighting lighting = new Lighting();

    PostFxStack postFxStack = new PostFxStack();

    CommandBuffer buffer = new CommandBuffer
    {
        name = bufferName
    };
    Material material;

    Texture2D missingTexture;

    static bool copyTextureSupported = SystemInfo.copyTextureSupport > CopyTextureSupport.None;

    bool useScaledRendering;
    //最终使用的缓冲区大小
    Vector2Int bufferSize;
    static int bufferSizeId = Shader.PropertyToID("_CameraBufferSize");

    public CameraRenderer(Shader shader)
    {
        material = CoreUtils.CreateEngineMaterial(shader);
        missingTexture = new Texture2D(1, 1)
        {
            hideFlags = HideFlags.HideAndDontSave,
            name = "Missing"
        };
        missingTexture.SetPixel(0, 0, Color.white * 0.5f);
        missingTexture.Apply(true, true);
    }

    public void Dispose()
    {
        CoreUtils.Destroy(material);
        CoreUtils.Destroy(missingTexture);
    }

    public void Render(
        ScriptableRenderContext context,
        Camera camera,
        CameraBufferSettings cameraBufferSettings,
        bool useDynamicBatching,
        bool useGPUInstancing,
        bool useLightsPerObject,
        ShadowSettings shadowSettings,
        PostFXSetting postFXSettings,
        int colorLUTResolution
    )
    {
        this.context = context;
        this.camera = camera;
        CustomRenderPipelineCamera crpCamera = camera.GetComponent<CustomRenderPipelineCamera>();
        CameraSettings cameraSettings = crpCamera ? crpCamera.Settings : defaultCameraSettings;
        //useDepthTexture = true;
        if(camera.cameraType == CameraType.Reflection)
        {
            useDepthTexture = cameraBufferSettings.copyDepthReflection;
            useColorTexture = cameraBufferSettings.copyColorReflection;
        }
        else
        {
            useDepthTexture = cameraBufferSettings.copyDepth && cameraSettings.copyDepth;
            useColorTexture = cameraBufferSettings.copyColor && cameraSettings.copyColor;
        }
        //如果需要覆盖后处理配置,将渲染管线的后处理配置替换成该相机的后处理配置
        if (cameraSettings.overrideSettings)
        {
            postFXSettings = cameraSettings.postFXSetting;
        }

        float renderScale = cameraSettings.GetRenderScale(cameraBufferSettings.renderScale);
        useScaledRendering = renderScale < 0.99f || renderScale > 1.01f;

        //设置命令缓冲区的名字
        PrepareBuffer();

        //在Game视图绘制的几何体也绘制到Scene视图中
        PrepareForSceneWindow();

        if(!Cull(shadowSettings.maxDistance))
        {
            return;
        }
        useHDR = cameraBufferSettings.allowHDR && camera.allowHDR;
        //按比例缩放相机屏幕像素尺寸
        if(useScaledRendering)
        {
            renderScale = Mathf.Clamp(renderScale, 0.1f, 2.0f);
            bufferSize.x = (int)(camera.pixelWidth * renderScale);
            bufferSize.y = (int)(camera.pixelHeight * renderScale);
        }
        else
        {
            bufferSize.x = camera.pixelWidth;
            bufferSize.y = camera.pixelHeight;
        }
        buffer.BeginSample(SampleName);
        buffer.SetGlobalVector(bufferSizeId, new Vector4(
            1.0f / bufferSize.x,
            1.0f / bufferSize.y,
            bufferSize.x,
            bufferSize.y
        ));
        ExcuteBuffer();
        //设置光照参数
        lighting.SetUp(
            context,
            cullResults,
            shadowSettings,
            useLightsPerObject,
            cameraSettings.maskLights ? cameraSettings.renderingLayerMask : -1
        );
        postFxStack.Setup(
            context,
            camera,
            postFXSettings,
            useHDR,
            colorLUTResolution,
            cameraSettings.finalBlendMode,
            bufferSize,
            cameraBufferSettings.bicubicRescaling
        );
        buffer.EndSample(SampleName);
        Setup();
        //绘制SRP不支持的着色器类型
        DrawUnsupportedShader();
        //绘制可见集合体
        DrawVisibleGeometry(useDynamicBatching, useGPUInstancing, useLightsPerObject, cameraSettings.renderingLayerMask);
        //绘制Fizmos
        DrawGizmosBeforeFX();
        if(postFxStack.IsActive)
        {
            postFxStack.Render(colorAttachmentId);
        }
        else if(useIntermediateBuffer)
        {
            Draw(colorAttachmentId, BuiltinRenderTextureType.CameraTarget);
            ExcuteBuffer();
        }
        DrawGizmosAfterFX();
        Cleanup();
        Submit();
    }

    void Setup()
    {
        context.SetupCameraProperties(camera);
        //得到相机的clear flags
        CameraClearFlags falgs = camera.clearFlags;

        useIntermediateBuffer = useScaledRendering || useDepthTexture || postFxStack.IsActive || useColorTexture;
        if (useIntermediateBuffer)
        {
            if (falgs > CameraClearFlags.Color)
            {
                falgs = CameraClearFlags.Color;
            }
            buffer.GetTemporaryRT(
                colorAttachmentId,
                bufferSize.x,
                bufferSize.y,
                0,
                FilterMode.Bilinear,
                useHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default
            );
            buffer.GetTemporaryRT(
                depthAttachmentId,
                bufferSize.x,
                bufferSize.y,
                32,
                FilterMode.Point,
                RenderTextureFormat.Depth
            );
            //SetRenderTarget调用后会把图像渲染到frameBufferId对应的图像上而不是相机上
            buffer.SetRenderTarget(
                colorAttachmentId, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store,
                depthAttachmentId, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store
            );
        }
        //设置相机的清除状态
        buffer.ClearRenderTarget(
            falgs <= CameraClearFlags.Depth,
            falgs == CameraClearFlags.Color,
            falgs == CameraClearFlags.Color ? camera.backgroundColor.linear : Color.clear
        );
        buffer.BeginSample(SampleName);
        buffer.SetGlobalTexture(depthTextureId, missingTexture);
        buffer.SetGlobalTexture(colorTextureId, missingTexture);
        ExcuteBuffer();
    }

    void Cleanup()
    {
        lighting.Cleanup();
        if(useIntermediateBuffer)
        {
            buffer.ReleaseTemporaryRT(colorAttachmentId);
            buffer.ReleaseTemporaryRT(depthAttachmentId);
            //释放临时深度纹理
            if (useDepthTexture)
            {
                buffer.ReleaseTemporaryRT(depthTextureId);
            }
            if (useColorTexture)
            {
                buffer.ReleaseTemporaryRT(colorTextureId);
            }
        }
    }

    //拷贝深度数据
    void CopyAttachments()
    {
        if(useColorTexture)
        {
            buffer.GetTemporaryRT(
                colorTextureId,
                bufferSize.x,
                bufferSize.y,
                0,
                FilterMode.Bilinear,
                useHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default
            );
            if (copyTextureSupported)
            {
                buffer.CopyTexture(colorAttachmentId, colorTextureId);
            }
            else
            {
                Draw(colorAttachmentId, colorTextureId, true);
            }
        }
        if(useDepthTexture)
        {
            buffer.GetTemporaryRT(
                depthTextureId,
                bufferSize.x,
                bufferSize.y,
                32,
                FilterMode.Point,
                RenderTextureFormat.Depth
            );
            if (copyTextureSupported)
            {
                buffer.CopyTexture(depthAttachmentId, depthTextureId);
            }
            else
            {
                Draw(depthAttachmentId, depthTextureId, true);
            }
        }
        if(!copyTextureSupported)
        {
            buffer.SetRenderTarget(
                colorAttachmentId, RenderBufferLoadAction.Load, RenderBufferStoreAction.Store,
                depthAttachmentId, RenderBufferLoadAction.Load, RenderBufferStoreAction.Store
            );
        }
        ExcuteBuffer();
    }

    void Submit()
    {
        buffer.EndSample(SampleName);
        ExcuteBuffer();
        context.Submit();
    }

    void ExcuteBuffer()
    {
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();
    }

    void DrawVisibleGeometry(bool useDynamicBatching, bool useGPUInstancing, bool useLightsPerObject, int renderingLatyerMask)
    {
        PerObjectData lightPerObjectFlags = useLightsPerObject ? PerObjectData.LightData | PerObjectData.LightIndices : PerObjectData.None;
        SortingSettings sortingSettings = new SortingSettings(camera)
        {
            criteria = SortingCriteria.CommonOpaque
        };
        DrawingSettings drawingSettings = new DrawingSettings(unlitShaderTagId, sortingSettings)
        {
            //设置渲染时批处理的使用状态
            enableDynamicBatching = useDynamicBatching,
            enableInstancing = useGPUInstancing,
            perObjectData = 
                PerObjectData.Lightmaps
                | PerObjectData.ShadowMask
                | PerObjectData.LightProbe
                | PerObjectData.LightProbeProxyVolume
                | PerObjectData.OcclusionProbe
                | PerObjectData.OcclusionProbeProxyVolume
                | PerObjectData.ReflectionProbes
                | lightPerObjectFlags
        };
        drawingSettings.SetShaderPassName(1, litShaderTagId);

        FilteringSettings filterSettings = new FilteringSettings(RenderQueueRange.opaque, renderingLayerMask: (uint)renderingLatyerMask);

        context.DrawRenderers(cullResults, ref drawingSettings, ref filterSettings);

        context.DrawSkybox(camera);
        if (useColorTexture || useDepthTexture)
        {
            CopyAttachments();
        }
        sortingSettings.criteria = SortingCriteria.CommonTransparent;
        drawingSettings.sortingSettings = sortingSettings;
        filterSettings.renderQueueRange = RenderQueueRange.transparent;
        context.DrawRenderers(cullResults, ref drawingSettings, ref filterSettings);
    }

    void Draw(RenderTargetIdentifier from, RenderTargetIdentifier to, bool isDepth = false)
    {
        buffer.SetGlobalTexture(sourceTextureId, from);
        buffer.SetRenderTarget(to, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
        buffer.DrawProcedural(Matrix4x4.identity, material, isDepth ? 1 : 0, MeshTopology.Triangles, 3);
    }

    bool Cull(float maxShadowDistance)
    {
        if (camera.TryGetCullingParameters(out ScriptableCullingParameters scriptablCullparams))
        {
            //得到最大的阴影距离，合相机远截面作比较，取最小的那个作为阴影距离
            scriptablCullparams.shadowDistance = Mathf.Min(maxShadowDistance, camera.farClipPlane);
            cullResults = context.Cull(ref scriptablCullparams);
            return true;
        }
        return false;
    }
}
