using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

// Villageois de la carte v5 (03/10/2026, demande de Quentin : « vide les bâtiments des meubles, sors les PNJ dehors, chacun devant sa
// maison »). Les bâtiments Tripo sont fermés : plus d'intérieur (V5RetirerInterieur), les villageois sont dehors, debout, au pied de leur
// maison, tournés vers l'allée et la place (le +Z du bâtiment regarde Nyxessa). Rien d'ajouté autour d'eux (ni comptoir, ni enclume, ni
// tonneau) : une pose de repos, et c'est tout.
//   Forgeron   : barbare KayKit sans bonnet ni écharpe, debout au bord de l'atelier ouvert de la forge (ancre Enclume_Ancre du prefab, côté place)
//   Tavernière : la Bavaroise (TavernierBuilder), à côté de la porte ; l'ancre d'échange Villageois/Ancre_Echange_Taverne (composant Taverne posé
//                par Partie.Start) est un mètre devant elle : on lui parle (E) dans les 2,4 m, comme au comptoir de l'ancienne taverne
//   Mécano     : l'ingénieur KayKit (Engineer.fbx), à côté de la porte (nouveau : il n'était pas dans le jeu ; pas de boutique pour l'instant)
//   Druide     : le druide KayKit (Druid.fbx), à côté de la porte (nouveau : pas de potions à vendre pour l'instant)
//   Sorcier    : l'objet racine « Sorcier » (Sorcier.cs) vient de lui-même à Villageois/Poste_Sorcier, le jour et à l'aube ; il part de là à la
//                tombée de la nuit (il marche jusqu'à Nyxessa et incante, comme avant)
// Jour et nuit : les villageois dehors restent à leur place (seul le sorcier a un travail de nuit). Reproductible : tout est retiré puis refait.
// Menu : Deathless > Village > v5 > 1b. Villageois ; fait aussi partie de « Tout appliquer ».
public static partial class VillageBuilder
{
    public const string V5VillageoisGroupe = "Villageois";
    /// Position devant la maison, repère de la porte : décalage latéral (m, - = côté droit vu de la façade, du côté de la fenêtre), distance
    /// devant le bord du soubassement (hors marches), lacet ajouté au regard du bâtiment (positif : vers l'axe de la porte et de l'allée).
    public const float V5VillageoisLateral = -3.2f, V5VillageoisAvant = 1.4f, V5VillageoisLacet = 20f;
    const string V5VillageoisDossier = "Assets/Jeu/Villageois";
    const string V5VillageoisAnims = "Assets/Art/KayKit/KayKit_Character_Animations_1.1/Animations/fbx/Rig_Medium/Rig_Medium_General.fbx";
    const string V5DruideFbx = "Assets/Art/KayKit/KayKit_Adventurers_2.0_EXTRA/Characters/fbx/Druid.fbx";
    const string V5MecanoFbx = "Assets/Art/KayKit/KayKit_Adventurers_2.0_EXTRA/Characters/fbx/Engineer.fbx";
    /// Échelle des personnages KayKit adultes (comme les héros : Modele à 0,8).
    const float V5VillageoisEchelle = 0.8f;

    /// Façade d'un bâtiment Tripo : axe de la porte (x local), bord avant du soubassement (z local, hors marches) et bord des marches.
    sealed class V5Facade
    {
        public Transform bat;
        public float xPorte, zSoub, zMarches;
        /// Point au sol (monde) à `dx` m de l'axe de la porte et `dz` m devant le soubassement.
        public Vector3 Poste(float dx, float dz)
        {
            Vector3 p = bat.TransformPoint(new Vector3(xPorte + dx, 0f, zSoub + dz));
            p.y = GroundHeight(p.x, p.z);
            return p;
        }
        public float Lacet(float d) { return bat.eulerAngles.y + d; }
    }

    static V5Facade V5FacadeDe(Transform ms, string role)
    {
        int i = System.Array.IndexOf(V5MaisonsRoles, role);
        Transform rendu = i >= 0 ? V5RenduTripo(ms, i) : null;
        if (rendu == null) return null;
        Transform bat = ms.Find("Batiment_" + role), ent = rendu.parent.Find("Entree");
        if (bat == null || ent == null) return null;
        var f = new V5Facade { bat = bat, xPorte = bat.InverseTransformPoint(ent.position).x, zSoub = -999f, zMarches = -999f };
        foreach (Vector3 v in rendu.GetComponent<MeshFilter>().sharedMesh.vertices)
        {
            Vector3 p = bat.InverseTransformPoint(rendu.TransformPoint(v));
            if (p.y >= 0.1f) continue;
            f.zMarches = Mathf.Max(f.zMarches, p.z);
            if (Mathf.Abs(p.x - f.xPorte) > 2.4f) f.zSoub = Mathf.Max(f.zSoub, p.z);   // le soubassement, sans les marches de la porte
        }
        return f;
    }

    /// Contrôleur des villageois au repos : un seul état, Repos (Idle_A des KayKit Character Animations).
    static AnimatorController V5ControleurRepos()
    {
        string chemin = V5VillageoisDossier + "/Villageois_Repos.controller";
        if (!AssetDatabase.IsValidFolder(V5VillageoisDossier)) AssetDatabase.CreateFolder("Assets/Jeu", "Villageois");
        AssetDatabase.DeleteAsset(chemin);
        AnimationClip repos = null;
        foreach (var o in AssetDatabase.LoadAllAssetsAtPath(V5VillageoisAnims)) if (o is AnimationClip c && c.name == "Idle_A") repos = c;
        var ctrl = AnimatorController.CreateAnimatorControllerAtPath(chemin);
        var sm = ctrl.layers[0].stateMachine;
        var s = sm.AddState("Repos"); s.motion = repos;
        sm.defaultState = s;
        return ctrl;
    }

    /// Villageois debout (racine « nom » : collider capsule + modèle KayKit au repos) à `pos`, regard de lacet `lacet`.
    static GameObject V5Pnj(Transform parent, string nom, string fbx, Vector3 pos, float lacet, AnimatorController repos, System.Func<GameObject, string> habiller)
    {
        GameObject modeleFbx = AssetDatabase.LoadAssetAtPath<GameObject>(fbx);
        if (modeleFbx == null) { Debug.LogWarning("Villageois : modèle introuvable " + fbx); return null; }
        var racine = new GameObject(nom);
        racine.transform.SetParent(parent, false);
        racine.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, lacet, 0f));
        var col = racine.AddComponent<CapsuleCollider>(); col.center = new Vector3(0f, 1.0f, 0f); col.height = 2.0f; col.radius = 0.35f;
        var modele = (GameObject)PrefabUtility.InstantiatePrefab(modeleFbx, racine.transform);
        modele.name = "Modele";
        modele.transform.localPosition = Vector3.zero; modele.transform.localRotation = Quaternion.identity; modele.transform.localScale = Vector3.one * V5VillageoisEchelle;
        if (habiller != null) habiller(modele);
        var anim = modele.GetComponentInChildren<Animator>(); if (anim == null) anim = modele.AddComponent<Animator>();
        anim.runtimeAnimatorController = repos; anim.applyRootMotion = false; anim.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
        if (repos != null && repos.animationClips.Length > 0) repos.animationClips[0].SampleAnimation(modele, 0f);
        return racine;
    }

    public static string V5Villageois(Transform root)
    {
        Transform ms = root.Find("Maisons");
        if (ms == null) return "Villageois : Maisons absentes";
        Kill(root.Find(V5VillageoisGroupe));
        Transform grp = Group(root, V5VillageoisGroupe);
        var sb = new StringBuilder("Villageois : ");
        AnimatorController repos = V5ControleurRepos();
        System.Func<V5Facade, Vector3> devant = f => f.Poste(V5VillageoisLateral, V5VillageoisAvant);

        // Tavernière : à côté de la porte ; ancre d'échange un mètre devant elle, à hauteur de comptoir (y + 0,95), regard vers la place
        V5Facade ft = V5FacadeDe(ms, "Taverne");
        if (ft != null)
        {
            Vector3 p = devant(ft); float lacet = ft.Lacet(V5VillageoisLacet);
            Transform t = Group(grp, "Taverne");
            var ancreV = new GameObject("Ancre_Villageois_Taverne").transform;
            ancreV.SetParent(t, false); ancreV.SetPositionAndRotation(p, Quaternion.Euler(0f, lacet, 0f));
            sb.Append("tavernière " + TavernierBuilder.Poser(t, ancreV) + " en " + V5Pt(p) + " ; ");
            var echange = new GameObject("Ancre_Echange_Taverne").transform;   // Partie.AncreTaverne() la trouve, Taverne s'y pose au démarrage
            echange.SetParent(grp, false);
            echange.SetPositionAndRotation(p + Quaternion.Euler(0f, lacet, 0f) * Vector3.forward * 1.0f + Vector3.up * 0.95f, Quaternion.Euler(0f, lacet, 0f));
        }
        else sb.Append("taverne absente ; ");

        // Forgeron : debout au bord de l'atelier ouvert (Enclume_Ancre du prefab : au sol, côté place), regard vers la place
        int iF = System.Array.IndexOf(V5MaisonsRoles, "Forge");
        Transform renduF = V5RenduTripo(ms, iF);
        Transform ancreF = renduF != null ? renduF.parent.Find("Enclume_Ancre") : null;
        if (ancreF != null)
        {
            Vector3 p = ancreF.position; p.y = GroundHeight(p.x, p.z);
            Transform bat = ms.Find("Batiment_Forge");
            GameObject g = V5Pnj(Group(grp, "Forge"), "Forgeron", ForgeronBuilder.ModeleBarbarian, p, bat.eulerAngles.y, repos, m => ForgeronBuilder.Habiller(m));
            sb.Append("forgeron " + (g != null ? "en " + V5Pt(p) : "introuvable") + " ; ");
        }
        else sb.Append("forge ou Enclume_Ancre absente ; ");

        // Mécano et druide : à côté de la porte
        foreach (var (role, fbx, nom) in new[] { ("Mecano", V5MecanoFbx, "Mecano"), ("Druide", V5DruideFbx, "Druide") })
        {
            V5Facade f = V5FacadeDe(ms, role);
            if (f == null) { sb.Append(role + " absent ; "); continue; }
            Vector3 p = devant(f);
            GameObject g = V5Pnj(Group(grp, role), nom, fbx, p, f.Lacet(V5VillageoisLacet), repos, null);
            sb.Append(nom.ToLowerInvariant() + " " + (g != null ? "en " + V5Pt(p) : "introuvable") + " ; ");
        }

        // Sorcier : l'objet racine « Sorcier » (script Sorcier) lit ce poste
        V5Facade fs = V5FacadeDe(ms, "Sorcier");
        if (fs != null)
        {
            Vector3 p = devant(fs);
            var poste = new GameObject("Poste_Sorcier").transform;
            poste.SetParent(grp, false); poste.SetPositionAndRotation(p, Quaternion.Euler(0f, fs.Lacet(V5VillageoisLacet), 0f));
            sb.Append("poste du sorcier en " + V5Pt(p) + " ; ");
        }
        else sb.Append("sorcier absent ; ");
        UnityEditor.AssetDatabase.SaveAssets();
        Physics.SyncTransforms();
        return sb.ToString();
    }

    static string V5Pt(Vector3 p) { return "(" + p.x.ToString("F1") + " ; " + p.z.ToString("F1") + ")"; }

    [MenuItem("Deathless/Village/v5/1b. Villageois")] public static string V5MenuVillageois() { return Etape(V5Villageois); }
}
