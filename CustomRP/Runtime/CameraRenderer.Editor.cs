using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;

public partial class CameraRenderer
{
    partial void DrawUnsupportedShader();
    //绘制Fizmos
    //partial void DrawGizmos();
    partial void DrawGizmosBeforeFX();
    partial void DrawGizmosAfterFX();
    //绘制UI
    partial void PrepareForSceneWindow();

    partial void PrepareBuffer();
#if UNITY_EDITOR
    static ShaderTagId[] legacyShaderTagIds =
    {
        new ShaderTagId("Always"),
        new ShaderTagId("ForwardBase"),
        new ShaderTagId("PrepassBase"),
        new ShaderTagId("Vertex"),
        new ShaderTagId("VertexLMRGBM"),
        new ShaderTagId("VertexLM"),

    };
    static Material errorMaterial;

    partial void DrawUnsupportedShader()
    {
        if (errorMaterial == null)
        {
            errorMaterial = new Material(Shader.Find("Hidden/InternalErrorShader"));
        }
        DrawingSettings drawingSettings = new DrawingSettings(legacyShaderTagIds[0], new SortingSettings(camera))
        {
            overrideMaterial = errorMaterial
        };
        for (int i = 1; i < legacyShaderTagIds.Length; ++i)
        {
            drawingSettings.SetShaderPassName(i, legacyShaderTagIds[i]);
        }
        FilteringSettings filterSettings = FilteringSettings.defaultValue;
        context.DrawRenderers(cullResults, ref drawingSettings, ref filterSettings);
    }

    partial void DrawGizmosBeforeFX()
    {
        if(Handles.ShouldRenderGizmos())
        {
            if (useIntermediateBuffer)
            {
                Draw(depthAttachmentId, BuiltinRenderTextureType.CameraTarget, true);
                ExcuteBuffer();
            }
            context.DrawGizmos(camera, GizmoSubset.PreImageEffects);
        }
    }

    partial void DrawGizmosAfterFX()
    {
        if(Handles.ShouldRenderGizmos())
        {
            context.DrawGizmos(camera, GizmoSubset.PostImageEffects);
        }
    }

    partial void PrepareForSceneWindow()
    {
        if(camera.cameraType == CameraType.SceneView)
        {
            //如果切换到了Scene试图，调用此方法完成绘制
            ScriptableRenderContext.EmitWorldGeometryForSceneView(camera);
            //禁用渲染缩放
            useScaledRendering = false;
        }
    }

    string SampleName { get; set;}

    partial void PrepareBuffer()
    {
        Profiler.BeginSample("Editor Only");
        buffer.name = SampleName = camera.name;
        Profiler.EndSample();
    }
#else
    const string SampleName = bufferName;
#endif
}

