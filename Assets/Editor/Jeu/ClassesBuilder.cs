using System.Collections.Generic;
using Deathless.Jeu;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Deathless.EditorTools
{
    /// Classes jouables (menu Deathless > Jeu > 7. Classes) : un contrôleur d'animation par classe construit à partir des
    /// clips de son style d'arme (règle « style → animation ») et des clips génériques, un prefab de héros par classe
    /// (modèle KayKit équipé par MannequinEquip + Heros + la ClasseHeros de la classe), le registre ClassesJeu et les
    /// références d'effets de la scène Village (EffetsJeu). Rejouable : les assets existants sont mis à jour.
    public static partial class JeuBuilder
    {
        const string Aventuriers = "Assets/Art/KayKit/KayKit_Adventurers_2.0_FREE/Characters/fbx/";
        const string RegistrePath = "Assets/Jeu/Resources/ClassesJeu.asset";

        struct Def
        {
            public string id, nom, element, modele, style, controleur;
            public System.Type classe;
        }

        static readonly Def[] s_Defs =
        {
            new Def { id = "paladin", nom = "Paladin", modele = "Knight", style = "SwordShield", controleur = "Paladin_Jeu", classe = typeof(ClassePaladin) },
            new Def { id = "mage", nom = "Mage", element = "feu", modele = "Mage", style = "Staff", controleur = "Mage_Jeu", classe = typeof(ClasseMage) },
            new Def { id = "rodeur", nom = "Rôdeur", modele = "Ranger", style = "BowQuiver", controleur = "Rodeur_Jeu", classe = typeof(ClasseRodeur) },
            new Def { id = "assassin", nom = "Assassin", modele = "Rogue_Hooded", style = "DaggerCrossbow", controleur = "Assassin_Jeu", classe = typeof(ClasseAssassin) },
            new Def { id = "viking", nom = "Viking", modele = "Barbarian", style = "Axe2H", controleur = "Viking_Jeu", classe = typeof(ClasseViking) },
        };

        static WeaponStyle Style(string nom) => AssetDatabase.LoadAssetAtPath<WeaponStyle>("Assets/WeaponStyles/" + nom + ".asset");

        [MenuItem("Deathless/Jeu/7. Classes (contrôleurs, prefabs, registre, scène)")]
        public static string Classes()
        {
            Dossier(PrefabDir);
            ControleurPaladin();
            ControleurMage();
            ControleurRodeur();
            ControleurAssassin();
            ControleurViking();
            AssetDatabase.SaveAssets();
            foreach (var d in s_Defs) PrefabClasse(d.id);
            AssetDatabase.SaveAssets();
            // Les contrôleurs viennent d'être recréés : on réimporte les prefabs pour que leur référence soit résolue.
            foreach (var d in s_Defs) AssetDatabase.ImportAsset(PrefabDir + "/Heros_" + Capitale(d.id) + ".prefab", ImportAssetOptions.ForceUpdate);
            var reg = Registre();
            AssetDatabase.SaveAssets();
            string r = "Classes : " + reg.classes.Count + " dans le registre";
            var fx = Object.FindAnyObjectByType<EffetsJeu>();
            if (fx != null)
            {
                RemplirEffetsClasses(fx);
                EditorUtility.SetDirty(fx);
                EditorSceneManager.MarkSceneDirty(fx.gameObject.scene);
                EditorSceneManager.SaveScene(fx.gameObject.scene);
                r += ", effets de la scène " + fx.gameObject.scene.name + " à jour";
            }
            Debug.Log(r);
            return r;
        }

        static ClassesJeu Registre()
        {
            Dossier("Assets/Jeu/Resources");
            var reg = AssetDatabase.LoadAssetAtPath<ClassesJeu>(RegistrePath);
            if (reg == null) { reg = ScriptableObject.CreateInstance<ClassesJeu>(); AssetDatabase.CreateAsset(reg, RegistrePath); }
            reg.classes.Clear();
            foreach (var d in s_Defs)
                reg.classes.Add(new ClassesJeu.ClasseDef
                {
                    id = d.id, nom = d.nom, element = d.element,
                    modele = AssetDatabase.LoadAssetAtPath<GameObject>(Aventuriers + d.modele + ".fbx"),
                    style = Style(d.style),
                    controleur = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(AnimDir + "/" + d.controleur + ".controller"),
                    prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabDir + "/Heros_" + Capitale(d.id) + ".prefab"),
                });
            EditorUtility.SetDirty(reg);
            return reg;
        }

        static string Capitale(string id) => id == "paladin" ? "Paladin" : char.ToUpperInvariant(id[0]) + id.Substring(1);

        public static void RemplirEffetsClasses(EffetsJeu fx)
        {
            GameObject P(string p) => AssetDatabase.LoadAssetAtPath<GameObject>(p);
            fx.modeleFleche = P("Assets/Art/KayKit/KayKit_Adventurers_2.0_FREE/Assets/fbx(unity)/arrow_bow.fbx");
            fx.modelePiece = P("Assets/Art/KayKit/KayKit_Dungeon_Pack_1.1_FREE/Assets/fbx(unity)/coin.fbx");
            fx.prefabArcBande = P("Assets/VFX/ArcBande/ArcBande.prefab");
            fx.prefabNuee = P("Assets/VFX/NueeDeFleches/NueeDeFleches.prefab");
            fx.prefabCone = P("Assets/VFX/ConeDeFlammes/ConeDeFlammes.prefab");
            fx.prefabBrulure = P("Assets/VFX/BurnFlammeches/BurnFlammeches.prefab");
            fx.prefabFumigene = P("Assets/VFX/Fumigene/Fumigene.prefab");
            fx.prefabTournante = P("Assets/VFX/AttaqueTournante/AttaqueTournante.prefab");
            fx.prefabRugissement = P("Assets/VFX/Rugissement/Rugissement.prefab");
            fx.prefabOndeSaut = P("Assets/VFX/OndeDeChoc/OndeDeChoc_SautPercutant.prefab");
        }

        // ================================================================= Prefabs

        public static void PrefabClasse(string id)
        {
            Def d = default;
            foreach (var x in s_Defs) if (x.id == id) d = x;
            var b = GameBalance.Courant;
            var style = Style(d.style);
            var racine = new GameObject("Heros_" + Capitale(id));
            var modele = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Aventuriers + d.modele + ".fbx"));
            PrefabUtility.UnpackPrefabInstance(modele, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            modele.name = "Modele";
            modele.transform.SetParent(racine.transform, false);
            modele.transform.localScale = Vector3.one * b.echellePersonnages;
            MannequinEquip.Equiper(modele, style);
            foreach (var t in modele.GetComponentsInChildren<Transform>(true))
                if (PrefabUtility.IsPartOfPrefabInstance(t.gameObject) && PrefabUtility.IsOutermostPrefabInstanceRoot(t.gameObject))
                    PrefabUtility.UnpackPrefabInstance(t.gameObject, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            var anim = modele.GetComponent<Animator>(); if (anim == null) anim = modele.AddComponent<Animator>();
            anim.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(AnimDir + "/" + d.controleur + ".controller");
            anim.applyRootMotion = false;
            HelmetVisor visiere = null;
            foreach (var r in modele.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                if (r.name.EndsWith("HelmetVisor")) { visiere = modele.AddComponent<HelmetVisor>(); visiere.open = true; break; }

            var cc = racine.AddComponent<CharacterController>();
            cc.radius = 0.4f; cc.height = 1.9f; cc.center = Vector3.up * 0.95f; cc.stepOffset = 0.35f; cc.slopeLimit = 45f; cc.skinWidth = 0.06f;
            racine.AddComponent<Sante>().equipe = Equipe.Heros;
            racine.AddComponent<HerosEntrees>();
            var classe = (ClasseHeros)racine.AddComponent(d.classe);
            var h = racine.AddComponent<Heros>();
            h.animator = anim;
            h.visiere = visiere;

            if (classe is ClassePaladin pal)
            {
                var auraPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/VFX/AuraSoin/AuraSoin.prefab");
                if (auraPrefab != null)
                {
                    var aura = (GameObject)PrefabUtility.InstantiatePrefab(auraPrefab, racine.transform);
                    aura.transform.localPosition = Vector3.zero;   // racine du prefab à y = −1 sous un centre de capsule : ici sous les pieds
                    pal.aura = aura.GetComponent<AuraSoin>();
                }
            }
            if (classe is ClasseAssassin)
            {
                // Mode furtif : même réglage que le composant du prefab Furtif (matériau des gemmes, opacité…).
                var furtif = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/VFX/Furtif/Furtif.prefab");
                var source = furtif != null ? furtif.GetComponentInChildren<ModeFurtif>(true) : null;
                var mf = racine.AddComponent<ModeFurtif>();
                if (source != null) EditorUtility.CopySerialized(source, mf);
            }
            AjouterReseau(racine);
            PrefabUtility.SaveAsPrefabAsset(racine, PrefabDir + "/Heros_" + Capitale(id) + ".prefab");
            Object.DestroyImmediate(racine);
        }

        // ================================================================= Sorcier et bouclier (wiki : village, nyxessa)

        const string SorcierPath = "Assets/Jeu/Prefabs/Sorcier.prefab";
        const string SorcierMat = "Assets/Jeu/Materiaux/KayKit_Mage_Sorcier.mat";
        const string BatonSorcier = "Assets/Art/KayKit/KayKit_FantasyWeaponsBits_1.0_FREE/Assets/fbx(unity)/staff_B.fbx";
        const string BatonTexture = "Assets/Art/KayKit/KayKit_FantasyWeaponsBits_1.0_FREE/Assets/fbx(unity)/weapons_bits_texture.png";

        /// Contrôleur du sorcier : socle commun (marche, mort vers l'arrière Death_A, et sur le haut du corps l'état
        /// « Touche » du coup reçu, Hit_A sur le déclencheur « Hit » commun à toutes les classes, Socle) et, sur ce même
        /// haut du corps, l'invocation (Ranged_Magic_Summon, bâton levé, calée sur la durée de l'incantation) puis le sort
        /// en boucle (Spellcasting). Réaction de coup (Quentin, retour 0.5.2) : le bouclier frappé déclenche ce « Hit »
        /// (Sorcier.JouerReactionCoup, fréquence limitée) ; la Touche du Socle revient à Vide, qui repart aussitôt vers
        /// Cone tant que le bouclier tient (transition déjà posée ci-dessous) : il ne perd donc sa pose qu'un instant.
        static RuntimeAnimatorController ControleurSorcier()
        {
            var style = Style("Staff");
            var c = Socle("Sorcier_Jeu", style, out var loco, out var haut, out var vide);
            c.AddParameter("Invoque", AnimatorControllerParameterType.Trigger);
            Booleen(c, "Cone");
            var invClip = Clip(Ranged, "Ranged_Magic_Summon");
            float vitesse = invClip != null ? invClip.length / Mathf.Max(0.5f, GameBalance.Courant.bouclierIncantation) : 1f;
            var inv = Etat(haut, "Invoque", invClip, new Vector3(450, -120), vitesse);
            var cone = Etat(haut, "Cone", Boucle(Clip(Ranged, "Ranged_Magic_Spellcasting"), "Ranged_Magic_Spellcasting_Loop"), new Vector3(450, 120));
            var ti = vide.AddTransition(inv); ti.hasExitTime = false; ti.duration = 0.15f; ti.AddCondition(AnimatorConditionMode.If, 0, "Invoque");
            Sortie(inv, cone, 0.95f, 0.2f);
            var ti2 = inv.AddTransition(vide); ti2.hasExitTime = false; ti2.duration = 0.15f; ti2.AddCondition(AnimatorConditionMode.IfNot, 0, "Cone");
            Si(vide, cone, "Cone", true, 0.12f);
            Si(cone, vide, "Cone", false, 0.15f);
            EditorUtility.SetDirty(c);
            AssetDatabase.SaveAssets();
            return c;
        }

        /// Matériau du bâton du sorcier : atlas du pack Fantasy Weapons Bits dont seuls les pixels bleus (le cristal) passent au
        /// vert Nyxessa, avec une légère émission limitée à ces pixels ; bois, ivoire et or inchangés. L'atlas d'origine
        /// n'est pas modifié (même méthode que les yeux du Nécromancien).
        static Material MateriauBatonSorcier()
        {
            Dossier("Assets/Jeu/Materiaux");
            string path = "Assets/Jeu/Materiaux/Baton_Sorcier.mat";
            var fbxMat = AssetDatabase.LoadAssetAtPath<GameObject>(BatonSorcier).GetComponentInChildren<Renderer>().sharedMaterial;
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(fbxMat); AssetDatabase.CreateAsset(m, path); }
            else m.CopyPropertiesFromMaterial(fbxMat);
            var imp = (TextureImporter)AssetImporter.GetAtPath(BatonTexture);
            bool lisible = imp.isReadable;
            if (!lisible) { imp.isReadable = true; imp.SaveAndReimport(); }
            var src = AssetDatabase.LoadAssetAtPath<Texture2D>(BatonTexture);
            var px = src.GetPixels();
            var emis = new Color[px.Length];
            Color vert = VfxPalette.Couleur(VfxTheme.Nyxessa, VfxRole.Coeur, new Color(0.62f, 0.91f, 0.44f));
            Color vif = VfxPalette.Couleur(VfxTheme.Nyxessa, VfxRole.Vif, new Color(0.25f, 0.68f, 0.35f));
            Color ombre = VfxPalette.Couleur(VfxTheme.Nyxessa, VfxRole.Ombre, new Color(0.10f, 0.35f, 0.22f));
            int n = 0;
            for (int i = 0; i < px.Length; i++)
            {
                Color.RGBToHSV(px[i], out float h, out float sat, out float v);
                bool bleu = h > 0.52f && h < 0.70f && sat > 0.35f && v > 0.25f;
                if (bleu)
                {
                    px[i] = v < 0.55f ? Color.Lerp(ombre, vif, v / 0.55f) : Color.Lerp(vif, vert, (v - 0.55f) / 0.45f);
                    emis[i] = Color.Lerp(vif, vert, v) * 0.6f;
                    n++;
                }
                else emis[i] = Color.black;
            }
            if (!lisible) { imp.isReadable = false; imp.SaveAndReimport(); }
            var tex = new Texture2D(src.width, src.height, TextureFormat.RGBA32, false); tex.SetPixels(px); tex.Apply();
            var tEm = new Texture2D(src.width, src.height, TextureFormat.RGBA32, false); tEm.SetPixels(emis); tEm.Apply();
            string pb = "Assets/Jeu/Materiaux/Baton_Sorcier_Base.png", pe = "Assets/Jeu/Materiaux/Baton_Sorcier_Emission.png";
            System.IO.File.WriteAllBytes(pb, tex.EncodeToPNG());
            System.IO.File.WriteAllBytes(pe, tEm.EncodeToPNG());
            foreach (var p in new[] { pb, pe })
            {
                AssetDatabase.ImportAsset(p);
                var ti = (TextureImporter)AssetImporter.GetAtPath(p);
                ti.mipmapEnabled = false; ti.filterMode = src.filterMode; ti.textureCompression = TextureImporterCompression.Uncompressed;
                ti.SaveAndReimport();
            }
            var b = AssetDatabase.LoadAssetAtPath<Texture2D>(pb);
            if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", b);
            if (m.HasProperty("_MainTex")) m.SetTexture("_MainTex", b);
            m.EnableKeyword("_EMISSION");
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            m.SetTexture("_EmissionMap", AssetDatabase.LoadAssetAtPath<Texture2D>(pe));
            // Lueur douce (Quentin) : visible la nuit, les facettes restent lisibles ; pas de lumière ni de halo (pas de bloom).
            m.SetColor("_EmissionColor", Color.white * 0.8f);
            EditorUtility.SetDirty(m);
            Debug.Log("Bâton du sorcier : " + n + " pixels du cristal passés au vert Nyxessa");
            return m;
        }


        /// Sorcier (modèle Mage du pack Adventurers, texture alternative A bleu-gris, bâton du style Staff, contrôleur du
        /// Mage) et bouclier de Nyxessa (BouclierRelique) posés dans la scène ouverte (Village) ; pièce d'or des vagues.
        [MenuItem("Deathless/Jeu/9. Sorcier et bouclier")]
        public static string SorcierEtBouclier()
        {
            var b = GameBalance.Courant;
            // Matériau : copie de KayKit_Mage avec la texture alternative A (le Mage joueur garde la sienne).
            Dossier("Assets/Jeu/Materiaux");
            var source = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/KayKit_Mage.mat");
            var mat = AssetDatabase.LoadAssetAtPath<Material>(SorcierMat);
            if (mat == null) { mat = new Material(source); AssetDatabase.CreateAsset(mat, SorcierMat); }
            else mat.CopyPropertiesFromMaterial(source);
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/KayKit/KayKit_Adventurers_2.0_EXTRA/Textures/mage_texture_alt_A.png");
            if (tex != null) { mat.SetTexture("_BaseMap", tex); if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", tex); }
            EditorUtility.SetDirty(mat);

            // Prefab.
            var racine = new GameObject("Sorcier");
            var modele = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Aventuriers + "Mage.fbx"));
            PrefabUtility.UnpackPrefabInstance(modele, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            modele.name = "Modele";
            modele.transform.SetParent(racine.transform, false);
            modele.transform.localScale = Vector3.one * b.echellePersonnages;
            MannequinEquip.Equiper(modele, Style("Staff"));
            foreach (var t in modele.GetComponentsInChildren<Transform>(true))
                if (PrefabUtility.IsPartOfPrefabInstance(t.gameObject) && PrefabUtility.IsOutermostPrefabInstanceRoot(t.gameObject))
                    PrefabUtility.UnpackPrefabInstance(t.gameObject, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            foreach (var rendu in modele.GetComponentsInChildren<Renderer>(true))
            {
                var mats = rendu.sharedMaterials;
                for (int i = 0; i < mats.Length; i++) if (mats[i] == source) mats[i] = mat;
                rendu.sharedMaterials = mats;
            }
            // Bâton du sorcier (Quentin) : staff_B du pack Fantasy Weapons Bits (hampe, bague ivoire, cornes dorées, cristal
            // en goutte), cristal passé au vert Nyxessa ; à la place du bâton d'Adventurers (celui du Mage joueur).
            Transform main = null;
            foreach (var t in modele.GetComponentsInChildren<Transform>(true)) if (t.name == "handslot.r") main = t;
            foreach (var t in modele.GetComponentsInChildren<Transform>(true)) if (t.name == "staff" && t.parent == main) Object.DestroyImmediate(t.gameObject);
            if (main != null)
            {
                var baton = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(BatonSorcier), main);
                PrefabUtility.UnpackPrefabInstance(baton, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                baton.name = "baton_sorcier";
                baton.transform.localPosition = Vector3.zero;
                baton.transform.localRotation = Quaternion.identity;
                baton.transform.localScale = Vector3.one;
                var matBaton = MateriauBatonSorcier();
                foreach (var rendu in baton.GetComponentsInChildren<Renderer>(true)) rendu.sharedMaterial = matBaton;
            }
            var anim = modele.GetComponent<Animator>(); if (anim == null) anim = modele.AddComponent<Animator>();
            anim.runtimeAnimatorController = ControleurSorcier();
            anim.applyRootMotion = false;
            var agent = racine.AddComponent<UnityEngine.AI.NavMeshAgent>();
            agent.radius = 0.35f; agent.height = 1.9f; agent.speed = b.sorcierVitesse; agent.angularSpeed = 540f; agent.acceleration = 12f;
            agent.obstacleAvoidanceType = UnityEngine.AI.ObstacleAvoidanceType.LowQualityObstacleAvoidance;
            racine.AddComponent<Sante>().equipe = Equipe.Relique;
            racine.AddComponent<Sorcier>().animator = anim;
            PrefabUtility.SaveAsPrefabAsset(racine, SorcierPath);
            Object.DestroyImmediate(racine);
            AssetDatabase.SaveAssets();

            string r = "Sorcier : prefab " + SorcierPath;
            var partie = Object.FindAnyObjectByType<Partie>();
            if (partie != null && partie.nyxessa != null)
            {
                var scene = partie.gameObject.scene;
                Vector3 nyx = partie.nyxessa.transform.position;
                float sol = partie.pointDepart != null ? partie.pointDepart.position.y : 0f;
                // Sorcier (une seule instance).
                var ancien = Object.FindAnyObjectByType<Sorcier>(FindObjectsInactive.Include);
                if (ancien != null) Object.DestroyImmediate(ancien.gameObject);
                var so = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(SorcierPath), scene);
                so.transform.position = new Vector3(nyx.x - 3f, sol, nyx.z);
                // Bouclier : objet au sol sous Nyxessa, effet validé BouclierRelique (racine à +0,8 m).
                var ab = Object.FindAnyObjectByType<BouclierNyxessa>(FindObjectsInactive.Include);
                if (ab != null) Object.DestroyImmediate(ab.gameObject);
                var bo = new GameObject("BouclierNyxessa");
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(bo, scene);
                bo.transform.position = new Vector3(nyx.x, sol + b.bouclierBase, nyx.z);   // sur la marche du bas du plateau
                var effet = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/VFX/BouclierRelique/BouclierRelique.prefab"), bo.transform);
                effet.transform.localPosition = Vector3.up * 0.8f;
                var etat = effet.GetComponent<RelicShieldEtat>();
                etat.radius = b.bouclierRayon; etat.height = b.bouclierHauteur; etat.castSeconds = b.bouclierIncantation;
                etat.maxHealth = b.Palier(b.bouclierEncaissement, 1);
                bo.AddComponent<BouclierNyxessa>().effet = etat;
                var fx = Object.FindAnyObjectByType<EffetsJeu>();
                if (fx != null) { RemplirEffetsClasses(fx); EditorUtility.SetDirty(fx); }
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                r += ", sorcier et bouclier posés dans " + scene.name;
            }
            Debug.Log(r);
            return r;
        }

        // ================================================================= Réseau (Docs/reseau.md)

        const string SalonReseauPath = "Assets/Jeu/Resources/Reseau/SalonReseau.prefab";
        const string PartieReseauPath = "Assets/Jeu/Resources/Reseau/PartieReseau.prefab";

        /// Squelette réseau : l'hôte fait foi (NetworkTransform et NetworkAnimator en autorité serveur ; échelle synchronisée
        /// pour les élites) ; EnnemiReseau fait des clients des marionnettes et relaie leurs coups.
        static void AjouterReseauEnnemi(GameObject racine)
        {
            if (racine.GetComponent<Unity.Netcode.NetworkObject>() == null) racine.AddComponent<Unity.Netcode.NetworkObject>();
            var nt = racine.GetComponent<Unity.Netcode.Components.NetworkTransform>();
            if (nt == null) nt = racine.AddComponent<Unity.Netcode.Components.NetworkTransform>();
            nt.AuthorityMode = Unity.Netcode.Components.NetworkTransform.AuthorityModes.Server;
            nt.SyncRotAngleX = false;
            nt.SyncRotAngleZ = false;
            nt.SyncScaleX = nt.SyncScaleY = nt.SyncScaleZ = true;
            nt.Interpolate = true;
            var na = racine.GetComponent<Unity.Netcode.Components.NetworkAnimator>();
            if (na == null) na = racine.AddComponent<Unity.Netcode.Components.NetworkAnimator>();
            na.Animator = racine.GetComponentInChildren<Animator>(true);
            na.AuthorityMode = Unity.Netcode.Components.NetworkAnimator.AuthorityModes.Server;
            if (racine.GetComponent<Deathless.Reseau.EnnemiReseau>() == null) racine.AddComponent<Deathless.Reseau.EnnemiReseau>();
        }

        /// Préfabs réseau : composants réseau des héros (sans reconstruire les prefabs) et objet du salon.
        [MenuItem("Deathless/Jeu/8. Réseau (préfabs des héros et du salon)")]
        public static string Reseau()
        {
            int n = 0;
            foreach (var d in s_Defs)
            {
                string chemin = PrefabDir + "/Heros_" + Capitale(d.id) + ".prefab";
                var racine = PrefabUtility.LoadPrefabContents(chemin);
                if (racine == null) continue;
                AjouterReseau(racine);
                PrefabUtility.SaveAsPrefabAsset(racine, chemin);
                PrefabUtility.UnloadPrefabContents(racine);
                n++;
            }
            // Squelettes (étape 2) : l'hôte les pilote ; position, échelle (élite) et animations répliquées.
            foreach (var nom in new[] { "Squelette_Sbire", "Squelette_Guerrier", "Squelette_Golem", "Squelette_Necromancien" })
            {
                string chemin = PrefabDir + "/" + nom + ".prefab";
                var racine = PrefabUtility.LoadPrefabContents(chemin);
                if (racine == null) continue;
                AjouterReseauEnnemi(racine);
                PrefabUtility.SaveAsPrefabAsset(racine, chemin);
                PrefabUtility.UnloadPrefabContents(racine);
                n++;
            }
            Dossier("Assets/Jeu/Resources/Reseau");
            var monde = new GameObject("PartieReseau");
            monde.AddComponent<Unity.Netcode.NetworkObject>();
            monde.AddComponent<Deathless.Reseau.PartieReseau>();
            PrefabUtility.SaveAsPrefabAsset(monde, PartieReseauPath);
            Object.DestroyImmediate(monde);
            var salon = new GameObject("SalonReseau");
            salon.AddComponent<Unity.Netcode.NetworkObject>();
            salon.AddComponent<Deathless.Reseau.SalonReseau>();
            PrefabUtility.SaveAsPrefabAsset(salon, SalonReseauPath);
            Object.DestroyImmediate(salon);
            // Identifiant réseau des préfabs (GlobalObjectIdHash) : calculé par NetworkObject.OnValidate sur l'asset.
            var valider = typeof(Unity.Netcode.NetworkObject).GetMethod("OnValidate", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
            foreach (var d in s_Defs) Valider(PrefabDir + "/Heros_" + Capitale(d.id) + ".prefab", valider);
            foreach (var nom in new[] { "Squelette_Sbire", "Squelette_Guerrier", "Squelette_Golem", "Squelette_Necromancien" }) Valider(PrefabDir + "/" + nom + ".prefab", valider);
            Valider(SalonReseauPath, valider);
            Valider(PartieReseauPath, valider);
            AssetDatabase.SaveAssets();
            string r = "Réseau : " + n + " héros équipés (NetworkObject, NetworkTransform, NetworkAnimator, HerosReseau), salon " + SalonReseauPath;
            Debug.Log(r);
            return r;
        }

        static void Valider(string chemin, System.Reflection.MethodInfo valider)
        {
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(chemin);
            var no = go != null ? go.GetComponent<Unity.Netcode.NetworkObject>() : null;
            if (no == null || valider == null) return;
            valider.Invoke(no, null);
            EditorUtility.SetDirty(no);
        }

        /// Héros réseau : le propriétaire fait foi pour sa position (NetworkTransform, lacet seul) et ses animations
        /// (NetworkAnimator : paramètres, poids des couches, déclencheurs) ; HerosReseau porte pseudo, classe et vie.
        static void AjouterReseau(GameObject racine)
        {
            if (racine.GetComponent<Unity.Netcode.NetworkObject>() == null) racine.AddComponent<Unity.Netcode.NetworkObject>();
            var nt = racine.GetComponent<Unity.Netcode.Components.NetworkTransform>();
            if (nt == null) nt = racine.AddComponent<Unity.Netcode.Components.NetworkTransform>();
            nt.AuthorityMode = Unity.Netcode.Components.NetworkTransform.AuthorityModes.Owner;
            nt.SyncRotAngleX = false;
            nt.SyncRotAngleZ = false;
            nt.SyncScaleX = nt.SyncScaleY = nt.SyncScaleZ = false;
            nt.Interpolate = true;
            var na = racine.GetComponent<Unity.Netcode.Components.NetworkAnimator>();
            if (na == null) na = racine.AddComponent<Unity.Netcode.Components.NetworkAnimator>();
            na.Animator = racine.GetComponentInChildren<Animator>(true);
            na.AuthorityMode = Unity.Netcode.Components.NetworkAnimator.AuthorityModes.Owner;
            if (racine.GetComponent<Deathless.Reseau.HerosReseau>() == null) racine.AddComponent<Deathless.Reseau.HerosReseau>();
        }

        // ================================================================= Contrôleurs

        const string Melee = AnimPack + "Rig_Medium_CombatMelee.fbx";
        const string Ranged = AnimPack + "Rig_Medium_CombatRanged.fbx";
        const string General = AnimPack + "Rig_Medium_General.fbx";
        const string Basique = AnimPack + "Rig_Medium_MovementBasic.fbx";
        const string Avance = AnimPack + "Rig_Medium_MovementAdvanced.fbx";

        /// Socle commun : locomotion du style, saut, esquive (avant, arrière), mort et réapparition ; couche « haut du
        /// corps » (masque chest) avec l'état vide et le coup reçu. Renvoie (contrôleur, locomotion, couche haute, vide).
        static AnimatorController Socle(string nom, WeaponStyle style, out AnimatorState loco, out AnimatorStateMachine haut, out AnimatorState vide)
        {
            var c = NouveauControleur(AnimDir + "/" + nom + ".controller");
            c.AddParameter("Speed", AnimatorControllerParameterType.Float);
            foreach (var p in new[] { "Grounded", "Dead" }) c.AddParameter(p, AnimatorControllerParameterType.Bool);
            foreach (var p in new[] { "Jump", "Dodge", "DodgeBack", "Hit", "Respawn" }) c.AddParameter(p, AnimatorControllerParameterType.Trigger);
            var sm = c.layers[0].stateMachine;
            loco = c.CreateBlendTreeInController("Locomotion", out BlendTree arbre, 0);
            arbre.blendParameter = "Speed";
            arbre.useAutomaticThresholds = false;
            arbre.AddChild(style.idle, 0f);
            arbre.AddChild(style.walk, 0.35f);
            arbre.AddChild(style.run, 1f);
            arbre.AddChild(style.run, 1.6f);
            var enfants = arbre.children; enfants[3].timeScale = 1.35f; arbre.children = enfants;
            sm.defaultState = loco;

            var b = GameBalance.Courant;
            var dodgeClip = Clip(Avance, "Dodge_Forward");
            var dodge = Etat(sm, "Esquive", dodgeClip, new Vector3(400, 120), dodgeClip != null ? dodgeClip.length / (b.esquiveDuree + 0.15f) : 1f);
            DeNimporte(sm, dodge, 0.05f).AddCondition(AnimatorConditionMode.If, 0, "Dodge");
            Sortie(dodge, loco, 0.9f);
            var backClip = Clip(Avance, "Dodge_Backward");
            var back = Etat(sm, "EsquiveArriere", backClip, new Vector3(400, 150), backClip != null ? backClip.length / (b.esquiveDuree + 0.15f) : 1f);
            DeNimporte(sm, back, 0.05f).AddCondition(AnimatorConditionMode.If, 0, "DodgeBack");
            Sortie(back, loco, 0.9f);

            var j1 = Etat(sm, "SautDepart", Clip(Basique, "Jump_Start"), new Vector3(400, 180), 1.4f);
            var j2 = Etat(sm, "SautAir", Clip(Basique, "Jump_Idle"), new Vector3(600, 180));
            var j3 = Etat(sm, "SautReception", Clip(Basique, "Jump_Land"), new Vector3(800, 180), 1.5f);
            DeNimporte(sm, j1, 0.05f).AddCondition(AnimatorConditionMode.If, 0, "Jump");
            Sortie(j1, j2, 0.9f, 0.05f);
            var tj = j1.AddTransition(j3); tj.hasExitTime = true; tj.exitTime = 0.6f; tj.duration = 0.05f; tj.AddCondition(AnimatorConditionMode.If, 0, "Grounded");
            var tl = j2.AddTransition(j3); tl.hasExitTime = false; tl.duration = 0.05f; tl.AddCondition(AnimatorConditionMode.If, 0, "Grounded");
            Sortie(j3, loco, 0.6f);

            var mort = Etat(sm, "Mort", Clip(General, "Death_A"), new Vector3(400, 360));
            DeNimporte(sm, mort, 0.1f).AddCondition(AnimatorConditionMode.If, 0, "Dead");
            var rev = mort.AddTransition(loco); rev.hasExitTime = false; rev.duration = 0.1f; rev.AddCondition(AnimatorConditionMode.If, 0, "Respawn");

            c.AddLayer("HautDuCorps");
            var couches = c.layers;
            couches[1].avatarMask = MasqueHautDuCorps();
            couches[1].defaultWeight = 0f;
            couches[1].blendingMode = AnimatorLayerBlendingMode.Override;
            c.layers = couches;
            haut = c.layers[1].stateMachine;
            vide = haut.AddState("Vide", new Vector3(250, 0));
            vide.writeDefaultValues = false;
            haut.defaultState = vide;
            var touche = Etat(haut, "Touche", Clip(General, "Hit_A"), new Vector3(450, 300), 1.3f);
            var th = haut.AddAnyStateTransition(touche); th.duration = 0.05f; th.hasExitTime = false; th.AddCondition(AnimatorConditionMode.If, 0, "Hit");
            Sortie(touche, vide, 0.85f);
            return c;
        }

        static void Declencheur(AnimatorController c, AnimatorStateMachine sm, AnimatorState s, string param, AnimatorState retour, float sortie, float duree = 0.08f)
        {
            if (System.Array.FindIndex(c.parameters, p => p.name == param) < 0) c.AddParameter(param, AnimatorControllerParameterType.Trigger);
            DeNimporte(sm, s, duree).AddCondition(AnimatorConditionMode.If, 0, param);
            if (retour != null) Sortie(s, retour, sortie);
        }

        static void Booleen(AnimatorController c, string param)
        {
            if (System.Array.FindIndex(c.parameters, p => p.name == param) < 0) c.AddParameter(param, AnimatorControllerParameterType.Bool);
        }

        static AnimatorStateTransition Si(AnimatorState de, AnimatorState vers, string param, bool valeur, float duree = 0.12f)
        {
            var t = de.AddTransition(vers);
            t.hasExitTime = false;
            t.duration = duree;
            t.AddCondition(valeur ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0, param);
            return t;
        }

        public static void ControleurViking()
        {
            var style = Style("Axe2H");
            var c = Socle("Viking_Jeu", style, out var loco, out var haut, out var vide);
            AjouterEmotes(c);   // roue à emotes (EmotesBuilder.cs)
            AjouterPortailArrivee(c);   // arrivée par un portail (EmotesBuilder.cs)
            var sm = c.layers[0].stateMachine;
            var chop = Clip(Melee, "Melee_2H_Attack_Chop");
            var slice = Clip(Melee, "Melee_2H_Attack_Slice");
            var b = GameBalance.Courant;
            Declencheur(c, sm, Etat(sm, "Attaque1", chop, new Vector3(650, -60), chop != null ? chop.length / (b.hacheIntervalle + 0.2f) : 1.4f), "Attack1", loco, 0.85f, 0.05f);
            Declencheur(c, sm, Etat(sm, "Attaque2", slice, new Vector3(650, 0), slice != null ? slice.length / (b.hacheIntervalle + 0.2f) : 1.4f), "Attack2", loco, 0.85f, 0.05f);
            // Attaque tournante maintenue : début du Spin, Spinning en boucle tant que « Tourne », fin du Spin.
            Booleen(c, "Tourne");
            var spin = Clip(Melee, "Melee_2H_Attack_Spin");
            var debut = Etat(sm, "TournanteDebut", spin, new Vector3(650, 240));
            var boucle = Etat(sm, "TournanteBoucle", Boucle(Clip(Melee, "Melee_2H_Attack_Spinning"), "Melee_2H_Attack_Spinning_Loop"), new Vector3(850, 240));
            var fin = Etat(sm, "TournanteFin", spin, new Vector3(1050, 240));
            if (spin != null) fin.cycleOffset = Mathf.Clamp01(1.25f / spin.length);
            var tDebut = sm.AddAnyStateTransition(debut); tDebut.hasExitTime = false; tDebut.duration = 0.08f; tDebut.canTransitionToSelf = false; tDebut.AddCondition(AnimatorConditionMode.If, 0, "Tourne");
            var tb = debut.AddTransition(boucle); tb.hasExitTime = true; tb.exitTime = spin != null ? Mathf.Clamp01(1.2f / spin.length) : 0.7f; tb.duration = 0.05f; tb.AddCondition(AnimatorConditionMode.If, 0, "Tourne");
            Si(debut, fin, "Tourne", false, 0.1f);
            Si(boucle, fin, "Tourne", false, 0.08f);
            Sortie(fin, loco, 0.95f);
            Declencheur(c, sm, Etat(sm, "Rugissement", Clip(Special, "Skeletons_Taunt_Longer"), new Vector3(650, 360), 1.6f), "Rugir", loco, 0.9f, 0.1f);
            Declencheur(c, sm, Etat(sm, "SautPercutant", Clip(Melee, "Melee_1H_Attack_Jump_Chop"), new Vector3(650, 420), 1.2f), "Saut", loco, 0.9f, 0.05f);
            EditorUtility.SetDirty(c);
        }

        public static void ControleurMage()
        {
            var style = Style("Staff");
            var c = Socle("Mage_Jeu", style, out var loco, out var haut, out var vide);
            AjouterEmotes(c);   // roue à emotes (EmotesBuilder.cs)
            AjouterPortailArrivee(c);   // arrivée par un portail (EmotesBuilder.cs)
            // Sur la couche du haut du corps : on marche en lançant.
            Declencheur(c, haut, Etat(haut, "Tir", Clip(Ranged, "Ranged_Magic_Shoot"), new Vector3(450, 0), 1.3f), "Attack1", vide, 0.9f, 0.05f);
            Booleen(c, "Cone");
            var cone = Etat(haut, "Cone", Boucle(Clip(Ranged, "Ranged_Magic_Spellcasting"), "Ranged_Magic_Spellcasting_Loop"), new Vector3(450, 120));
            Si(vide, cone, "Cone", true, 0.12f);
            Si(cone, vide, "Cone", false, 0.15f);
            EditorUtility.SetDirty(c);
        }

        public static void ControleurRodeur()
        {
            var style = Style("BowQuiver");
            var c = Socle("Rodeur_Jeu", style, out var loco, out var haut, out var vide);
            AjouterEmotes(c);   // roue à emotes (EmotesBuilder.cs)
            AjouterPortailArrivee(c);   // arrivée par un portail (EmotesBuilder.cs)
            var sm = c.layers[0].stateMachine;
            var b = GameBalance.Courant;
            Booleen(c, "Aiming");
            var drawClip = Clip(Ranged, "Ranged_Bow_Draw");
            var draw = Etat(haut, "Draw", drawClip, new Vector3(450, 0), drawClip != null ? drawClip.length / b.arcCharge : 1f);
            var tenue = Etat(haut, "Aiming_Idle", AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/WeaponStyles/Clips/Ranged_Bow_Aiming_Idle_Loop.anim"), new Vector3(650, 0));
            var lacher = Etat(haut, "Release", Clip(Ranged, "Ranged_Bow_Release"), new Vector3(650, 100), 1.3f);
            Si(vide, draw, "Aiming", true, 0.08f);
            var tt = draw.AddTransition(tenue); tt.hasExitTime = true; tt.exitTime = 0.98f; tt.duration = 0.05f; tt.AddCondition(AnimatorConditionMode.If, 0, "Aiming");
            if (System.Array.FindIndex(c.parameters, p => p.name == "Shoot") < 0) c.AddParameter("Shoot", AnimatorControllerParameterType.Trigger);
            foreach (var de in new[] { draw, tenue })
            {
                var t = de.AddTransition(lacher); t.hasExitTime = false; t.duration = 0.04f; t.AddCondition(AnimatorConditionMode.If, 0, "Shoot");
                Si(de, vide, "Aiming", false, 0.15f);
            }
            Sortie(lacher, vide, 0.9f);
            // Nuée : tir en cloche (corps entier).
            var haut1 = Etat(sm, "TirHaut", Clip(Ranged, "Ranged_Bow_Draw_Up"), new Vector3(650, 300), 2.2f);
            var haut2 = Etat(sm, "TirHautLacher", Clip(Ranged, "Ranged_Bow_Release_Up"), new Vector3(850, 300), 1.2f);
            Declencheur(c, sm, haut1, "TirHaut", null, 0f);
            Sortie(haut1, haut2, 0.95f, 0.05f);
            Sortie(haut2, loco, 0.9f);
            EditorUtility.SetDirty(c);
        }

        public static void ControleurAssassin()
        {
            var style = Style("DaggerCrossbow");
            var c = Socle("Assassin_Jeu", style, out var loco, out var haut, out var vide);
            AjouterEmotes(c);   // roue à emotes (EmotesBuilder.cs)
            AjouterPortailArrivee(c);   // arrivée par un portail (EmotesBuilder.cs)
            var sm = c.layers[0].stateMachine;
            // Marche discrète : seconde locomotion (Sneaking) tant que « Sneaking ».
            Booleen(c, "Sneaking");
            var discret = c.CreateBlendTreeInController("Discret", out BlendTree arbre, 0);
            arbre.blendParameter = "Speed";
            arbre.useAutomaticThresholds = false;
            arbre.AddChild(style.idle, 0f);
            arbre.AddChild(Boucle(style.sneak != null ? style.sneak : Clip(Avance, "Sneaking"), "Sneaking_Loop"), 0.35f);
            Si(loco, discret, "Sneaking", true, 0.2f);
            Si(discret, loco, "Sneaking", false, 0.2f);
            var stab = Clip(Melee, "Melee_1H_Attack_Stab");
            var b = GameBalance.Courant;
            var sStab = Etat(sm, "Stab", stab, new Vector3(650, 0), stab != null ? 0.5f / Mathf.Max(0.05f, b.dagueInstant) : 2f);
            Declencheur(c, sm, sStab, "Stab", loco, 0.8f, 0.05f);
            Declencheur(c, sm, Etat(sm, "Lancer", Clip(General, "Throw"), new Vector3(650, 80)), "Throw", loco, 0.85f, 0.08f);
            // Arbalète en main (couche haute) : visée tenue, tir.
            Booleen(c, "Crossbow");
            var vise = Etat(haut, "CrossbowAim", AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/WeaponStyles/Clips/Ranged_1H_Aiming_Loop.anim"), new Vector3(450, 0));
            var tir = Etat(haut, "CrossbowShoot", Clip(Ranged, "Ranged_1H_Shoot"), new Vector3(650, 0), 1.2f);
            Si(vide, vise, "Crossbow", true, 0.1f);
            if (System.Array.FindIndex(c.parameters, p => p.name == "Shoot") < 0) c.AddParameter("Shoot", AnimatorControllerParameterType.Trigger);
            var ts = vise.AddTransition(tir); ts.hasExitTime = false; ts.duration = 0.04f; ts.AddCondition(AnimatorConditionMode.If, 0, "Shoot");
            Sortie(tir, vise, 0.9f).AddCondition(AnimatorConditionMode.If, 0, "Crossbow");
            Sortie(tir, vide, 0.9f).AddCondition(AnimatorConditionMode.IfNot, 0, "Crossbow");
            Si(vise, vide, "Crossbow", false, 0.12f);
            EditorUtility.SetDirty(c);
        }
    }
}
