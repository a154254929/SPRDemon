using UnityEngine;
using UnityEngine.Rendering;

public partial class CustomRenderPipeline : RenderPipeline
{
    bool useDynamicBatching, useGPUInstancing, useLightsPerObject;
    bool allowHDR;
    CameraRenderer renderer = new CameraRenderer();
    ShadowSettings shadowSettings;
    PostFXSetting postFXSetting;
    int colorLUTResolution;

    //SRP合并测试
    public CustomRenderPipeline(
        bool allowHDR,
        bool useDynamicBatching,
        bool useGPUInstancing,
        bool useSPRBather,
        bool useLightsPerObject,
        ShadowSettings shadowSettings,
        PostFXSetting postFXSetting,
        int colorLUTResolution
    )
    {
        this.allowHDR = allowHDR;
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
    }

    protected override void Render(ScriptableRenderContext context, Camera[] cameras)
    {
        for (int i = 0; i < cameras.Length; ++i)
        {
            renderer.Render(
                context,
                cameras[i],
                allowHDR,
                useDynamicBatching,
                useGPUInstancing,
                useLightsPerObject,
                shadowSettings,
                postFXSetting,
                colorLUTResolution
            );
        }
    }
}
