using System.Collections.Generic;
using Deathless.Jeu;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Deathless.EditorTools
{
    /// Emotes des héros (roue à emotes, EmotesHeros) : sous-machine « Emotes » commune à toutes les classes jouables,
    /// ajoutée à la couche de base de chaque contrôleur par les builders (ControleurPaladin, ControleurMage…), et
    /// catalogue Assets/Jeu/Resources/Emotes.asset (chope de « Boire un coup », instants mesurés dans Use_Item_Boire,
    /// copie retouchée de Use_Item — Assets/Jeu/Animation/Clips/Use_Item_Boire.anim — le bras porte la chope à la
    /// bouche et la tête se renverse un peu).
    /// Menu 11 : met à jour le catalogue et les contrôleurs existants sur place (GUID gardés, prefabs intacts).
    public static partial class JeuBuilder
    {
        const string Simulation = AnimPack + "Rig_Medium_Simulation.fbx";
        const string CatalogueEmotesPath = "Assets/Jeu/Resources/Emotes.asset";
        const string ChopesDir = "Assets/Art/KayKit/KayKit_Adventurers_2.0_EXTRA/Assets/fbx(unity)/";
        const string SousMachineEmotes = "Emotes";
        /// Copie retouchée de Use_Item pour « Boire un coup » (26/09/2026) : le bras porte la chope jusqu'à la bouche
        /// et la tête se renverse un peu. Remplace Use_Item pour le geste joué et pour la mesure de la chope vide.
        const string ClipBoire = AnimDir + "/Clips/Use_Item_Boire.anim";
        static readonly string[] s_ControleursHeros = { "Paladin_Jeu", "Mage_Jeu", "Rodeur_Jeu", "Assassin_Jeu", "Viking_Jeu" };

        /// Pose de la chope dans le socket de la main (à régler en jeu : position en m dans le socket, euler en degrés).
        static readonly Vector3 s_ChopePosition = Vector3.zero;
        static readonly Vector3 s_ChopeRotation = Vector3.zero;
        /// Hauteur de la chope, en fraction de la hauteur du personnage (Knight) : l'échelle en découle.
        const float ChopeHauteurRelative = 0.13f;

        [MenuItem("Deathless/Jeu/11. Emotes (roue : contrôleurs des héros, chope)")]
        public static string Emotes()
        {
            var cat = ConstruireCatalogueEmotes();
            int n = 0;
            foreach (var nom in s_ControleursHeros)
            {
                var c = AssetDatabase.LoadAssetAtPath<AnimatorController>(AnimDir + "/" + nom + ".controller");
                if (c == null) { Debug.LogWarning("Emotes : contrôleur absent " + nom); continue; }
                AjouterEmotes(c);
                n++;
            }
            AssetDatabase.SaveAssets();
            string r = "Emotes : " + n + " contrôleurs à jour ; chope dans la main " + (cat.mainGauche ? "gauche" : "droite")
                + ", vide à " + (cat.instantVide * 100f).ToString("F0") + " % de Use_Item";
            Debug.Log(r);
            return r;
        }

        /// Catalogue des emotes : modèles de la chope (KayKit Adventurers EXTRA, mug_full puis mug_empty), pose dans la
        /// main, main et instant où la chope quitte la bouche, mesurés sur le Knight dans Use_Item.
        public static CatalogueEmotes ConstruireCatalogueEmotes()
        {
            Dossier("Assets/Jeu/Resources");
            var cat = AssetDatabase.LoadAssetAtPath<CatalogueEmotes>(CatalogueEmotesPath);
            if (cat == null) { cat = ScriptableObject.CreateInstance<CatalogueEmotes>(); AssetDatabase.CreateAsset(cat, CatalogueEmotesPath); }
            cat.chopePleine = AssetDatabase.LoadAssetAtPath<GameObject>(ChopesDir + "mug_full.fbx");
            cat.chopeVide = AssetDatabase.LoadAssetAtPath<GameObject>(ChopesDir + "mug_empty.fbx");
            if (cat.chopePleine == null || cat.chopeVide == null) Debug.LogError("Emotes : chopes introuvables dans " + ChopesDir);
            cat.position = s_ChopePosition;
            cat.rotation = s_ChopeRotation;
            float hPerso = Hauteur(AssetDatabase.LoadAssetAtPath<GameObject>(KnightPath));
            float hChope = Hauteur(cat.chopePleine);
            // Sous le socket, la chope hérite de l'échelle du modèle du héros : on vise une fraction de sa hauteur.
            cat.echelle = hPerso > 0.01f && hChope > 0.001f ? ChopeHauteurRelative * hPerso / hChope : 1f;
            Debug.Log("Emotes : chope de " + hChope.ToString("F3") + " (unités du modèle), personnage de " + hPerso.ToString("F2")
                + " : échelle " + cat.echelle.ToString("F2"));
            MesurerBoire(AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipBoire), out bool gauche, out float vide);
            cat.mainGauche = gauche;
            cat.instantVide = vide;
            EditorUtility.SetDirty(cat);
            return cat;
        }

        /// Ajoute (ou refait) la sous-machine « Emotes » de la couche de base : déclencheur « Emote » et entier « EmoteNum »
        /// (EmotesHeros : numéro de l'emote, 0 = fin douce, -1 = fin immédiate). États étiquetés « Emote » ; ceux où l'on
        /// se relève, « EmoteRelever ». La locomotion (état par défaut de la couche) est le retour.
        public static void AjouterEmotes(AnimatorController c)
        {
            RetirerEmotes(c);
            if (!AParametre(c, EmotesHeros.ParamDeclencheur)) c.AddParameter(EmotesHeros.ParamDeclencheur, AnimatorControllerParameterType.Trigger);
            if (!AParametre(c, EmotesHeros.ParamNumero)) c.AddParameter(EmotesHeros.ParamNumero, AnimatorControllerParameterType.Int);
            var sm = c.layers[0].stateMachine;
            var loco = sm.defaultState;
            var em = sm.AddStateMachine(SousMachineEmotes, new Vector3(1100, 480));

            // Clips : le pack n'est jamais modifié ; les boucles sont des copies (Assets/Jeu/Animation/Clips/*_Loop).
            var salut = Clip(Simulation, "Waving");
            var bravo = Clip(Simulation, "Cheering");
            var provoc = Clip(Special, "Skeletons_Taunt");
            var assisDescente = Clip(Simulation, "Sit_Floor_Down");
            var assisBoucle = BoucleSi(Clip(Simulation, "Sit_Floor_Idle"), "Sit_Floor_Idle_Loop");
            var assisRelever = Clip(Simulation, "Sit_Floor_StandUp");
            var coucheDescente = Clip(Simulation, "Lie_Down");
            var coucheBoucle = BoucleSi(Clip(Simulation, "Lie_Idle"), "Lie_Idle_Loop");
            var coucheRelever = Clip(Simulation, "Lie_StandUp");
            var pompes = BoucleSi(Clip(Simulation, "Push_Ups"), "Push_Ups_Loop");
            var boire = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipBoire);
            var mort = Clip(General, "Death_B");

            // Gestes joués une fois : retour à la locomotion à la fin, ou dès que EmoteNum change (déplacement, coup…).
            Geste(sm, em, loco, EmotesHeros.Salut, "Salut", salut, new Vector3(300, 0));
            Geste(sm, em, loco, EmotesHeros.Acclamation, "Acclamation", bravo, new Vector3(300, 60));
            Geste(sm, em, loco, EmotesHeros.Provocation, "Provocation", provoc, new Vector3(300, 120));
            Geste(sm, em, loco, EmotesHeros.Boire, EmotesHeros.EtatBoire, boire, new Vector3(300, 180));
            // Pompes : en boucle jusqu'à la fin.
            var sPompes = Entree(sm, em, EmotesHeros.Pompes, "Pompes", pompes, new Vector3(300, 240));
            SiNum(sPompes, loco, AnimatorConditionMode.NotEqual, EmotesHeros.Pompes, 0.2f);
            // Assis, couché : descente, boucle, puis on se relève (fin douce) ou retour immédiat (fin brusque).
            Pose(sm, em, loco, EmotesHeros.Assis, "Assis", assisDescente, assisBoucle, assisRelever, new Vector3(300, 320));
            Pose(sm, em, loco, EmotesHeros.Repos, "Couche", coucheDescente, coucheBoucle, coucheRelever, new Vector3(300, 420));
            // Faire le mort : Death_B reste au sol (dernière image tenue), puis Lie_StandUp.
            var sMort = Entree(sm, em, EmotesHeros.FaireLeMort, "FaireLeMort", mort, new Vector3(300, 520));
            var sMortRelever = Etat(em, "FaireLeMortRelever", coucheRelever, new Vector3(550, 520));
            sMortRelever.tag = EmotesHeros.TagRelever;
            SiNum(sMort, loco, AnimatorConditionMode.Less, EmotesHeros.Aucune, 0.12f);
            SiNum(sMort, sMortRelever, AnimatorConditionMode.Equals, EmotesHeros.Aucune, 0.2f);
            Relever(sMortRelever, loco);
            EditorUtility.SetDirty(c);
        }

        /// Retire la sous-machine « Emotes », les transitions « N'importe quel état » vers ses états et ses paramètres.
        static void RetirerEmotes(AnimatorController c)
        {
            var sm = c.layers[0].stateMachine;
            AnimatorStateMachine ancienne = null;
            foreach (var enfant in sm.stateMachines) if (enfant.stateMachine.name == SousMachineEmotes) ancienne = enfant.stateMachine;
            if (ancienne == null) return;
            var etats = new HashSet<AnimatorState>();
            foreach (var e in ancienne.states) etats.Add(e.state);
            foreach (var t in sm.anyStateTransitions)
                if (t.destinationState == null || etats.Contains(t.destinationState)) sm.RemoveAnyStateTransition(t);
            sm.RemoveStateMachine(ancienne);
        }

        // ================================================================= Portail (arrivée)

        const string SousMachinePortail = "Portail";

        [MenuItem("Deathless/Jeu/12. Portail (arrivée) : contrôleurs des héros")]
        public static string PortailArrivee()
        {
            int n = 0;
            foreach (var nom in s_ControleursHeros)
            {
                var c = AssetDatabase.LoadAssetAtPath<AnimatorController>(AnimDir + "/" + nom + ".controller");
                if (c == null) { Debug.LogWarning("Portail (arrivée) : contrôleur absent " + nom); continue; }
                AjouterPortailArrivee(c);
                n++;
            }
            AssetDatabase.SaveAssets();
            string r = "Portail (arrivée) : " + n + " contrôleurs à jour (Spawn_Air au donjon, Spawn_Ground au village et au rappel)";
            Debug.Log(r);
            return r;
        }

        /// Ajoute (ou refait) la sous-machine « Portail » de la couche de base : déclencheur « PortailArrivee » et
        /// booléen « PortailAir » (PortailAnim), joués en entier depuis n'importe quel état, retour à la locomotion à la
        /// fin du clip. Même modèle que AjouterEmotes ; joué par DonjonJeu.Transit via Heros.DeclencherPortail.
        public static void AjouterPortailArrivee(AnimatorController c)
        {
            RetirerPortailArrivee(c);
            if (!AParametre(c, PortailAnim.ParamDeclencheur)) c.AddParameter(PortailAnim.ParamDeclencheur, AnimatorControllerParameterType.Trigger);
            if (!AParametre(c, PortailAnim.ParamAir)) c.AddParameter(PortailAnim.ParamAir, AnimatorControllerParameterType.Bool);
            var sm = c.layers[0].stateMachine;
            var loco = sm.defaultState;
            var pm = sm.AddStateMachine(SousMachinePortail, new Vector3(1100, 640));

            // Spawn_Air et Spawn_Ground sont génériques (Rig_Medium_General.fbx), pas Rig_Medium_Special (squelettes) :
            // Skeletons_Spawn_Ground est un clip distinct, propre aux squelettes.
            var air = Clip(General, "Spawn_Air");
            var sol = Clip(General, "Spawn_Ground");

            var sAir = Etat(pm, PortailAnim.EtatAir, air, new Vector3(300, 0));
            sAir.tag = PortailAnim.TagEtat;
            var tAir = sm.AddAnyStateTransition(sAir);
            tAir.hasExitTime = false; tAir.duration = 0.05f; tAir.canTransitionToSelf = false;
            tAir.AddCondition(AnimatorConditionMode.If, 0, PortailAnim.ParamDeclencheur);
            tAir.AddCondition(AnimatorConditionMode.If, 0, PortailAnim.ParamAir);
            Sortie(sAir, loco, 0.95f, 0.15f);

            var sSol = Etat(pm, PortailAnim.EtatSol, sol, new Vector3(300, 80));
            sSol.tag = PortailAnim.TagEtat;
            var tSol = sm.AddAnyStateTransition(sSol);
            tSol.hasExitTime = false; tSol.duration = 0.05f; tSol.canTransitionToSelf = false;
            tSol.AddCondition(AnimatorConditionMode.If, 0, PortailAnim.ParamDeclencheur);
            tSol.AddCondition(AnimatorConditionMode.IfNot, 0, PortailAnim.ParamAir);
            Sortie(sSol, loco, 0.95f, 0.15f);

            EditorUtility.SetDirty(c);
        }

        /// Retire la sous-machine « Portail » et les transitions « N'importe quel état » vers ses états (paramètres gardés).
        static void RetirerPortailArrivee(AnimatorController c)
        {
            var sm = c.layers[0].stateMachine;
            AnimatorStateMachine ancienne = null;
            foreach (var enfant in sm.stateMachines) if (enfant.stateMachine.name == SousMachinePortail) ancienne = enfant.stateMachine;
            if (ancienne == null) return;
            var etats = new HashSet<AnimatorState>();
            foreach (var e in ancienne.states) etats.Add(e.state);
            foreach (var t in sm.anyStateTransitions)
                if (t.destinationState == null || etats.Contains(t.destinationState)) sm.RemoveAnyStateTransition(t);
            sm.RemoveStateMachine(ancienne);
        }

        static bool AParametre(AnimatorController c, string nom) => System.Array.FindIndex(c.parameters, p => p.name == nom) >= 0;

        static AnimationClip BoucleSi(AnimationClip source, string nom) => source != null ? Boucle(source, nom) : null;

        /// État d'emote, entré de n'importe quel état par le déclencheur « Emote » quand EmoteNum vaut `numero`.
        static AnimatorState Entree(AnimatorStateMachine sm, AnimatorStateMachine em, int numero, string nom, Motion clip, Vector3 pos)
        {
            var s = Etat(em, nom, clip, pos);
            s.tag = EmotesHeros.TagEmote;
            var t = sm.AddAnyStateTransition(s);
            t.hasExitTime = false;
            t.duration = 0.2f;
            t.canTransitionToSelf = false;
            t.AddCondition(AnimatorConditionMode.If, 0, EmotesHeros.ParamDeclencheur);
            t.AddCondition(AnimatorConditionMode.Equals, numero, EmotesHeros.ParamNumero);
            return s;
        }

        static AnimatorStateTransition SiNum(AnimatorState de, AnimatorState vers, AnimatorConditionMode mode, int valeur, float duree)
        {
            var t = de.AddTransition(vers);
            t.hasExitTime = false;
            t.duration = duree;
            t.AddCondition(mode, valeur, EmotesHeros.ParamNumero);
            return t;
        }

        static void Geste(AnimatorStateMachine sm, AnimatorStateMachine em, AnimatorState loco, int numero, string nom, AnimationClip clip, Vector3 pos)
        {
            var s = Entree(sm, em, numero, nom, clip, pos);
            SiNum(s, loco, AnimatorConditionMode.NotEqual, numero, 0.15f);
            Sortie(s, loco, 0.92f, 0.25f);
        }

        static void Pose(AnimatorStateMachine sm, AnimatorStateMachine em, AnimatorState loco, int numero, string nom,
            AnimationClip descente, AnimationClip boucle, AnimationClip relever, Vector3 pos)
        {
            var sDescente = Entree(sm, em, numero, nom + "Descente", descente, pos);
            var sBoucle = Etat(em, nom + "Boucle", boucle, pos + new Vector3(250, 0));
            sBoucle.tag = EmotesHeros.TagEmote;
            var sRelever = Etat(em, nom + "Relever", relever, pos + new Vector3(500, 0));
            sRelever.tag = EmotesHeros.TagRelever;
            SiNum(sDescente, loco, AnimatorConditionMode.Less, EmotesHeros.Aucune, 0.12f);
            Sortie(sDescente, sBoucle, 0.95f, 0.15f);
            SiNum(sBoucle, loco, AnimatorConditionMode.Less, EmotesHeros.Aucune, 0.12f);
            SiNum(sBoucle, sRelever, AnimatorConditionMode.Equals, EmotesHeros.Aucune, 0.2f);
            Relever(sRelever, loco);
        }

        /// Fin du clip pour se relever (ou fin brusque : coup, mort, action).
        static void Relever(AnimatorState s, AnimatorState loco)
        {
            SiNum(s, loco, AnimatorConditionMode.Less, EmotesHeros.Aucune, 0.1f);
            Sortie(s, loco, 0.9f, 0.2f);
        }

        /// Hauteur (axe Y) des rendus d'un modèle posé à l'origine, à l'échelle 1.
        static float Hauteur(GameObject modele)
        {
            if (modele == null) return 0f;
            var go = Object.Instantiate(modele);
            go.hideFlags = HideFlags.HideAndDontSave;
            try
            {
                go.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                go.transform.localScale = Vector3.one;
                bool premier = true;
                var b = new Bounds();
                foreach (var r in go.GetComponentsInChildren<Renderer>(true))
                {
                    if (premier) { b = r.bounds; premier = false; } else b.Encapsulate(r.bounds);
                }
                return premier ? 0f : b.size.y;
            }
            finally { Object.DestroyImmediate(go); }
        }

        /// Use_Item sur le Knight, clip échantillonné hors jeu : la main qui s'approche le plus de la tête tient la chope ;
        /// elle « quitte la bouche » quand, après le moment où elle en est le plus près, elle s'en est éloignée du tiers du
        /// chemin qui la ramène à sa distance de repos. Renvoie cet instant en temps normalisé du clip.
        static void MesurerBoire(AnimationClip clip, out bool gauche, out float instantVide)
        {
            gauche = false;
            instantVide = 0.6f;
            var modele = AssetDatabase.LoadAssetAtPath<GameObject>(KnightPath);
            if (modele == null || clip == null || clip.length <= 0f) return;
            var go = Object.Instantiate(modele);
            go.hideFlags = HideFlags.HideAndDontSave;
            try
            {
                go.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                go.transform.localScale = Vector3.one;
                Transform tete = null, mainD = null, mainG = null;
                foreach (var t in go.GetComponentsInChildren<Transform>(true))
                {
                    if (t.name == "head") tete = t;
                    else if (t.name == "handslot.r") mainD = t;
                    else if (t.name == "handslot.l") mainG = t;
                }
                if (tete == null || mainD == null || mainG == null) return;
                const int n = 240;
                var dD = new float[n + 1];
                var dG = new float[n + 1];
                for (int i = 0; i <= n; i++)
                {
                    clip.SampleAnimation(go, clip.length * i / n);
                    // Bouche : un peu devant et sous le centre de la tête.
                    Vector3 bouche = tete.position + go.transform.forward * 0.12f - Vector3.up * 0.05f;
                    dD[i] = Vector3.Distance(mainD.position, bouche);
                    dG[i] = Vector3.Distance(mainG.position, bouche);
                }
                float minD = Mathf.Min(dD), minG = Mathf.Min(dG);
                gauche = minG < minD;
                var d = gauche ? dG : dD;
                int iMin = System.Array.IndexOf(d, gauche ? minG : minD);
                float repos = d[n];
                float seuil = d[iMin] + (Mathf.Max(repos, d[0]) - d[iMin]) / 3f;
                for (int i = iMin; i <= n; i++)
                    if (d[i] >= seuil) { instantVide = (float)i / n; break; }
                Debug.Log("Use_Item : main " + (gauche ? "gauche" : "droite") + ", au plus près de la bouche à "
                    + (clip.length * iMin / n).ToString("F2") + " s (" + d[iMin].ToString("F2") + " m), chope vide à "
                    + (clip.length * instantVide).ToString("F2") + " s sur " + clip.length.ToString("F2") + " s");
            }
            finally { Object.DestroyImmediate(go); }
        }
    }
}
