using UnityEngine;
using UnityEngine.VFX;

[ExecuteInEditMode]
public class PlexusTransforms : MonoBehaviour
{
    public Material lineMaterial;
    public VisualEffect vfxGraph;
    public Transform parent;
    struct PointData
    {
        public Vector3 position;
    }

    public float animationSpeed = 10f; // Koliko linija se upali u sekundi
    public float currentLineProgress = 0f;
    public float globalFadeOutSpeed = 2f; // Brzina Fade Out-a celog sistema
    public bool triggerGlobalFadeOut = false;
    private float globalFadeMultiplier = 1f; // 1 = potpuno vidljivo, 0 = potpuno nevidljivo

    GraphicsBuffer pointBuffer;
    GraphicsBuffer indexBuffer;
    PointData[] cpuData;
    int pointLength;

    void Start()
    {
        if (parent.childCount < 2) return;

        int count = parent.childCount;

        pointBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, count, 12);
        pointLength = count;
        cpuData = new PointData[count];
        int numLines = (count * (count - 1)) / 2;
        int indexCount = numLines * 2;
        int[] indices = new int[indexCount];

        int counter = 0;
        for (int i = 0; i < count; i++)
        {
            for (int j = i + 1; j < count; j++)
            {
                indices[counter] = i;      // Prva tacka
                indices[counter + 1] = j;  // Druga tacka
                counter += 2;
            }
        }

        indexBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, indexCount, sizeof(int));
        indexBuffer.SetData(indices);
    }

    private void Update()
    {
        if(pointLength != parent.childCount)
        {
            pointLength = parent.childCount;
            pointBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, pointLength, 12);
            cpuData = new PointData[pointLength];
            int numLines = (pointLength * (pointLength - 1)) / 2;
            int indexCount = numLines * 2;
            int[] indices = new int[indexCount];

            int counter = 0;
            for (int i = 0; i < pointLength; i++)
            {
                for (int j = i + 1; j < pointLength; j++)
                {
                    indices[counter] = i;      // Prva tacka
                    indices[counter + 1] = j;  // Druga tacka
                    counter += 2;
                }
            }
            indexBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, indexCount, sizeof(int));
            indexBuffer.SetData(indices);
        }
        if (pointLength == 0) return;

        for (int i = 0; i < pointLength; i++)
        {
            Transform point=parent.GetChild(i);
            if (point == null) continue;
            cpuData[i].position = point.position;
        }
        pointBuffer.SetData(cpuData);

        int totalLines = (pointLength * (pointLength - 1)) / 2;

        if (triggerGlobalFadeOut)
        {
            // Ako je Fade Out aktivan, smanjujemo globalni multiplier
            globalFadeMultiplier -= Time.deltaTime * globalFadeOutSpeed;
            if (globalFadeMultiplier < 0)
            {
                triggerGlobalFadeOut = false;
                currentLineProgress = 0;
            }
            globalFadeMultiplier = Mathf.Clamp01(globalFadeMultiplier); // Da ne ode ispod 0
        }
        else
        {
            // Ako Fade Out NIJE aktivan, radimo standardni Fade In pojedinacnih linija
            currentLineProgress += Time.deltaTime * animationSpeed;

            // Postepeno vracamo globalni multiplier na 1 (ako smo se predomislili)
            globalFadeMultiplier += Time.deltaTime * globalFadeOutSpeed * 0.5f; // Malo sporiji povratak
            globalFadeMultiplier = Mathf.Clamp01(globalFadeMultiplier);
        }


        if (currentLineProgress > totalLines + 18) triggerGlobalFadeOut = true;

        if (lineMaterial != null)
        {
            lineMaterial.SetBuffer("_Points", pointBuffer);
            lineMaterial.SetBuffer("_Indices", indexBuffer);
            lineMaterial.SetFloat("_VisibleLinesProgress", currentLineProgress);
            lineMaterial.SetFloat("_GlobalFadeOutMultiplier", globalFadeMultiplier);
        }

        if (vfxGraph != null)
        {
            vfxGraph.SetInt("PointCount", pointLength);
            vfxGraph.SetGraphicsBuffer("PointBuffer", pointBuffer);
        }
    }

    private void OnRenderObject()
    {
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