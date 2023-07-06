using UnityEngine;
using System;

[CreateAssetMenu(menuName = "Rendering/Custom Post Fx Settings")]
public class PostFXSetting : ScriptableObject
{

    [SerializeField]
    Shader shader = default;

    [NonSerialized]
    Material material;

    public Material Material
    {
        get
        {
            if(material == null && shader != null)
            {
                material = new Material(shader);
                material.hideFlags = HideFlags.HideAndDontSave;
            }
            return material;
        }
    }

    [Serializable]
    public struct BloomSettings
    {
        [Range(0.0f, 16.0f)]
        public int maxIterations;

        [Min(1.0f)]
        public int downscaleLimit;

        public bool bicubicUpsampling;

        [Min(0.0f)]
        public float threshold;

        [Range(0.0f, 1.0f)]
        public float thresholdKnee;

        [Min(0.0f)]
        public float intensity;

        //淡化闪烁
        public bool fadeFireflies;
        public enum Mode
        {
            Additive,
            Scattering,
        }

        public Mode mode;

        [Range(0.05f, 0.95f)]
        public float scatter;

        //是否忽略渲染缩放
        public bool ignoreRenderScale;
    }

    [SerializeField]
    BloomSettings bloom = new BloomSettings
    {
        scatter = 0.7f
    };

    public BloomSettings Bloom => bloom;

    [Serializable]
    public struct ToneMappingSettings
    {
        public enum Mode
        {
            None,
            ACES,
            Neutral,
            Reinhard,
        }
        public Mode mode;
    }

    [SerializeField]
    ToneMappingSettings toneMapping = default;

    public ToneMappingSettings ToneMapping => toneMapping;

    [Serializable]
    public struct ColorAdjustmentSettings
    {
        //后曝光,调整场景的整体曝光度
        public float postExposure;

        //对比度
        [Range(-100.0f, 100.0f)]
        public float constrast;

        //颜色滤镜,通过乘以颜色来给渲染器着色
        [ColorUsage(false, true)]
        public Color colorFilter;

        //颜色偏移,改变所有颜色的强度
        [Range(-180.0f, 180.0f)]
        public float hueShift;

        //饱和度
        [Range(-100.0f, 100.0f)]
        public float saturation;

    }

    [SerializeField]
    ColorAdjustmentSettings colorAdjustments = new ColorAdjustmentSettings
    {
        colorFilter = Color.white
    };

    public ColorAdjustmentSettings ColorAdjustments => colorAdjustments;

    [Serializable]
    public struct WhiteBalanceSettings
    {
        //色温,调整白平衡的冷暖偏向
        [Range(-100.0f, 100.0f)]
        public float temperature;
        //色调,调整温度变化后的颜色
        [Range(-100.0f, 100.0f)]
        public float tint;
    }

    [SerializeField]
    WhiteBalanceSettings whiteBalance = default;

    public WhiteBalanceSettings WhiteBalance => whiteBalance;

    [Serializable]
    public struct SplitToningSettings
    {
        //用于对阴影和高光着色
        [ColorUsage(false)]
        public Color shadows, highlights;

        //设置阴影和高光直接平衡的滑块
        [Range(-100.0f, 100.0f)]
        public float balance;
    }

    [SerializeField]
    SplitToningSettings splitToning = new SplitToningSettings
    {
        shadows = Color.gray,
        highlights = Color.gray,
    };

    public SplitToningSettings SplitToning => splitToning;

    [Serializable]
    public struct ChannelMixerSettings
    {
        public Vector3 red, green, blue;
    }

    [SerializeField]
    ChannelMixerSettings channelMixer = new ChannelMixerSettings
    {
        red = Vector3.right,
        green = Vector3.up,
        blue = Vector3.forward,
    };

    public ChannelMixerSettings ChannelMixer => channelMixer;

    [Serializable]
    public struct ShadowsMidtonesHighlightsSettings
    {
        [ColorUsage(false, true)]
        public Color shadows, midtones, highlights;

        [Range(0.0f, 2.0f)]
        public float shadowsStart, shadowsEnd, highlightsStart, highlightsEnd;
    };

    [SerializeField]
    ShadowsMidtonesHighlightsSettings shadowsMidtonesHighlights = new ShadowsMidtonesHighlightsSettings
    {
        shadows = Color.white,
        midtones = Color.white,
        highlights = Color.white,
        shadowsEnd = 0.3f,
        highlightsStart = 0.55f,
        highlightsEnd = 1.0f,
    };

    public ShadowsMidtonesHighlightsSettings ShadowsMidtonesHighlight => shadowsMidtonesHighlights;
}
