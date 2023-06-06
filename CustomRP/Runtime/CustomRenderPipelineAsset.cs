using UnityEngine;
using UnityEngine.Rendering;

[CreateAssetMenu(menuName = "Rendering/Create Custom Render Pipeline")]

public partial class CustomRenderPipelineAsset : RenderPipelineAsset
{
    [SerializeField]
    bool 
        useDynamicBatching = true,
        useGPUInstancing = true,
        useSRPBatching = true,
        useLightsPerObject = true;

    [SerializeField]
    bool allowHDR = true;

    //��Ӱ����
    [SerializeField]
    ShadowSettings shadowSettings = default;

    [SerializeField]
    //��Ч�ʲ�����
    PostFXSetting postFxSetting = default;

    public enum ColorLUTResolution
    {
        _16 = 16,
        _32 = 32,
        _64 = 64,
    }

    //LUS�ֱ���
    [SerializeField]
    ColorLUTResolution colorLUTResolution = ColorLUTResolution._32;

    protected override RenderPipeline CreatePipeline()
    {
        return new CustomRenderPipeline(
            allowHDR,
            useDynamicBatching,
            useGPUInstancing,
            useSRPBatching,
            useLightsPerObject,
            shadowSettings,
            postFxSetting,
            (int)colorLUTResolution
        );
    }
}
