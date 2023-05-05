using UnityEngine;
using UnityEngine.Rendering;

public partial class CustomRenderPipeline : RenderPipeline
{
    bool useDynamicBatching, useGPUInstancing, useLightsPerObject;
    CameraRenderer renderer = new CameraRenderer();
    ShadowSettings shadowSettings;

    //SRP�ϲ�����
    public CustomRenderPipeline(bool useDynamicBatching, bool useGPUInstancing, bool useSPRBather, bool useLightsPerObject, ShadowSettings shadowSettings)
    {
        //���ú�������״̬
        this.useDynamicBatching = useDynamicBatching;
        this.useGPUInstancing = useGPUInstancing;
        this.useLightsPerObject = useLightsPerObject;
        GraphicsSettings.useScriptableRenderPipelineBatching = useSPRBather;
        //�ƹ�ǿ��������
        GraphicsSettings.lightsUseLinearIntensity = true;

        this.shadowSettings = shadowSettings;
        InitializeForEditor();
    }

    protected override void Render(ScriptableRenderContext context, Camera[] cameras)
    {
        for (int i = 0; i < cameras.Length; ++i)
        {
            renderer.Render(context, cameras[i], useDynamicBatching, useGPUInstancing, useLightsPerObject, shadowSettings);
        }
    }
}
