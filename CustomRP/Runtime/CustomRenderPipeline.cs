using UnityEngine;
using UnityEngine.Rendering;

public class CustomRenderPipeline : RenderPipeline
{
    bool useDynamicBatching, useGPUInstancing;
    CameraRenderer renderer = new CameraRenderer();
    ShadowSettings shadowSettings;

    //SRP�ϲ�����
    public CustomRenderPipeline(bool useDynamicBatching, bool useGPUInstancing, bool useSPRBather, ShadowSettings shadowSettings)
    {
        //���ú�������״̬
        this.useDynamicBatching = useDynamicBatching;
        this.useGPUInstancing = useGPUInstancing;
        GraphicsSettings.useScriptableRenderPipelineBatching = useSPRBather;
        //�ƹ�ǿ��������
        GraphicsSettings.lightsUseLinearIntensity = true;

        this.shadowSettings = shadowSettings;
    }

    protected override void Render(ScriptableRenderContext context, Camera[] cameras)
    {
        for (int i = 0; i < cameras.Length; ++i)
        {
            renderer.Render(context, cameras[i], useDynamicBatching, useGPUInstancing, shadowSettings);
        }
    }
}
