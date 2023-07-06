Shader "CustomRP/Lit"
{
    Properties
    {
        _MainTex("Main Texture", 2D) = "white" {}
        [NoScaleOffset]_NormalMap("Normals", 2D) = "bump" {}
        [Toggle(_NORMAL_MAP)] _NormalMapToggle("Normal Map", float) = 0
        _NormalScale("Normal Scale", Range(0.0, 1.0)) = 1.0
        [HideInInspector] _Color("Color for Lightmap", Color) = (0.5, 0.5, 0.5, 1.0)
        _BaseColor("Color", Color) = (0.5, 0.5, 0.5, 1.0)
        _Metallic("Matelllic", Range(0.0, 1.0)) = 0
        _Smoothness("Smoothness", Range(0.0, 1.0)) = 0.5
        _Fresnel("Fresnel", Range(0.0, 1.0)) = 1.0
        _Cutoff("Alpha Cutoff", range(0.0, 1.0)) = 0.5
        [NoScaleOffset]_EmissionMap("Emission", 2D) = "white" {}
        [HDR]_EmissionColor("Emission", Color) = (1.0, 1.0, 1.0, 1.0)
        [Toggle(_MASK_MAP)] _MaskMapToggle("Mask Map", float) = 0
        [NoScaleOffset]_MaskMap("Mask(MODS)", 2D) = "white" {}
        //遮挡强度
        _Occlusion("Occlusion", Range(0,1)) = 1
        //细节纹理
        [Toggle(_DETAIL_MAP)] _DetailMapToggle("Detail Map", float) = 0
        _DetailMap("Details", 2D) = "linearGray" {}
        _DetailAlbedo("Detail Albedo", Range(0,1)) = 1
        _DetailSmoothness("Detail Smoothness", Range(0,1)) = 1
        [NoScaleOffset]_DetailNormalMap("Detail Normals", 2D) = "bump" {}
        _DetailNormalScale("Detail Normal Scale", Range(0.0, 1.0)) = 1.0

        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend("Src Blend", int) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend("Dst Blend", int) = 0

        [Enum(Off, 0, On, 1)]_ZWrite("Z Write", int) = 1
        [KeywordEnum(On, Clip, Dither, Off)]_Shadows("Shadows", int) = 0

        [Toggle(_CLIPPING)] _Clipping("Alpha Clipping", int) = 0
        [Toggle(_PREMULTIPLY_ALPHA)] _PremulAlpha("Premultipy Alpha", int) = 0
        [Toggle(_RECEIVE_SHADOWS)] _ReceiveShadows("Receive Shadows", int) = 1
    }
    SubShader
    {
        HLSLINCLUDE
        #include "../ShaderLibrary/Common.hlsl"
        #include "LitInput.hlsl"
        ENDHLSL
        Pass
        {
            Tags
            {
                "LightMode" = "CustomLit"
            }
            Blend [_SrcBlend] [_DstBlend], One OneMinusSrcAlpha
            ZWrite [_ZWrite]
            HLSLPROGRAM
            #pragma target 3.5
            #pragma shader_feature _CLIPPING
            #pragma shader_feature _RECEIVE_SHADOWS
            #pragma shader_feature _NORMAL_MAP
            #pragma shader_feature _MASK_MAP
            #pragma shader_feature _DETAIL_MAP
            //是否透明通道预乘
            #pragma shader_feature _PREMULTIPLY_ALPHA
            #pragma multi_compile _ _DIRECTIONAL_PCF3 _DIRECTIONAL_PCF5 _DIRECTIONAL_PCF7
            #pragma multi_compile _ _OTHER_PCF3 _OTHER_PCF5 _OTHER_PCF7
            #pragma multi_compile _ _CASCADE_BLEND_SOFT _CASCADE_BLEND_DITHER
            #pragma multi_compile _ _SHADOW_MASK_ALWAYS _SHADOW_MASK_DISTANCE
            #pragma multi_compile _ LOD_FADE_CROSSFADE
            #pragma multi_compile _ LIGHTMAP_ON
            //是否使用逐对象光源
            #pragma multi_compile _ _LIGHTS_PER_OBJECT
            #pragma multi_compile_instancing 
            #pragma vertex LitPassVertex
            #pragma fragment LitPassFragment
            #include "LitPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Tags
            {
                "LightMode" = "ShadowCaster"
            }
            Cull Off
            ColorMask 0

            HLSLPROGRAM
            #pragma target 3.5
            //#pragma shader_feature _CLIPPING
            #pragma shader_feature _ _SHADOWS_CLIP _SHADOWS_DITHER
            #pragma multi_compile_instancing 
            #pragma multi_compile _ LOD_FADE_CROSSFADE
            #pragma vertex ShadowCasterPassVertex
            #pragma fragment ShadowCasterPassFragment
            #include "ShadowCasterPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Tags
            {
                "LightMode" = "Meta"
            }

            Cull Off

            HLSLPROGRAM
            #pragma target 3.5
            //#pragma shader_feature _CLIPPING
            //#pragma multi_compile_instancing 
            #pragma vertex MetaPassVertex
            #pragma fragment MetaPassFragment
            #include "MetaPass.hlsl"
            ENDHLSL
        }
    }
    CustomEditor "CustomShaderGUI"
}
