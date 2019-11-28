using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Camera))]
[AddComponentMenu("Effects/Raymarch (Generic)")]
public class RaymarchSimple : MonoBehaviour
{

    Material blitMaterial;

    [Header("Rendering")]
    public FilterMode filterMode = FilterMode.Bilinear;

    public float resolution = 0.5f;
    [Range(0f, 1f)]
    public float lerp = 1f;

    [Range(0f, 5f)]
    public float gamma = 2.2f;
    [Range(0f, 1f)]
    public float shadowStrength = 1f;

    [Header("Direct lighting")]
    public bool showShadowMap = false;
    [Range(0f, 5f)]
    public float dweight = 1f;
    [Range(0f, 1f)]
    public float dlerp = 1f;

    [System.Serializable]
    struct Box
    {
        public Vector2 position;
        public Vector2 halfSize;
        public float theta;

        public Color diffuse;
    }

    [Header("Tracer Settings")]

    public float bounceBias = 0.1f;

    Texture2D randomTex;
    Camera camera;

    ComputeBuffer objBuffer;
    Box[] boxes;

    [Header("Shaders")]
    public ComputeShader sdfShader;
    public ComputeShader tracer;
    public Shader blitShader;

    [Header("Render Textures")]
    public RenderTexture sdf;
    public RenderTexture diffuseMap;
    public RenderTexture shadowMap;
    public RenderTexture tracerOutput;
    public RenderTexture lightingMap;

    GaussianBlurStatic blur;

    void Start()
    {
        blur = GetComponent<GaussianBlurStatic>();
        diffuseMap = new RenderTexture((int)(Screen.width * resolution / 8) * 8, (int)(Screen.height * resolution / 8) * 8, 1, RenderTextureFormat.ARGBFloat);
        diffuseMap.enableRandomWrite = true;
        diffuseMap.Create();

        sdfShader.SetTexture(0, "Diffuse", diffuseMap);
        tracer.SetTexture(0, "_Diffuse", diffuseMap);
        tracer.SetTexture(1, "_Diffuse", diffuseMap);

        sdf = new RenderTexture((int)(Screen.width * resolution / 8) * 8, (int)(Screen.height * resolution / 8) * 8, 1, RenderTextureFormat.RFloat);
        sdf.enableRandomWrite = true;
        sdf.Create();

        sdfShader.SetTexture(0, "SDF", sdf);
        tracer.SetTexture(0, "_SDF", sdf);
        tracer.SetTexture(1, "_SDF", sdf);
        tracer.SetInt("SDFWidth", sdf.width);
        tracer.SetInt("SDFHeight", sdf.height);

        tracerOutput = new RenderTexture((int)(Screen.width * resolution / 8) * 8, (int)(Screen.height * resolution / 8) * 8, 1, RenderTextureFormat.ARGBFloat);
        tracerOutput.enableRandomWrite = true;
        tracerOutput.Create();

        tracer.SetTexture(0, "Result", tracerOutput);

        shadowMap = new RenderTexture((int)(Screen.width * resolution / 8) * 8, (int)(Screen.height * resolution / 8) * 8, 1, RenderTextureFormat.ARGBFloat);
        shadowMap.enableRandomWrite = true;
        shadowMap.Create();

        tracer.SetTexture(1, "Result", shadowMap);
        tracer.SetTexture(0, "_ShadowMap", shadowMap);

        lightingMap = new RenderTexture((int)(Screen.width * resolution / 8) * 8, (int)(Screen.height * resolution / 8) * 8, 1, RenderTextureFormat.ARGBFloat);
        lightingMap.enableRandomWrite = true;
        lightingMap.Create();

        camera = GetComponent<Camera>();

        blitMaterial = new Material(blitShader);
        //blitMaterial.SetTexture("_Traced", tracerOutput);

        sdBox[] sdboxes = FindObjectsOfType<sdBox>();
        boxes = new Box[sdboxes.Length];

        for(int i = 0; i < sdboxes.Length; i++)
        {
            Box box;

            box.position = sdboxes[i].transform.position;
            box.theta = sdboxes[i].transform.eulerAngles.z * Mathf.Deg2Rad;
            box.halfSize = sdboxes[i].transform.localScale * 0.5f;
            box.diffuse = sdboxes[i].color * sdboxes[i].intensity;

            boxes[i] = box;
        }

        objBuffer = new ComputeBuffer(sdboxes.Length, sizeof(float) * 9);
        objBuffer.SetData(boxes);

        sdfShader.SetBuffer(0, "Objects", objBuffer);
    }

    private void OnDestroy()
    {
        if(objBuffer != null)
            objBuffer.Dispose();
    }

    void Update()
    {
        Vector3 rorg = camera.ScreenPointToRay(Vector3.zero).origin;
        Vector3 roff = camera.ScreenPointToRay(new Vector3(Screen.width, Screen.height)).origin - rorg;
        roff.x /= sdf.width;
        roff.y /= sdf.height;

        /** Calculate SDF **/
        sdBox[] sdboxes = FindObjectsOfType<sdBox>();
        if (boxes.Length != sdboxes.Length)
        {
            Debug.Log("Object buffer rebuilt");

            objBuffer.Dispose();
            objBuffer = new ComputeBuffer(sdboxes.Length, sizeof(float) * 9);
            sdfShader.SetBuffer(0, "Objects", objBuffer);

            boxes = new Box[sdboxes.Length];
        }

        for (int i = 0; i < sdboxes.Length; i++)
        {
            Box box;

            box.position = sdboxes[i].transform.position;
            box.theta = sdboxes[i].transform.eulerAngles.z * Mathf.Deg2Rad;
            box.halfSize = sdboxes[i].transform.localScale * 0.5f;
            box.diffuse = sdboxes[i].color * sdboxes[i].intensity;

            boxes[i] = box;
        }

        objBuffer.SetData(boxes);
        sdfShader.SetInt("ObjLength", sdboxes.Length);

        sdfShader.SetVector("CameraOrigin", new Vector2(rorg.x, rorg.y));
        sdfShader.SetVector("CameraOffset", new Vector2(roff.x, roff.y));
        sdfShader.Dispatch(0, sdf.width / 8, sdf.height / 8, 1);

        /** Calculate Lighting **/

        tracer.SetFloat("seed", Random.Range(0, 15.0f));
        tracer.SetFloat("DirectLerp", dlerp);
        tracer.SetFloat("Lerp", lerp);
        tracer.SetFloat("BounceBias", bounceBias);
        tracer.SetFloat("DirectWeight", dweight);

        tracer.SetVector("CameraOrigin", new Vector2(rorg.x, rorg.y));
        tracer.SetVector("CameraOffset", new Vector2(roff.x, roff.y));
        
        tracer.Dispatch(1, tracerOutput.width / 8, tracerOutput.height / 8, 1);
        tracer.Dispatch(0, tracerOutput.width / 8, tracerOutput.height / 8, 1);

        /** Apply post FX **/
        blur.BlurImage(tracerOutput, lightingMap);

        if (lightingMap.filterMode != filterMode)
            lightingMap.filterMode = filterMode;

        blitMaterial.SetFloat("_InvGamma", 1f / gamma);
        blitMaterial.SetFloat("_ShadowStrength", shadowStrength);
        blitMaterial.SetTexture("_Traced", showShadowMap ? shadowMap : lightingMap);
    }

    [ImageEffectOpaque]
    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        Graphics.Blit(source, destination, blitMaterial, 0); // use given effect shader as image effect
    }
}