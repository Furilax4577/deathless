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
// tout le village), enfoncé de V5KKEnfoncement (3 cm) pour qu'aucun jour ne reste entre la pierre et l'herbe ; les marches de l'escalier
// partent du sol (quatre marches de 39 cm, 52 cm de giron) ; aucun vide, ni creux, ni plancher surélevé sous le bâtiment.
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
    /// Enfoncement du modèle (m) : pas de jour sous la pierre.
    public const float V5KKEnfoncement = 0.03f;

    /// Repère de chaque pièce (mètres à l'échelle 7,5, x vers la droite vue de la façade, z vers l'extérieur) : axe de la porte (centre de
    /// l'escalier, ou de la porte de la cabane), plan de la façade au droit de la porte, position de la lanterne de porte.
    sealed class V5KKPieceInfo { public float porteX, facadeZ; public Vector2 lanterne; public Vector2 enclume; }
    static readonly Dictionary<string, V5KKPieceInfo> s_V5KKPieces = new Dictionary<string, V5KKPieceInfo> {
        // escalier de x = -0,28 à 0 (unités du modèle), façade du rez-de-chaussée à z = 0,28 ; lanterne : 35 cm hors de l'escalier, côté gauche (comme avant : (-2,45 ; 4,55))
        { "home_B",     new V5KKPieceInfo { porteX = -1.05f, facadeZ = 2.10f, lanterne = new Vector2(-2.45f, 4.55f) } },
        // escalier de x = 0,01 à 0,28, façade à z = 0,35 ; lanterne du côté de l'auvent, à 35 cm de l'escalier
        { "tavern",     new V5KKPieceInfo { porteX = 1.09f, facadeZ = 2.62f, lanterne = new Vector2(-0.35f, 4.55f) } },
        // cabane de x = 0,15 à 0,58, porte au milieu, façade à z = 0,08 ; pas d'escalier : la porte est au sol ; enclume au milieu du devant du four
        { "blacksmith", new V5KKPieceInfo { porteX = 2.55f, facadeZ = 0.60f, lanterne = new Vector2(4.6f, 2.6f), enclume = new Vector2(0.15f, 6.9f) } },
    };

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
        r.transform.localPosition = new Vector3(0f, -V5KKEnfoncement, 0f); r.transform.localScale = Vector3.one * V5KKEchelle;
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
        return role + " (KayKit " + piece + " " + V5MaisonsKayKitCoul[i] + ", lacet " + racine.eulerAngles.y.ToString("F1") + ", porte à " + e.transform.position.ToString("F2") + suite + lanterneInfo + ") ; ";
    }

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
