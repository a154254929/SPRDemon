using UnityEngine;
using UnityEngine.Rendering;

[CreateAssetMenu(menuName = "Rendering/Create Custom Render Pipeline")]

public class CustomRenderPipelineAsset : RenderPipelineAsset
{
    [SerializeField]
    bool 
        useDynamicBatching = true,
        useGPUInstancing = true,
        useSRPBatching = true,
        useLightsPerObject = true;

    //��Ӱ����
    [SerializeField]
    ShadowSettings shadowSettings = default;

    [SerializeField]
    //��Ч�ʲ�����
    PostFXSetting postFxSetting = default;

    protected override RenderPipeline CreatePipeline()
    {
        return new CustomRenderPipeline(
            useDynamicBatching,
            useGPUInstancing,
            useSRPBatching,
            useLightsPerObject,
            shadowSettings,
            postFxSetting
        );
    }
}
