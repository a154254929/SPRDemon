using UnityEngine;
using UnityEngine.Rendering;
using static PostFXSetting;

partial class PostFxStack
{
    enum Pass
    {
        BloomHorizontal,
        BloomVertical,
        BloomAdd,
        BloomScatter,
        BloomScatterFinal,
        BloomPrefilter,
        BloomPrefilterFireflies,
        ToneMappingNone,
        ToneMappingACES,
        ToneMappingNeutral,
        ToneMappingReinhard,
        Copy,
        ColorGradingNone,
        ColorGradingACES,
        ColorGradingNeutral,
        ColorGradingReinhard,
        Final,
        FinalScale,
    }

    public bool IsActive { get => settings != null; }
    const string bufferName = "Post Fx";
    CommandBuffer buffer = new CommandBuffer
    {
        name = bufferName,
    };
    ScriptableRenderContext context;

    Camera camera;
    PostFXSetting settings;

    const int maxBloomPyramidLevels = 16;
    //纹理标识符
    int bloomPyramidId;

    bool useHDR;

    CameraSettings.FinalBlendMode finalBlendMode;

    Vector2Int bufferSize;
    public PostFxStack()
    {
        bloomPyramidId = Shader.PropertyToID("_BloomPyramid0");
        for (int i = 1; i < maxBloomPyramidLevels; ++i)
        {
            Shader.PropertyToID("_BloomPyramid" + i);
        }
    }
    public void Setup(
        ScriptableRenderContext context,
        Camera camera,
        PostFXSetting settings,
        bool useHDR,
        int colorLUTResolution,
        CameraSettings.FinalBlendMode finalBlendMode,
        Vector2Int bufferSize,
        CameraBufferSettings.BicubicRescalingMode bicubicRecaleing
    )
    {
        this.context = context;
        this.camera = camera;
        this.settings = camera.cameraType <= CameraType.SceneView ? settings : null;
        this.useHDR = useHDR;
        this.colorLUTResolution = colorLUTResolution;
        this.finalBlendMode = finalBlendMode;
        this.bufferSize = bufferSize;
        this.bicubicRecaleing =
            bicubicRecaleing == CameraBufferSettings.BicubicRescalingMode.UpAndDowm 
            || bicubicRecaleing == CameraBufferSettings.BicubicRescalingMode.UpOnly && bufferSize.x < camera.pixelWidth;
            ;
        ApplySceneViewState();
    }

    int colorLUTResolution;

    int fxSourceId = Shader.PropertyToID("_PostFXSource");
    int fxSource2Id = Shader.PropertyToID("_PostFXSource2");
    int bloomBicubicUpSamplingId = Shader.PropertyToID("_BloomBicubicUpsampling");
    int bloomPrefilterId = Shader.PropertyToID("_BloomPrefilter");
    int bloomThresholdId = Shader.PropertyToID("_BloomThreshold");
    int bloomIntensityId = Shader.PropertyToID("_BloomIntensity");
    int bloomResultId = Shader.PropertyToID("_BloomResult");
    int colorAdjustmentsId = Shader.PropertyToID("_ColorAdjustments");
    int colorFilterId = Shader.PropertyToID("_ColorFilter");
    int whiterBalanceId = Shader.PropertyToID("_WhiteBalance");
    int splitToningShadowId = Shader.PropertyToID("_SplitToningShadows");
    int splitToningHighlightId = Shader.PropertyToID("_SplitToningHighlights");
    int channelMixerRedId = Shader.PropertyToID("_ChannelMixerRed");
    int channelMixerGreenId = Shader.PropertyToID("_ChannelMixerGreen");
    int channelMixerBlueId = Shader.PropertyToID("_ChannelMixerBlue");
    int smhShadowsId = Shader.PropertyToID("_SMHShadows");
    int smhMidtonesId = Shader.PropertyToID("_SMHMidtones");
    int smhHighlightsId = Shader.PropertyToID("_SMHHighlights");
    int smhRangeId = Shader.PropertyToID("_SMHRange");
    int colorGradingLUTId = Shader.PropertyToID("_ColorGradingLUT");
    int colorGradingLUTParametersId = Shader.PropertyToID("_ColorGradingLUTParameters");
    int clorGradingLUTInLohCId = Shader.PropertyToID("_ColorGradingLUTInLohC");
    int finalSrcBlendId = Shader.PropertyToID("_FinalSrcBlend");
    int finalDstBlendId = Shader.PropertyToID("_FinalDstBlend");
    int finalResulId = Shader.PropertyToID("_FinalResult");
    int copyBicubicId = Shader.PropertyToID("_CopyBicubic");
    bool bicubicRecaleing;
    void Draw(RenderTargetIdentifier from, RenderTargetIdentifier to, Pass pass)
    {
        buffer.SetGlobalTexture(fxSourceId, from);
        buffer.SetRenderTarget(to, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
        buffer.DrawProcedural(Matrix4x4.identity, settings.Material, (int)pass, MeshTopology.Triangles, 3);
    }

    void DrawFinal(RenderTargetIdentifier from, Pass pass)
    {
        buffer.SetGlobalFloat(finalSrcBlendId, (float)finalBlendMode.source);
        buffer.SetGlobalFloat(finalDstBlendId, (float)finalBlendMode.destination);
        buffer.SetGlobalTexture(fxSourceId, from);
        buffer.SetRenderTarget(BuiltinRenderTextureType.CameraTarget,
            finalBlendMode.destination == BlendMode.Zero ? RenderBufferLoadAction.DontCare : RenderBufferLoadAction.Load,
            RenderBufferStoreAction.Store
        );
        //设置视口
        buffer.SetViewport(camera.pixelRect);
        buffer.DrawProcedural(Matrix4x4.identity, settings.Material, (int)pass, MeshTopology.Triangles, 3);
    }

    public void Render(int scourceId)
    {

        //buff.Blit(scourceId, BuiltinRenderTextureType.CameraTarget);
        //Draw(scourceId, BuiltinRenderTextureType.CameraTarget, Pass.Copy);
        if(DoBloom(scourceId))
        {
            DoColorGradingAndToneMapping(bloomResultId);
            buffer.ReleaseTemporaryRT(bloomResultId);
        }
        else
        {
            DoColorGradingAndToneMapping(scourceId);
        }

        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();
    }

    bool DoBloom(int sourceId)
    {
        BloomSettings bloom = settings.Bloom;
        int width, height;
        if (settings.Bloom.ignoreRenderScale)
        {
            width = camera.pixelWidth;
            height = camera.pixelHeight;
        }
        else
        {
            width = bufferSize.x / 2;
            height = bufferSize.y / 2;
        }
        if (bloom.maxIterations == 0
            || bloom.intensity <= 0.0f
            || height < bloom.downscaleLimit * 2
            || width < bloom.downscaleLimit * 2
        )
        {
            //Draw(sourceId, BuiltinRenderTextureType.CameraTarget, Pass.Copy);
            //buffer.EndSample("Bloom");
            return false;
        }

        buffer.BeginSample("Bloom");
        Vector4 threshold;
        threshold.x = Mathf.GammaToLinearSpace(bloom.threshold);
        threshold.y = threshold.x * bloom.thresholdKnee;
        threshold.z = 2.0f * threshold.y;
        threshold.w = 0.25f /(threshold.y + 0.00001f);
        threshold.y -= threshold.x;
        buffer.SetGlobalVector(bloomThresholdId, threshold);


        RenderTextureFormat format = useHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default;
        buffer.GetTemporaryRT(bloomPrefilterId, width, height, 0, FilterMode.Bilinear, format);
        Draw(sourceId, bloomPrefilterId, bloom.fadeFireflies ? Pass.BloomPrefilterFireflies : Pass.BloomPrefilter);
        width /= 2;
        height /= 2;
        
        int fromId = bloomPrefilterId;
        int toId = bloomPyramidId + 1;
        int i;
        for (i = 0; i < bloom.maxIterations
            && height >= bloom.downscaleLimit
            && width >= bloom.downscaleLimit; ++i, toId += 2)
        {
            int midId = toId - 1;
            buffer.GetTemporaryRT(midId, width, height, 0, FilterMode.Bilinear, format);
            buffer.GetTemporaryRT(toId, width, height, 0, FilterMode.Bilinear, format);
            Draw(fromId, midId, Pass.BloomHorizontal);
            Draw(midId, toId, Pass.BloomVertical);
            fromId = toId;
            width /= 2;
            height /= 2;
        }
        buffer.ReleaseTemporaryRT(bloomPrefilterId);
        //Draw(fromId, BuiltinRenderTextureType.CameraTarget, Pass.Copy);
        //Draw(fromId, BuiltinRenderTextureType.CameraTarget, Pass.BloomHorizontal);
        buffer.SetGlobalFloat(bloomBicubicUpSamplingId, bloom.bicubicUpsampling ? 1.0f : 0.0f);
        Pass combinePass, finalPass;
        float finalIntensity;
        if (bloom.mode == BloomSettings.Mode.Scattering)
        {
            combinePass = Pass.BloomScatter;
            finalPass = Pass.BloomScatterFinal;
            //finalPass = Pass.BloomScatter;
            buffer.SetGlobalFloat(bloomIntensityId, bloom.scatter);
            finalIntensity = Mathf.Min(bloom.intensity, 0.95f);
        }
        else
        {
            combinePass = finalPass = Pass.BloomAdd;
            buffer.SetGlobalFloat(bloomIntensityId, 1.0f);
            finalIntensity = bloom.intensity;
        }
        if(i > 1)
        {
            buffer.ReleaseTemporaryRT(fromId - 1);
            toId -= 5;
            for( i -= 1; i > 0; --i, toId -= 2)
            {
                buffer.SetGlobalTexture(fxSource2Id, toId + 1);
                Draw(fromId, toId, combinePass);
                buffer.ReleaseTemporaryRT(fromId);
                buffer.ReleaseTemporaryRT(toId + 1);
                fromId = toId;
            }
        }
        else
        {
            buffer.ReleaseTemporaryRT(bloomPyramidId);
        }
        buffer.SetGlobalFloat(bloomIntensityId, finalIntensity);
        buffer.SetGlobalTexture(fxSource2Id, sourceId);
        buffer.GetTemporaryRT(
            bloomResultId,
            bufferSize.x,
            bufferSize.y,
            0,
            FilterMode.Bilinear,
            format
        );
        Draw(fromId, bloomResultId, finalPass);
        buffer.ReleaseTemporaryRT(fromId);
        buffer.EndSample("Bloom");
        return true;
    }

    void DoColorGradingAndToneMapping(int sourceId)
    {
        ConfigureColorAdjustment();
        ConfigureWhiteBalance();
        ConfigSplitToning();
        ConfigChannelMixer();
        ConfigShadowsMidtonesHighlights();
        int lutHeight = colorLUTResolution;
        int lutWidth = lutHeight * lutHeight;
        buffer.GetTemporaryRT(colorGradingLUTId, lutWidth, lutHeight, 0, FilterMode.Bilinear, RenderTextureFormat.DefaultHDR);
        buffer.SetGlobalVector(colorGradingLUTParametersId, new Vector4(lutHeight, 0.5f / lutWidth, 0.5f / lutHeight, lutHeight / (lutHeight - 1.0f)));
        ToneMappingSettings.Mode mode = settings.ToneMapping.mode;
        Pass pass = Pass.ColorGradingNone + (int)mode;
        buffer.SetGlobalFloat(clorGradingLUTInLohCId, useHDR && pass != Pass.ColorGradingNone ? 1.0f : 0.0f);
        Draw(sourceId, colorGradingLUTId, pass);

        buffer.SetGlobalVector(colorGradingLUTParametersId, new Vector4(1.0f / lutWidth, 1.0f / lutHeight, lutHeight - 1.0f));
        if (bufferSize.x == camera.pixelWidth)
        {
            DrawFinal(sourceId, Pass.Final);
        }
        else
        {
            buffer.SetGlobalFloat(finalSrcBlendId, 1.0f);
            buffer.SetGlobalFloat(finalDstBlendId, 0.0f);
            buffer.GetTemporaryRT(
                finalResulId,
                bufferSize.x,
                bufferSize.y,
                0,
                FilterMode.Bilinear,
                RenderTextureFormat.Default
            );
            Draw(sourceId, finalResulId, Pass.Final);
            buffer.SetGlobalFloat(copyBicubicId, bicubicRecaleing ? 1.0f : 0.0f);
            DrawFinal(finalResulId, Pass.FinalScale);
            buffer.ReleaseTemporaryRT(finalResulId);
        }
        buffer.ReleaseTemporaryRT(colorGradingLUTId);
    }

    //获取颜色调整的配置
    void ConfigureColorAdjustment()
    {
        ColorAdjustmentSettings colorAdjustments = settings.ColorAdjustments;
        buffer.SetGlobalVector(colorAdjustmentsId, new Vector4(
                Mathf.Pow(2.0f, colorAdjustments.postExposure),
                colorAdjustments.constrast * 0.01f + 1.0f,
                colorAdjustments.hueShift / 360.0f,
                colorAdjustments.saturation * 0.01f + 1.0f
            )
        );
        buffer.SetGlobalColor(colorFilterId, colorAdjustments.colorFilter.linear);
    }

    void ConfigureWhiteBalance()
    {
        WhiteBalanceSettings whiteBalance = settings.WhiteBalance;
        buffer.SetGlobalVector(whiterBalanceId, ColorUtils.ColorBalanceToLMSCoeffs(whiteBalance.temperature, whiteBalance.tint));
    }

    void ConfigSplitToning()
    {
        SplitToningSettings splitToning = settings.SplitToning;
        Color splitColor = splitToning.shadows;
        splitColor.a = splitToning.balance * 0.01f;
        buffer.SetGlobalColor(splitToningShadowId, splitColor);
        buffer.SetGlobalColor(splitToningHighlightId, splitToning.highlights);
    }

    void ConfigChannelMixer()
    {
        ChannelMixerSettings channelMixer = settings.ChannelMixer;
        buffer.SetGlobalVector(channelMixerRedId, channelMixer.red);
        buffer.SetGlobalVector(channelMixerGreenId, channelMixer.green);
        buffer.SetGlobalVector(channelMixerBlueId, channelMixer.blue);
    }

    void ConfigShadowsMidtonesHighlights()
    {
        ShadowsMidtonesHighlightsSettings smh = settings.ShadowsMidtonesHighlight;
        buffer.SetGlobalColor(smhShadowsId, smh.shadows);
        buffer.SetGlobalColor(smhMidtonesId, smh.midtones);
        buffer.SetGlobalColor(smhHighlightsId, smh.highlights);
        buffer.SetGlobalVector(smhRangeId, new Vector4(
                smh.shadowsStart,
                smh.shadowsEnd,
                smh.highlightsStart,
                smh.highlightsEnd
            )
        );
    }
}
