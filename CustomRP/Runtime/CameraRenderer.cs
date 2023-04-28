using UnityEngine;
using UnityEngine.Rendering;

public partial class CameraRenderer
{

    ScriptableRenderContext context;

    Camera camera;

    CullingResults cullResults;

    const string bufferName = "Render Camera";

    static ShaderTagId unlitShaderTagId = new ShaderTagId("SRPDefaultUnlit");
    static ShaderTagId litShaderTagId = new ShaderTagId("CustomLit");

    Lighting lighting = new Lighting();

    CommandBuffer buffer = new CommandBuffer
    {
        name = bufferName
    };

    public void Render(
        ScriptableRenderContext context,
        Camera camera,
        bool useDynamicBatching,
        bool useGPUInstancing,
        ShadowSettings shadowSettings
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
        buffer.BeginSample(SampleName);
        ExcuteBuffer();
        //���ù��ղ���
        lighting.SetUp(context, cullResults, shadowSettings);
        buffer.EndSample(SampleName);
        Setup();
        //����SRP��֧�ֵ���ɫ������
        DrawUnsupportedShader();
        //���ƿɼ�������
        DrawVisibleGeometry(useDynamicBatching, useGPUInstancing);
        //����Fizmos
        DrawFizmos();
        lighting.Cleanup();
        Submit();
    }

    void Setup()
    {
        context.SetupCameraProperties(camera);
        //�õ������clear flags
        CameraClearFlags falgs = camera.clearFlags;
        //������������״̬
        buffer.ClearRenderTarget(falgs <= CameraClearFlags.Depth, falgs == CameraClearFlags.Color,
            falgs == CameraClearFlags.Color ? camera.backgroundColor.linear : Color.clear);
        buffer.BeginSample(SampleName);
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

    void DrawVisibleGeometry(bool useDynamicBatching, bool useGPUInstancing)
    {
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
