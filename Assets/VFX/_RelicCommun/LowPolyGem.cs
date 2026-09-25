using System.Collections.Generic;
using UnityEngine;

// Petite gemme low poly (octaèdre irrégulier) écrite dans un maillage partagé : 8 facettes, 24 sommets, chaque facette
// ombrée selon son orientation du moment (lumière peinte, valeur absolue : les deux côtés sont éclairés pareil). Sert à
// la soupe du portail (PortalVisual) et à la gerbe de gemmes (GemBurst). Shader Relic/VertexColorUnlit.
public static class LowPolyGem
{
    public const int VerticesPerGem = 24;

    // Octaèdre : six pointes (±x, ±y, ±z) et huit faces, chacune dans l'ordre qui la tourne vers l'extérieur (calculé
    // une fois : sens horaire vu de l'extérieur, la face avant de Unity).
    private static readonly Vector3[] Tips = { Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back };
    private static int[] faces;
    private static readonly Vector3[] tipBuffer = new Vector3[6];

    public static readonly Vector3 DefaultLight = new Vector3(0.45f, 0.75f, -0.5f).normalized;

    private static void EnsureFaces()
    {
        if (faces != null)
            return;
        List<int> list = new List<int>();
        int[] xs = { 0, 1 }, ys = { 2, 3 }, zs = { 4, 5 };
        foreach (int x in xs)
            foreach (int y in ys)
                foreach (int z in zs)
                {
                    Vector3 a = Tips[x], b = Tips[y], c = Tips[z];
                    Vector3 n = Vector3.Cross(b - a, c - a);
                    if (Vector3.Dot(n, a + b + c) > 0f) { list.Add(x); list.Add(y); list.Add(z); }
                    else { list.Add(x); list.Add(z); list.Add(y); }
                }
        faces = list.ToArray();
    }

    // Indices d'un maillage de `count` gemmes (un triangle par facette, sommets propres).
    public static int[] Triangles(int count)
    {
        int[] triangles = new int[count * VerticesPerGem];
        for (int i = 0; i < triangles.Length; i++)
            triangles[i] = i;
        return triangles;
    }

    // Ecrit la gemme `index` : centre, taille (distance centre-pointe), proportions, orientation, couleur.
    public static void Write(Vector3[] vertices, Color[] colors, int index, Vector3 center, float size, Vector3 stretch,
        Quaternion rotation, Color color, Vector3 light)
    {
        EnsureFaces();
        int v = index * VerticesPerGem;
        for (int k = 0; k < 6; k++)
            tipBuffer[k] = center + rotation * Vector3.Scale(Tips[k], stretch) * size;
        for (int f = 0; f < 8; f++)
        {
            Vector3 a = tipBuffer[faces[f * 3]], b = tipBuffer[faces[f * 3 + 1]], c = tipBuffer[faces[f * 3 + 2]];
            Vector3 n = Vector3.Cross(b - a, c - a);
            float shade = n.sqrMagnitude > 1e-12f ? 0.45f + 0.6f * Mathf.Abs(Vector3.Dot(n.normalized, light)) : 1f;
            Color shaded = color * shade;
            vertices[v] = a; vertices[v + 1] = b; vertices[v + 2] = c;
            colors[v] = colors[v + 1] = colors[v + 2] = shaded;
            v += 3;
        }
    }
}
