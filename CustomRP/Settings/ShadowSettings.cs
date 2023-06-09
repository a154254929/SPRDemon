using UnityEngine;

//阴影属性设置
[System.Serializable]
public class ShadowSettings
{
    //阴影的最大距离
    [Min(0f)]
    public float maxDistance = 100.0f;
    //阴影过渡距离
    [Range(0.001f, 1.0f)]
    public float distanceFade = 0.1f;

    //阴影贴图大小
    public enum TextureSize
    {
        _256 = 256,
        _512 = 512,
        _1024 = 1024,
        _2048 = 2048,
        _4096 = 4096,
        _8192 = 8192,
    }

    //PCF滤波模式
    public enum FilterMode
    {
        PCF2x2,
        PCF3x3,
        PCF5x5,
        PCF7x7,
    }

    public enum CascadeBlendMode
    {
        Hard,
        Soft,
        Dither
    }

    //平行光的阴影设置
    [System.Serializable]
    public struct Directional
    {
        public TextureSize atlasSize;
        public FilterMode filter;

        //级联数量
        [Range(1, 4)]
        public int cascadeCount;
        //级联比例
        [Range(0.0f, 1.0f)]
        public float cascadeRatio1, cascadeRatio2, cascadeRatio3;

        public Vector3 CascadeRadios => new Vector3(cascadeRatio1, cascadeRatio2, cascadeRatio3);

        //级联阴影淡入值
        [Range(0.001f, 1.0f)]
        public float cascadeFade;

        public CascadeBlendMode cascadeBlend;

    }
    
    //其他类型光的阴影图集设置
    [System.Serializable]
    public struct Other
    {
        public TextureSize atlasSize;
        public FilterMode filter;
    }

    //默认阴影贴图尺寸为1024*1024
    public Directional directional = new Directional
    {
        atlasSize = TextureSize._1024,
        filter = FilterMode.PCF2x2,
        cascadeCount = 4,
        cascadeRatio1 = 0.1f,
        cascadeRatio2 = 0.25f,
        cascadeRatio3 = 0.5f,
        cascadeFade = 0.1f,
        cascadeBlend = CascadeBlendMode.Hard,
    };

    //默认阴影贴图尺寸为1024*1024
    public Other other = new Other
    {
        atlasSize = TextureSize._1024,
        filter = FilterMode.PCF2x2,
    };
}
