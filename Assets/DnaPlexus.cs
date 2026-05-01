using UnityEngine;
using UnityEngine.VFX;

public class DnaPlexus : MonoBehaviour
{
    [Header("DNA Geometrija")]
    public int pointsPerStrand = 50; // Ukupno 100 tacaka
    public float radius = 1.5f;
    public float heightStep = 0.3f;
    public float twistAngle = 0.4f;
    public float rotationSpeed = 1.0f; // Brzina rotacije celog lanca

    [Header("Animacija")]
    public float animationSpeed = 20f; // Brzina gradjenja linija
    public float currentLineProgress = 0f;
    public float globalFadeOutSpeed = 2f;
    public bool triggerGlobalFadeOut = false;
    private float globalFadeMultiplier = 1f;

    [Header("Reference")]
    public Material lineMaterial;
    public VisualEffect vfxGraph;

    private GraphicsBuffer pointBuffer;
    private GraphicsBuffer indexBuffer;
    private PointData[] cpuData;
    private int totalPoints;

    struct PointData { public Vector3 position; }

    void Start()
    {
        InitializeDna();
    }
    bool isInit = false;
    void InitializeDna()
    {
        if(isInit) return;
        totalPoints = pointsPerStrand * 2;
        cpuData = new PointData[totalPoints];
        pointBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, totalPoints, 12);

        // Definisanje broja linija
        int verticalLinesPerStrand = pointsPerStrand - 1;
        int horizontalLines = pointsPerStrand;
        int totalLines = (verticalLinesPerStrand * 2) + horizontalLines;

        int[] indices = new int[totalLines * 2];
        int counter = 0;

        for (int i = 0; i < pointsPerStrand; i++)
        {
            if (i < pointsPerStrand - 1)
            {
                indices[counter++] = i;
                indices[counter++] = i + 1;
            }

            if (i < pointsPerStrand - 1)
            {
                indices[counter++] = i + pointsPerStrand;
                indices[counter++] = i + 1 + pointsPerStrand;
            }

            indices[counter++] = i;
            indices[counter++] = i + pointsPerStrand;
        }

        indexBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, indices.Length, sizeof(int));
        indexBuffer.SetData(indices);

        if (vfxGraph != null)
        {
            vfxGraph.SetInt("PointCount", totalPoints);
            vfxGraph.SetGraphicsBuffer("PointBuffer", pointBuffer);
        }
        isInit = true;
    }

    void UpdateAnimation()
    {
        if (indexBuffer == null) return;

        int totalLineCount = indexBuffer.count / 2;

        if (triggerGlobalFadeOut)
        {
            globalFadeMultiplier -= Time.deltaTime * globalFadeOutSpeed;
            if (globalFadeMultiplier <= 0)
            {
                triggerGlobalFadeOut = false;
                currentLineProgress = 0;
                globalFadeMultiplier = 0;
            }
        }
        else
        {
            currentLineProgress += Time.deltaTime * animationSpeed;
            globalFadeMultiplier = Mathf.MoveTowards(globalFadeMultiplier, 1f, Time.deltaTime);

            if (currentLineProgress > totalLineCount + 10)
            {
                triggerGlobalFadeOut = true;
            }
        }
        globalFadeMultiplier = Mathf.Clamp01(globalFadeMultiplier);
    }

    public float totalHeight = 10f;
    void Update()
    {
        //InitializeDna();
        if (pointBuffer == null) return;

        float timeOffset = Time.time * rotationSpeed;

        // RACUNANJE POZICIJA ZA DVA LANCA
        for (int i = 0; i < pointsPerStrand; i++)
        {
            float angle = i * twistAngle + timeOffset;
            float yPos = ((float)i / (pointsPerStrand - 1)) * totalHeight;

            // Pozicija na Lancu A
            Vector3 posA = new Vector3(Mathf.Cos(angle) * radius, yPos, Mathf.Sin(angle) * radius);
            cpuData[i].position = transform.TransformPoint(posA);

            // Pozicija na Lancu B (offset 180 stepeni / PI)
            Vector3 posB = new Vector3(Mathf.Cos(angle + Mathf.PI) * radius, yPos, Mathf.Sin(angle + Mathf.PI) * radius);
            cpuData[i + pointsPerStrand].position = transform.TransformPoint(posB);
        }
        pointBuffer.SetData(cpuData);

        // LOGIKA ANIMACIJE (Zadrzana originalna)
        UpdateAnimation();

        // SLANJE PODATAKA SHADERU
        if (lineMaterial != null)
        {
            lineMaterial.SetBuffer("_Points", pointBuffer);
            lineMaterial.SetBuffer("_Indices", indexBuffer);
            lineMaterial.SetFloat("_VisibleLinesProgress", currentLineProgress);
            lineMaterial.SetFloat("_GlobalFadeOutMultiplier", globalFadeMultiplier);
        }
    }

    private void OnRenderObject()
    {
        // Ponovo ukljucujemo crtanje linija
        if (indexBuffer == null || lineMaterial == null) return;
        lineMaterial.SetPass(0);
        Graphics.DrawProceduralNow(MeshTopology.Lines, indexBuffer.count);
    }

    private void OnDestroy()
    {
        pointBuffer?.Release();
        indexBuffer?.Release();
    }
}