using System;
using UnityEngine;
using UnityEngine.UIElements;

public class ExampleClass : MonoBehaviour
{
    private Material material;
    public float lag1;
    public float lag2;
    public AudioClip audioClip;
    public ComputeShader filterComputeShader;
    private Mesh mesh;
    private Vector3[] vertices;
    private int[] indices;
    private float[] signal;
    private int nSignal;
    [System.Serializable]
    public struct FilterParams {
        [Range(0.00001f, 100.0f)]
        public float stdDev;
    }
    public bool increment1;
    public float incrementRate1 = 1.0f;
    public bool increment2;
    public float incrementRate2 = 1.0f;

    [System.Serializable]
    public struct ShaderParams {
        public float minDepth;
        public float maxDepth;
        public float minIntensity;
        public float maxIntensity;
        public float ambientK;
        public Color ambientColor;
        public float diffuseK;
        public Color diffuseColor;
        public float specularK;
        public float specularPower;
        public Color specularColor;

    }
    public ShaderParams shaderParams;
    public FilterParams filterParams;

    private GraphicsBuffer gMeshIndices;
    private GraphicsBuffer gSignal;
    private GraphicsBuffer gFilteredSignal;



    int[] LineStripIndices(ref Vector3[] verts)
    {
        int n = verts.Length;
        int[] indices = new int[n];
        for (int i = 0; i < n; i++)
        {
            indices[i] = i;
        }
        return indices;
    }

    Vector3[] genVertices(int k)
    {
        Vector3[] verts = new Vector3[k];
        for (int i = 0; i < k; i++)
        {
            verts[i] = Vector3.zero;
        }
        return verts;
    }


    void Start()
    {
        signal = new float[audioClip.samples];
        audioClip.GetData(signal, 10000);
        nSignal = signal.Length;

        mesh = new Mesh();
        material = new Material(Shader.Find("LineShader"));
        vertices = genVertices(nSignal);
        indices = LineStripIndices(ref vertices);
        mesh.SetVertices(vertices);
        mesh.SetIndices(indices, MeshTopology.LineStrip, 0);

        filterComputeShader = Resources.Load<ComputeShader>("Filter");

        gMeshIndices = new GraphicsBuffer(GraphicsBuffer.Target.Structured, indices.Length, sizeof(int));
        gSignal = new GraphicsBuffer(GraphicsBuffer.Target.Structured, nSignal, sizeof(float));
        gFilteredSignal = new GraphicsBuffer(GraphicsBuffer.Target.Structured, nSignal, sizeof(float));

        gMeshIndices.SetData(indices);
        gSignal.SetData(signal);
    }

    void OnDestroy()
    {
        gMeshIndices?.Dispose();
        gFilteredSignal?.Dispose();
        gSignal?.Dispose();
    }

    void Update()
    {
        if (increment1) lag1 += Time.deltaTime*incrementRate1;
        if (increment2) lag2 += Time.deltaTime*incrementRate2;

        // Run compute shader filter
        filterComputeShader.SetBuffer(0, "InputSignal", gSignal);
        filterComputeShader.SetBuffer(0, "OutputSignal", gFilteredSignal);
        filterComputeShader.SetInt("nSignal", nSignal);
        filterComputeShader.SetFloat("stdDev", filterParams.stdDev);
        filterComputeShader.Dispatch(0, Mathf.CeilToInt(nSignal/256.0f), 1, 1);

        Matrix4x4 mat = gameObject.transform.localToWorldMatrix;

        RenderParams rp = new RenderParams(material);
        rp.worldBounds = new Bounds(Vector3.zero, 10000 * Vector3.one);
        rp.matProps = new MaterialPropertyBlock();
        rp.matProps.SetBuffer("uIndices", gMeshIndices);
        rp.matProps.SetBuffer("uSignalBuffer", gFilteredSignal);
        rp.matProps.SetInt("uStartIndex", (int)mesh.GetIndexStart(0));
        rp.matProps.SetInt("uBaseVertexIndex", (int)mesh.GetBaseVertex(0));
        rp.matProps.SetFloat("uLag1", lag1);
        rp.matProps.SetFloat("uLag2", lag2);
        rp.matProps.SetInt("uNumVerts", vertices.Length);
        rp.matProps.SetMatrix("uObjectToWorld", mat);
        rp.matProps.SetFloat("uNumInstances", 1.0f);
        rp.matProps.SetFloat("uNumInstances", 1.0f);
        rp.matProps.SetFloat("uMinDepth", shaderParams.minDepth);
        rp.matProps.SetFloat("uMaxDepth", shaderParams.maxDepth);
        rp.matProps.SetFloat("uMinIntensity", shaderParams.minIntensity);
        rp.matProps.SetFloat("uMaxIntensity", shaderParams.maxIntensity);

        rp.matProps.SetFloat("kAmbient", shaderParams.ambientK);
        rp.matProps.SetFloat("kDiffuse", shaderParams.diffuseK);
        rp.matProps.SetFloat("kSpecular", shaderParams.specularK);
        rp.matProps.SetFloat("nSpecular", shaderParams.specularPower);
        rp.matProps.SetVector("colorAmbient", (Vector4)shaderParams.ambientColor);
        rp.matProps.SetVector("colorDiffuse", (Vector4)shaderParams.diffuseColor);
        rp.matProps.SetVector("colorSpecular", (Vector4)shaderParams.specularColor);

        Graphics.RenderPrimitives(rp, MeshTopology.LineStrip, (int)mesh.GetIndexCount(0), 1);

    }
}