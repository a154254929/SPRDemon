Shader "CustomRP/Transparent"
{
    Properties
    {
        _MainTex("Main Texture", 2D) = "white" {}
        [HideInInspector] _Color("Color for Lightmap", Color) = (0.5, 0.5, 0.5, 1.0)
        _BaseColor("Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _Cutoff("Alpha Cutoff", range(0.0, 1.0)) = 0.5
        [Toggle(_CLIPPING)]_Clipping("Alpha Clipping", int) = 0
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend("Src Blend", int) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend("Dst Blend", int) = 0

        [Enum(Off, 0, On, 1)] _ZWrite("Z Write", int) = 1
        [KeywordEnum(On, Clip, Dither, Off)] _Shadows("Shadows", int) = 0
        
    }
    SubShader
    {
        HLSLINCLUDE
        #include "../ShaderLibrary/Common.hlsl"
        #include "TransparentInput.hlsl"
        ENDHLSL
        Pass
        {
            Blend [_SrcBlend] [_DstBlend], One OneMinusSrcAlpha
            ZWrite [_ZWrite]
            HLSLPROGRAM
            #pragma shader_feature _CLIPPING
            #pragma multi_compile_instancing 
            #pragma vertex TransparentPassVertex
            #pragma fragment TransparentPassFragment
            #include "TransparentPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Tags
            {
                "LightMode" = "ShadowCaster"
            }
            ColorMask 0

            HLSLPROGRAM
            #pragma target 3.5
            //#pragma shader_feature _CLIPPING
            #pragma shader_feature _ _SHADOWS_CLIP _SHADOWS_DITHER
            #pragma multi_compile_instancing 
            #pragma vertex ShadowCasterPassVertex
            #pragma fragment ShadowCasterPassFragment
            #include "ShadowCasterPass.hlsl"
            ENDHLSL
        }
    }
    CustomEditor "CustomShaderGUI"
}
