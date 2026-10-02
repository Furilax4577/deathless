using System.IO;
using UnityEditor;
using UnityEngine;

/// Captures de vérification de la carte v5 (02/10/2026) : une caméra temporaire rend la scène ouverte dans un
/// RenderTexture 1920 x 1080 et écrit le PNG dans Assets/Screenshots/retoursv5_<nom>.png (jamais de focus de l'éditeur).
/// Utilisable aussi en Play (les effets de particules y tournent).
public static class CaptureV5
{
    /// Préfixe des fichiers (retoursv5_ jusqu'au 02/10 midi ; retoursv5b_ pour la seconde série de retours).
    public static string Prefixe = "retoursv5b_";
    /// Depuis la caméra de jeu (troisième personne) telle qu'elle est en Play : même position, même visée, même champ.
    public static string PrendreCameraJeu(string nom, int largeur = 1920, int hauteur = 1080)
    {
        Camera c = Camera.main;
        if (c == null) return "pas de caméra principale";
        Vector3 pos = c.transform.position, cible = pos + c.transform.forward * 10f;
        return Prendre(nom, pos, cible, c.fieldOfView, largeur, hauteur);
    }

    public static string Prendre(string nom, Vector3 pos, Vector3 cible, float fov = 62f, int largeur = 1920, int hauteur = 1080)
    {
        var go = new GameObject("_CaptureV5");
        try
        {
            var cam = go.AddComponent<Camera>();
            cam.fieldOfView = fov; cam.nearClipPlane = 0.15f; cam.farClipPlane = 600f;
            cam.clearFlags = CameraClearFlags.Skybox; cam.useOcclusionCulling = false;
            go.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
            go.transform.position = pos; go.transform.LookAt(cible);
            var rt = new RenderTexture(largeur, hauteur, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            cam.targetTexture = rt;
            cam.Render();
            var prev = RenderTexture.active; RenderTexture.active = rt;
            var tex = new Texture2D(largeur, hauteur, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, largeur, hauteur), 0, 0); tex.Apply();
            RenderTexture.active = prev;
            string chemin = Path.Combine(Directory.GetCurrentDirectory(), "Assets/Screenshots/" + Prefixe + nom + ".png");
            File.WriteAllBytes(chemin, tex.EncodeToPNG());
            Object.DestroyImmediate(tex); cam.targetTexture = null; rt.Release(); Object.DestroyImmediate(rt);
            return chemin;
        }
        finally { Object.DestroyImmediate(go); }
    }
}
