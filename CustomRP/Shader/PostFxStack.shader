Shader "CustomRP/Post Fx Stack"
{
    Properties
    {

    }
    SubShader
    {
        Cull Off

        ZTest Always
        ZWrite Off
        HLSLINCLUDE
            #include "../ShaderLibrary/Common.hlsl"
            #include "PostFxStackPasses.hlsl"
        ENDHLSL
        Pass
        {
            Name "Bloom Horizontal"
            HLSLPROGRAM
                #pragma target 3.5
                #pragma vertex DefaultPassVertex
                #pragma fragment BloomHorizontalPassFragment
            ENDHLSL
        }
        Pass
        {
            Name "Bloom Vertical"
            HLSLPROGRAM
                #pragma target 3.5
                #pragma vertex DefaultPassVertex
                #pragma fragment BloomVerticalPassFragment
            ENDHLSL
        }
        Pass
        {
            Name "Bloom Combine"
            HLSLPROGRAM
                #pragma target 3.5
                #pragma vertex DefaultPassVertex
                #pragma fragment BloomCombinePassFragment
            ENDHLSL
        }
        Pass
        {
            Name "Bloom Prefilter"
            HLSLPROGRAM
                #pragma target 3.5
                #pragma vertex DefaultPassVertex
                #pragma fragment BloomPrefilterPassFragment
            ENDHLSL
        }
        Pass
        {
            Name "Cpoy"
            HLSLPROGRAM
                #pragma target 3.5
                #pragma vertex DefaultPassVertex
                #pragma fragment CpoyPassFragment
            ENDHLSL
        }
    }
}
