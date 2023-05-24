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

    //SRP�ϲ�����
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
        //���ú�������״̬
        this.useDynamicBatching = useDynamicBatching;
        this.useGPUInstancing = useGPUInstancing;
        this.useLightsPerObject = useLightsPerObject;
        this.colorLUTResolution = colorLUTResolution;
        GraphicsSettings.useScriptableRenderPipelineBatching = useSPRBather;
        //�ƹ�ǿ��������
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
