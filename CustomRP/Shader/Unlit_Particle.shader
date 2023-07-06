Shader "CustomRP/Unlit_Particle"
{
    Properties
    {
         _MainTex("Texture for Lightmap", 2D) = "white" {}
        [HideInInspector] _Color("Color for Lightmap", Color) = (0.5, 0.5, 0.5, 1.0)
        [HDR]_BaseColor("Color", Color) = (1.0, 1.0, 1.0, 1.0)
        [Toggle(_VERTEX_COLORS)] _VertexColors("Vertex Color", float) = 0
        [Toggle(_FLIPBOOK_BLENDING)] _FlipbookBlending("Flipbook Blending", int) = 0
        [Toggle(_NEAR_FADE)] _NearFade("Near Fade", int) = 0
        _NearFadeDistance("Near Fade Distance", Range(0.0, 10.0) )= 1.0
        _NearFadeRange("Near Fade Range", Range(0.01, 10.0)) = 1.0
        [Toggle(_SOFT_PARTICLES)] _SoftParticles("Soft Particles", int) = 0
        _SoftParticlesDistance("Soft Particle Distance", Range(0.0, 10.0)) = 0
        _SoftParticlesRange("Soft Particle Range", Range(0.01, 10.0)) = 1
        [Toggle(_DISTORTION)] _Distortion("Distortion", int) = 0
        [NoScaleOffset] _DistortionMap("Distortion Vectors", 2D) = "bumb" {}
        _DistortionStrength("Distortion Strength", Range(0, 1)) = 0.1
        _DistortionBlend("Distortion Blend", Range(0.0, 1.0)) = 1
        
        [Toggle(_CLIPPING)]_Clipping("Alpha Clipping", int) = 0
        _Cutoff("Alpha Cutoff", range(0.0, 1.0)) = 0.5
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend("Src Blend", int) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend("Dst Blend", int) = 0
        [Enum(Off, 0, On, 1)]_ZWrite("Z Write", int) = 1
    }
    SubShader
    {
        Blend [_SrcBlend] [_DstBlend], One OneMinusSrcAlpha
        ZWrite [_ZWrite]
        HLSLINCLUDE
        #include "../ShaderLibrary/Common.hlsl"
        #include "UnlitInput.hlsl"
        ENDHLSL
        Pass
        {
            HLSLPROGRAM
            #pragma shader_feature _VERTEX_COLORS 
            #pragma shader_feature _FLIPBOOK_BLENDING
            #pragma shader_feature _NEAR_FADE
            #pragma shader_feature _SOFT_PARTICLES
            #pragma shader_feature _DISTORTION
            #pragma shader_feature _CLIPPING
            #pragma multi_compile_instancing 
            #pragma vertex UnlitPassVertex
            #pragma fragment UnlitPassFragment
            #include "UnlitPass.hlsl"
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
