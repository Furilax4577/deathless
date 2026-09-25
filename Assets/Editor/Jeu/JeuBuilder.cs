using System.Collections.Generic;
using System.IO;
using System.Linq;
using Deathless.Jeu;
using Deathless.UI;
using Deathless.UI.Ecrans;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UIElements;

namespace Deathless.EditorTools
{
    /// Construit la version 0.1 jouable (menu Deathless > Jeu). Chaque étape est rejouable et met à jour les assets
    /// existants (GUID gardés) : réglages, catalogue des sons, contrôleurs d'animation, prefabs du héros et des
    /// squelettes, scène Assets/Scenes/Village.unity (décor du village + racine Jeu), cuisson du NavMesh.
    public static partial class JeuBuilder
    {
        const string Racine = "Assets/Jeu";
        const string ReglagesPath = "Assets/Jeu/Resources/GameBalance.asset";
        const string SonsPath = "Assets/Jeu/Audio/SonsCatalogue.asset";
        const string AnimDir = "Assets/Jeu/Animation";
        const string PrefabDir = "Assets/Jeu/Prefabs";
        const string MatDir = "Assets/Jeu/Materiaux";
        const string ScenePath = "Assets/Scenes/Village.unity";
        const string VillagePath = "Assets/Scenes/VillageBlockout.unity";

        const string AnimPack = "Assets/Art/KayKit/KayKit_Character_Animations_1.1/Animations/fbx/Rig_Medium/";
        const string Special = "Assets/VFX/SortieDeTerre/Rig_Medium_Special.fbx";
        const string LargePack = "Assets/Art/KayKit/KayKit_Skeletons_1.1_EXTRA/Animations/fbx/Rig_Large/";
        const string Skel = "Assets/Art/KayKit/KayKit_Skeletons_1.1_EXTRA/";
        const string KnightPath = "Assets/Art/KayKit/KayKit_Adventurers_2.0_FREE/Characters/fbx/Knight.fbx";

        [MenuItem("Deathless/Jeu/Tout construire (0.1)")]
        public static void Tout()
        {
            Reglages();
            ImporterSons();
            Controleurs();
            Prefabs();
            Scene();
            CuireNavMesh();
        }

        // ================================================================= Réglages

        [MenuItem("Deathless/Jeu/1. Réglages (GameBalance)")]
        public static GameBalance Reglages()
        {
            Dossier("Assets/Jeu/Resources");
            var b = AssetDatabase.LoadAssetAtPath<GameBalance>(ReglagesPath);
            if (b == null)
            {
                b = ScriptableObject.CreateInstance<GameBalance>();
                AssetDatabase.CreateAsset(b, ReglagesPath);
            }
            return b;
        }

        // ================================================================= Sons

        [System.Serializable] class SonJson { public string id, nom, categorie, usage, fichier, source, licence, statut; public string[] variantes; }
        [System.Serializable] class ListeSons { public SonJson[] e; }

        [MenuItem("Deathless/Jeu/2. Importer le catalogue des sons")]
        public static string ImporterSons()
        {
            Dossier("Assets/Jeu/Audio");
            string json = File.ReadAllText(Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Wiki/data/sons.json"));
            var liste = JsonUtility.FromJson<ListeSons>("{\"e\":" + json + "}");
            var cat = AssetDatabase.LoadAssetAtPath<SonsCatalogue>(SonsPath);
            if (cat == null) { cat = ScriptableObject.CreateInstance<SonsCatalogue>(); AssetDatabase.CreateAsset(cat, SonsPath); }
            cat.entrees.Clear();
            var absents = new List<string>();
            foreach (var s in liste.e)
            {
                if (s == null || string.IsNullOrEmpty(s.id) || s.statut == "a_creer" || string.IsNullOrEmpty(s.fichier)) continue;
                var fichiers = s.variantes != null && s.variantes.Length > 0 ? s.variantes : new[] { s.fichier };
                var clips = new List<AudioClip>();
                foreach (var f in fichiers)
                {
                    var c = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/" + f);
                    if (c != null) clips.Add(c); else absents.Add(f);
                }
                if (clips.Count == 0) continue;
                cat.entrees.Add(new SonsCatalogue.Entree { id = s.id, nom = s.nom, statut = s.statut, boucle = s.fichier.Contains("_loop"), clips = clips.ToArray() });
            }
            EditorUtility.SetDirty(cat);
            AssetDatabase.SaveAssets();
            string r = "Catalogue des sons : " + cat.entrees.Count + " sons" + (absents.Count > 0 ? ", fichiers absents : " + string.Join(", ", absents) : "");
            Debug.Log(r);
            return r;
        }

        // ================================================================= Animation

        static AnimationClip Clip(string fbx, string nom)
        {
            foreach (var o in AssetDatabase.LoadAllAssetsAtPath(fbx))
                if (o is AnimationClip c && c.name == nom) return c;
            Debug.LogError("Clip introuvable : " + nom + " dans " + fbx);
            return null;
        }

        /// Copie bouclée d'un clip (le pack n'est jamais modifié).
        static AnimationClip Boucle(AnimationClip source, string nom)
        {
            string path = AnimDir + "/Clips/" + nom + ".anim";
            Dossier(AnimDir + "/Clips");
            var existant = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            var copie = Object.Instantiate(source);
            copie.name = nom;
            var s = AnimationUtility.GetAnimationClipSettings(copie);
            s.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(copie, s);
            if (existant != null) { EditorUtility.CopySerialized(copie, existant); Object.DestroyImmediate(copie); return existant; }
            AssetDatabase.CreateAsset(copie, path);
            return copie;
        }

        /// Clip Rig_Medium rejoué sur le rig Large (Golem) : chemins Rig_Medium/… → Rig_Large/…, rotations seulement.
        static AnimationClip VersLarge(AnimationClip source, string nom)
        {
            string path = AnimDir + "/Clips/" + nom + ".anim";
            Dossier(AnimDir + "/Clips");
            var clip = new AnimationClip { name = nom, frameRate = source.frameRate };
            foreach (var bnd in AnimationUtility.GetCurveBindings(source))
            {
                if (!bnd.propertyName.StartsWith("m_LocalRotation")) continue;
                var courbe = AnimationUtility.GetEditorCurve(source, bnd);
                var nb = bnd;
                nb.path = bnd.path.Replace("Rig_Medium", "Rig_Large");
                AnimationUtility.SetEditorCurve(clip, nb, courbe);
            }
            clip.EnsureQuaternionContinuity();
            var existant = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (existant != null) { EditorUtility.CopySerialized(clip, existant); Object.DestroyImmediate(clip); return existant; }
            AssetDatabase.CreateAsset(clip, path);
            return clip;
        }

        static AnimatorState Etat(AnimatorStateMachine sm, string nom, Motion m, Vector3 pos, float vitesse = 1f)
        {
            var s = sm.AddState(nom, pos);
            s.motion = m;
            s.speed = vitesse;
            return s;
        }

        static AnimatorStateTransition DeNimporte(AnimatorStateMachine sm, AnimatorState cible, float duree = 0.1f)
        {
            var t = sm.AddAnyStateTransition(cible);
            t.duration = duree;
            t.hasExitTime = false;
            t.canTransitionToSelf = false;
            return t;
        }

        static AnimatorStateTransition Sortie(AnimatorState de, AnimatorState vers, float exitTime, float duree = 0.15f)
        {
            var t = de.AddTransition(vers);
            t.hasExitTime = true;
            t.exitTime = exitTime;
            t.duration = duree;
            return t;
        }

        static AnimatorController NouveauControleur(string path)
        {
            Dossier(Path.GetDirectoryName(path).Replace('\\', '/'));
            if (AssetDatabase.LoadAssetAtPath<AnimatorController>(path) != null) AssetDatabase.DeleteAsset(path);
            return AnimatorController.CreateAnimatorControllerAtPath(path);
        }

        [MenuItem("Deathless/Jeu/3. Contrôleurs d'animation")]
        public static void Controleurs()
        {
            ControleurPaladin();
            ControleurSquelette();
            ControleurGolem();
            AssetDatabase.SaveAssets();
        }

        public static AnimatorController ControleurPaladin()
        {
            var style = AssetDatabase.LoadAssetAtPath<WeaponStyle>("Assets/WeaponStyles/SwordShield.asset");
            var c = NouveauControleur(AnimDir + "/Paladin_Jeu.controller");
            foreach (var p in new[] { "Speed" }) c.AddParameter(p, AnimatorControllerParameterType.Float);
            foreach (var p in new[] { "Grounded", "Guard", "Dead" }) c.AddParameter(p, AnimatorControllerParameterType.Bool);
            foreach (var p in new[] { "Attack1", "Attack2", "Dodge", "Jump", "Charge", "Heal", "BlockHit", "Hit", "Respawn" }) c.AddParameter(p, AnimatorControllerParameterType.Trigger);
            var b = GameBalance.Courant;

            var sm = c.layers[0].stateMachine;
            // Le style donne la locomotion, les attaques, la garde (règle : arme / style → animation).
            var loco = c.CreateBlendTreeInController("Locomotion", out BlendTree arbre, 0);
            arbre.blendParameter = "Speed";
            arbre.useAutomaticThresholds = false;
            arbre.AddChild(style.idle, 0f);
            arbre.AddChild(style.walk, 0.35f);
            arbre.AddChild(style.run, 1f);
            arbre.AddChild(style.run, 1.6f);
            var enfants = arbre.children; enfants[3].timeScale = 1.35f; arbre.children = enfants;
            sm.defaultState = loco;
            loco.writeDefaultValues = true;

            var a1 = Etat(sm, "Attaque1", style.attacks[0], new Vector3(400, 0), b.epeeVitesseClip);
            var a2 = Etat(sm, "Attaque2", Clip(AnimPack + "Rig_Medium_CombatMelee.fbx", "Melee_1H_Attack_Slice_Horizontal"), new Vector3(400, 60), b.epeeVitesseClip * 1.25f);
            DeNimporte(sm, a1, 0.05f).AddCondition(AnimatorConditionMode.If, 0, "Attack1");
            DeNimporte(sm, a2, 0.05f).AddCondition(AnimatorConditionMode.If, 0, "Attack2");
            Sortie(a1, loco, 0.8f); Sortie(a2, loco, 0.75f);

            var dodgeClip = Clip(AnimPack + "Rig_Medium_MovementAdvanced.fbx", "Dodge_Forward");
            var dodge = Etat(sm, "Esquive", dodgeClip, new Vector3(400, 120), dodgeClip != null ? dodgeClip.length / (b.esquiveDuree + 0.15f) : 1f);
            DeNimporte(sm, dodge, 0.05f).AddCondition(AnimatorConditionMode.If, 0, "Dodge");
            Sortie(dodge, loco, 0.9f);

            var j1 = Etat(sm, "SautDepart", Clip(AnimPack + "Rig_Medium_MovementBasic.fbx", "Jump_Start"), new Vector3(400, 180), 1.4f);
            var j2 = Etat(sm, "SautAir", Clip(AnimPack + "Rig_Medium_MovementBasic.fbx", "Jump_Idle"), new Vector3(600, 180));
            var j3 = Etat(sm, "SautReception", Clip(AnimPack + "Rig_Medium_MovementBasic.fbx", "Jump_Land"), new Vector3(800, 180), 1.5f);
            DeNimporte(sm, j1, 0.05f).AddCondition(AnimatorConditionMode.If, 0, "Jump");
            Sortie(j1, j2, 0.9f, 0.05f);
            var tj = j1.AddTransition(j3); tj.hasExitTime = true; tj.exitTime = 0.6f; tj.duration = 0.05f; tj.AddCondition(AnimatorConditionMode.If, 0, "Grounded");
            var tl = j2.AddTransition(j3); tl.hasExitTime = false; tl.duration = 0.05f; tl.AddCondition(AnimatorConditionMode.If, 0, "Grounded");
            Sortie(j3, loco, 0.6f);

            var charge = Etat(sm, "Charge", Clip(AnimPack + "Rig_Medium_CombatMelee.fbx", "Melee_Block_Attack"), new Vector3(400, 240), 1.2f);
            DeNimporte(sm, charge, 0.05f).AddCondition(AnimatorConditionMode.If, 0, "Charge");
            Sortie(charge, loco, 0.9f);

            var soinClip = Clip(AnimPack + "Rig_Medium_CombatRanged.fbx", "Ranged_Magic_Raise");
            var soin = Etat(sm, "Soin", soinClip, new Vector3(400, 300));
            DeNimporte(sm, soin, 0.1f).AddCondition(AnimatorConditionMode.If, 0, "Heal");
            Sortie(soin, loco, soinClip != null ? Mathf.Clamp01((b.soinIncantation + 0.5f) / soinClip.length) : 0.5f, 0.25f);

            var mort = Etat(sm, "Mort", Clip(AnimPack + "Rig_Medium_General.fbx", "Death_A"), new Vector3(400, 360));
            DeNimporte(sm, mort, 0.1f).AddCondition(AnimatorConditionMode.If, 0, "Dead");
            var rev = mort.AddTransition(loco); rev.hasExitTime = false; rev.duration = 0.1f; rev.AddCondition(AnimatorConditionMode.If, 0, "Respawn");

            // Couche « haut du corps » (poids piloté par Heros) : garde, coup bloqué, touché.
            var masque = MasqueHautDuCorps();
            c.AddLayer("HautDuCorps");
            var couches = c.layers;
            couches[1].avatarMask = masque;
            couches[1].defaultWeight = 0f;
            couches[1].blendingMode = AnimatorLayerBlendingMode.Override;
            c.layers = couches;
            var sh = c.layers[1].stateMachine;
            var vide = sh.AddState("Vide", new Vector3(250, 0));
            vide.writeDefaultValues = false;
            sh.defaultState = vide;
            var garde = Etat(sh, "Garde", style.guard, new Vector3(450, 0));
            var tg = vide.AddTransition(garde); tg.hasExitTime = false; tg.duration = 0.1f; tg.AddCondition(AnimatorConditionMode.If, 0, "Guard");
            var tv = garde.AddTransition(vide); tv.hasExitTime = false; tv.duration = 0.1f; tv.AddCondition(AnimatorConditionMode.IfNot, 0, "Guard");
            var bloque = Etat(sh, "CoupBloque", style.guardHit, new Vector3(450, 80), 1.6f);
            var tb = sh.AddAnyStateTransition(bloque); tb.duration = 0.05f; tb.hasExitTime = false; tb.AddCondition(AnimatorConditionMode.If, 0, "BlockHit");
            Sortie(bloque, garde, 0.8f).AddCondition(AnimatorConditionMode.If, 0, "Guard");
            Sortie(bloque, vide, 0.8f).AddCondition(AnimatorConditionMode.IfNot, 0, "Guard");
            var touche = Etat(sh, "Touche", Clip(AnimPack + "Rig_Medium_General.fbx", "Hit_A"), new Vector3(450, 160), 1.3f);
            var th = sh.AddAnyStateTransition(touche); th.duration = 0.05f; th.hasExitTime = false; th.AddCondition(AnimatorConditionMode.If, 0, "Hit");
            Sortie(touche, vide, 0.85f);
            EditorUtility.SetDirty(c);
            return c;
        }

        static AvatarMask MasqueHautDuCorps()
        {
            string path = AnimDir + "/HautDuCorps.mask";
            var m = AssetDatabase.LoadAssetAtPath<AvatarMask>(path);
            if (m == null) { m = new AvatarMask(); AssetDatabase.CreateAsset(m, path); }
            var knight = AssetDatabase.LoadAssetAtPath<GameObject>(KnightPath);
            var chemins = new List<string>();
            foreach (var t in knight.GetComponentsInChildren<Transform>(true))
            {
                if (t == knight.transform) continue;
                chemins.Add(AnimationUtility.CalculateTransformPath(t, knight.transform));
            }
            m.transformCount = chemins.Count;
            for (int i = 0; i < chemins.Count; i++)
            {
                m.SetTransformPath(i, chemins[i]);
                m.SetTransformActive(i, chemins[i].Contains("/chest"));
            }
            EditorUtility.SetDirty(m);
            return m;
        }

        public static AnimatorController ControleurSquelette()
        {
            var c = NouveauControleur(AnimDir + "/Squelette_Jeu.controller");
            c.AddParameter("Speed", AnimatorControllerParameterType.Float);
            c.AddParameter("VitesseAttaque", AnimatorControllerParameterType.Float);
            foreach (var p in new[] { "Stun", "Dead" }) c.AddParameter(p, AnimatorControllerParameterType.Bool);
            foreach (var p in new[] { "Attack", "Hit", "Shoot", "Cast" }) c.AddParameter(p, AnimatorControllerParameterType.Trigger);
            var ps = c.parameters; foreach (var p in ps) if (p.name == "VitesseAttaque") p.defaultFloat = 1f; c.parameters = ps;
            var b = GameBalance.Courant;
            var sm = c.layers[0].stateMachine;

            var sortie = Etat(sm, "SortieDeTerre", Clip(Special, "Skeletons_Spawn_Ground"), new Vector3(0, -80), b.vitesseSortieDeTerre);
            sm.defaultState = sortie;
            var loco = c.CreateBlendTreeInController("Locomotion", out BlendTree arbre, 0);
            arbre.blendParameter = "Speed";
            arbre.useAutomaticThresholds = false;
            arbre.AddChild(Boucle(Clip(Special, "Skeletons_Idle"), "Skeletons_Idle_Loop"), 0f);
            arbre.AddChild(Clip(AnimPack + "Rig_Medium_MovementBasic.fbx", "Walking_A"), 0.5f);
            var enf = arbre.children; enf[1].timeScale = 1.35f; arbre.children = enf;
            Sortie(sortie, loco, 0.98f, 0.1f);

            var attaque = Etat(sm, "Attaque", Clip(AnimPack + "Rig_Medium_CombatMelee.fbx", "Melee_1H_Attack_Chop"), new Vector3(400, 0));
            attaque.speedParameterActive = true; attaque.speedParameter = "VitesseAttaque";
            DeNimporte(sm, attaque, 0.08f).AddCondition(AnimatorConditionMode.If, 0, "Attack");
            Sortie(attaque, loco, 0.95f);
            var touche = Etat(sm, "Touche", Clip(AnimPack + "Rig_Medium_General.fbx", "Hit_A"), new Vector3(400, 60), 1.2f);
            var tt = DeNimporte(sm, touche, 0.05f); tt.AddCondition(AnimatorConditionMode.If, 0, "Hit"); tt.AddCondition(AnimatorConditionMode.IfNot, 0, "Dead");
            Sortie(touche, loco, 0.9f);
            var etourdi = Etat(sm, "Etourdi", Clip(AnimPack + "Rig_Medium_General.fbx", "Hit_B"), new Vector3(400, 120), 0.6f);
            var te = DeNimporte(sm, etourdi, 0.1f); te.AddCondition(AnimatorConditionMode.If, 0, "Stun"); te.AddCondition(AnimatorConditionMode.IfNot, 0, "Dead");
            var tfin = etourdi.AddTransition(loco); tfin.hasExitTime = false; tfin.duration = 0.15f; tfin.AddCondition(AnimatorConditionMode.IfNot, 0, "Stun");
            // Touche ne doit pas couper l'étourdissement : Touche revient vers Etourdi si Stun.
            var tts = touche.AddTransition(etourdi); tts.hasExitTime = true; tts.exitTime = 0.9f; tts.duration = 0.1f; tts.AddCondition(AnimatorConditionMode.If, 0, "Stun");
            var tir = Etat(sm, "Tir", Clip(AnimPack + "Rig_Medium_CombatRanged.fbx", "Ranged_Magic_Shoot"), new Vector3(400, 180));
            DeNimporte(sm, tir, 0.08f).AddCondition(AnimatorConditionMode.If, 0, "Shoot");
            Sortie(tir, loco, 0.9f);
            var invoc = Etat(sm, "Invocation", Clip(AnimPack + "Rig_Medium_CombatRanged.fbx", "Ranged_Magic_Summon"), new Vector3(400, 240), 1.3f);
            DeNimporte(sm, invoc, 0.1f).AddCondition(AnimatorConditionMode.If, 0, "Cast");
            Sortie(invoc, loco, 0.9f);
            var mort = Etat(sm, "Mort", Clip(Special, "Skeletons_Death"), new Vector3(400, 300), 1.3f);
            DeNimporte(sm, mort, 0.08f).AddCondition(AnimatorConditionMode.If, 0, "Dead");
            EditorUtility.SetDirty(c);
            return c;
        }

        public static AnimatorController ControleurGolem()
        {
            var c = NouveauControleur(AnimDir + "/Golem_Jeu.controller");
            c.AddParameter("Speed", AnimatorControllerParameterType.Float);
            c.AddParameter("VitesseAttaque", AnimatorControllerParameterType.Float);
            foreach (var p in new[] { "Stun", "Dead" }) c.AddParameter(p, AnimatorControllerParameterType.Bool);
            foreach (var p in new[] { "Attack", "Hit" }) c.AddParameter(p, AnimatorControllerParameterType.Trigger);
            var ps = c.parameters; foreach (var p in ps) if (p.name == "VitesseAttaque") p.defaultFloat = 1f; c.parameters = ps;
            var sm = c.layers[0].stateMachine;
            var sortie = Etat(sm, "SortieDeTerre", VersLarge(Clip(Special, "Skeletons_Spawn_Ground"), "Golem_SortieDeTerre"), new Vector3(0, -80), GameBalance.Courant.vitesseSortieDeTerre);
            sm.defaultState = sortie;
            var loco = c.CreateBlendTreeInController("Locomotion", out BlendTree arbre, 0);
            arbre.blendParameter = "Speed";
            arbre.useAutomaticThresholds = false;
            arbre.AddChild(Clip(LargePack + "Rig_Large_General.fbx", "Idle_A"), 0f);
            arbre.AddChild(Clip(LargePack + "Rig_Large_MovementBasic.fbx", "Walking_A"), 0.5f);
            Sortie(sortie, loco, 0.98f, 0.1f);
            var attaque = Etat(sm, "Attaque", VersLarge(Clip(AnimPack + "Rig_Medium_CombatMelee.fbx", "Melee_2H_Attack_Chop"), "Golem_Attaque"), new Vector3(400, 0));
            attaque.speedParameterActive = true; attaque.speedParameter = "VitesseAttaque";
            DeNimporte(sm, attaque, 0.1f).AddCondition(AnimatorConditionMode.If, 0, "Attack");
            Sortie(attaque, loco, 0.95f);
            var touche = Etat(sm, "Touche", Clip(LargePack + "Rig_Large_General.fbx", "Hit_A"), new Vector3(400, 60));
            var tt = DeNimporte(sm, touche, 0.05f); tt.AddCondition(AnimatorConditionMode.If, 0, "Stun"); tt.AddCondition(AnimatorConditionMode.IfNot, 0, "Dead");
            var tfin = touche.AddTransition(loco); tfin.hasExitTime = false; tfin.duration = 0.15f; tfin.AddCondition(AnimatorConditionMode.IfNot, 0, "Stun");
            var mort = Etat(sm, "Mort", Clip(LargePack + "Rig_Large_General.fbx", "Death_A"), new Vector3(400, 120));
            DeNimporte(sm, mort, 0.08f).AddCondition(AnimatorConditionMode.If, 0, "Dead");
            EditorUtility.SetDirty(c);
            return c;
        }

        // ================================================================= Prefabs

        [MenuItem("Deathless/Jeu/4. Prefabs (héros, squelettes)")]
        public static void Prefabs()
        {
            Dossier(PrefabDir);
            Dossier(MatDir);
            PrefabHeros();
            var ennemi = MateriauEnnemi();
            PrefabSquelette("Squelette_Sbire", Skel + "characters/fbx/Skeleton_Minion.fbx", new[] { "Skeleton_Blade" }, null, typeof(Squelette), "Squelette_Jeu", ennemi, null, 0.4f, 1.8f, 0.58f);
            PrefabSquelette("Squelette_Guerrier", Skel + "characters/fbx/Skeleton_Warrior.fbx", new[] { "Skeleton_Axe" }, "Skeleton_Shield_Small_A", typeof(Squelette), "Squelette_Jeu", ennemi, null, 0.45f, 1.9f, 0.58f);
            PrefabSquelette("Squelette_Golem", Skel + "characters/fbx/Skeleton_Golem.fbx", new[] { "Skeleton_Golem_Axe_Large" }, null, typeof(Golem), "Golem_Jeu", ennemi, null, 0.9f, 3.6f, 0.9f);
            PrefabSquelette("Squelette_Necromancien", Skel + "characters/fbx/Necromancer.fbx", new[] { "Skeleton_Scythe" }, null, typeof(Necromancien), "Squelette_Jeu", ennemi, MateriauNecromancien(), 0.45f, 2.0f, 0.58f);
            AssetDatabase.SaveAssets();
        }

        static Material MateriauEnnemi()
        {
            string path = MatDir + "/Squelette_Ennemi.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                var src = AssetDatabase.LoadAssetAtPath<Material>("Assets/VFX/SortieDeTerre/KayKit_Skeleton_Enemy.mat");
                m = new Material(src);
                AssetDatabase.CreateAsset(m, path);
            }
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(Skel + "textures/skeleton_texture_A.png");
            if (tex != null) { if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", tex); if (m.HasProperty("_MainTex")) m.SetTexture("_MainTex", tex); }
            // Os naturel : pas de la lueur rouge des ennemis de Relic (elle rendait les squelettes roses) ; les yeux gardent
            // leur matériau « Glow » d'origine.
            m.DisableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", Color.black);
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.EmissiveIsBlack;
            EditorUtility.SetDirty(m);
            return m;
        }

        /// Yeux du Nécromancien en vert Nyxessa (décision utilisateur) : copie du matériau des ennemis avec une texture
        /// dont les pixels cyan des yeux sont remplacés par le vert Nyxessa, et une carte d'émission limitée à ces pixels.
        /// La texture partagée des squelettes n'est pas modifiée.
        public static Material MateriauNecromancien()
        {
            string path = MatDir + "/Necromancien.mat";
            var baseMat = MateriauEnnemi();
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(baseMat); AssetDatabase.CreateAsset(m, path); }
            else m.CopyPropertiesFromMaterial(baseMat);
            string texPath = Skel + "textures/skeleton_texture_A.png";
            var imp = (TextureImporter)AssetImporter.GetAtPath(texPath);
            bool lisible = imp.isReadable;
            if (!lisible) { imp.isReadable = true; imp.SaveAndReimport(); }
            var src = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
            var px = src.GetPixels();
            var emis = new Color[px.Length];
            Color vert = VfxPalette.Couleur(VfxTheme.Nyxessa, VfxRole.Coeur, new Color(0.62f, 0.91f, 0.44f));
            Color vif = VfxPalette.Couleur(VfxTheme.Nyxessa, VfxRole.Vif, new Color(0.25f, 0.68f, 0.35f));
            int n = 0;
            for (int i = 0; i < px.Length; i++)
            {
                Color.RGBToHSV(px[i], out float h, out float s, out float v);
                bool cyan = h > 0.42f && h < 0.58f && s > 0.35f && v > 0.45f;
                if (cyan) { px[i] = Color.Lerp(vif, vert, v); emis[i] = vert; n++; }
                else emis[i] = Color.black;
            }
            if (!lisible) { imp.isReadable = false; imp.SaveAndReimport(); }
            var tex = new Texture2D(src.width, src.height, TextureFormat.RGBA32, false); tex.SetPixels(px); tex.Apply();
            var tEm = new Texture2D(src.width, src.height, TextureFormat.RGBA32, false); tEm.SetPixels(emis); tEm.Apply();
            File.WriteAllBytes(MatDir + "/Necromancien_Base.png", tex.EncodeToPNG());
            File.WriteAllBytes(MatDir + "/Necromancien_Emission.png", tEm.EncodeToPNG());
            AssetDatabase.ImportAsset(MatDir + "/Necromancien_Base.png");
            AssetDatabase.ImportAsset(MatDir + "/Necromancien_Emission.png");
            foreach (var p in new[] { MatDir + "/Necromancien_Base.png", MatDir + "/Necromancien_Emission.png" })
            {
                var ti = (TextureImporter)AssetImporter.GetAtPath(p);
                ti.mipmapEnabled = false; ti.filterMode = FilterMode.Bilinear; ti.textureCompression = TextureImporterCompression.Uncompressed;
                ti.SaveAndReimport();
            }
            var b = AssetDatabase.LoadAssetAtPath<Texture2D>(MatDir + "/Necromancien_Base.png");
            var e = AssetDatabase.LoadAssetAtPath<Texture2D>(MatDir + "/Necromancien_Emission.png");
            if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", b);
            if (m.HasProperty("_MainTex")) m.SetTexture("_MainTex", b);
            m.EnableKeyword("_EMISSION");
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            m.SetTexture("_EmissionMap", e);
            m.SetColor("_EmissionColor", Color.white * 2.2f);
            EditorUtility.SetDirty(m);
            Debug.Log("Nécromancien : " + n + " pixels d'yeux recolorés en vert Nyxessa");
            return m;
        }

        static GameObject Arme(string nom) => AssetDatabase.LoadAssetAtPath<GameObject>(Skel + "assets/fbx(unity)/" + nom + ".fbx");

        static void PrefabSquelette(string nom, string modelePath, string[] armesDroite, string armeGauche, System.Type ia, string controleur, Material ennemi, Material special, float rayon, float hauteur, float instantCoup)
        {
            var b = GameBalance.Courant;
            var racine = new GameObject(nom);
            var modeleAsset = AssetDatabase.LoadAssetAtPath<GameObject>(modelePath);
            var modele = (GameObject)PrefabUtility.InstantiatePrefab(modeleAsset);
            PrefabUtility.UnpackPrefabInstance(modele, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            modele.name = "Modele";
            modele.transform.SetParent(racine.transform, false);
            float echelle = ia == typeof(Golem) ? b.golemEchelle : ia == typeof(Necromancien) ? b.necroEchelle : b.echellePersonnages;
            modele.transform.localScale = Vector3.one * echelle;
            foreach (var r in modele.GetComponentsInChildren<Renderer>())
            {
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                    if (mats[i] == null || !mats[i].name.Contains("Glow")) mats[i] = special != null ? special : ennemi;
                r.sharedMaterials = mats;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            }
            foreach (var a in armesDroite)
            {
                var slot = MannequinEquip.Trouver(modele.transform, "handslot.r");
                var arme = Arme(a);
                if (slot != null && arme != null) { var go = (GameObject)PrefabUtility.InstantiatePrefab(arme, slot); PrefabUtility.UnpackPrefabInstance(go, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction); ArmeMat(go, ennemi); }
            }
            if (armeGauche != null)
            {
                var slot = MannequinEquip.Trouver(modele.transform, "handslot.l");
                var arme = Arme(armeGauche);
                if (slot != null && arme != null) { var go = (GameObject)PrefabUtility.InstantiatePrefab(arme, slot); PrefabUtility.UnpackPrefabInstance(go, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction); ArmeMat(go, ennemi); }
            }
            var anim = modele.GetComponent<Animator>(); if (anim == null) anim = modele.AddComponent<Animator>();
            anim.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(AnimDir + "/" + controleur + ".controller");
            anim.applyRootMotion = false;
            anim.cullingMode = AnimatorCullingMode.CullUpdateTransforms;

            var agent = racine.AddComponent<NavMeshAgent>();
            agent.radius = rayon;
            agent.height = hauteur;
            agent.baseOffset = 0f;
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.MedQualityObstacleAvoidance;
            agent.autoBraking = true;
            var col = racine.AddComponent<CapsuleCollider>();
            col.radius = rayon; col.height = hauteur; col.center = Vector3.up * hauteur * 0.5f;
            var body = racine.AddComponent<Rigidbody>(); body.isKinematic = true;
            racine.AddComponent<Sante>().equipe = Equipe.Ennemis;
            var sq = (Squelette)racine.AddComponent(ia);
            sq.modele = modele.transform;
            sq.animator = anim;
            sq.instantCoupClip = instantCoup;
            var clip = Clip(Special, "Skeletons_Spawn_Ground");
            sq.dureeSortie = clip != null ? clip.length / Mathf.Max(0.1f, b.vitesseSortieDeTerre) : 1.6f;
            PrefabUtility.SaveAsPrefabAsset(racine, PrefabDir + "/" + nom + ".prefab");
            Object.DestroyImmediate(racine);
        }

        static void ArmeMat(GameObject arme, Material m)
        {
            foreach (var r in arme.GetComponentsInChildren<Renderer>())
            {
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++) mats[i] = m;
                r.sharedMaterials = mats;
            }
        }

        static void PrefabHeros() => PrefabClasse("paladin");

        // ================================================================= Scène

        static Vector3 Polar(float az, float r) => new Vector3(Mathf.Sin(az * Mathf.Deg2Rad) * r, 0f, Mathf.Cos(az * Mathf.Deg2Rad) * r);

        [MenuItem("Deathless/Jeu/5. Scène Village (jouable)")]
        public static void Scene()
        {
            if (!File.Exists(ScenePath))
            {
                var v = EditorSceneManager.OpenScene(VillagePath, OpenSceneMode.Single);
                EditorSceneManager.SaveScene(v, ScenePath, true);
            }
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var ancien = GameObject.Find("Jeu");
            if (ancien != null) Object.DestroyImmediate(ancien);
            var village = GameObject.Find("VillageBlockout").transform;
            var b = Reglages();
            var jeu = new GameObject("Jeu");

            // Nyxessa : santé et défense sur l'instance de la scène (le prefab de référence n'est pas modifié).
            var nyxGo = village.Find("Nexus/GemmeNyxessa").gameObject;
            var sante = nyxGo.GetComponent<Sante>(); if (sante == null) sante = nyxGo.AddComponent<Sante>();
            sante.equipe = Equipe.Relique;
            if (nyxGo.GetComponent<DefenseNyxessa>() == null) nyxGo.AddComponent<DefenseNyxessa>();

            // Effets partagés.
            var fx = jeu.AddComponent<EffetsJeu>();
            fx.gemmes = AssetDatabase.LoadAssetAtPath<Material>("Assets/VFX/_RelicCommun/PortalVoxel.mat");
            fx.terre = AssetDatabase.LoadAssetAtPath<Material>("Assets/VFX/_RelicCommun/FireBurst.mat");
            fx.formeCrane = AssetDatabase.LoadAssetAtPath<GemShape>("Assets/VFX/MissileMagique/SkullGemShape.asset");
            fx.prefabChargeBelier = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/VFX/ChargeBelier/ChargeBelier.prefab");
            fx.prefabAuraSoin = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/VFX/AuraSoin/AuraSoin.prefab");
            fx.prefabOndeGolem = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/VFX/OndeDeChoc/OndeDeChoc_SautPercutant.prefab");
            RemplirEffetsClasses(fx);
            var mort = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/VFX/MortAllie/MortAllie.prefab");
            if (mort != null) PrefabUtility.InstantiatePrefab(mort, jeu.transform);

            // Audio.
            var audio = jeu.AddComponent<AudioBank>();
            audio.catalogue = AssetDatabase.LoadAssetAtPath<SonsCatalogue>(SonsPath);

            // Caméra.
            foreach (var cam in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None)) Object.DestroyImmediate(cam.gameObject);
            var camGo = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            camGo.tag = "MainCamera";
            camGo.transform.SetParent(jeu.transform, false);
            var camera = camGo.GetComponent<Camera>();
            camera.fieldOfView = 60f; camera.nearClipPlane = 0.1f; camera.farClipPlane = 400f;
            var urp = camGo.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
            urp.renderPostProcessing = true;
            var camEp = camGo.AddComponent<CameraEpaule>();

            // Points de réapparition autour de Nyxessa (r = 10 m, hors du plateau, des maisons et de la place du portail).
            var points = new GameObject("PointsReapparition").transform;
            points.SetParent(jeu.transform, false);
            var liste = new List<Transform>();
            foreach (float az in new[] { 180f, 130f, 230f, 10f, 290f, 350f })
            {
                var p = new GameObject("Reapparition_" + az).transform;
                p.SetParent(points, false);
                p.position = Polar(az, 10.5f);
                p.rotation = Quaternion.LookRotation(-p.position.normalized);
                liste.Add(p);
            }
            var depart = new GameObject("PointDepart").transform;
            depart.SetParent(jeu.transform, false);
            depart.position = Polar(180f, 9.5f);
            depart.rotation = Quaternion.LookRotation(-depart.position.normalized);

            // Autorité de la partie.
            var partie = jeu.AddComponent<Partie>();
            partie.reglages = b;
            partie.prefabHeros = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabDir + "/Heros_Paladin.prefab");
            partie.nyxessa = sante;
            partie.pointsReapparition = liste.ToArray();
            partie.pointDepart = depart;
            partie.cameraJeu = camEp;

            // Vagues et zones d'apparition sur les clairières.
            var dv = jeu.AddComponent<DirecteurVagues>();
            var spawns = village.Find("Foret/Spawns");
            dv.clairieres = new[] { spawns.Find("Spawn_Nord"), spawns.Find("Spawn_SudEst"), spawns.Find("Spawn_SudOuest") };
            var zonesRacine = new GameObject("Zones").transform;
            zonesRacine.SetParent(jeu.transform, false);
            var zonePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/VFX/ZoneApparition/ZoneApparition.prefab");
            dv.zones = new ZoneApparition[3];
            for (int i = 0; i < 3; i++)
            {
                if (zonePrefab == null) break;
                var z = (GameObject)PrefabUtility.InstantiatePrefab(zonePrefab, zonesRacine);
                z.name = "Zone_" + dv.clairieres[i].name.Replace("Spawn_", "");
                z.transform.position = dv.clairieres[i].position;
                z.transform.rotation = dv.clairieres[i].rotation;
                dv.zones[i] = z.GetComponent<ZoneApparition>();
            }
            var ennemis = new GameObject("Ennemis").transform;
            ennemis.SetParent(jeu.transform, false);
            dv.conteneur = ennemis;
            dv.prefabSbire = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabDir + "/Squelette_Sbire.prefab");
            dv.prefabGuerrier = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabDir + "/Squelette_Guerrier.prefab");
            dv.prefabGolem = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabDir + "/Squelette_Golem.prefab");
            dv.prefabNecromancien = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabDir + "/Squelette_Necromancien.prefab");

            // Vue du cycle (ambiance du village, portail, musique).
            var vue = jeu.AddComponent<VueCycle>();
            vue.cycle = village.GetComponentInChildren<CycleJourNuit>(true);
            vue.portail = village.GetComponentInChildren<PortalVisual>(true);

            // Interface : écrans de l'agent ui (NavigateurEcrans) branchés sur HudPresenter.
            var actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/Input/DeathlessControls.inputactions");
            var es = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            es.transform.SetParent(jeu.transform, false);
            Deathless.UI.EditorTools.DeathlessUISetup.AssignUIModule(es.GetComponent<InputSystemUIInputModule>(), actions);
            var panel = AssetDatabase.LoadAssetAtPath<PanelSettings>("Assets/UI/PanelSettings/DeathlessPanel.asset");
            var uiGo = new GameObject("UI");
            uiGo.transform.SetParent(jeu.transform, false);
            var doc = uiGo.AddComponent<UIDocument>();
            doc.panelSettings = panel;
            doc.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(Racine + "/UI/Jeu.uxml");
            uiGo.AddComponent<UIScale>().panels = new[] { panel };
            var hud = jeu.AddComponent<HudPresenter>();
            var nav = uiGo.AddComponent<NavigateurEcrans>();
            nav.actions = actions;
            VisualTreeAsset Uxml(string n) => AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/Screens/" + n + "/" + n + ".uxml");
            nav.menuPrincipal = Uxml("MenuPrincipal");
            nav.options = Uxml("Options");
            nav.credits = Uxml("Credits");
            nav.hud = Uxml("Hud");
            nav.pause = Uxml("Pause");
            nav.score = Uxml("Score");
            nav.choixClasse = Uxml("ChoixClasse");

            // NavMesh (géométrie : colliders physiques ; les feuillages ne bloquent pas).
            var navGo = new GameObject("NavMesh");
            navGo.transform.SetParent(jeu.transform, false);
            var surf = navGo.AddComponent<NavMeshSurface>();
            surf.collectObjects = CollectObjects.All;
            surf.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            surf.layerMask = ~0;

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            // Build : Village en premier.
            var scenes = EditorBuildSettings.scenes.Where(s => s.path != ScenePath).ToList();
            scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
            Debug.Log("Scène Village construite");
        }

        [MenuItem("Deathless/Jeu/6. Cuire le NavMesh")]
        public static string CuireNavMesh()
        {
            var surf = Object.FindAnyObjectByType<NavMeshSurface>();
            if (surf == null) return "pas de NavMeshSurface";
            // Réglages de l'agent par défaut (Humanoid) : rayon 0,4 m, hauteur 1,8 m, marche 0,35 m, pente 45°.
            var st = NavMesh.GetSettingsByID(surf.agentTypeID);
            surf.overrideVoxelSize = true;
            surf.voxelSize = 0.13f;
            surf.BuildNavMesh();
            var data = surf.navMeshData;
            string dir = Path.GetDirectoryName(surf.gameObject.scene.path).Replace('\\', '/') + "/" + surf.gameObject.scene.name;
            Dossier(dir);
            string path = dir + "/NavMesh.asset";
            if (AssetDatabase.LoadAssetAtPath<NavMeshData>(path) != null) AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(data, path);
            surf.navMeshData = data;
            EditorUtility.SetDirty(surf);
            EditorSceneManager.MarkSceneDirty(surf.gameObject.scene);
            EditorSceneManager.SaveScene(surf.gameObject.scene);
            var tri = NavMesh.CalculateTriangulation();
            string r = "NavMesh cuit : " + tri.vertices.Length + " sommets, agent rayon " + st.agentRadius + " hauteur " + st.agentHeight + " marche " + st.agentClimb + " pente " + st.agentSlope;
            Debug.Log(r);
            return r;
        }

        static void Dossier(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            Dossier(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
