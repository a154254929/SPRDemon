using System;
using UnityEngine;
using UnityEngine.Rendering;

[Serializable]
public class CameraSettings
{
    public bool overrideSettings = false;
    public PostFXSetting postFXSetting = default;
    [RenderingLayerMaskField]
    public int renderingLayerMask = -1;
    public bool maskLights = false;
    public bool copyDepth = true;
    public bool copyColor = true;
    [Serializable]
    public struct FinalBlendMode
    {
        public BlendMode source, destination;
    }

    public FinalBlendMode finalBlendMode = new FinalBlendMode
    {
        source = BlendMode.One,
        destination = BlendMode.Zero,
    };
    
    public enum RenderScaleMode
    {
        Inherit,
        Multiply,
        Override
    }

    public RenderScaleMode renderScaleMode = RenderScaleMode.Inherit;

    [Range(0.1f, 2.0f)]
    public float renderScale = 1.0f;

    public float GetRenderScale(float scale)
    {
        switch (renderScaleMode)
        {
            case RenderScaleMode.Inherit:
                return scale;
            case RenderScaleMode.Multiply:
                return scale * renderScale;
            case RenderScaleMode.Override:
                return renderScale;
            default:
                return 1.0f;
        }
    }
}
