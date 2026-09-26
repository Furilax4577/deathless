using System.Reflection;
using Deathless.Jeu;
using UnityEditor;
using UnityEngine;

namespace Deathless.EditorTools
{
    /// <summary>
    /// Construit (ou reconstruit) les deux prefabs du mini-boss Morgrim (wiki : ennemis.md, Morgrim ; décidé le
    /// 26/09/2026), à partir du Golem existant (Assets/Jeu/Prefabs/Squelette_Golem.prefab) : mêmes composants
    /// (NavMeshAgent, Sante, NetworkObject, NetworkTransform, NetworkAnimator, EnnemiReseau — Docs/reseau.md), même
    /// squelette et animations (aucun clip n'est écrit spécifiquement pour ces coups, comme dans le bac à sable
    /// sandbox-rig), seul le comportement (Golem → MorgrimMassue / MorgrimMartache) et l'arme ou les yeux changent :
    /// - Morgrim_Massue : remplace la hache-marteau par la massue (Skeleton_Mace_Large), yeux inchangés (Yeux_Squelette).
    /// - Morgrim_Martache : garde la hache-marteau actuelle (Skeleton_Golem_Axe_Large, déjà en place sur le Golem),
    ///   yeux bleu glacé (Yeux_Glace.mat) pour la distinguer au premier coup d'œil.
    /// Script relançable (menu Deathless > Jeu > Morgrim) : à lancer une fois l'éditeur main piloté (temps 2 du plan),
    /// jamais depuis un appel MCP pendant que d'autres agents l'utilisent.
    /// </summary>
    public static class MorgrimPrefabBuilder
    {
        const string PrefabDir = "Assets/Jeu/Prefabs";
        const string GolemSource = PrefabDir + "/Squelette_Golem.prefab";
        const string Skel = "Assets/Art/KayKit/KayKit_Skeletons_1.1_EXTRA/";
        const string ArmeMassue = Skel + "assets/fbx(unity)/Skeleton_Mace_Large.fbx";
        const string MatCorps = "Assets/Jeu/Materiaux/Squelette_Ennemi.mat";
        const string MatYeuxGlace = "Assets/Jeu/Materiaux/Yeux_Glace.mat";

        [MenuItem("Deathless/Jeu/Morgrim (prefabs Massue et Martache)")]
        public static void Construire()
        {
            string massue = ConstruireVariante("Morgrim_Massue", true);
            string martache = ConstruireVariante("Morgrim_Martache", false);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            string r = "Morgrim : prefabs construits — " + massue + ", " + martache
                + ". Reste à assigner DirecteurVagues.prefabMorgrimMassue / prefabMorgrimMartache dans la scène Village.";
            Debug.Log(r);
        }

        static string ConstruireVariante(string nom, bool massue)
        {
            string chemin = PrefabDir + "/" + nom + ".prefab";
            var golemAsset = AssetDatabase.LoadAssetAtPath<GameObject>(GolemSource);
            if (golemAsset == null)
            {
                Debug.LogError("Morgrim : " + GolemSource + " introuvable — construire d'abord le Golem (Deathless > Jeu > 4. Prefabs).");
                return chemin + " (échec)";
            }
            var racine = PrefabUtility.LoadPrefabContents(GolemSource);
            racine.name = nom;

            // Arme : Massue remplace la hache-marteau par la massue ; Martache garde celle du Golem (déjà la bonne arme,
            // wiki : elle porte une tête de marteau au dos de la lame en croissant, aucune arme à générer).
            if (massue)
            {
                var handslot = TrouverEnfant(racine.transform, "handslot.r");
                if (handslot != null)
                {
                    for (int i = handslot.childCount - 1; i >= 0; i--) Object.DestroyImmediate(handslot.GetChild(i).gameObject);
                    var armeAsset = AssetDatabase.LoadAssetAtPath<GameObject>(ArmeMassue);
                    var matCorps = AssetDatabase.LoadAssetAtPath<Material>(MatCorps);
                    if (armeAsset != null)
                    {
                        var arme = (GameObject)PrefabUtility.InstantiatePrefab(armeAsset, handslot);
                        PrefabUtility.UnpackPrefabInstance(arme, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                        arme.transform.localPosition = Vector3.zero;
                        arme.transform.localRotation = Quaternion.identity;
                        arme.transform.localScale = Vector3.one;
                        if (matCorps != null)
                            foreach (var r in arme.GetComponentsInChildren<Renderer>())
                            {
                                var mats = r.sharedMaterials;
                                for (int i = 0; i < mats.Length; i++) mats[i] = matCorps;
                                r.sharedMaterials = mats;
                            }
                    }
                    else Debug.LogWarning("Morgrim : " + ArmeMassue + " introuvable, la Massue garde la hache du Golem.");
                }
            }

            // Yeux : bleu glacé pour la Martache seulement (wiki : distinction visuelle des deux versions, décidée).
            if (!massue)
            {
                var matYeux = AssetDatabase.LoadAssetAtPath<Material>(MatYeuxGlace);
                if (matYeux != null)
                    foreach (var r in racine.GetComponentsInChildren<Renderer>(true))
                        if (r.name.EndsWith("_Eyes")) r.sharedMaterial = matYeux;
            }

            // Comportement : remplace Golem par la variante (MorgrimMassue / MorgrimMartache), champs communs recopiés.
            var ancien = racine.GetComponent<Golem>();
            Transform modele = ancien != null ? ancien.modele : null;
            Animator animator = ancien != null ? ancien.animator : null;
            Material yeuxElite = ancien != null ? ancien.yeuxElite : null;
            float dureeSortie = ancien != null ? ancien.dureeSortie : 2.3777778f;
            float instantCoupClip = ancien != null ? ancien.instantCoupClip : 0.9f;
            if (ancien != null) Object.DestroyImmediate(ancien);
            Golem nouveau = massue ? (Golem)racine.AddComponent<MorgrimMassue>() : racine.AddComponent<MorgrimMartache>();
            nouveau.modele = modele;
            nouveau.animator = animator;
            nouveau.yeuxElite = yeuxElite;
            nouveau.dureeSortie = dureeSortie;
            nouveau.instantCoupClip = instantCoupClip;

            PrefabUtility.SaveAsPrefabAsset(racine, chemin);
            PrefabUtility.UnloadPrefabContents(racine);

            // Réseau (Docs/reseau.md) : les composants réseau viennent déjà du Golem cloné (NetworkObject,
            // NetworkTransform en autorité serveur, NetworkAnimator, EnnemiReseau) ; seul l'identifiant réseau
            // (GlobalObjectIdHash) doit être recalculé pour ce nouveau chemin d'asset, sinon il entrerait en collision
            // avec celui de Squelette_Golem.prefab (même méthode que ClassesBuilder.Reseau, « 8. Réseau »).
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(chemin);
            var no = go != null ? go.GetComponent<Unity.Netcode.NetworkObject>() : null;
            if (no != null)
            {
                var valider = typeof(Unity.Netcode.NetworkObject).GetMethod("OnValidate", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                valider?.Invoke(no, null);
                EditorUtility.SetDirty(no);
            }
            return chemin;
        }

        static Transform TrouverEnfant(Transform racine, string nom)
        {
            if (racine.name == nom) return racine;
            for (int i = 0; i < racine.childCount; i++)
            {
                var t = TrouverEnfant(racine.GetChild(i), nom);
                if (t != null) return t;
            }
            return null;
        }
    }
}
