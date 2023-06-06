using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;

[CanEditMultipleObjects]
[CustomEditorForRenderPipeline(typeof(Light), typeof(CustomRenderPipelineAsset))]
public class CustomLightEditor : LightEditor
{
    static GUIContent renderingLayerMaskLabel = new GUIContent("Rendering Layer Mask", "Functional version of above property");
    
    //重写灯光Inspector面板
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        DrawRenderingLayerMask();
        if (!settings.lightType.hasMultipleDifferentValues && (LightType)settings.lightType.enumValueIndex == LightType.Spot)
        {
            settings.DrawInnerAndOuterSpotAngle();
            //settings.ApplyModifiedProperties();
        }
        settings.ApplyModifiedProperties();
        //如果光源的CullingMask不是Everything层，显示警告:CullingMask只影响阴影
        //如果不是定向光源，则提示开启逐对象光照，除了影响阴影还可以影响物体受光
        Light light = target as Light;
        if(light.cullingMask != -1)
        {
            EditorGUILayout.HelpBox(light.type == LightType.Directional ?
                "Culling Mask only affects shadows(CullingMask只影响阴影)." :
                "Culling Mask only affects shadows unless Use Light Per Object is on(CullingMask只影响阴影,在UseLightPerObject开启时影响光照).",
                MessageType.Warning
            );
        }
    }

    void DrawRenderingLayerMask()
    {
        SerializedProperty property = settings.renderingLayerMask;
        EditorGUI.showMixedValue = property.hasMultipleDifferentValues;
        EditorGUI.BeginChangeCheck();
        int mask = property.intValue;
        if(mask == int.MaxValue)
        {
            mask = -1;
        }
        mask = EditorGUILayout.MaskField(
            renderingLayerMaskLabel,
            mask,
            GraphicsSettings.currentRenderPipeline.renderingLayerMaskNames
        );
        if(EditorGUI.EndChangeCheck())
        {
            property.intValue = mask == -1 ? int.MaxValue : mask;
        }
        EditorGUI.showMixedValue = false;
    }
}
