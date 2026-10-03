using System.Collections.Generic;
using System.Text;
using Deathless.Jeu;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEngine;

// Bâtiments KayKit provisoires de la carte v5 (03/10/2026, demande de Quentin : « les maisons Tripo ne sont pas abouties : en attendant,
// utilise cette maison KayKit pour tous ; essaie juste de la poser sur le sol plutôt qu'être sur un vide sanitaire »). Les six bâtiments
// (maison de base, sorcier, druide, mécano, forge, taverne) redeviennent les modèles du pack KayKit Medieval Hexagon, en attendant les
// bâtiments que Quentin fera venir (Tripo par texte) ; les prefabs Tripo (Assets/Art/Decor/Maisons/) restent dans le projet, et
// V5MaisonsTripo[rôle] != null les remet en place.
//   maison de base, sorcier, druide, mécano : building_home_B (maison à étage, toit à lucarne, soubassement de pierre et escalier de
//                         quatre marches) ; toit bleu (maison de base, sorcier), vert (druide), rouge (mécano) : les trois coloris du pack
//                         partagent la même texture, seul le dessin des UV change, donc le même matériau à vitres émissives la nuit
//   forge                : building_blacksmith (cabane à toit bleu collée au grand four de pierre, enclume, marteau, râtelier, seau, dalles)
//   taverne              : building_tavern (toit en tonneau couché, auvent de bois avec tables, soubassement de pierre et escalier)
// Pose « sur le sol » : le bas du maillage (soubassement de pierre) est à y = 0, le sol sous les bâtiments est plat (GroundHeight = 0 sur
// tout le village), enfoncé par modèle (V5KKEnfoncementMaison 1,15 m, V5KKEnfoncementForge 0,74 m : le sol arrive à la dernière marche, il reste une marche
// de 40 cm devant la porte) pour qu'aucun jour ne reste entre la pierre et l'herbe ; les deux premières marches de l'escalier sont sous terre ; aucun vide, ni creux, ni plancher surélevé sous le bâtiment.
// Structure sous Maisons/Batiment_<Rôle> (comme les prefabs Tripo : V5RenduTripo, V5EmpriseTripo, V5FacadeDe les lisent tels quels) :
//   KayKit_<pièce>/Rendu       maillage KayKit à l'échelle 7,5 (porte de 2,10 m), matériau des maisons (vitres émissives la nuit)
//   KayKit_<pièce>/Collision   MeshCollider du même maillage + NavMeshModifier « Not Walkable » (les marches se montent à pied, le corps
//                              du bâtiment est infranchissable et hors NavMesh)
//   KayKit_<pièce>/Entree      repère de la porte (déclencheur Ignore Raycast, axe avant vers l'extérieur)
//   KayKit_<pièce>/Enclume_Ancre (forge seulement) : au sol devant l'enclume du modèle, place du forgeron
// Lanterne de porte (lantern_standing, cycle jour / nuit) : Ambiance/Lanternes/Lanterne_<Maison_n_X>, comme à l'origine.
public static partial class VillageBuilder
{
    /// Pièce KayKit du rôle (même ordre que V5MaisonsRoles : Taverne, Mecano, Maison, Forge, Sorcier, Druide) ; null = pas de modèle KayKit.
    public static readonly string[] V5MaisonsKayKitPiece = { "tavern", "home_B", "home_B", "blacksmith", "home_B", "home_B" };
    /// Coloris du pack (dossier buildings/<coloris>/) : toit bleu ou rouge de la maison, vert du druide (le sorcier garde le bleu de l'image de Quentin).
    public static readonly string[] V5MaisonsKayKitCoul = { "blue", "red", "blue", "blue", "blue", "green" };
    /// Écart de pose par rapport au centre du plan (m, x ; z) : les bâtiments KayKit tiennent presque tous au centre du plan. Sorcier et druide
    /// reculent vers le sud : à (-27 ; 28) et (21 ; 28) le fond du toit (9,6 m de haut) touche la falaise (rochers à moins de 3 m, puis à moins de 1 m).
    public static readonly Vector2[] V5MaisonsKayKitEcart = { Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(0f, -3f), new Vector2(3f, -4f) };
    /// Échelle des bâtiments KayKit : celle de l'ancienne carte (HouseDoorTarget / HouseDoorLocal = 2,1 / 0,28), porte de 2,10 m.
    public const float V5KKEchelle = 7.5f;
    /// Enfoncement du modèle (m), propre à chaque modèle (03/10/2026, Quentin : « le soubassement nettement enfoncé »). Mesuré sur les modèles à
    /// l'échelle 7,5 (aire des faces horizontales par hauteur) : home_B et tavern ont leur plancher à 1,57 m et des marches à 0,40 / 0,80 / 1,20 m ;
    /// 1,15 m d'enfoncement met le sol à 5 cm sous le dessus de la dernière marche (reste une marche de 0,40 m devant la porte, plancher à 0,42 m).
    /// building_blacksmith n'a pas de marches : un dallage de pierre à 0,79 m devant la cabane (dessus à 0,79 à 0,88 m) puis le plancher de la
    /// cabane à 1,16 m, soit la même marche de 0,37 m ; 0,74 m d'enfoncement = même rendu (sol 5 cm sous le dallage, plancher à 0,42 m).
    public const float V5KKEnfoncementMaison = 1.15f, V5KKEnfoncementForge = 0.74f;
    /// Distance (m) entre le bord avant des marches (ou le devant de la porte) et le pied de la porte où le héros réapparaît en sortant.
    public const float V5KKSeuil = 0.9f;

    /// Repère de chaque pièce (mètres à l'échelle 7,5, x vers la droite vue de la façade, z vers l'extérieur) : axe de la porte (centre de
    /// l'escalier, ou de la porte de la cabane), plan de la façade au droit de la porte, position de la lanterne de porte.
    sealed class V5KKPieceInfo { public float porteX, facadeZ, pied, enfoncement; public Vector2 lanterne; public Vector2 enclume; }
    static readonly Dictionary<string, V5KKPieceInfo> s_V5KKPieces = new Dictionary<string, V5KKPieceInfo> {
        // escalier de x = -0,28 à 0 (unités du modèle), façade du rez-de-chaussée à z = 0,28 ; lanterne : 35 cm hors de l'escalier, côté gauche ;
        // pied = bord avant de la dernière marche visible (z = 0,42 du modèle ; les deux premières marches sont sous terre), lanterne reculée d'autant
        { "home_B",     new V5KKPieceInfo { porteX = -1.05f, facadeZ = 2.10f, pied = 3.15f, enfoncement = V5KKEnfoncementMaison, lanterne = new Vector2(-2.45f, 3.51f) } },
        // escalier de x = 0,01 à 0,28, façade à z = 0,35 ; lanterne du côté de l'auvent, à 35 cm de l'escalier
        { "tavern",     new V5KKPieceInfo { porteX = 1.09f, facadeZ = 2.62f, pied = 3.68f, enfoncement = V5KKEnfoncementMaison, lanterne = new Vector2(-0.35f, 3.51f) } },
        // cabane de x = 0,15 à 0,58, porte au milieu, façade à z = 0,08 ; pas d'escalier : la porte est au sol ; enclume au milieu du devant du four
        { "blacksmith", new V5KKPieceInfo { porteX = 2.55f, facadeZ = 0.60f, pied = 2.30f, enfoncement = V5KKEnfoncementForge, lanterne = new Vector2(4.6f, 2.6f), enclume = new Vector2(0.15f, 6.9f) } },
    };

    /// Pied de l'escalier visible (bord avant de la dernière marche, ou devant de la porte) d'un bâtiment KayKit : racine Batiment_<Rôle>, dans son repère.
    static bool V5PiedKayKit(Transform racine, out float pied)
    {
        pied = 0f;
        if (racine == null || !racine.name.StartsWith("Batiment_")) return false;
        int i = System.Array.IndexOf(V5MaisonsRoles, racine.name.Substring("Batiment_".Length));
        if (i < 0 || V5MaisonsTripo[i] != null || V5MaisonsKayKitPiece[i] == null) return false;
        pied = s_V5KKPieces[V5MaisonsKayKitPiece[i]].pied;
        return true;
    }

    static string V5KKChemin(int i)
    {
        string c = V5MaisonsKayKitCoul[i], p = V5MaisonsKayKitPiece[i];
        return "Assets/Art/KayKit/KayKit_Medieval_Hexagon_Pack_1.0_FREE/Assets/fbx(unity)/buildings/" + c + "/building_" + p + "_" + c + ".fbx";
    }

    /// Pose le bâtiment KayKit du rôle `i` : retire l'ancien bâtiment (racine, modèle, lanterne, intérieur) puis recrée Batiment_<Rôle> au
    /// centre du plan (pivot au sol, y = 0, façade +Z vers Nyxessa) avec le modèle posé sur le sol, sa collision, son repère d'entrée et sa
    /// lanterne de porte, branchés sur le cycle jour / nuit. Reproductible.
    static string V5PoserMaisonKayKit(Transform root, Transform ms, int i)
    {
        string role = V5MaisonsRoles[i], nom = V5MaisonsNoms[i], piece = V5MaisonsKayKitPiece[i];
        V5KKPieceInfo info = s_V5KKPieces[piece];
        GameObject fbx = AssetDatabase.LoadAssetAtPath<GameObject>(V5KKChemin(i));
        if (fbx == null) return role + " : modèle KayKit introuvable (" + V5KKChemin(i) + ") ; ";
        Mesh maillage = fbx.GetComponentInChildren<MeshFilter>().sharedMesh;
        Material mat = HouseNightMaterial();
        Transform lan = root.Find("Ambiance/Lanternes");
        if (lan != null) Kill(lan.Find("Lanterne_" + nom));
        Transform ancien = TrouverMaison(root, nom);
        Kill(ms.Find("Batiment_" + role));
        if (ancien != null) Kill(ancien);
        Vector3 centre = new Vector3(V5MaisonsCentres[i].x + V5MaisonsKayKitEcart[i].x, 0f, V5MaisonsCentres[i].y + V5MaisonsKayKitEcart[i].y);
        Transform racine = new GameObject("Batiment_" + role).transform;
        racine.SetParent(ms, false);
        racine.SetPositionAndRotation(centre, Quaternion.Euler(0f, YawToward(centre, Vector3.zero), 0f));
        racine.SetSiblingIndex(i);
        Transform mod = new GameObject(V5NomModele(i)).transform;
        mod.SetParent(racine, false);

        GameObject r = new GameObject("Rendu"); r.transform.SetParent(mod, false);
        r.transform.localPosition = new Vector3(0f, -info.enfoncement, 0f); r.transform.localScale = Vector3.one * V5KKEchelle;
        r.AddComponent<MeshFilter>().sharedMesh = maillage;
        var mr = r.AddComponent<MeshRenderer>(); mr.sharedMaterial = mat;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        GameObjectUtility.SetStaticEditorFlags(r, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic);

        GameObject c = new GameObject("Collision"); c.transform.SetParent(mod, false);
        c.transform.localPosition = r.transform.localPosition; c.transform.localScale = r.transform.localScale;
        c.AddComponent<MeshCollider>().sharedMesh = maillage;
        var nm = c.AddComponent<NavMeshModifier>(); nm.overrideArea = true; nm.area = 1;   // zone « Not Walkable » : jamais marchable pour le NavMesh

        GameObject e = new GameObject("Entree"); e.layer = 2; e.transform.SetParent(mod, false);
        e.transform.localPosition = new Vector3(info.porteX, 0f, info.facadeZ);
        var bc = e.AddComponent<BoxCollider>(); bc.isTrigger = true;
        bc.size = new Vector3(1.8f, 2.6f, 1.6f); bc.center = new Vector3(0f, 1.3f, 0.8f);
        // Bâtiment entrable (03/10/2026) : touche Interagir devant la porte -> pièce du bâtiment (EntreeBatiment, PiecesBatiments). Le pied de la
        // porte (réapparition à la sortie) est à V5KKSeuil m devant le bord avant des marches (la forge n'a pas d'escalier : devant le râtelier).
        V5ConfigurerEntree(e, role, info);

        if (piece == "blacksmith")
        {
            var a = new GameObject("Enclume_Ancre").transform; a.SetParent(mod, false);
            a.localPosition = new Vector3(info.enclume.x, 0f, info.enclume.y);
        }

        var cycle = Object.FindFirstObjectByType<CycleJourNuit>();
        string suite = V5RetirerInterieur(root, role);
        string lanterneInfo = V5LanterneKayKit(root, racine, info, nom, cycle);
        if (cycle != null)
        {
            var rs = new List<Renderer>(cycle.maisons ?? new Renderer[0]); rs.RemoveAll(x => x == null);
            rs.Add(mr); cycle.maisons = rs.ToArray();
            EditorUtility.SetDirty(cycle);
        }
        string effets = V5FumeeKayKit(mod, piece, info);
        if (piece == "blacksmith") effets += V5FeuForgeKayKit(mod, info) + V5MetalForgeKayKit(mod);
        return role + " (KayKit " + piece + " " + V5MaisonsKayKitCoul[i] + ", lacet " + racine.eulerAngles.y.ToString("F1") + ", porte à " + e.transform.position.ToString("F2") + suite + lanterneInfo + effets + ") ; ";
    }

    /// Sommet de la cheminée (m, repère du modèle à l'échelle 7,5, avant enfoncement ; x déjà retourné : l'import Unity inverse l'axe X du FBX) et réglages de
    /// la fumée (03/10/2026, Quentin) : maisons = filet gris, fin et léger ; forge = fumée noire, plus dense et plus large, lisible sans masquer le bâtiment.
    /// Relevé sur les maillages (face horizontale grise du couronnement) : home_B (-1,05 ; 9,60 ; -3,15), blacksmith (-1,58 ; 7,35 ; -1,05).
    /// La taverne KayKit n'a pas de cheminée (celle de la taverne Tripo porte déjà sa fumée).
    static string V5FumeeKayKit(Transform mod, string piece, V5KKPieceInfo info)
    {
        Vector3 sommet; bool forge = piece == "blacksmith";
        if (piece == "home_B") sommet = new Vector3(-1.05f, 9.60f, -3.15f);
        else if (forge) sommet = new Vector3(-1.58f, 7.35f, -1.05f);
        else return "";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/VFX/BiereFuite/BiereFuite.mat");
        if (mat == null) mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/VFX/_RelicCommun/PortalVoxel.mat");
        if (mat == null) return ", pas de matériau de gemmes : pas de fumée";
        var a = new GameObject("Cheminee"); a.transform.SetParent(mod, false);
        a.transform.localPosition = new Vector3(sommet.x, sommet.y - info.enfoncement + 0.08f, sommet.z);
        var f = a.AddComponent<FumeeCheminee>();
        f.materiau = mat; f.theme = VfxTheme.Fumee;
        if (forge) { f.teinteA = "Suie"; f.teinteB = "Noire"; f.debit = 3.2f; f.taille = 1.25f; f.montee = 1.1f; f.rayon = 0.3f; f.duree = 1.25f; f.capacite = 110; f.distanceMax = 80f; }
        else { f.teinteA = "Grise"; f.teinteB = "Claire"; f.debit = 0.9f; f.taille = 0.55f; f.montee = 0.8f; f.rayon = 0.12f; f.duree = 1.15f; f.capacite = 30; f.distanceMax = 60f; }
        EditorUtility.SetDirty(f);
        return ", fumée " + (forge ? "noire" : "grise") + " en " + a.transform.localPosition.ToString("F2");
    }

    /// Feu dans la bouche du four de pierre de la forge KayKit : l'effet ForgeFeu (flammes, étincelles et braises en gemmes de la palette Feu, comme
    /// les flammes des sorts du mage ; jamais de vert) et une lumière chaude qui vacille (portée 8 m, sans ombre), allumés jour et nuit.
    /// Géométrie relevée sur building_blacksmith (échelle 7,5, x retourné) : l'arche du four a ses piédroits à x = -2,35 et -0,80 (bouche de 1,55 m),
    /// son linteau à 1,58 m ; le plancher de la bouche est à 0,49 m (la forge est enfoncée de 0,74 m : il passe à 0,25 m sous le sol, on
    /// pose le feu à 0,12 m au-dessus du sol), le fond à z = 0,79 et l'avant du linteau à z = 1,45. Le feu est au milieu de la bouche (z = 1,6), à l'échelle 1,6 de
    /// l'effet de l'intérieur (lit de 1,4 x 1,1 m) et de hauteur réduite pour ne pas traverser le linteau.
    static string V5FeuForgeKayKit(Transform mod, V5KKPieceInfo info)
    {
        Material gemmes = AssetDatabase.LoadAssetAtPath<Material>("Assets/VFX/_RelicCommun/PortalVoxel.mat");
        if (gemmes == null) return ", PortalVoxel.mat absent : pas de feu";
        Vector3 bouche = new Vector3(-1.58f, Mathf.Max(0.12f, 0.49f - info.enfoncement + 0.12f), 1.6f);
        var go = new GameObject("Forge_FeuVivant"); go.transform.SetParent(mod, false); go.transform.localPosition = bouche;
        var lg = new GameObject("Feu_Forge"); lg.transform.SetParent(mod, false); lg.transform.localPosition = bouche + new Vector3(0f, 0.8f, 0.5f);
        Light l = lg.AddComponent<Light>();
        l.type = LightType.Point; l.range = 8f; l.shadows = LightShadows.None;
        l.color = Color.Lerp(VfxPalette.Couleur(VfxTheme.Feu, VfxRole.Vif, new Color(1f, 0.38f, 0.04f)), VfxPalette.Couleur(VfxTheme.Feu, VfxRole.Coeur, new Color(1f, 0.9f, 0.4f)), 0.3f);
        l.intensity = 2.2f;
        var feu = go.AddComponent<ForgeFeu>();
        feu.materiau = gemmes; feu.lumiere = l; feu.intensite = 2.2f; feu.partJour = 1f;   // allumé aussi fort le jour que la nuit
        feu.demiLit = new Vector2(0.45f, 0.35f); feu.hauteur = 0.4f; feu.flammes = 40; feu.etincelles = 8; feu.braisesVives = 18;
        go.transform.localScale = Vector3.one * 1.6f;
        EditorUtility.SetDirty(feu);
        return ", feu ForgeFeu + lumière chaude dans la bouche du four (" + bouche.ToString("F2") + ")";
    }

    /// Pièces de fer de la forge KayKit pour les bruits de pas (03/10/2026) : le modèle est d'un seul maillage, sa collision est marquée
    /// pierre ; on pose, exactement sur le dessus de l'enclume et de la lame posée devant le four, des boîtes de collision minces (une par bande de 15 cm, dessus au 90e centile des hauteurs relevées)
    /// (Metal_<pièce>, marqueur MatiereSol métal, MatieresSolBuilder.Regle les garde en métal) : le rayon des pas (PasMatiere, sol le
    /// plus proche) les touche avant la pierre, la marche est inchangée (dessus à 1 cm au plus du maillage). Zones relevées sur
    /// building_blacksmith (repère du modèle enfoncé, m) : enclume x -1,2 à 0,9, z 3,0 à 4,8, dessus au-dessus de 0,4 m ; lame
    /// x -3,1 à -1,6, z 2,0 à 2,9, au-dessus de 0,12 m (le dallage de pierre dessous reste pierre). Le plancher de la cabane reste
    /// pierre. Le seau et les outils du râtelier ne se marchent pas (verticaux ou hors d'atteinte). Idempotent.
    public static string V5MetalForgeKayKit(Transform mod)
    {
        Transform col = mod.Find("Collision");
        var mc = col != null ? col.GetComponent<MeshCollider>() : null;
        if (mc == null) return ", pas de collision : pas de pièces de fer";
        for (int k = mod.childCount - 1; k >= 0; k--) if (mod.GetChild(k).name.StartsWith("Metal_")) Object.DestroyImmediate(mod.GetChild(k).gameObject);
        Physics.SyncTransforms();
        // rayons lancés de yDepart (sous le linteau du four, qui surplombe la lame) ; dessus = 90e centile des hauteurs touchées
        var zones = new (string nom, float x0, float x1, float z0, float z1, float yMin, float yDepart)[] { ("Enclume", -1.2f, 0.9f, 3.0f, 4.8f, 0.4f, 1.2f), ("Lame", -3.1f, -1.6f, 2.0f, 2.9f, 0.12f, 0.6f) };
        var sb = new StringBuilder();
        const float pas = 0.05f, bande = 0.15f, ep = 0.06f;
        foreach (var z in zones)
        {
            // une boîte par bande de 15 cm en z (étendue en x et dessus de la bande) : la forme de la pièce est suivie, pas de rebord invisible
            GameObject go = null; int n = 0, boites = 0;
            for (float b0 = z.z0; b0 < z.z1; b0 += bande)
            {
                float x0 = 99f, x1 = -99f; var hauts = new List<float>();
                for (float x = z.x0; x <= z.x1; x += pas)
                    for (float zz = b0; zz < b0 + bande - 0.001f; zz += pas)
                    {
                        Vector3 w = mod.TransformPoint(new Vector3(x, z.yDepart, zz));
                        if (!mc.Raycast(new Ray(w, Vector3.down), out RaycastHit h, z.yDepart + 1f)) continue;
                        float y = mod.InverseTransformPoint(h.point).y;
                        if (y < z.yMin) continue;
                        x0 = Mathf.Min(x0, x); x1 = Mathf.Max(x1, x); hauts.Add(y); n++;
                    }
                if (hauts.Count < 3) continue;
                hauts.Sort(); float haut = hauts[Mathf.Min(hauts.Count - 1, (int)(hauts.Count * 0.9f))];
                if (go == null) { go = new GameObject("Metal_" + z.nom); go.transform.SetParent(mod, false); go.AddComponent<Deathless.Jeu.MatiereSol>().matiere = Deathless.Jeu.Matiere.Metal; }
                var bc = go.AddComponent<BoxCollider>();
                bc.size = new Vector3(x1 - x0 + pas, ep, bande);
                bc.center = new Vector3((x0 + x1) / 2f, haut + 0.005f - ep / 2f, b0 + bande / 2f);
                boites++;
            }
            sb.Append(", fer ").Append(z.nom).Append(go != null ? " (" + n + " points, " + boites + " boîtes)" : " introuvable");
        }
        return sb.ToString();
    }

    /// Pose (ou remet à jour) le composant EntreeBatiment sur un repère Entree : rôle du bâtiment et pied de la porte. Idempotent.
    static void V5ConfigurerEntree(GameObject entree, string role, V5KKPieceInfo info)
    {
        var eb = entree.GetComponent<EntreeBatiment>();
        if (eb == null) eb = entree.AddComponent<EntreeBatiment>();
        eb.role = role;
        eb.seuilLocal = new Vector3(0f, 0f, info.pied + V5KKSeuil - info.facadeZ);
        EditorUtility.SetDirty(eb);
    }

    /// Étape 1c : pose les composants EntreeBatiment sur les repères Entree des bâtiments KayKit déjà en place (sans refaire les bâtiments).
    public static string V5Entrees(Transform root)
    {
        Transform ms = root.Find("Maisons");
        if (ms == null) return "Entrées : Maisons absentes";
        var sb = new StringBuilder("Entrées : ");
        for (int i = 0; i < V5MaisonsRoles.Length; i++)
        {
            Transform bat = ms.Find("Batiment_" + V5MaisonsRoles[i]);
            string piece = V5MaisonsKayKitPiece[i];
            Transform ent = bat != null && piece != null ? bat.Find(V5NomModele(i) + "/Entree") : null;
            if (ent == null || !s_V5KKPieces.TryGetValue(piece, out V5KKPieceInfo info)) { sb.Append(V5MaisonsRoles[i] + " sans repère Entree ; "); continue; }
            V5ConfigurerEntree(ent.gameObject, V5MaisonsRoles[i], info);
            sb.Append(V5MaisonsRoles[i] + " (pied en " + ent.GetComponent<EntreeBatiment>().PointRetour.ToString("F2") + ") ; ");
        }
        return sb.ToString();
    }

    [MenuItem("Deathless/Village/v5/1c. Entrées des bâtiments (portes)")] public static string V5MenuEntrees() { return Etape(V5Entrees); }

    /// Lanterne de porte (lantern_standing de KayKit Halloween Bits, comme à l'origine : flamme et lumière au centre de la cage, vitres
    /// émissives, scintillement ; allumage par CycleJourNuit).
    static string V5LanterneKayKit(Transform root, Transform racine, V5KKPieceInfo info, string nom, CycleJourNuit cycle)
    {
        Transform lanternes = root.Find("Ambiance/Lanternes");
        if (lanternes == null) return ", pas de groupe Ambiance/Lanternes";
        Vector3 f = racine.forward; f.y = 0f; f.Normalize();
        Vector3 p = racine.TransformPoint(new Vector3(info.lanterne.x, 0f, info.lanterne.y)); p.y = GroundHeight(p.x, p.z);
        GameObject lan = Place(lanternes, HalloweenRoot + "lantern_standing", p, racine.eulerAngles.y, LanternScale);
        if (lan == null) return ", lanterne introuvable";
        lan.name = "Lanterne_" + nom;
        Material flameMat = FlatMaterial("Assets/Art/Materials/Lanterne_Flamme.mat", "Universal Render Pipeline/Unlit", new Color(1f, 0.62f, 0.28f) * 2.2f);
        Vector3 glass = p + Vector3.up * 0.62f * LanternScale;
        GameObject fl = GameObject.CreatePrimitive(PrimitiveType.Sphere); fl.name = "Flamme";
        Object.DestroyImmediate(fl.GetComponent<Collider>());
        fl.transform.SetParent(lan.transform, false); fl.transform.position = glass; fl.transform.localScale = Vector3.one * 0.14f / LanternScale;
        MeshRenderer fr = fl.GetComponent<MeshRenderer>(); fr.sharedMaterial = flameMat; fr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        fl.SetActive(false);
        GameObject lg = new GameObject("Lumiere"); lg.transform.SetParent(lan.transform, false); lg.transform.position = glass + f * 0.15f;
        Light l = lg.AddComponent<Light>(); l.type = LightType.Point; l.range = LanternRange; l.color = new Color(1f, 0.64f, 0.34f); l.intensity = 0f; l.shadows = LightShadows.None; l.enabled = false;
        LanterneLumiere.Configurer(lan, l, fl.transform, LanterneAssets.Materiau(), LanterneAssets.Reglages(), 2.2f, false, false);
        if (cycle != null)
        {
            var ls = new List<Light>(cycle.lanternes ?? new Light[0]); ls.RemoveAll(x => x == null); ls.Add(l); cycle.lanternes = ls.ToArray();
            var fs = new List<GameObject>(cycle.flammes ?? new GameObject[0]); fs.RemoveAll(x => x == null); fs.Add(fl); cycle.flammes = fs.ToArray();
            EditorUtility.SetDirty(cycle);
        }
        return ", lanterne en " + p.ToString("F2");
    }
}
