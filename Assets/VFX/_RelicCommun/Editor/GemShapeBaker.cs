using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// COPIE de Relic/Assets/Editor/GemShapeBaker.cs (outil de cuisson des formes en gemmes, 25/09/2026) : calcule une forme
// en gemmes (GemShape) à partir d'un modèle : points tirés sur ses triangles au prorata de leur aire, luminosité (canal le
// plus faible) lue dans la texture du modèle, assombrie par l'occlusion. Ajout : filtre par nom de maillage (un seul
// sous-objet du modèle, ex. le casque du squelette KayKit) et menu Deathless.
// Menu Deathless > VFX > Heaume du rugissement en gemmes : le casque du Skeleton_Warrior (KayKit Skeletons 1.1).
public static class GemShapeBaker
{
    private const string WarriorModel = "Assets/VFX/SortieDeTerre/Skeleton_Warrior.fbx";
    private const string HelmetShape = "Assets/VFX/Rugissement/HeaumeGemShape.asset";

    [MenuItem("Deathless/VFX/Heaume du rugissement en gemmes")]
    public static void BakeHelmet()
    {
        GemShape shape = Bake(AssetDatabase.LoadAssetAtPath<GameObject>(WarriorModel), 1800, HelmetShape, 29, "Skeleton_Warrior_Helmet");
        Debug.Log("Heaume en gemmes : " + (shape != null ? shape.points.Length + " points" : "échec"));
    }

    public static GemShape Bake(GameObject model, int count, string path, int seed)
    {
        return Bake(model, count, path, seed, null);
    }

    // `meshName` : ne garder que le sous-objet de ce nom (null = tout le modèle).
    public static GemShape Bake(GameObject model, int count, string path, int seed, string meshName)
    {
        if (model == null)
            return null;
        List<Vector3> triA = new List<Vector3>(), triB = new List<Vector3>(), triC = new List<Vector3>();
        List<Vector2> uvA = new List<Vector2>(), uvB = new List<Vector2>(), uvC = new List<Vector2>();
        List<float> areas = new List<float>();
        Texture2D texture = null;
        foreach (MeshFilter filter in model.GetComponentsInChildren<MeshFilter>(true))
        {
            Mesh mesh = filter.sharedMesh;
            if (mesh == null || (meshName != null && filter.name != meshName))
                continue;
            Matrix4x4 toRoot = model.transform.worldToLocalMatrix * filter.transform.localToWorldMatrix;
            Vector3[] vertices = mesh.vertices;
            Vector2[] uvs = mesh.uv;
            int[] triangles = mesh.triangles;
            for (int t = 0; t < triangles.Length; t += 3)
            {
                Vector3 a = toRoot.MultiplyPoint3x4(vertices[triangles[t]]);
                Vector3 b = toRoot.MultiplyPoint3x4(vertices[triangles[t + 1]]);
                Vector3 c = toRoot.MultiplyPoint3x4(vertices[triangles[t + 2]]);
                triA.Add(a); triB.Add(b); triC.Add(c);
                bool hasUv = uvs != null && uvs.Length == vertices.Length;
                uvA.Add(hasUv ? uvs[triangles[t]] : Vector2.zero);
                uvB.Add(hasUv ? uvs[triangles[t + 1]] : Vector2.zero);
                uvC.Add(hasUv ? uvs[triangles[t + 2]] : Vector2.zero);
                areas.Add(Vector3.Cross(b - a, c - a).magnitude * 0.5f);
            }
            MeshRenderer renderer = filter.GetComponent<MeshRenderer>();
            if (texture == null && renderer != null && renderer.sharedMaterial != null)
                texture = renderer.sharedMaterial.mainTexture as Texture2D;
        }
        if (areas.Count == 0)
            return null;

        Texture2D readable = ReadableCopy(texture);
        float total = 0f;
        foreach (float a in areas) total += a;
        System.Random random = new System.Random(seed);
        List<Vector3> points = new List<Vector3>();
        List<Vector3> normals = new List<Vector3>();
        List<float> brightness = new List<float>();
        for (int n = 0; n < count; n++)
        {
            // Triangle tiré au prorata de son aire, point uniforme dans le triangle.
            float pick = (float)random.NextDouble() * total;
            int i = 0;
            for (; i < areas.Count - 1; i++)
            {
                pick -= areas[i];
                if (pick <= 0f) break;
            }
            float u = (float)random.NextDouble(), v = (float)random.NextDouble();
            if (u + v > 1f) { u = 1f - u; v = 1f - v; }
            points.Add(triA[i] + (triB[i] - triA[i]) * u + (triC[i] - triA[i]) * v);
            normals.Add(Vector3.Cross(triB[i] - triA[i], triC[i] - triA[i]).normalized);
            Vector2 uv = uvA[i] + (uvB[i] - uvA[i]) * u + (uvC[i] - uvA[i]) * v;
            Color c = readable != null ? readable.GetPixelBilinear(uv.x, uv.y) : Color.white;
            // Canal le plus faible plutôt que luminance : l'os est blanc crème, les orbites du crâne KayKit sont vertes (claires
            // en luminance, sombres ici).
            brightness.Add(Mathf.Min(c.r, Mathf.Min(c.g, c.b)));
        }
        if (readable != null)
            Object.DestroyImmediate(readable);

        // Creux (orbites, nez, dents) : la texture du crâne les distingue à peine. On y ajoute une occlusion : la part des
        // rayons partis du point vers l'extérieur qui retouchent le modèle. Plus un point est enfoncé, plus il est sombre.
        float[] occlusion = Occlusion(triA, triB, triC, points, normals, random);
        for (int n = 0; n < brightness.Count; n++)
            brightness[n] *= 1f - 0.85f * occlusion[n];

        // Centrés, le plus grand côté ramené à 1.
        Bounds bounds = new Bounds(points[0], Vector3.zero);
        foreach (Vector3 p in points) bounds.Encapsulate(p);
        float largest = Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z));
        for (int n = 0; n < points.Count; n++)
            points[n] = (points[n] - bounds.center) / Mathf.Max(0.0001f, largest);

        GemShape shape = AssetDatabase.LoadAssetAtPath<GemShape>(path);
        if (shape == null)
        {
            shape = ScriptableObject.CreateInstance<GemShape>();
            AssetDatabase.CreateAsset(shape, path);
        }
        shape.points = points.ToArray();
        shape.brightness = brightness.ToArray();
        shape.normals = normals.ToArray();
        EditorUtility.SetDirty(shape);
        AssetDatabase.SaveAssets();
        return shape;
    }

    // Occlusion de chaque point (0 dégagé, 1 enfermé) : 24 rayons dans l'hémisphère de sa normale, testés contre tous les
    // triangles (Möller-Trumbore). Le sens des normales du modèle n'est pas garanti : on garde le côté le plus dégagé.
    private static float[] Occlusion(List<Vector3> a, List<Vector3> b, List<Vector3> c, List<Vector3> points, List<Vector3> normals,
        System.Random random)
    {
        const int Rays = 24;
        Bounds bounds = new Bounds(points[0], Vector3.zero);
        foreach (Vector3 p in points) bounds.Encapsulate(p);
        float reach = Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z)) * 0.35f;
        float[] result = new float[points.Count];
        for (int n = 0; n < points.Count; n++)
        {
            float best = 1f;
            Vector3 outward = normals[n];
            for (int side = -1; side <= 1; side += 2)
            {
                Vector3 normal = normals[n] * side;
                Vector3 origin = points[n] + normal * reach * 0.01f;
                int hits = 0;
                for (int r = 0; r < Rays; r++)
                {
                    Vector3 dir = new Vector3((float)random.NextDouble() * 2f - 1f, (float)random.NextDouble() * 2f - 1f, (float)random.NextDouble() * 2f - 1f);
                    if (dir.sqrMagnitude < 1e-4f || dir.sqrMagnitude > 1f) { r--; continue; }
                    dir.Normalize();
                    if (Vector3.Dot(dir, normal) < 0f) dir = -dir;
                    for (int t = 0; t < a.Count; t++)
                        if (Hit(origin, dir, a[t], b[t], c[t], reach)) { hits++; break; }
                }
                if (hits / (float)Rays < best)
                {
                    best = hits / (float)Rays;
                    outward = normal;
                }
            }
            result[n] = best;
            normals[n] = outward;   // le côté le plus dégagé est l'extérieur
        }
        return result;
    }

    private static bool Hit(Vector3 origin, Vector3 dir, Vector3 a, Vector3 b, Vector3 c, float reach)
    {
        Vector3 e1 = b - a, e2 = c - a;
        Vector3 p = Vector3.Cross(dir, e2);
        float det = Vector3.Dot(e1, p);
        if (Mathf.Abs(det) < 1e-9f) return false;
        float inv = 1f / det;
        Vector3 s = origin - a;
        float u = Vector3.Dot(s, p) * inv;
        if (u < 0f || u > 1f) return false;
        Vector3 q = Vector3.Cross(s, e1);
        float v = Vector3.Dot(dir, q) * inv;
        if (v < 0f || u + v > 1f) return false;
        float t = Vector3.Dot(e2, q) * inv;
        return t > 1e-5f && t < reach;
    }

    // Copie lisible d'une texture (même non lisible à l'import) : rendu dans une RenderTexture puis relecture.
    private static Texture2D ReadableCopy(Texture2D source)
    {
        if (source == null)
            return null;
        RenderTexture rt = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        Graphics.Blit(source, rt);
        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = rt;
        Texture2D copy = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
        copy.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
        copy.Apply();
        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(rt);
        return copy;
    }
}
