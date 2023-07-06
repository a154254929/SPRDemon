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
            Name "Bloom Add"
            HLSLPROGRAM
                #pragma target 3.5
                #pragma vertex DefaultPassVertex
                #pragma fragment BloomAddPassFragment
            ENDHLSL
        }
        Pass
        {
            Name "Bloom Scatter"
            HLSLPROGRAM
                #pragma target 3.5
                #pragma vertex DefaultPassVertex
                #pragma fragment BloomScatterPassFragment
            ENDHLSL
        }
        Pass
        {
            Name "Bloom Final"
            HLSLPROGRAM
                #pragma target 3.5
                #pragma vertex DefaultPassVertex
                #pragma fragment BloomScatterFinalPassFragment
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
            Name "Bloom Prefilter Firefiltes"
            HLSLPROGRAM
                #pragma target 3.5
                #pragma vertex DefaultPassVertex
                #pragma fragment BloomPrefilterFirefilesPassFragment
            ENDHLSL
        }
        Pass
        {
            Name "Tone Mapping None"
            HLSLPROGRAM
                #pragma target 3.5
                #pragma vertex DefaultPassVertex
                #pragma fragment ToneMappingNonePassFragment
            ENDHLSL
        }
        Pass
        {
            Name "Tone Mapping ACES"
            HLSLPROGRAM
                #pragma target 3.5
                #pragma vertex DefaultPassVertex
                #pragma fragment ToneMappingACESPassFragment
            ENDHLSL
        }
        Pass
        {
            Name "Tone Mapping Neutral"
            HLSLPROGRAM
                #pragma target 3.5
                #pragma vertex DefaultPassVertex
                #pragma fragment ToneMappingNeutralPassFragment
            ENDHLSL
        }
        Pass
        {
            Name "Tone Mapping Reinhard"
            HLSLPROGRAM
                #pragma target 3.5
                #pragma vertex DefaultPassVertex
                #pragma fragment ToneMappingReinhardPassFragment
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
        Pass
        {
            Name "Color Grading None"
            HLSLPROGRAM
                #pragma target 3.5
                #pragma vertex DefaultPassVertex
                #pragma fragment ColorGradingNonePassFragment
            ENDHLSL
        }
        Pass
        {
            Name "Color Grading ACES"
            HLSLPROGRAM
                #pragma target 3.5
                #pragma vertex DefaultPassVertex
                #pragma fragment ColorGradingACESPassFragment
            ENDHLSL
        }
        Pass
        {
            Name "Color Grading Neutral"
            HLSLPROGRAM
                #pragma target 3.5
                #pragma vertex DefaultPassVertex
                #pragma fragment ColorGradingNeutralPassFragment
            ENDHLSL
        }
        Pass
        {
            Name "Color Grading Reinhard"
            HLSLPROGRAM
                #pragma target 3.5
                #pragma vertex DefaultPassVertex
                #pragma fragment ColorGradingReinhardPassFragment
            ENDHLSL
        }
        Pass
        {
            Name "Final"
            Blend [_FinalSrcBlend] [_FinalDstBlend]
            HLSLPROGRAM
                #pragma target 3.5
                #pragma vertex DefaultPassVertex
                #pragma fragment FinalPassFragment
            ENDHLSL
        }
        pass
        {
            Name "Final Rescale"
            Blend [_FinalSrcBlend] [_FinalDstBlend]
            HLSLPROGRAM
                #pragma target 3.5
                #pragma vertex DefaultPassVertex
                #pragma fragment FinalPassFragmentRescale
            ENDHLSL
        }
    }
}
