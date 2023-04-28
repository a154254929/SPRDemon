using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshBallGO : MonoBehaviour
{
    static int baseColorId = Shader.PropertyToID("_BaseColor");
    static int metallicId = Shader.PropertyToID("_Metallic");
    static int smoothnessId = Shader.PropertyToID("_Smoothness");

    [SerializeField]
    Mesh mesh = default;
    [SerializeField]
    Material material = default;
    [SerializeField, Range(250, 1023)]
    int instanceCount = 250;

    Matrix4x4[] matrixes = new Matrix4x4[1023];
    Vector4[] baseColors = new Vector4[1023];
    float[] metallic = new float[1023];
    float[] smoothness = new float[1023];
    GameObject[] gameObjects = new GameObject[1023];

    private void OnEnable()
    {
        MaterialPropertyBlock mpb = new MaterialPropertyBlock();
        for (int i = 0; i < instanceCount; ++i)
        {
            GameObject go = new GameObject("sphere" + i);
            go.transform.SetParent(this.gameObject.transform);
            go.transform.position = Random.insideUnitSphere * 10.0f;
            go.transform.rotation = Quaternion.Euler(
                    Random.value * 360f,
                    Random.value * 360f,
                    Random.value * 360f
            );
            //go.transform.localScale = Vector3.one * Random.Range(0.5f, 1.5f);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer mr = go.AddComponent<MeshRenderer>();
            mr.material = material;
            mr.material.SetVector(baseColorId, new Vector4(
                Random.value,
                Random.value,
                Random.value,
                Random.Range(0.5f, 1.0f)
            ));
            //mpb.SetFloat(metallicId, Random.value < 0.25 ? 1.0f : 0.0f);
            //mpb.SetFloat(smoothnessId, Random.Range(0.05f, 0.95f));
            mr.SetPropertyBlock(mpb);
            gameObjects[i] = go;
        }
    }
    private void OnDisable()
    {
        for (int i = 0; i < instanceCount; ++i)
        {
            Destroy(gameObjects[i]);
        }
    }

    private void Update()
    {

    }
}
