using System;
using UnityEngine.Rendering;

[Serializable]
public class CameraSettings
{
    public bool overrideSettings = false;
    public PostFXSetting postFXSetting = default;
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
}
