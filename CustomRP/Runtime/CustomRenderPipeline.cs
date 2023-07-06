using UnityEngine;
using UnityEngine.Rendering;

public partial class CustomRenderPipeline : RenderPipeline
{
    bool useDynamicBatching, useGPUInstancing, useLightsPerObject;
    CameraBufferSettings cameraBufferSettings;
    //CameraRenderer renderer = new CameraRenderer();
    CameraRenderer cameraRenderer;
    ShadowSettings shadowSettings;
    PostFXSetting postFXSetting;
    int colorLUTResolution;

    //SRP合并测试
    public CustomRenderPipeline(
        CameraBufferSettings cameraBufferSettings,
        bool useDynamicBatching,
        bool useGPUInstancing,
        bool useSPRBather,
        bool useLightsPerObject,
        ShadowSettings shadowSettings,
        PostFXSetting postFXSetting,
        int colorLUTResolution,
        Shader cameraRendererShader

    )
    {
        this.cameraBufferSettings = cameraBufferSettings;
        //设置合批启用状态
        this.useDynamicBatching = useDynamicBatching;
        this.useGPUInstancing = useGPUInstancing;
        this.useLightsPerObject = useLightsPerObject;
        this.colorLUTResolution = colorLUTResolution;
        GraphicsSettings.useScriptableRenderPipelineBatching = useSPRBather;
        //灯光强度用线性
        GraphicsSettings.lightsUseLinearIntensity = true;

        this.shadowSettings = shadowSettings;
        this.postFXSetting = postFXSetting;
        InitializeForEditor();
        cameraRenderer = new CameraRenderer(cameraRendererShader);
    }

    protected override void Render(ScriptableRenderContext context, Camera[] cameras)
    {
        for (int i = 0; i < cameras.Length; ++i)
        {
            cameraRenderer.Render(
                context,
                cameras[i],
                cameraBufferSettings,
                useDynamicBatching,
                useGPUInstancing,
                useLightsPerObject,
                shadowSettings,
                postFXSetting,
                colorLUTResolution
            );
        }
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        DisposeForEditor();
        cameraRenderer.Dispose();
    }
}
