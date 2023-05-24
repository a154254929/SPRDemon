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

    static int frameBufferId = Shader.PropertyToID("_CameraFrameBuffer");

    Lighting lighting = new Lighting();

    PostFxStack postFxStack = new PostFxStack();

    CommandBuffer buffer = new CommandBuffer
    {
        name = bufferName
    };

    public void Render(
        ScriptableRenderContext context,
        Camera camera,
        bool allowHDR,
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

        //�����������������
        PrepareBuffer();

        //��Game��ͼ���Ƶļ�����Ҳ���Ƶ�Scene��ͼ��
        PrepareForSceneWindow();

        if(!Cull(shadowSettings.maxDistance))
        {
            return;
        }
        useHDR = allowHDR && camera.allowHDR;
        buffer.BeginSample(SampleName);
        ExcuteBuffer();
        //���ù��ղ���
        lighting.SetUp(context, cullResults, shadowSettings, useLightsPerObject);
        postFxStack.Setup(context, camera, postFXSettings, useHDR, colorLUTResolution);
        buffer.EndSample(SampleName);
        Setup();
        //����SRP��֧�ֵ���ɫ������
        DrawUnsupportedShader();
        //���ƿɼ�������
        DrawVisibleGeometry(useDynamicBatching, useGPUInstancing, useLightsPerObject);
        //����Fizmos
        DrawGizmosBeforeFX();
        if(postFxStack.IsActive)
        {
            postFxStack.Render(frameBufferId);
        }
        DrawGizmosAfterFX();
        Cleanup();
        Submit();
    }

    void Setup()
    {
        context.SetupCameraProperties(camera);
        //�õ������clear flags
        CameraClearFlags falgs = camera.clearFlags;

        if (postFxStack.IsActive)
        {
            //if (falgs > CameraClearFlags.Color)
            //{
            //    falgs = CameraClearFlags.Color;
            //}
            buffer.GetTemporaryRT(
                frameBufferId,
                camera.pixelWidth,
                camera.pixelHeight,
                32,
                FilterMode.Bilinear,
                useHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default
            );
            //SetRenderTarget���ú���ͼ����Ⱦ��frameBufferId��Ӧ��ͼ���϶����������
            buffer.SetRenderTarget(
                frameBufferId,
                RenderBufferLoadAction.DontCare,
                RenderBufferStoreAction.Store
            );
        }
        //������������״̬
        buffer.ClearRenderTarget(falgs <= CameraClearFlags.Depth, falgs == CameraClearFlags.Color,
            falgs == CameraClearFlags.Color ? camera.backgroundColor.linear : Color.clear);
        buffer.BeginSample(SampleName);
        ExcuteBuffer();
    }

    void Cleanup()
    {
        lighting.Cleanup();
        if(postFxStack.IsActive)
        {
            buffer.ReleaseTemporaryRT(frameBufferId);
        }
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

    void DrawVisibleGeometry(bool useDynamicBatching, bool useGPUInstancing, bool useLightsPerObject)
    {
        PerObjectData lightPerObjectFlags = useLightsPerObject ? PerObjectData.LightData | PerObjectData.LightIndices : PerObjectData.None;
        SortingSettings sortingSettings = new SortingSettings(camera)
        {
            criteria = SortingCriteria.CommonOpaque
        };
        DrawingSettings drawingSettings = new DrawingSettings(unlitShaderTagId, sortingSettings)
        {
            //������Ⱦʱ�������ʹ��״̬
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

        FilteringSettings filterSettings = new FilteringSettings(RenderQueueRange.opaque);

        context.DrawRenderers(cullResults, ref drawingSettings, ref filterSettings);

        context.DrawSkybox(camera);

        sortingSettings.criteria = SortingCriteria.CommonTransparent;
        drawingSettings.sortingSettings = sortingSettings;
        filterSettings.renderQueueRange = RenderQueueRange.transparent;
        context.DrawRenderers(cullResults, ref drawingSettings, ref filterSettings);
    }

    bool Cull(float maxShadowDistance)
    {
        if (camera.TryGetCullingParameters(out ScriptableCullingParameters scriptablCullparams))
        {
            //�õ�������Ӱ���룬�����Զ�������Ƚϣ�ȡ��С���Ǹ���Ϊ��Ӱ����
            scriptablCullparams.shadowDistance = Mathf.Min(maxShadowDistance, camera.farClipPlane);
            cullResults = context.Cull(ref scriptablCullparams);
            return true;
        }
        return false;
    }
}
