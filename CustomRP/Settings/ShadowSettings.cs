using UnityEngine;

//��Ӱ��������
[System.Serializable]
public class ShadowSettings
{
    //��Ӱ��������
    [Min(0f)]
    public float maxDistance = 100.0f;
    //��Ӱ���ɾ���
    [Range(0.001f, 1.0f)]
    public float distanceFade = 0.1f;

    //��Ӱ��ͼ��С
    public enum TextureSize
    {
        _256 = 256,
        _512 = 512,
        _1024 = 1024,
        _2048 = 2048,
        _4096 = 4096,
        _8192 = 8192,
    }

    //PCF�˲�ģʽ
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

    //ƽ�й����Ӱ����
    [System.Serializable]
    public struct Directional
    {
        public TextureSize atlasSize;
        public FilterMode filter;

        //��������
        [Range(1, 4)]
        public int cascadeCount;
        //��������
        [Range(0.0f, 1.0f)]
        public float cascadeRatio1, cascadeRatio2, cascadeRatio3;

        public Vector3 CascadeRadios => new Vector3(cascadeRatio1, cascadeRatio2, cascadeRatio3);

        //������Ӱ����ֵ
        [Range(0.001f, 1.0f)]
        public float cascadeFade;

        public CascadeBlendMode cascadeBlend;

    }
    
    //�������͹����Ӱͼ������
    [System.Serializable]
    public struct Other
    {
        public TextureSize atlasSize;
        public FilterMode filter;
    }

    //Ĭ����Ӱ��ͼ�ߴ�Ϊ1024*1024
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

    //Ĭ����Ӱ��ͼ�ߴ�Ϊ1024*1024
    public Other other = new Other
    {
        atlasSize = TextureSize._1024,
        filter = FilterMode.PCF2x2,
    };
}
