using UnityEngine;

[System.Serializable]
public class CameraBufferSettings
{
    public bool allowHDR;
    public bool copyDepth;
    public bool copyDepthReflection;
    public bool copyColor;
    public bool copyColorReflection;
    [Range(0.1f, 2.0f)]
    public float renderScale;

    public enum BicubicRescalingMode
    {
        Off,
        UpOnly,
        UpAndDowm
    }
    public BicubicRescalingMode bicubicRescaling;
}
