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

    /// Vue de dessus orthographique (nord en haut, est à droite) : `demiHauteur` m du centre au bord haut de l'image (40 : toute la carte
    /// du village, 80 m de haut sur 142 m de large).
    public static string PrendreDessus(string nom, Vector3 centre, float demiHauteur, int largeur = 1920, int hauteur = 1080)
    {
        // les nuages du ciel (Ciel_Nuages) passeraient entre la caméra et le sol : écartés le temps du rendu
        GameObject nuages = GameObject.Find("VillageBlockout/Ciel_Nuages");
        bool etait = nuages != null && nuages.activeSelf;
        if (etait) nuages.SetActive(false);
        bool brouillard = RenderSettings.fog; RenderSettings.fog = false;   // le brouillard de distance blanchirait la carte vue de 150 m
        try { return Prendre(nom, centre + Vector3.up * 150f, centre, 62f, largeur, hauteur, demiHauteur); }
        finally { RenderSettings.fog = brouillard; if (etait) nuages.SetActive(true); }
    }

    /// Vue joueur « épaule » (celle de CameraEpaule : 5,5 m derrière le héros, tangage de 22°, champ de 62°) : le héros est debout en
    /// `heros`, regard de lacet `lacet` ; l'épaule droite est à 0,6 m. La caméra ne tient pas compte des colliders (capture de contrôle).
    public static string PrendreEpaule(string nom, Vector3 heros, float lacet, float distance = 5.5f, float tangage = 22f, int largeur = 1920, int hauteur = 1080)
    {
        Quaternion rot = Quaternion.Euler(tangage, lacet, 0f);
        Vector3 pivot = heros + Vector3.up * 1.6f + Quaternion.Euler(0f, lacet, 0f) * Vector3.right * 0.6f;
        Vector3 pos = pivot + rot * Vector3.back * distance;
        return Prendre(nom, pos, pos + rot * Vector3.forward * 10f, 62f, largeur, hauteur);
    }

    /// Série de contrôle des bâtiments Tripo (carte v5) : vue de dessus de toute la carte (40 m), puis pour chaque bâtiment Tripo une vue de
    /// dessus de près et la vue joueur « épaule » (5,5 m, 22°) à 3 m devant les marches, dans l'axe de l'embrasure ; la forge a en plus deux
    /// vues d'atelier (au poste de chauffe, face au foyer ; au poste de frappe). Fichiers Assets/Screenshots/<prefixe><...>.png.
    public static string SerieTripo(string prefixe)
    {
        string anc = Prefixe; Prefixe = prefixe;
        var sb = new System.Text.StringBuilder();
        try
        {
            sb.AppendLine(PrendreDessus("dessus_carte", Vector3.zero, 40f));
            Transform ms = GameObject.Find("VillageBlockout/Maisons").transform;
            foreach (Transform bat in ms)
            {
                Transform rendu = null, entree = null;
                foreach (Transform t in bat.GetComponentsInChildren<Transform>()) { if (t.name == "Rendu") rendu = t; else if (t.name == "Entree") entree = t; }
                if (rendu == null || entree == null) continue;
                string role = bat.name.Replace("Batiment_", "").ToLowerInvariant();
                Vector3 c = bat.position; c.y = 0f;
                sb.AppendLine(PrendreDessus(role + "_dessus", c, 14f));
                // pied des marches : bord avant des sommets au sol (repère de la bat), dans l'axe de l'embrasure
                float zAvant = -999f;
                foreach (Vector3 v in rendu.GetComponent<MeshFilter>().sharedMesh.vertices)
                { Vector3 p = bat.InverseTransformPoint(rendu.TransformPoint(v)); if (p.y < 0.1f) zAvant = Mathf.Max(zAvant, p.z); }
                Vector3 heros = bat.TransformPoint(new Vector3(bat.InverseTransformPoint(entree.position).x, 0f, zAvant + 3f));
                heros.y = VillageBuilder.GroundHeight(heros.x, heros.z);
                float lacet = bat.eulerAngles.y + 180f;
                sb.AppendLine(PrendreEpaule(role + "_joueur_face", heros, lacet));
                // trois-quarts droit et gauche : héros décalé de 5 m sur le côté, regard vers la porte
                foreach (float cote in new[] { -1f, 1f })
                {
                    Vector3 h2 = heros + bat.right * cote * 5f; h2.y = VillageBuilder.GroundHeight(h2.x, h2.z);
                    Vector3 vers = bat.TransformPoint(new Vector3(bat.InverseTransformPoint(entree.position).x, 0f, zAvant)) - h2; vers.y = 0f;
                    sb.AppendLine(PrendreEpaule(role + (cote > 0f ? "_joueur_gauche" : "_joueur_droite"), h2, Quaternion.LookRotation(vers).eulerAngles.y));
                }
                Transform enseigne = null; foreach (Transform t in bat.GetComponentsInChildren<Transform>()) if (t.name == "Enseigne") enseigne = t;
                if (enseigne != null) sb.AppendLine(Prendre(role + "_enseigne", enseigne.position + bat.forward * 7f + Vector3.up * 0.4f, enseigne.position, 50f));
                if (bat.name == "Batiment_Forge")
                {
                    foreach (string poste in new[] { "Poste_Chauffe", "Poste_Frappe", "Poste_Trempe" })
                    {
                        Transform a = null; foreach (Transform t in bat.GetComponentsInChildren<Transform>()) if (t.name == poste) a = t;
                        if (a == null) continue;
                        Vector3 h3 = a.position; h3.y = VillageBuilder.GroundHeight(h3.x, h3.z);
                        sb.AppendLine(PrendreEpaule("forge_atelier_" + poste.ToLowerInvariant(), h3, a.eulerAngles.y));
                    }
                    Transform foyer = null; foreach (Transform t in bat.GetComponentsInChildren<Transform>()) if (t.name == "Foyer_Feu") foyer = t;
                    if (foyer != null) sb.AppendLine(Prendre("forge_foyer", foyer.position + foyer.forward * 3.5f + Vector3.up * 0.8f, foyer.position, 50f));
                }
            }
        }
        finally { Prefixe = anc; }
        return sb.ToString();
    }

    /// Série des villageois dehors (03/10/2026) : vue de dessus de la carte, puis pour chaque villageois (forgeron, tavernière, mécano, druide,
    /// sorcier) la vue joueur « épaule » (5,5 m, 22°) à 3,5 m devant lui, regard vers lui, et la vue joueur dans l'axe de la porte de sa
    /// maison (à 3 m devant les marches, comme SerieTripo) où il doit se voir à côté de l'allée, et une vue de dessus de près.
    public static string SerieVillageois(string prefixe)
    {
        string anc = Prefixe; Prefixe = prefixe;
        var sb = new System.Text.StringBuilder();
        try
        {
            sb.AppendLine(PrendreDessus("dessus_carte", Vector3.zero, 40f));
            Transform vil = GameObject.Find("VillageBlockout/Villageois").transform;
            string[] roles = { "Forge", "Taverne", "Mecano", "Druide", "Sorcier" };
            string[] pnj = { "Forgeron", "Tavernier", "Mecano", "Druide", null };
            for (int k = 0; k < roles.Length; k++)
            {
                Transform bat = GameObject.Find("VillageBlockout/Maisons/Batiment_" + roles[k]).transform;
                Transform p = pnj[k] != null ? vil.Find(roles[k] + "/" + pnj[k]) : vil.Find("Poste_Sorcier");
                if (p == null) { sb.AppendLine(roles[k] + " : villageois absent"); continue; }
                string nom = roles[k].ToLowerInvariant();
                Vector3 c = p.position; c.y = 0f;
                sb.AppendLine(PrendreDessus(nom + "_dessus", c, 9f));
                // héros 3,5 m devant le villageois (dans le sens du regard du bâtiment), regard vers lui
                Vector3 h = p.position + bat.forward * 3.5f; h.y = VillageBuilder.GroundHeight(h.x, h.z);
                Vector3 vers = p.position - h; vers.y = 0f;
                sb.AppendLine(PrendreEpaule(nom + "_joueur_devant", h, Quaternion.LookRotation(vers).eulerAngles.y));
                // héros dans l'axe de l'embrasure, 3 m devant les marches, regard vers la porte
                Transform rendu = null, entree = null;
                foreach (Transform t in bat.GetComponentsInChildren<Transform>()) { if (t.name == "Rendu") rendu = t; else if (t.name == "Entree") entree = t; }
                float zAvant = -999f;
                foreach (Vector3 v in rendu.GetComponent<MeshFilter>().sharedMesh.vertices)
                { Vector3 q = bat.InverseTransformPoint(rendu.TransformPoint(v)); if (q.y < 0.1f) zAvant = Mathf.Max(zAvant, q.z); }
                Vector3 h2 = bat.TransformPoint(new Vector3(bat.InverseTransformPoint(entree.position).x, 0f, zAvant + 3f));
                h2.y = VillageBuilder.GroundHeight(h2.x, h2.z);
                sb.AppendLine(PrendreEpaule(nom + "_joueur_porte", h2, bat.eulerAngles.y + 180f));
            }
        }
        finally { Prefixe = anc; }
        return sb.ToString();
    }

    public static string Prendre(string nom, Vector3 pos, Vector3 cible, float fov = 62f, int largeur = 1920, int hauteur = 1080, float ortho = 0f)
    {
        var go = new GameObject("_CaptureV5");
        try
        {
            var cam = go.AddComponent<Camera>();
            cam.fieldOfView = fov; cam.nearClipPlane = 0.15f; cam.farClipPlane = 600f;
            if (ortho > 0f) { cam.orthographic = true; cam.orthographicSize = ortho; }
            cam.clearFlags = CameraClearFlags.Skybox; cam.useOcclusionCulling = false;
            go.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
            go.transform.position = pos;
            if (ortho > 0f) go.transform.rotation = Quaternion.Euler(90f, 0f, 0f); else go.transform.LookAt(cible);
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
