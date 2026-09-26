using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.Rendering;

// Forgeron de la forge (Maison_4_B, Interieur_Forgeron) : modèle KayKit Barbarian sans le bonnet d'ours
// (Barbarian_BearHat masqué) ni l'écharpe (triangles retirés d'une copie du maillage du corps, voir RetirerEcharpe),
// texture alternative (il ne ressemble pas au Viking des joueurs), marteau RPG Tools dans handslot.r, debout à
// Ancre_Villageois_Forgeron, tourné comme elle, l'enclume devant sa main droite. Contrôleur Contact (pose posée : clip
// Melee_1H_Attack_Chop figé à l'instant du contact, vitesse 0, état par défaut) / Frappe (le clip) ; runtime : ForgeronForge.
// Relancé par InterieursBuilder.Construire ; menu seul : Deathless > Niveau > Forgeron (pose aussi le feu vivant).
//
// Contact sans pénétration (retour de Quentin, 26/09/2026 : « le marteau doit frapper l'enclume et pas rentrer dedans ») :
// - relief de l'enclume : carte des hauteurs de son maillage (rayons tirés d'en haut, pas de 4 mm, maximum des cellules
//   voisines : prudent), plus le billot (cylindre sous l'enclume) ; la table est la partie plate du dessus ;
// - instant du contact : premier instant, après le haut de l'élan, où le point le plus bas de la tête du marteau (moitié
//   haute du maillage) descend à la hauteur de la table, affiné par dichotomie (au dixième de milliseconde) du côté
//   au-dessus : la face repose sur la table, jeu de quelques dixièmes de millimètre ;
// - geste complet mesuré dans le repère du forgeron, comme le joue l'Animator : fondu de 0,3 s de la pose posée vers le
//   début du clip (graphe de lecture : deux fois le clip, mélangés), puis le clip jusqu'au contact, tous les 1/120 s ;
//   tous les sommets du marteau et un sommet sur trois du corps ;
// - placement : lacet (±20°) et point de chute sur la table (grille de 1,5 cm) ; la face de la tête doit reposer sur la
//   table (marge de 1,5 cm), aucun sommet du marteau ni du corps dans l'enclume ou le billot pendant tout le geste ni
//   au contact ; parmi les placements valides, le plus proche de l'ancre, du milieu de la table et du lacet de l'ancre.
// Mesure en jeu : DemarrerMesure / ArreterMesure (pénétration à chaque image rendue, pause à l'instant du contact) et
// CapturesContact (côté et 3/4, Assets/Screenshots/forge_*.png).
public static class ForgeronBuilder
{
    const string Modele = "Assets/Art/KayKit/KayKit_Adventurers_2.0_EXTRA/Characters/fbx/Barbarian.fbx";
    const string Texture = "Assets/Art/KayKit/KayKit_Adventurers_2.0_EXTRA/Textures/barbarian_texture_alt_B.png";
    const string Marteau = "Assets/Art/KayKit/KayKit_RPGToolsBits_1.0_FREE/Assets/fbx(unity)/hammer.fbx";
    const string Anims = "Assets/Art/KayKit/KayKit_Character_Animations_1.1/Animations/fbx/Rig_Medium/";
    const string Gemmes = "Assets/VFX/_RelicCommun/PortalVoxel.mat";
    const string Dossier = "Assets/Jeu/Forgeron";
    const float Echelle = 0.8f;   // comme les héros (Modele à 0,8)
    const float Fondu = 0.3f;     // ForgeronForge : fondu (temps fixe) de la pose posée vers le début du geste
    const float Pas = 1f / 120f;  // échantillonnage du geste (s)

    [MenuItem("Deathless/Niveau/Forgeron")]
    public static void Menu()
    {
        Debug.Log(Poser());
        var v = GameObject.Find("VillageBlockout");
        if (v != null) { EditorSceneManager.MarkSceneDirty(v.scene); EditorSceneManager.SaveScene(v.scene); }
    }

    public static string Poser()
    {
        var it = GameObject.Find("VillageBlockout/Interieurs/Interieur_Forgeron");
        if (it == null) return "Forgeron : Interieur_Forgeron introuvable (lancer Deathless > Niveau > Intérieurs)";
        Transform ancre = null, enclume = null;
        foreach (var t in it.GetComponentsInChildren<Transform>(true))
        {
            if (t.name == "Ancre_Villageois_Forgeron") ancre = t;
            else if (enclume == null && t.name.ToLower().StartsWith("anvil")) enclume = t;
        }
        if (ancre == null || enclume == null) return "Forgeron : ancre ou enclume introuvable";
        var vieux = it.transform.Find("Forgeron");
        if (vieux != null) Object.DestroyImmediate(vieux.gameObject);
        System.IO.Directory.CreateDirectory(Dossier);

        // Enclume : bornes, relief, table.
        Bounds be = new Bounds(enclume.position, Vector3.zero); bool premier = true;
        foreach (var r in enclume.GetComponentsInChildren<Renderer>()) { if (premier) { be = r.bounds; premier = false; } else be.Encapsulate(r.bounds); }
        var relief = new Relief(enclume, be, InterieursBuilder.BillotRayon * 1.1f + 0.01f, ancre.position.y);
        Vector3 dessus = relief.CentreTable();
        // Le marteau posé sur l'enclume par InterieursBuilder : c'est maintenant celui du forgeron, dans sa main.
        var large = be; large.Expand(0.3f);
        var poses = new List<GameObject>();
        foreach (var t in it.GetComponentsInChildren<Transform>(true)) if (t.name == "hammer" && large.Contains(t.position)) poses.Add(t.gameObject);
        foreach (var g in poses) Object.DestroyImmediate(g);
        Vector3 face = ancre.forward; face.y = 0f;
        if (face.sqrMagnitude < 1e-4f) face = Vector3.forward;
        face.Normalize();

        // Racine (collider, script) et modèle.
        var racine = new GameObject("Forgeron");
        racine.transform.SetParent(it.transform, false);
        racine.transform.SetPositionAndRotation(ancre.position, Quaternion.LookRotation(face));
        var col = racine.AddComponent<CapsuleCollider>(); col.center = new Vector3(0f, 0.9f, 0f); col.height = 1.8f; col.radius = 0.32f;
        var modele = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Modele), racine.transform);
        modele.name = "Modele";
        modele.transform.localPosition = Vector3.zero; modele.transform.localRotation = Quaternion.identity; modele.transform.localScale = Vector3.one * Echelle;

        // Sans bonnet d'ours ; sans écharpe ; texture alternative.
        var mat = Materiau();
        string echarpe = "";
        foreach (var smr in modele.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            if (smr.name == "Barbarian_BearHat") { smr.gameObject.SetActive(false); continue; }
            if (smr.name == "Barbarian_Body") echarpe = RetirerEcharpe(smr);
            smr.sharedMaterial = mat;
        }

        // Marteau dans la main droite ; point de la tête.
        Transform main = null;
        foreach (var t in modele.GetComponentsInChildren<Transform>(true)) if (t.name == "handslot.r") main = t;
        var marteau = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Marteau), main);
        marteau.name = "Marteau";
        marteau.transform.localPosition = Vector3.zero; marteau.transform.localRotation = Quaternion.identity; marteau.transform.localScale = Vector3.one * 0.8f;
        foreach (var c in marteau.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);
        var tete = new GameObject("Tete").transform; tete.SetParent(marteau.transform, false);
        var mfm = marteau.GetComponentInChildren<MeshFilter>();
        tete.position = mfm.transform.TransformPoint(TeteLocale(mfm.sharedMesh));
        var vm = mfm.sharedMesh.vertices;
        var iTete = IndicesTete(mfm.sharedMesh);

        var frappe = Clip(Anims + "Rig_Medium_CombatMelee.fbx", "Melee_1H_Attack_Chop");
        if (frappe == null) return "Forgeron : clip de frappe introuvable";
        var anim = modele.GetComponent<Animator>(); if (anim == null) anim = modele.AddComponent<Animator>();
        anim.runtimeAnimatorController = null; anim.applyRootMotion = false; anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        var corps = new List<SkinnedMeshRenderer>();
        foreach (var smr in modele.GetComponentsInChildren<SkinnedMeshRenderer>()) corps.Add(smr);

        float duree = frappe.length, tImpact, jeu, controle, depart = 0f;
        Vector3 hLocal;
        Quaternion meilleurRot; Vector3 meilleurPos;
        string essais;
        var mesure = new Echantillonneur(anim, frappe);
        try
        {
            float BasTete()
            {
                float m = float.MaxValue; var M = mfm.transform.localToWorldMatrix;
                foreach (int i in iTete) m = Mathf.Min(m, M.MultiplyPoint3x4(vm[i]).y);
                return m;
            }
            // Contrôle : le graphe de lecture donne la même pose que SampleAnimation.
            frappe.SampleAnimation(modele, 0.5f); Vector3 ref05 = tete.position;
            mesure.Pose(0.5f, 0f, 0f); controle = Vector3.Distance(ref05, tete.position);

            // Haut de l'élan, puis contact : la tête descend à la hauteur de la table (dichotomie, côté au-dessus).
            float tHaut = 0f, yHaut = float.MinValue;
            for (float t = 0f; t <= duree; t += Pas) { mesure.Pose(t, 0f, 0f); if (tete.position.y > yHaut) { yHaut = tete.position.y; tHaut = t; } }
            float t1 = -1f;
            for (float t = tHaut; t <= duree; t += Pas) { mesure.Pose(t, 0f, 0f); if (BasTete() <= relief.haut) { t1 = t; break; } }
            if (t1 < 0f) return "Forgeron : le geste n'atteint pas la table de l'enclume (dessus à " + (relief.haut - ancre.position.y).ToString("0.00") + " m du plancher)";
            float a = Mathf.Max(tHaut, t1 - Pas), b = t1;
            for (int k = 0; k < 30; k++) { float m = (a + b) * 0.5f; mesure.Pose(m, 0f, 0f); if (BasTete() > relief.haut) a = m; else b = m; }
            tImpact = a;
            mesure.Pose(tImpact, 0f, 0f); jeu = BasTete() - relief.haut;

            // Geste complet, dans le repère du forgeron (racine à l'ancre) : fondu de la pose posée vers l'instant `depart`
            // du clip, puis le clip jusqu'au contact. Le départ le plus tôt qui permet un placement sans pénétration
            // l'emporte : partir du tout début du clip (marteau bas, près du corps) tirerait la tête à travers la table.
            Matrix4x4 versRacine = racine.transform.worldToLocalMatrix;
            var cuit = new Mesh();
            void Capturer(List<Vector3[]> lm, List<Vector3[]> lcorps)
            {
                var M = versRacine * mfm.transform.localToWorldMatrix;
                var pm = new Vector3[vm.Length]; for (int i = 0; i < vm.Length; i++) pm[i] = M.MultiplyPoint3x4(vm[i]);
                lm.Add(pm);
                var lc = new List<Vector3>();
                foreach (var smr in corps)
                {
                    smr.BakeMesh(cuit, true);
                    var Ms = versRacine * smr.transform.localToWorldMatrix; var vs = cuit.vertices;
                    for (int i = 0; i < vs.Length; i += 3) lc.Add(Ms.MultiplyPoint3x4(vs[i]));
                }
                lcorps.Add(lc.ToArray());
            }
            var cm = new List<Vector3[]>(); var cc = new List<Vector3[]>();
            mesure.Pose(tImpact, 0f, 0f); Capturer(cm, cc);
            var contact = cm[0];
            // Face de la tête qui repose sur la table : sommets de la tête à moins de 5 mm du plus bas.
            float yBas = float.MaxValue; foreach (int i in iTete) yBas = Mathf.Min(yBas, contact[i].y);
            var faceTete = new List<Vector3>(); foreach (int i in iTete) if (contact[i].y <= yBas + 0.005f) faceTete.Add(contact[i]);
            hLocal = Vector3.zero; foreach (var p in faceTete) hLocal += p; hLocal /= faceTete.Count; hLocal.y = 0f;

            // Placements candidats, du meilleur au moins bon.
            float lacet0 = Quaternion.LookRotation(face).eulerAngles.y;
            var cands = new List<(float note, float d, Vector3 pos, Vector3 chute)>();
            foreach (var chute in relief.PointsTable(0.015f))
                for (float d = -20f; d <= 20.01f; d += 5f)
                {
                    Quaternion q = Quaternion.Euler(0f, lacet0 + d, 0f);
                    Vector3 pos = new Vector3(chute.x, ancre.position.y, chute.z) - q * hLocal;
                    float recul = Vector2.Distance(new Vector2(chute.x, chute.z), new Vector2(dessus.x, dessus.z));
                    cands.Add((Mathf.Abs(d) * 1f + Vector3.Distance(pos, ancre.position) * 30f + recul * 30f, d, pos, chute));
                }
            cands.Sort((x, y) => x.note.CompareTo(y.note));
            // Candidats dont la face repose sur la table (marge de 1,5 cm).
            var surTable = new List<int>();
            for (int c = 0; c < cands.Count; c++)
            {
                var M = Matrix4x4.TRS(cands[c].pos, Quaternion.Euler(0f, lacet0 + cands[c].d, 0f), Vector3.one);
                bool ok = true;
                foreach (var p in faceTete)
                {
                    Vector3 w = M.MultiplyPoint3x4(p);
                    for (int k = 0; k < 5 && ok; k++)
                    {
                        float dx = k == 1 ? 0.015f : k == 2 ? -0.015f : 0f, dz = k == 3 ? 0.015f : k == 4 ? -0.015f : 0f;
                        float h = relief.Hauteur(w.x + dx, w.z + dz);
                        if (float.IsNaN(h) || h < relief.haut - 0.002f) ok = false;
                    }
                    if (!ok) break;
                }
                if (ok) surTable.Add(c);
            }
            if (surTable.Count == 0) return "Forgeron : aucun placement ne pose la face du marteau sur la table de l'enclume";

            float[] departs = { 0f, 0.05f, 0.1f, 0.15f, 0.2f, 0.25f, 0.3f };
            var posesM = new List<Vector3[]>(); var posesC = new List<Vector3[]>();
            int choisi = -1, testes = 0, nFondu = 0; float meilleurPen = float.MaxValue;
            (float note, float d, Vector3 pos, Vector3 chute) retenu = cands[surTable[0]];
            var parDepart = new System.Text.StringBuilder();
            foreach (float dep in departs)
            {
                var lm = new List<Vector3[]>(); var lc = new List<Vector3[]>();
                int nf = 0;
                for (float u = Pas; u < Fondu; u += Pas) { mesure.Pose(tImpact, dep + u, u / Fondu); Capturer(lm, lc); nf++; }
                for (float t = dep + Fondu; t < tImpact; t += Pas) { mesure.Pose(0f, t, 1f); Capturer(lm, lc); }
                lm.Add(cm[0]); lc.Add(cc[0]);
                float penDep = float.MaxValue; int trouve = -1; (float note, float d, Vector3 pos, Vector3 chute) mieux = cands[surTable[0]];
                foreach (int c in surTable)
                {
                    var M = Matrix4x4.TRS(cands[c].pos, Quaternion.Euler(0f, lacet0 + cands[c].d, 0f), Vector3.one);
                    testes++;
                    float pen = 0f;
                    for (int k = 0; k < lm.Count && pen <= 0.0005f; k++)
                    {
                        foreach (var p in lm[k]) { pen = Mathf.Max(pen, relief.Profondeur(M.MultiplyPoint3x4(p))); if (pen > 0.0005f) break; }
                        if (pen > 0.0005f) break;
                        foreach (var p in lc[k]) { pen = Mathf.Max(pen, relief.Profondeur(M.MultiplyPoint3x4(p))); if (pen > 0.0005f) break; }
                    }
                    if (pen < penDep) { penDep = pen; mieux = cands[c]; }
                    if (pen <= 0.0005f) { trouve = c; break; }
                }
                parDepart.Append("départ " + dep.ToString("0.00") + " s : " + (trouve >= 0 ? "valide" : "pénétration " + (penDep * 1000f).ToString("0.0") + " mm") + " ; ");
                if (trouve >= 0 || penDep < meilleurPen)
                {
                    meilleurPen = penDep; retenu = trouve >= 0 ? cands[trouve] : mieux; depart = dep; posesM = lm; posesC = lc; nFondu = nf;
                }
                if (trouve >= 0) { choisi = trouve; break; }
            }
            Object.DestroyImmediate(cuit);
            meilleurPos = retenu.pos; meilleurRot = Quaternion.Euler(0f, lacet0 + retenu.d, 0f);

            // Mesure finale sur le placement retenu : pénétration maximale (marteau, corps) sur tout le geste.
            var Mf = Matrix4x4.TRS(meilleurPos, meilleurRot, Vector3.one);
            float penM = 0f, penC = 0f, garde = float.MaxValue; int kPire = -1;
            for (int k = 0; k < posesM.Count; k++)
            {
                foreach (var p in posesM[k])
                {
                    Vector3 w = Mf.MultiplyPoint3x4(p);
                    float pr = relief.Profondeur(w); if (pr > penM) { penM = pr; kPire = k; }
                    if (k < posesM.Count - 1) { float h = relief.Hauteur(w.x, w.z); if (!float.IsNaN(h)) garde = Mathf.Min(garde, w.y - h); }
                }
                foreach (var p in posesC[k]) penC = Mathf.Max(penC, relief.Profondeur(Mf.MultiplyPoint3x4(p)));
            }
            string pire = kPire < 0 ? "" : " (pire pose : " + (kPire < nFondu ? "fondu, " + ((kPire + 1) * Pas).ToString("0.000") + " s" : kPire == posesM.Count - 1 ? "contact" : "clip à " + (depart + Fondu + (kPire - nFondu) * Pas).ToString("0.000") + " s") + ")";
            essais = (choisi >= 0 ? "placement sans pénétration trouvé" : "AUCUN placement sans pénétration, repli sur le moins mauvais")
                + " (" + testes + " placements mesurés sur " + surTable.Count + " où la face repose sur la table ; " + parDepart + ") : départ du geste à "
                + depart.ToString("0.00") + " s du clip, lacet " + retenu.d.ToString("+0;-0;0") + "°, chute à "
                + Vector2.Distance(new Vector2(retenu.chute.x, retenu.chute.z), new Vector2(dessus.x, dessus.z)).ToString("0.00")
                + " m du milieu de la table ; geste (" + posesM.Count + " poses : fondu " + Fondu + " s, clip, contact) : pénétration maximale du marteau "
                + (penM * 1000f).ToString("0.0") + " mm" + pire + ", du corps " + (penC * 1000f).ToString("0.0") + " mm ; plus petit écart marteau-enclume avant le contact "
                + (garde == float.MaxValue ? "(jamais au-dessus)" : (garde * 1000f).ToString("0.0") + " mm");
        }
        finally { mesure.Detruire(); }

        racine.transform.SetPositionAndRotation(meilleurPos, meilleurRot);
        float avance = Vector3.Distance(racine.transform.position, ancre.position);
        Vector3 chuteMonde = racine.transform.TransformPoint(hLocal);

        var ctrl = Controleur(frappe, tImpact / duree);
        anim.runtimeAnimatorController = ctrl; anim.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
        // Pose posée, visible dans l'éditeur. L'Animator remet la pose d'avant le graphe de mesure à l'image suivante de
        // l'éditeur : on échantillonne donc aussi juste après.
        frappe.SampleAnimation(modele, tImpact);
        EditorApplication.delayCall += () => { if (modele != null) { frappe.SampleAnimation(modele, tImpact); EditorSceneManager.MarkSceneDirty(modele.scene); } };
        var forge = racine.AddComponent<ForgeronForge>();
        forge.animator = anim; forge.teteMarteau = tete; forge.pointEnclume = new Vector3(chuteMonde.x, relief.haut, chuteMonde.z); forge.impact = tImpact / duree; forge.depart = depart;
        EditorUtility.SetDirty(forge);
        return "Forgeron posé : enclume à l'échelle " + enclume.localScale.x.ToString("0.00") + ", table à " + (relief.haut - ancre.position.y).ToString("0.000")
            + " m du plancher ; contact à " + tImpact.ToString("0.0000") + " s / " + duree.ToString("0.000") + " s (" + (tImpact / duree * 100f).ToString("0.0") + " %"
            + "), jeu tête-table au contact " + (jeu * 1000f).ToString("0.00") + " mm ; recalé de " + avance.ToString("0.00") + " m de l'ancre ; " + essais
            + " ; contrôle graphe/SampleAnimation " + (controle * 1000f).ToString("0.0") + " mm ; " + echarpe + "\n" + PoserFeu(it);
    }

    // ---------------- Relief de l'enclume ----------------
    // Carte des hauteurs du dessus de l'enclume (rayons tirés d'en haut sur son maillage) et billot en cylindre dessous.
    // Un point est dans l'enclume s'il est sous la surface du dessus (au-dessus du bas de l'enclume) : les surplombs
    // (bigorne, taille) comptent comme pleins, la mesure est prudente.
    public class Relief
    {
        public float haut, bas;
        readonly float x0, z0, pas = 0.004f, sol, rayon;
        readonly int nx, nz;
        readonly float[] h;
        readonly Vector2 billot;

        public Relief(Transform enclume, Bounds be, float rayonBillot, float plancher)
        {
            haut = be.max.y; bas = be.min.y; sol = plancher; rayon = rayonBillot;
            billot = new Vector2(enclume.position.x, enclume.position.z);
            x0 = be.min.x - 0.02f; z0 = be.min.z - 0.02f;
            nx = Mathf.CeilToInt((be.size.x + 0.04f) / pas) + 1; nz = Mathf.CeilToInt((be.size.z + 0.04f) / pas) + 1;
            h = new float[nx * nz];
            var mcs = new List<MeshCollider>();
            foreach (var mf in enclume.GetComponentsInChildren<MeshFilter>()) { var mc = mf.gameObject.AddComponent<MeshCollider>(); mc.sharedMesh = mf.sharedMesh; mcs.Add(mc); }
            Physics.SyncTransforms();
            for (int ix = 0; ix < nx; ix++)
                for (int iz = 0; iz < nz; iz++)
                {
                    float v = float.NaN;
                    var ray = new Ray(new Vector3(x0 + ix * pas, haut + 1f, z0 + iz * pas), Vector3.down);
                    foreach (var mc in mcs) if (mc.Raycast(ray, out var hit, 3f) && (float.IsNaN(v) || hit.point.y > v)) v = hit.point.y;
                    h[ix * nz + iz] = v;
                }
            foreach (var mc in mcs) Object.DestroyImmediate(mc);
        }

        // Hauteur du dessus en (x, z), maximum des quatre cellules voisines ; NaN hors de l'enclume.
        public float Hauteur(float x, float z)
        {
            int ix = Mathf.FloorToInt((x - x0) / pas), iz = Mathf.FloorToInt((z - z0) / pas);
            float m = float.NaN;
            for (int a = 0; a < 2; a++)
                for (int b = 0; b < 2; b++)
                {
                    int i = ix + a, k = iz + b;
                    if (i < 0 || k < 0 || i >= nx || k >= nz) continue;
                    float v = h[i * nz + k];
                    if (!float.IsNaN(v) && (float.IsNaN(m) || v > m)) m = v;
                }
            return m;
        }

        // Profondeur (m) d'un point dans l'enclume ou le billot ; 0 dehors.
        public float Profondeur(Vector3 w)
        {
            if (w.y >= haut) return 0f;
            float p = 0f;
            if (w.y < bas && w.y > sol)
            {
                float r = Vector2.Distance(new Vector2(w.x, w.z), billot);
                if (r < rayon) p = Mathf.Min(bas - w.y, rayon - r);
            }
            float hh = Hauteur(w.x, w.z);
            if (!float.IsNaN(hh) && w.y < hh && w.y > bas - 0.001f) p = Mathf.Max(p, hh - w.y);
            return p;
        }

        bool Table(int i) { return !float.IsNaN(h[i]) && h[i] >= haut - 0.002f; }

        // Centre de la table plate (partie du dessus à moins de 2 mm du haut), à sa hauteur.
        public Vector3 CentreTable()
        {
            Vector3 s = Vector3.zero; int n = 0;
            for (int ix = 0; ix < nx; ix++) for (int iz = 0; iz < nz; iz++) if (Table(ix * nz + iz)) { s += new Vector3(x0 + ix * pas, 0f, z0 + iz * pas); n++; }
            s = n > 0 ? s / n : new Vector3(x0 + nx * pas * 0.5f, 0f, z0 + nz * pas * 0.5f);
            s.y = haut;
            return s;
        }

        // Points de la table, grille de `ecart` m.
        public List<Vector3> PointsTable(float ecart)
        {
            var l = new List<Vector3>(); int k = Mathf.Max(1, Mathf.RoundToInt(ecart / pas));
            for (int ix = 0; ix < nx; ix += k) for (int iz = 0; iz < nz; iz += k) if (Table(ix * nz + iz)) l.Add(new Vector3(x0 + ix * pas, haut, z0 + iz * pas));
            return l;
        }
    }

    // Graphe de lecture : deux fois le clip de frappe, mélangés comme le fondu de l'Animator (a : pose de départ, poids
    // 1 - w ; b : pose d'arrivée, poids w).
    sealed class Echantillonneur
    {
        PlayableGraph g; AnimationClipPlayable a, b; AnimationMixerPlayable mix;
        public Echantillonneur(Animator anim, AnimationClip clip)
        {
            g = PlayableGraph.Create("Forgeron_Mesure");
            g.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            var sortie = AnimationPlayableOutput.Create(g, "Sortie", anim);
            mix = AnimationMixerPlayable.Create(g, 2);
            a = AnimationClipPlayable.Create(g, clip); b = AnimationClipPlayable.Create(g, clip);
            g.Connect(a, 0, mix, 0); g.Connect(b, 0, mix, 1);
            sortie.SetSourcePlayable(mix);
        }
        public void Pose(float ta, float tb, float w)
        {
            a.SetTime(ta); b.SetTime(tb);
            mix.SetInputWeight(0, 1f - w); mix.SetInputWeight(1, w);
            g.Evaluate();
        }
        public void Detruire() { if (g.IsValid()) g.Destroy(); }
    }

    // ---------------- Feu vivant de la forge ----------------
    // ForgeFeu (Forge_FeuVivant) sur le lit de braises : flammes et étincelles en gemmes, braises vives qui palpitent,
    // respiration de l'émission des braises, vacillement de ±10 % de la lumière Feu_Forge. InterieursAmbiance ne pilote
    // plus cette lumière ni ces braises. Les anciennes flammes fixes au-dessus du lit sont retirées du maillage Flammes
    // de l'intérieur (idempotent : rien à retirer après une reconstruction, InterieursBuilder n'en pose plus).
    static string PoserFeu(GameObject it)
    {
        var t = it.transform;
        var vieux = t.Find("Forge_FeuVivant");
        if (vieux != null) Object.DestroyImmediate(vieux.gameObject);
        Light lum = null;
        foreach (var l in it.GetComponentsInChildren<Light>(true)) if (l.name == "Feu_Forge") lum = l;
        Transform mob = t.Find("Mobilier"), tb = mob != null ? mob.Find("Braises") : null;
        var lit = tb != null ? tb.GetComponent<MeshRenderer>() : null;
        if (lum == null || lit == null) return "Feu de la forge : lumière Feu_Forge ou braises introuvables";
        // Lit de braises : rectangle exact dans le repère de l'intérieur.
        var mfl = lit.GetComponent<MeshFilter>(); Bounds bl = new Bounds(); bool premier = true;
        foreach (var v in mfl.sharedMesh.vertices)
        {
            Vector3 l = t.InverseTransformPoint(mfl.transform.TransformPoint(v));
            if (premier) { bl = new Bounds(l, Vector3.zero); premier = false; } else bl.Encapsulate(l);
        }
        int retires = RetirerFlammesFixes(t, bl);
        // Ce feu pilote lui-même sa lumière et ses braises.
        var amb = it.GetComponentInParent<InterieursAmbiance>();
        float intensite = lum.intensity, part = 0.75f;
        if (amb != null)
        {
            part = amb.partJour;
            if (amb.feux != null)
            {
                var feux = new List<Light>(); var fi = new List<float>();
                for (int i = 0; i < amb.feux.Length; i++)
                {
                    if (amb.feux[i] == lum) { if (amb.feuxIntensite != null && i < amb.feuxIntensite.Length) intensite = amb.feuxIntensite[i]; continue; }
                    feux.Add(amb.feux[i]); fi.Add(amb.feuxIntensite != null && i < amb.feuxIntensite.Length ? amb.feuxIntensite[i] : 1f);
                }
                amb.feux = feux.ToArray(); amb.feuxIntensite = fi.ToArray();
            }
            if (amb.braises != null) amb.braises = System.Array.FindAll(amb.braises, r => r != lit);
            EditorUtility.SetDirty(amb);
        }
        var go = new GameObject("Forge_FeuVivant");
        go.transform.SetParent(t, false);
        go.transform.localPosition = new Vector3(bl.center.x, bl.max.y - 0.03f, bl.center.z);
        var feu = go.AddComponent<ForgeFeu>();
        feu.materiau = AssetDatabase.LoadAssetAtPath<Material>(Gemmes);
        feu.demiLit = new Vector2(bl.extents.x - 0.04f, bl.extents.z - 0.04f);
        feu.lumiere = lum; feu.intensite = intensite; feu.partJour = part; feu.braises = lit;
        lum.intensity = intensite;
        lum.color = Color.Lerp(VfxPalette.Couleur(VfxTheme.Feu, VfxRole.Vif, lum.color), VfxPalette.Couleur(VfxTheme.Feu, VfxRole.Coeur, lum.color), 0.3f);
        EditorUtility.SetDirty(feu); EditorUtility.SetDirty(lum);
        return "Feu de la forge : ForgeFeu sur le lit de braises (" + (bl.size.x).ToString("0.00") + " × " + (bl.size.z).ToString("0.00") + " m), "
            + (feu.flammes + feu.etincelles + feu.braisesVives) + " gemmes au plus, lumière Feu_Forge " + intensite.ToString("0.0") + " ± " + (feu.amplitude * 100f).ToString("0") + " %"
            + (retires > 0 ? ", " + retires + " triangles de flammes fixes retirés du maillage Flammes" : "");
    }

    // Retire du maillage Flammes de l'intérieur les triangles posés au-dessus du lit de braises (anciennes flammes fixes).
    static int RetirerFlammesFixes(Transform it, Bounds lit)
    {
        var tf = it.Find("Mobilier/Flammes"); if (tf == null) return 0;
        var mf = tf.GetComponent<MeshFilter>(); if (mf == null || mf.sharedMesh == null) return 0;
        Mesh m = mf.sharedMesh;
        var zone = new Bounds(new Vector3(lit.center.x, lit.max.y + 0.3f, lit.center.z), new Vector3(lit.size.x + 0.2f, 0.9f, lit.size.z + 0.2f));
        Vector3[] v = m.vertices, n = m.normals; Vector2[] uv = m.uv; Color[] co = m.colors; int[] tr = m.triangles;
        var garde = new List<int>(); int retires = 0;
        for (int i = 0; i < tr.Length; i += 3)
        {
            Vector3 c = it.InverseTransformPoint(mf.transform.TransformPoint((v[tr[i]] + v[tr[i + 1]] + v[tr[i + 2]]) / 3f));
            if (zone.Contains(c)) { retires++; continue; }
            garde.Add(tr[i]); garde.Add(tr[i + 1]); garde.Add(tr[i + 2]);
        }
        if (retires == 0) return 0;
        // Maillage compacté (sommets encore utilisés seulement).
        var neuf = new Dictionary<int, int>(); var nv = new List<Vector3>(); var nn = new List<Vector3>(); var nuv = new List<Vector2>(); var nc = new List<Color>();
        var ntr = new int[garde.Count];
        for (int i = 0; i < garde.Count; i++)
        {
            int o = garde[i];
            if (!neuf.TryGetValue(o, out int k))
            {
                k = nv.Count; neuf[o] = k; nv.Add(v[o]);
                if (n.Length == v.Length) nn.Add(n[o]);
                if (uv.Length == v.Length) nuv.Add(uv[o]);
                if (co.Length == v.Length) nc.Add(co[o]);
            }
            ntr[i] = k;
        }
        m.Clear();
        m.SetVertices(nv); if (nn.Count == nv.Count) m.SetNormals(nn); if (nuv.Count == nv.Count) m.SetUVs(0, nuv); if (nc.Count == nv.Count) m.SetColors(nc);
        m.SetTriangles(ntr, 0);
        m.RecalculateBounds();
        EditorUtility.SetDirty(m);
        return retires;
    }

    // ---------------- Mesure et captures en jeu ----------------
    // DemarrerMesure : à chaque image rendue, profondeur maximale des sommets du marteau dans l'enclume (même relief
    // qu'à la pose) ; à chaque contact (ForgeronForge.DernierImpact == Time.time), jeu tête-table ; pause de l'éditeur
    // au contact si demandé (CapturesContact prend alors l'image exacte du contact). ArreterMesure : rapport.
    static System.Action<ScriptableRenderContext, List<Camera>> s_Crochet;
    static float s_Pen, s_PenContact, s_JeuMin, s_JeuMax; static int s_Images, s_Impacts, s_Derniere, s_Pauses;
    static bool s_Pause;

    public static string DemarrerMesure(bool pauseAuContact)
    {
        if (!Application.isPlaying) return "Mesure : lancer le mode Play d'abord.";
        ArreterMesure();
        var forge = Object.FindAnyObjectByType<ForgeronForge>();
        var it = GameObject.Find("VillageBlockout/Interieurs/Interieur_Forgeron");
        if (forge == null || it == null) return "Mesure : forgeron introuvable";
        Transform enclume = null, ancre = null;
        foreach (var t in it.GetComponentsInChildren<Transform>(true)) { if (enclume == null && t.name.ToLower().StartsWith("anvil")) enclume = t; if (t.name == "Ancre_Villageois_Forgeron") ancre = t; }
        Bounds be = new Bounds(enclume.position, Vector3.zero); bool premier = true;
        foreach (var r in enclume.GetComponentsInChildren<Renderer>()) { if (premier) { be = r.bounds; premier = false; } else be.Encapsulate(r.bounds); }
        var relief = new Relief(enclume, be, InterieursBuilder.BillotRayon * 1.1f + 0.01f, ancre.position.y);
        var mf = forge.teteMarteau.parent.GetComponentInChildren<MeshFilter>();
        var vm = mf.sharedMesh.vertices;
        var iTete = IndicesTete(mf.sharedMesh);
        s_Pen = 0f; s_PenContact = 0f; s_JeuMin = float.MaxValue; s_JeuMax = float.MinValue; s_Images = 0; s_Impacts = 0; s_Derniere = -1; s_Pauses = 0;
        s_Pause = pauseAuContact;
        s_Crochet = (ctx, cams) =>
        {
            if (forge == null || Time.frameCount == s_Derniere) return;
            s_Derniere = Time.frameCount; s_Images++;
            var M = mf.transform.localToWorldMatrix; float pen = 0f;
            foreach (var v in vm) pen = Mathf.Max(pen, relief.Profondeur(M.MultiplyPoint3x4(v)));
            s_Pen = Mathf.Max(s_Pen, pen);
            if (forge.DernierImpact == Time.time)
            {
                s_Impacts++; s_PenContact = Mathf.Max(s_PenContact, pen);
                float bas = float.MaxValue; foreach (int i in iTete) bas = Mathf.Min(bas, M.MultiplyPoint3x4(vm[i]).y);
                s_JeuMin = Mathf.Min(s_JeuMin, bas - relief.haut); s_JeuMax = Mathf.Max(s_JeuMax, bas - relief.haut);
                if (s_Pause) { EditorApplication.isPaused = true; s_Pauses++; }
            }
        };
        RenderPipelineManager.beginContextRendering += s_Crochet;
        return "Mesure démarrée (" + vm.Length + " sommets du marteau, pause au contact : " + pauseAuContact + ")";
    }

    public static void PauseAuContact(bool oui) { s_Pause = oui; }

    public static string ArreterMesure()
    {
        if (s_Crochet != null) RenderPipelineManager.beginContextRendering -= s_Crochet;
        s_Crochet = null;
        return RapportMesure();
    }

    public static string RapportMesure()
    {
        return "Mesure en jeu : " + s_Images + " images rendues, " + s_Impacts + " contacts ; pénétration maximale du marteau dans l'enclume "
            + (s_Pen * 1000f).ToString("0.0") + " mm (au contact " + (s_PenContact * 1000f).ToString("0.0") + " mm) ; jeu tête-table au contact "
            + (s_Impacts > 0 ? (s_JeuMin * 1000f).ToString("0.00") + " à " + (s_JeuMax * 1000f).ToString("0.00") + " mm" : "-");
    }

    // Captures de l'instant du contact (à appeler en pause au contact) : de côté (à droite du forgeron, à hauteur de la
    // table) et de 3/4 (devant à droite, en plongée), plus une vue du feu. Chemins : Assets/Screenshots/forge_*.png.
    public static string CapturesContact(string suffixe)
    {
        var forge = Object.FindAnyObjectByType<ForgeronForge>();
        if (forge == null) return "Captures : forgeron introuvable";
        Transform f = forge.transform; Vector3 c = forge.pointEnclume;
        var sb = new System.Text.StringBuilder();
        sb.Append(InterieursVerif.CaptureLibre(c + f.right * 1.25f + Vector3.up * 0.05f + f.forward * 0.02f, c + Vector3.up * 0.25f - f.right * 0.1f, 50f, "Assets/Screenshots/forge_contact_cote" + suffixe + ".png") + "\n");
        sb.Append(InterieursVerif.CaptureLibre(c + f.right * 0.9f + f.forward * 1.3f + Vector3.up * 0.75f, c + Vector3.up * 0.3f - f.right * 0.15f, 50f, "Assets/Screenshots/forge_contact_3q" + suffixe + ".png") + "\n");
        sb.Append(InterieursVerif.CaptureLibre(c + f.right * 0.45f + Vector3.up * 0.012f + f.forward * -0.02f, c + Vector3.up * 0.012f - f.right * 0.5f, 40f, "Assets/Screenshots/forge_contact_ras" + suffixe + ".png") + "\n");
        return sb.ToString();
    }

    // Vue du feu de la forge, de côté (par-dessus le seau, derrière le forgeron).
    public static string CaptureFeu(string suffixe)
    {
        var feu = Object.FindAnyObjectByType<ForgeFeu>();
        if (feu == null) return "Captures : feu introuvable";
        Transform t = feu.transform, it = t.parent;
        Vector3 p = t.position;
        return InterieursVerif.CaptureLibre(p + it.right * 1.7f + it.forward * 0.3f + Vector3.up * 0.6f, p + Vector3.up * 0.2f, 55f, "Assets/Screenshots/forge_feu" + suffixe + ".png");
    }

    static Material Materiau()
    {
        string chemin = Dossier + "/Forgeron_Barbarian.mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(chemin);
        if (m == null)
        {
            Material src = null;
            foreach (var r in AssetDatabase.LoadAssetAtPath<GameObject>(Modele).GetComponentsInChildren<Renderer>(true)) { src = r.sharedMaterial; break; }
            m = new Material(src);
            AssetDatabase.CreateAsset(m, chemin);
        }
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(Texture);
        m.mainTexture = tex;
        if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", tex);
        EditorUtility.SetDirty(m);
        return m;
    }

    // Écharpe du Barbarian : dans Barbarian_Body, c'est l'îlot de triangles (composante connexe) qui entoure le cou,
    // au-dessus des épaules. On la retire d'une copie du maillage (Assets/Jeu/Forgeron/Forgeron_Corps.asset) : le
    // maillage KayKit d'origine n'est pas touché. Critère : composante dont le centre est dans la bande du cou
    // (entre 78 % et 92 % de la hauteur du corps, dans l'espace du maillage) et qui fait le tour du cou (large en x et z).
    static string RetirerEcharpe(SkinnedMeshRenderer smr)
    {
        Mesh src = smr.sharedMesh;
        var tris = src.triangles; var v = src.vertices;
        int n = v.Length;
        // Composantes connexes par sommets partagés (positions soudées).
        var parent = new int[n]; for (int i = 0; i < n; i++) parent[i] = i;
        int Racine(int x) { while (parent[x] != x) { parent[x] = parent[parent[x]]; x = parent[x]; } return x; }
        void Unir(int a, int b) { a = Racine(a); b = Racine(b); if (a != b) parent[a] = b; }
        var soude = new Dictionary<Vector3Int, int>();
        for (int i = 0; i < n; i++)
        {
            var k = new Vector3Int(Mathf.RoundToInt(v[i].x * 10000f), Mathf.RoundToInt(v[i].y * 10000f), Mathf.RoundToInt(v[i].z * 10000f));
            if (soude.TryGetValue(k, out int j)) Unir(i, j); else soude[k] = i;
        }
        for (int t = 0; t < tris.Length; t += 3) { Unir(tris[t], tris[t + 1]); Unir(tris[t + 1], tris[t + 2]); }
        var boites = new Dictionary<int, Bounds>(); var nbTris = new Dictionary<int, int>();
        for (int t = 0; t < tris.Length; t += 3)
        {
            int r = Racine(tris[t]);
            Vector3 c = (v[tris[t]] + v[tris[t + 1]] + v[tris[t + 2]]) / 3f;
            if (boites.TryGetValue(r, out var b)) { b.Encapsulate(c); boites[r] = b; nbTris[r]++; } else { boites[r] = new Bounds(c, Vector3.zero); nbTris[r] = 1; }
        }
        Bounds tout = src.bounds;
        var infos = new System.Text.StringBuilder();
        int echarpe = -1; float meilleur = 0f;
        foreach (var kv in boites)
        {
            var b = kv.Value;
            float h = (b.center.y - tout.min.y) / Mathf.Max(0.001f, tout.size.y);
            infos.Append("[" + nbTris[kv.Key] + " tri, h " + h.ToString("F2") + ", l " + b.size.x.ToString("F2") + "x" + b.size.z.ToString("F2") + "] ");
            if (h < 0.7f || h > 0.97f) continue;
            float tour = Mathf.Min(b.size.x / tout.size.x, b.size.z / tout.size.z);
            if (tour > meilleur && nbTris[kv.Key] < tris.Length / 3 * 0.6f) { meilleur = tour; echarpe = kv.Key; }
        }
        if (echarpe < 0) return "écharpe : aucune composante au cou (" + infos + ")";
        var garde = new List<int>(tris.Length);
        for (int t = 0; t < tris.Length; t += 3) if (Racine(tris[t]) != echarpe) { garde.Add(tris[t]); garde.Add(tris[t + 1]); garde.Add(tris[t + 2]); }
        string chemin = Dossier + "/Forgeron_Corps.asset";
        var copie = AssetDatabase.LoadAssetAtPath<Mesh>(chemin);
        if (copie == null) { copie = Object.Instantiate(src); AssetDatabase.CreateAsset(copie, chemin); }
        else { EditorUtility.CopySerialized(src, copie); }
        copie.name = "Forgeron_Corps";
        copie.subMeshCount = 1;
        copie.SetTriangles(garde, 0);
        copie.RecalculateBounds();
        EditorUtility.SetDirty(copie);
        smr.sharedMesh = copie;
        return "écharpe retirée : " + nbTris[echarpe] + " triangles sur " + tris.Length / 3 + " (composantes : " + infos + ")";
    }

    // Axe principal du maillage du marteau (le manche) : l'axe le plus long de ses bornes.
    static int Axe(Bounds b) { return b.size.y >= b.size.x && b.size.y >= b.size.z ? 1 : b.size.x >= b.size.z ? 0 : 2; }

    // Tête du marteau : moyenne des sommets les plus éloignés de la poignée (le quart haut le long de l'axe principal).
    static Vector3 TeteLocale(Mesh m)
    {
        var v = m.vertices; Bounds b = m.bounds;
        int axe = Axe(b);
        float seuil = b.max[axe] - b.size[axe] * 0.2f;
        Vector3 s = Vector3.zero; int k = 0;
        foreach (var p in v) if (p[axe] >= seuil) { s += p; k++; }
        return k > 0 ? s / k : b.center;
    }

    // Sommets de la tête (moitié haute le long du manche : toute la tête et le haut du manche) : c'est elle qui touche.
    static int[] IndicesTete(Mesh m)
    {
        var v = m.vertices; Bounds b = m.bounds; int axe = Axe(b);
        float seuil = b.min[axe] + b.size[axe] * 0.5f;
        var l = new List<int>();
        for (int i = 0; i < v.Length; i++) if (v[i][axe] >= seuil) l.Add(i);
        return l.ToArray();
    }

    static AnimationClip Clip(string fbx, string nom)
    {
        foreach (var o in AssetDatabase.LoadAllAssetsAtPath(fbx)) if (o is AnimationClip c && c.name == nom) return c;
        Debug.LogError("Forgeron : clip introuvable " + nom);
        return null;
    }

    // Contact : le clip de frappe figé à l'instant du contact (vitesse 0, départ à `contact` en temps normalisé), état par
    // défaut ; Frappe : le clip.
    static AnimatorController Controleur(AnimationClip frappe, float contact)
    {
        string chemin = Dossier + "/Forgeron.controller";
        AssetDatabase.DeleteAsset(chemin);
        var c = AnimatorController.CreateAnimatorControllerAtPath(chemin);
        var sm = c.layers[0].stateMachine;
        var sContact = sm.AddState("Contact"); sContact.motion = frappe; sContact.speed = 0f; sContact.cycleOffset = contact;
        var sFrappe = sm.AddState("Frappe"); sFrappe.motion = frappe;
        sm.defaultState = sContact;
        return c;
    }
}
