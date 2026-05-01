using UnityEngine;
using UnityEditor;
using System.IO;

public class MeshInverterEditor : EditorWindow
{
    [MenuItem("Tools/Invert Mesh and Save")]
    public static void InvertAndSaveMesh()
    {
        GameObject selected = Selection.activeGameObject;

        if (selected == null || selected.GetComponent<MeshFilter>() == null)
        {
            EditorUtility.DisplayDialog("Greska", "Moras selektovati objekat koji ima MeshFilter!", "OK");
            return;
        }

        MeshFilter meshFilter = selected.GetComponent<MeshFilter>();
        Mesh oldMesh = meshFilter.sharedMesh;

        if (oldMesh == null) return;

        // Kreiramo kopiju mesha da ne bismo unistili originalni fajl
        Mesh newMesh = Instantiate(oldMesh);
        newMesh.name = oldMesh.name + "_Inverted";

        // 1. Invertovanje normala
        Vector3[] normals = newMesh.normals;
        for (int i = 0; i < normals.Length; i++)
        {
            normals[i] = -normals[i];
        }
        newMesh.normals = normals;

        // 2. Invertovanje trouglova (namotavanje)
        for (int m = 0; m < newMesh.subMeshCount; m++)
        {
            int[] triangles = newMesh.GetTriangles(m);
            for (int i = 0; i < triangles.Length; i += 3)
            {
                int temp = triangles[i];
                triangles[i] = triangles[i + 1];
                triangles[i + 1] = temp;
            }
            newMesh.SetTriangles(triangles, m);
        }

        // Recalculate bounds da bi culling radio kako treba
        newMesh.RecalculateBounds();

        // 3. Cuvanje mesha kao .asset fajl
        string path = "Assets/InvertedMeshes/";
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }

        string assetPath = path + newMesh.name + ".asset";
        AssetDatabase.CreateAsset(newMesh, assetPath);
        AssetDatabase.SaveAssets();

        // Dodeli novi mesh objektu
        meshFilter.sharedMesh = newMesh;

        EditorUtility.DisplayDialog("Uspesno", "Novi mesh je sacuvan u: " + assetPath, "Super");
    }
}