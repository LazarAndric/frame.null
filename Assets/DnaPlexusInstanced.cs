using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.VFX;

public class DnaPlexusInstanced : MonoBehaviour
{
    [Header("DNA Geometrija")]
    public int pointsPerStrand = 50;
    public float radius = 1.5f;
    public float totalHeight = 10f;
    public float twistAngle = 0.4f;
    public float rotationSpeed = 1.0f;

    [Header("Izgled Segmenata")]
    public float strandThickness = 0.05f; // Debljina spiralnih lanaca
    public float rungThickness = 0.03f;    
    public Mesh segmentMesh;              

    [Header("Animacija")]
    public float animationSpeed = 20f;
    public float currentLineProgress = 0f;
    public float globalFadeOutSpeed = 2f;
    public bool triggerGlobalFadeOut = false;
    private float globalFadeMultiplier = 1f;

    [Header("Reference")]
    public Material lineMaterial;
    public VisualEffect vfxGraph;

    private GraphicsBuffer pointBuffer;
    private GraphicsBuffer segmentDataBuffer;
    private PointData[] cpuPointData;
    private SegmentData[] cpuSegmentData;
    private int totalPoints;
    private int totalSegments;

    struct PointData { public Vector3 position; }

    [System.Runtime.InteropServices.StructLayout(LayoutKind.Sequential)]
    struct SegmentData
    {
        public Vector3 start;     // 12 bajtova
        public Vector3 end;       // 12 bajtova
        public float thickness;   // 4 bajta
        public float segmentID;   // 4 bajta
                                  // Ukupno: 32 bajta. Savršeno se poklapa!
    }

    void Start()
    {
        if (segmentMesh == null)
        {
            GameObject tempQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            segmentMesh = tempQuad.GetComponent<MeshFilter>().sharedMesh;
            Destroy(tempQuad);
        }
        InitializeDna();
    }

    void InitializeDna()
    {
        totalPoints = pointsPerStrand * 2;
        cpuPointData = new PointData[totalPoints];
        pointBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, totalPoints, 12);

        int verticalSegments = (pointsPerStrand - 1) * 2;
        int horizontalSegments = pointsPerStrand;
        totalSegments = verticalSegments + horizontalSegments;

        cpuSegmentData = new SegmentData[totalSegments];
        segmentDataBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, totalSegments, 32);

        if (vfxGraph != null)
        {
            vfxGraph.SetInt("PointCount", totalPoints);
            vfxGraph.SetGraphicsBuffer("PointBuffer", pointBuffer);
        }
    }

    void Update()
    {
        if (pointBuffer == null || segmentDataBuffer == null) return;

        float timeOffset = Time.time * rotationSpeed;

        for (int i = 0; i < pointsPerStrand; i++)
        {
            float angle = i * twistAngle + timeOffset;
            float yPos = ((float)i / (pointsPerStrand - 1)) * totalHeight;

            Vector3 posA = new Vector3(Mathf.Cos(angle) * radius, yPos, Mathf.Sin(angle) * radius);
            cpuPointData[i].position = transform.TransformPoint(posA);

            Vector3 posB = new Vector3(Mathf.Cos(angle + Mathf.PI) * radius, yPos, Mathf.Sin(angle + Mathf.PI) * radius);
            cpuPointData[i + pointsPerStrand].position = transform.TransformPoint(posB);
        }
        pointBuffer.SetData(cpuPointData);

        int counter = 0;
        for (int i = 0; i < pointsPerStrand; i++)
        {
            // Segment lanca A
            if (i < pointsPerStrand - 1)
            {
                cpuSegmentData[counter++] = CreateSegment(cpuPointData[i].position, cpuPointData[i + 1].position, strandThickness, counter);
            }
            // Segment lanca B
            if (i < pointsPerStrand - 1)
            {
                cpuSegmentData[counter++] = CreateSegment(cpuPointData[i + pointsPerStrand].position, cpuPointData[i + 1 + pointsPerStrand].position, strandThickness, counter);
            }
            cpuSegmentData[counter++] = CreateSegment(cpuPointData[i].position, cpuPointData[i + pointsPerStrand].position, rungThickness, counter);
        }
        segmentDataBuffer.SetData(cpuSegmentData);

        UpdateAnimation();

        if (lineMaterial != null && totalSegments > 0)
        {
            lineMaterial.SetBuffer("_SegmentData", segmentDataBuffer);
            lineMaterial.SetFloat("_VisibleLinesProgress", currentLineProgress);
            lineMaterial.SetFloat("_GlobalFadeOutMultiplier", globalFadeMultiplier);

            Bounds bounds = new Bounds(transform.position, Vector3.one * (totalHeight + radius * 2));
            Graphics.DrawMeshInstancedProcedural(segmentMesh, 0, lineMaterial, bounds, totalSegments);
        }
    }

    SegmentData CreateSegment(Vector3 start, Vector3 end, float thickness, int id)
    {
        SegmentData seg = new SegmentData();
        seg.start = start;
        seg.end = end;
        seg.thickness = thickness;
        seg.segmentID = (float)id;
        return seg;
    }

    void UpdateAnimation()
    {
        if (segmentDataBuffer == null) return;
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

            if (currentLineProgress > totalSegments + 10)
            {
                triggerGlobalFadeOut = true;
            }
        }
        globalFadeMultiplier = Mathf.Clamp01(globalFadeMultiplier);
    }

    private void OnDestroy()
    {
        pointBuffer?.Release();
        segmentDataBuffer?.Release();
    }
}