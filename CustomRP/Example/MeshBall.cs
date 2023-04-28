using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class MeshBall : MonoBehaviour
{
    static int baseColorId = Shader.PropertyToID("_BaseColor");
    static int metallicId = Shader.PropertyToID("_Metallic");
    static int smoothnessId = Shader.PropertyToID("_Smoothness");
    static int cutoffId = Shader.PropertyToID("_Cutoff");
    
    [SerializeField]
    Mesh mesh = default;
    [SerializeField]
    Material material = default;
    [SerializeField, Range(0f, 1.0f)]
    float cutoff = 0.5f;
    [SerializeField, Range(250, 1023)]
    int instanceCount = 250;
    [SerializeField]
    LightProbeProxyVolume lightProbeProxyVolume = null;

    Matrix4x4[] matrixes = new Matrix4x4[1023];
    Vector4[] baseColors = new Vector4[1023];
    float[] metallic = new float[1023];
    float[] smoothness = new float[1023];

    MaterialPropertyBlock block;

    private void Awake()
    {
        for(int i = 0; i < instanceCount; ++i)
        {
            matrixes[i] = Matrix4x4.TRS(
                Random.insideUnitSphere * 10.0f,
                Quaternion.Euler(
                    Random.value * 360f,
                    Random.value * 360f,
                    Random.value * 360f),
                Vector3.one //* Random.Range(0.5f, 1.5f)
            );
            baseColors[i] = new Vector4(
                Random.value,
                Random.value,
                Random.value,
                Random.Range(0.5f, 1.0f)
            );
            metallic[i] = Random.value < 0.25 ? 1.0f : 0.0f;
            smoothness[i] = Random.Range(0.05f, 0.95f);
        }
    }

    private void Update()
    {
        if (block == null)
        {
            block = new MaterialPropertyBlock();
            block.SetVectorArray(baseColorId, baseColors);
            block.SetFloatArray(metallicId, metallic);
            block.SetFloatArray(smoothnessId, smoothness);
            if(!lightProbeProxyVolume)
            {
                Vector3[] positions = new Vector3[instanceCount];
                for (int i = 0; i < matrixes.Length; ++i)
                {
                    positions[i] = matrixes[i].GetColumn(3);
                }
                SphericalHarmonicsL2[] lightProbes = new SphericalHarmonicsL2[instanceCount];
                Vector4[] occlusionProbes = new Vector4[instanceCount];
                LightProbes.CalculateInterpolatedLightAndOcclusionProbes(positions, lightProbes, null);
                block.CopySHCoefficientArraysFrom(lightProbes);
                block.CopyProbeOcclusionArrayFrom(occlusionProbes);
            }
            block.SetFloat(cutoffId, cutoff);
        }
        Graphics.DrawMeshInstanced(
            mesh,
            0,
            material,
            matrixes,
            instanceCount,
            block,
            ShadowCastingMode.On,
            true,
            0,
            null,
            lightProbeProxyVolume ? LightProbeUsage.UseProxyVolume : LightProbeUsage.CustomProvided,
            lightProbeProxyVolume
        );
    }
}
