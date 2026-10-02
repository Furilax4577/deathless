using System.Collections.Generic;
using Deathless.Audio;
using Deathless.UI.Donnees;
using UnityEngine;

namespace Deathless.Jeu
{
    /// Roue à emotes et emotes du héros (Wiki : interface, commandes ; Docs/ui-socle.md). Composant ajouté par Heros.Awake
    /// sur tous les héros, locaux comme distants.
    ///
    /// - **Héros local** : maintenir Gameplay/Emote (croix bas, B) ouvre la roue (DonneesUI.RoueEmotes, calque du HUD) ;
    ///   le stick droit ou la souris pointe un secteur depuis le centre, la caméra ne tourne plus ; relâcher lance l'emote
    ///   pointée, relâcher au centre annule. Une emote ne part que si le héros est vivant, au sol, libre (Heros.PeutAgir :
    ///   ni action de classe, ni transit, en partie) et sans maintien de classe (garde, visée). Elle est interrompue par
    ///   un déplacement, une attaque, une compétence, l'esquive, le saut, un coup reçu ou la mort ; assis ou couché, se
    ///   déplacer joue d'abord le clip pour se relever (pas sous un coup ni à la mort). Ni dégâts, ni coût.
    /// - **Animator** : sous-machine « Emotes » de la couche de base (JeuBuilder.AjouterEmotes), déclencheur « Emote » et
    ///   entier « EmoteNum » (numéro de l'emote ; 0 = fin, en se relevant si besoin ; -1 = fin immédiate). Le déclencheur
    ///   passe par Heros.Declencher et l'entier par le NetworkAnimator du propriétaire : les autres postes jouent l'emote.
    /// - **Chope** (Boire un coup, tous les postes) : déduite de l'état « Boire » de l'Animator, comme le penché de la
    ///   charge : la chope remplace l'arme de la main qui la tient, pleine puis vide dès qu'elle quitte la bouche
    ///   (CatalogueEmotes.instantVide). Depuis le 01/10/2026 (retour de Quentin : « la chope est toute petite, on ne voit
    ///   pas qu'il boit »), elle est plus grosse (échelle du catalogue), coiffée d'un dôme de mousse, et pendant la
    ///   boisson (instantBouche → instantVide) elle bascule vers la tête et son bord est amené à la bouche (os « head »),
    ///   quelle que soit la silhouette du héros (MajChope). Rien à diffuser en plus.
    [DisallowMultipleComponent]
    public class EmotesHeros : MonoBehaviour, IRoueEmotes
    {
        /// Une emote de la roue. Numero = valeur de « EmoteNum » (1 à 8), dans l'ordre des secteurs (haut, puis sens horaire).
        public sealed class Definition : IEmoteRoue
        {
            public int Numero { get; }
            public string Id { get; }
            public string Nom { get; }
            public string Icone { get; }
            /// Assis ou couché : la fin douce joue le clip pour se relever.
            public bool Relever { get; }
            public Definition(int numero, string id, string nom, bool relever)
            {
                Numero = numero; Id = id; Nom = nom; Icone = "emote_" + id; Relever = relever;
            }
        }

        public const int Salut = 1, Acclamation = 2, Provocation = 3, Assis = 4, Repos = 5, Pompes = 6, Boire = 7, FaireLeMort = 8;
        /// Valeurs spéciales de « EmoteNum ».
        public const int Aucune = 0, Brusque = -1;

        /// Les 8 emotes retenues (26/09/2026), rig Medium : Waving, Cheering, Skeletons_Taunt, Sit_Floor_*, Lie_*,
        /// Push_Ups, Use_Item (chope), Death_B puis Lie_StandUp.
        public static readonly IReadOnlyList<Definition> Definitions = new[]
        {
            new Definition(Salut, "salut", "Salut", false),
            new Definition(Acclamation, "acclamation", "Acclamation", false),
            new Definition(Provocation, "provocation", "Provocation", false),
            new Definition(Assis, "assis", "S'asseoir", true),
            new Definition(Repos, "repos", "Se reposer", true),
            new Definition(Pompes, "pompes", "Pompes", false),
            new Definition(Boire, "boire", "Boire un coup", false),
            new Definition(FaireLeMort, "mort", "Faire le mort", true),
        };

        /// Noms des états de la sous-machine (JeuBuilder.AjouterEmotes) et étiquettes.
        public const string EtatBoire = "Boire", TagEmote = "Emote", TagRelever = "EmoteRelever";
        public const string ParamDeclencheur = "Emote", ParamNumero = "EmoteNum";

        static readonly int P_Emote = Animator.StringToHash(ParamDeclencheur);
        static readonly int P_EmoteNum = Animator.StringToHash(ParamNumero);
        static readonly int H_Boire = Animator.StringToHash(EtatBoire);
        static readonly int H_TagEmote = Animator.StringToHash(TagEmote);
        static readonly int H_TagRelever = Animator.StringToHash(TagRelever);

        /// Roue : zone morte du centre (fraction du rayon), souris (pixels pour aller du centre au bord), délai de grâce
        /// quand le stick revient au centre (on le lâche souvent juste avant la touche).
        public static float CentreRoue = 0.35f, PixelsRoue = 260f, GraceStick = 0.18f;
        /// Délai maximal pour que l'Animator entre dans l'emote, et pour se relever.
        const float DelaiEntree = 0.6f, DelaiReleve = 4f;

        Heros m_Heros;
        bool m_Init, m_Local, m_Parametres;
        // Roue
        bool m_Ouverte, m_RoueTest;
        Vector2 m_Curseur;
        int m_Pointee = -1;
        float m_CentreDepuis;
        // La roue est pilotée à la souris (01/10/2026) : quand la souris s'arrête, l'action Look n'a plus de contrôle actif
        // et RegardBrut ne la reconnaît plus ; sans ce drapeau on la prenait pour un stick revenu au centre et le curseur
        // repartait au milieu après GraceStick, juste avant que le joueur ne relâche B.
        bool m_RoueSouris;
        // Emote en cours (héros local)
        int m_Active;
        bool m_Vue, m_AttendNeutre, m_Releve;
        float m_Depuis, m_ReleveDepuis;
        // Chope (tous les postes)
        Transform m_Main, m_Tete;
        GameObject m_Chope, m_Pleine, m_Vide;
        bool m_ChopeVisible;
        /// Matériau de la mousse (un pour tous les héros).
        static Material s_Mousse;
        readonly List<(GameObject, bool)> m_Armes = new List<(GameObject, bool)>();
        static bool s_AvertiParametres, s_AvertiCatalogue;

        Animator Anim => m_Heros != null ? m_Heros.animator : null;

        // ----------------------------------------------------------------- IRoueEmotes

        public bool Ouverte => m_Ouverte;
        public IReadOnlyList<IEmoteRoue> Emotes => Definitions;
        public int Pointee => m_Pointee;

        // ----------------------------------------------------------------- État lu par Heros et les tests

        /// La roue est ouverte : Heros ne tourne plus la caméra.
        public bool RoueOuverte => m_Ouverte;
        /// Numéro de l'emote en cours (0 : aucune).
        public int Active => m_Active;
        /// Emote en cours, ou héros qui se relève (déplacement bloqué).
        public bool EnCours => m_Active > 0 || m_Releve;
        /// Chope visible en ce moment (tests) : 0 aucune, 1 pleine, 2 vide.
        public int EtatChope => !m_ChopeVisible ? 0 : m_Vide != null && m_Vide.activeSelf ? 2 : 1;

        void Awake() => m_Heros = GetComponent<Heros>();

        void OnDestroy()
        {
            if (ReferenceEquals(DonneesUI.RoueEmotes, this)) DonneesUI.RoueEmotes = null;
        }

        /// Après Heros.Initialiser (Distant est alors connu).
        void Initialiser()
        {
            m_Init = true;
            m_Local = !m_Heros.Distant;
            var anim = Anim;
            if (anim != null)
            {
                bool declencheur = false, numero = false;
                foreach (var p in anim.parameters)
                {
                    if (p.nameHash == P_Emote && p.type == AnimatorControllerParameterType.Trigger) declencheur = true;
                    if (p.nameHash == P_EmoteNum && p.type == AnimatorControllerParameterType.Int) numero = true;
                }
                m_Parametres = declencheur && numero;
            }
            if (!m_Parametres && !s_AvertiParametres)
            {
                s_AvertiParametres = true;
                Debug.LogWarning("[Emotes] contrôleur sans « Emote » / « EmoteNum » (" + name + ") : lancer Deathless > Jeu > 11. Emotes");
            }
            PreparerChope();
            if (m_Local)
            {
                DonneesUI.RoueEmotes = this;
                m_Heros.Sante.Touche += (info, reel) => { if (reel > 0f) Interrompre(true); };
                m_Heros.Sante.Tue += _ => { FermerRoue(); Interrompre(true); };
            }
        }

        // ----------------------------------------------------------------- Entrées (héros local, appelées par Heros)

        /// Action résolue (Heros.OnAction, héros vivant et en partie). Vrai si l'action est consommée (Emote).
        public bool SurAction(string action)
        {
            if (!m_Local) return false;
            switch (action)
            {
                case "Emote":
                    OuvrirRoue();
                    return true;
                case "Jump": case "Dodge": case "AttackPrimary": case "AttackSecondary":
                case "Skill1": case "Skill2": case "Skill3": case "Ultimate": case "DrinkPotion":
                    if (m_Ouverte && !m_RoueTest) FermerRoue();
                    Interrompre(true);
                    return false;
                default:
                    return false;
            }
        }

        /// Déplacement demandé (Heros.Update, état libre) : un déplacement met fin à l'emote (en se relevant si besoin) ;
        /// pendant l'emote et pendant qu'on se relève, le héros ne bouge pas. Si l'emote est lancée en marchant, le
        /// déplacement n'est compté qu'après un retour au neutre (le héros s'arrête pour la faire).
        public Vector3 FiltrerDeplacement(Vector3 dir)
        {
            if (!m_Local) return dir;
            bool bouge = dir.sqrMagnitude > 0.01f;
            if (m_AttendNeutre && !bouge) m_AttendNeutre = false;
            if (m_Releve) return Vector3.zero;
            if (m_Active > 0)
            {
                if (!bouge || m_AttendNeutre) return Vector3.zero;
                Interrompre(false);
                return m_Releve ? Vector3.zero : dir;
            }
            return dir;
        }

        // ----------------------------------------------------------------- Roue

        void OuvrirRoue()
        {
            if (!m_Heros.Vivant || !m_Heros.EnJeu || m_Heros.EnTransit) return;
            m_Ouverte = true;
            m_RoueTest = false;
            m_Curseur = Vector2.zero;
            m_Pointee = -1;
            m_CentreDepuis = 0f;
            m_RoueSouris = false;
        }

        void FermerRoue()
        {
            m_Ouverte = false;
            m_RoueTest = false;
            m_Pointee = -1;
        }

        void MajRoue(float dt)
        {
            var entrees = m_Heros.Entrees;
            if (m_RoueTest) return;
            if (!m_Heros.Vivant || !m_Heros.EnJeu || entrees == null || !entrees.CarteJeuActive)
            {
                FermerRoue();   // menu ouvert, mort… : rien ne part
                return;
            }
            if (!entrees.EmoteMaintenue)
            {
                int choix = m_Pointee;
                FermerRoue();
                if (choix >= 0) Lancer(Definitions[choix].Numero);
                return;
            }
            Vector2 v = entrees.RegardBrut(out bool souris);
            if (souris)
            {
                m_RoueSouris = true;
                m_Curseur = Vector2.ClampMagnitude(m_Curseur + v / Mathf.Max(1f, PixelsRoue), 1f);
            }
            else if (v.magnitude >= 0.5f)
            {
                m_RoueSouris = false;   // le stick reprend la main
                m_Curseur = Vector2.ClampMagnitude(v, 1f);
                m_CentreDepuis = 0f;
            }
            else if (m_RoueSouris)
            {
                // Souris immobile : le curseur reste où le joueur l'a laissé.
            }
            else if (m_Curseur != Vector2.zero)
            {
                m_CentreDepuis += dt;
                if (m_CentreDepuis >= GraceStick) m_Curseur = Vector2.zero;
            }
            m_Pointee = Secteur(m_Curseur);
        }

        /// Secteur d'une direction (0 = haut, sens horaire, 8 secteurs de 45°), -1 au centre.
        public static int Secteur(Vector2 v)
        {
            if (v.magnitude < CentreRoue) return -1;
            float angle = Mathf.Atan2(v.x, v.y) * Mathf.Rad2Deg;   // 0 en haut, positif vers la droite
            if (angle < 0f) angle += 360f;
            return Mathf.RoundToInt(angle / 45f) % Definitions.Count;
        }

        // ----------------------------------------------------------------- Emotes

        bool PeutLancer
        {
            get
            {
                var c = m_Heros.Classe;
                return m_Parametres && !m_Releve && m_Heros.PeutAgir && m_Heros.AuSol
                    && (c == null || (!c.HautDuCorps && !c.FaceVisee));
            }
        }

        /// Lance l'emote `numero` (1 à 8). Faux si le héros ne peut pas (son de refus).
        public bool Lancer(int numero)
        {
            if (!m_Local || numero < 1 || numero > Definitions.Count) return false;
            if (numero == m_Active) return true;   // déjà en cours (assis, couché, pompes…)
            if (!PeutLancer)
            {
                VolumesAudio.JouerInterface(SonInterface.Refus, 0.6f);
                return false;
            }
            m_Active = numero;
            m_Vue = false;
            m_Depuis = 0f;
            m_AttendNeutre = m_Heros.Entrees != null && m_Heros.Entrees.Deplacement.sqrMagnitude > 0.01f;
            Anim.SetInteger(P_EmoteNum, numero);
            m_Heros.Declencher(P_Emote);
            Deathless.Succes.ServiceSucces.Emote(m_Heros);   // succès « Danse de la victoire »
            return true;
        }

        /// Fin de l'emote : brusque (coup, mort, attaque, compétence, esquive) ou douce (déplacement : assis ou couché, le
        /// héros se relève d'abord).
        public void Interrompre(bool brusque)
        {
            if (!m_Local || !m_Parametres || (m_Active <= 0 && !m_Releve)) return;
            var anim = Anim;
            if (brusque || m_Releve)
            {
                anim.SetInteger(P_EmoteNum, Brusque);
                if (!m_Vue) m_Heros.AnnulerDeclencheur(P_Emote);
                m_Releve = false;
            }
            else
            {
                var def = Definitions[m_Active - 1];
                anim.SetInteger(P_EmoteNum, Aucune);
                if (!m_Vue) m_Heros.AnnulerDeclencheur(P_Emote);
                else if (def.Relever) { m_Releve = true; m_ReleveDepuis = 0f; }
            }
            m_Active = 0;
        }

        /// Tests et captures : roue ouverte, curseur imposé (null : fermée, sans rien lancer).
        public void TesterRoue(Vector2? curseur)
        {
            if (curseur == null) { FermerRoue(); return; }
            m_Ouverte = true;
            m_RoueTest = true;
            m_Curseur = Vector2.ClampMagnitude(curseur.Value, 1f);
            m_Pointee = Secteur(m_Curseur);
        }

        /// L'Animator (couche de base) est dans une emote ou s'en relève (état courant ou visé par la transition).
        bool DansEmote(Animator anim)
        {
            var cur = anim.GetCurrentAnimatorStateInfo(0);
            if (cur.tagHash == H_TagEmote || cur.tagHash == H_TagRelever) return true;
            if (!anim.IsInTransition(0)) return false;
            var next = anim.GetNextAnimatorStateInfo(0);
            return next.tagHash == H_TagEmote || next.tagHash == H_TagRelever;
        }

        void Update()
        {
            if (m_Heros == null) return;
            if (!m_Init)
            {
                if (m_Heros.Partie == null) return;
                Initialiser();
            }
            if (!m_Local) return;
            float dt = Time.deltaTime;
            if (m_Ouverte) MajRoue(dt);
            if (!m_Parametres) return;
            if (m_Heros.EnTransit || !m_Heros.Vivant) { Interrompre(true); return; }
            var anim = Anim;
            bool dans = DansEmote(anim);
            if (m_Active > 0)
            {
                m_Depuis += dt;
                if (dans) m_Vue = true;
                else if (m_Vue || m_Depuis > DelaiEntree)
                {
                    // Fin naturelle (geste joué une fois) ou emote jamais entrée : EmoteNum revient à 0 (les autres postes
                    // ne rejoueront pas une vieille valeur au prochain déclencheur).
                    if (!m_Vue) m_Heros.AnnulerDeclencheur(P_Emote);
                    anim.SetInteger(P_EmoteNum, Aucune);
                    m_Active = 0;
                }
            }
            if (m_Releve)
            {
                m_ReleveDepuis += dt;
                if ((!dans && m_ReleveDepuis > 0.2f) || m_ReleveDepuis > DelaiReleve) m_Releve = false;
            }
        }

        // ----------------------------------------------------------------- Chope (tous les postes)

        void PreparerChope()
        {
            var cat = CatalogueEmotes.Courant;
            var anim = Anim;
            if (cat == null || cat.chopePleine == null || anim == null)
            {
                if (!s_AvertiCatalogue) { s_AvertiCatalogue = true; Debug.LogWarning("[Emotes] pas de chope (Resources/Emotes.asset) : lancer Deathless > Jeu > 11. Emotes"); }
                return;
            }
            string socket = cat.mainGauche ? "handslot.l" : "handslot.r";
            foreach (var t in anim.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == socket) m_Main = t;
                else if (t.name == "head") m_Tete = t;
            }
            if (m_Main == null) return;
            // Construite à l'origine (identité) : les bornes des rendus sont alors celles du modèle, et la mousse se pose
            // sur le haut de la chope pleine sans dépendre de la pose de la main.
            m_Chope = new GameObject("Chope");
            m_Chope.layer = m_Main.gameObject.layer;
            m_Chope.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            m_Pleine = Modele(cat.chopePleine, "Pleine");
            m_Vide = cat.chopeVide != null ? Modele(cat.chopeVide, "Vide") : null;
            if (cat.mousse) PoserMousse(cat);
            m_Chope.transform.SetParent(m_Main, false);
            m_Chope.transform.localPosition = cat.position;
            m_Chope.transform.localRotation = Quaternion.Euler(cat.rotation);
            m_Chope.transform.localScale = Vector3.one * (cat.echelle > 0f ? cat.echelle : 1f);
            m_Chope.SetActive(false);
        }

        /// Dôme de mousse blanche sur la chope pleine (01/10/2026) : une sphère aplatie, à moitié enfoncée dans le haut du
        /// modèle, matériau Lit uni (le modèle KayKit n'a qu'un disque pâle). Rien sur la chope vide.
        void PoserMousse(CatalogueEmotes cat)
        {
            if (m_Pleine == null || cat.mousseHauteur <= 0f) return;
            bool etaitActif = m_Pleine.activeSelf;
            m_Pleine.SetActive(true);
            bool premier = true;
            var b = new Bounds();
            foreach (var r in m_Pleine.GetComponentsInChildren<Renderer>(true))
            {
                if (premier) { b = r.bounds; premier = false; } else b.Encapsulate(r.bounds);
            }
            m_Pleine.SetActive(etaitActif);
            if (premier || b.size.y <= 0.0001f) return;
            if (s_Mousse == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) return;
                s_Mousse = new Material(shader) { name = "Mousse (emote)" };
                s_Mousse.SetFloat("_Smoothness", 0.25f);
            }
            s_Mousse.color = cat.mousseCouleur;
            var mousse = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            mousse.name = "Mousse";
            mousse.layer = m_Chope.layer;
            foreach (var c in mousse.GetComponents<Collider>()) Destroy(c);
            mousse.GetComponent<Renderer>().sharedMaterial = s_Mousse;
            // La chope KayKit a une anse sur un côté : la mousse se centre sur la partie étroite (le corps), pas sur la boîte
            // entière, en prenant la plus petite des deux largeurs.
            float largeur = Mathf.Min(b.size.x, b.size.z) * cat.mousseLargeur;
            float hauteur = b.size.y * cat.mousseHauteur;
            mousse.transform.SetParent(m_Pleine.transform, true);
            mousse.transform.position = new Vector3(b.center.x, b.max.y - hauteur * 0.15f, b.center.z);
            mousse.transform.rotation = Quaternion.identity;
            mousse.transform.localScale = Vector3.Scale(mousse.transform.localScale, new Vector3(largeur, hauteur, largeur));
            // Hauteur du bord (pivot de la chope à son haut) mesurée ici si le catalogue ne la donne pas (MajChope).
            m_HauteurBord = cat.hauteurBord > 0f ? cat.hauteurBord : b.max.y;
        }

        /// Hauteur du bord de la chope au-dessus de son pivot (unités du modèle).
        float m_HauteurBord = 0.3f;

        GameObject Modele(GameObject source, string nom)
        {
            var go = Instantiate(source, m_Chope.transform, false);
            go.name = nom;
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            foreach (var c in go.GetComponentsInChildren<Collider>(true)) Destroy(c);
            foreach (var t in go.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = m_Chope.layer;
            go.SetActive(false);
            return go;
        }

        /// Après l'Animator : chope visible pendant l'état « Boire », pleine puis vide.
        void LateUpdate()
        {
            if (m_Chope == null) return;
            var anim = Anim;
            bool boire = false;
            float t = 0f;
            if (anim != null && anim.isActiveAndEnabled && m_Heros.Vivant)
            {
                var cur = anim.GetCurrentAnimatorStateInfo(0);
                bool trans = anim.IsInTransition(0);
                if (cur.shortNameHash == H_Boire && !trans) { boire = true; t = cur.normalizedTime; }
                else if (trans)
                {
                    var next = anim.GetNextAnimatorStateInfo(0);
                    if (next.shortNameHash == H_Boire) { boire = true; t = next.normalizedTime; }
                }
            }
            if (boire != m_ChopeVisible)
            {
                m_ChopeVisible = boire;
                if (boire) CacherArmes(); else RendreArmes();
                m_Chope.SetActive(boire);
            }
            if (boire)
            {
                var cat = CatalogueEmotes.Courant;
                bool vide = m_Vide != null && cat != null && t >= cat.instantVide;
                if (m_Pleine.activeSelf == vide) m_Pleine.SetActive(!vide);
                if (m_Vide != null && m_Vide.activeSelf != vide) m_Vide.SetActive(vide);
                if (cat != null) MajChope(cat, t);
            }
        }

        /// Pose de la chope pendant le geste (01/10/2026). Hors de la boisson : la pose du catalogue dans la main. Pendant la
        /// boisson (de instantBouche à instantVide, fondus de part et d'autre) : elle bascule de `bascule` degrés vers la
        /// tête, puis son bord (pivot + hauteurBord × échelle, le long de son axe) est amené vers la bouche (os « head »,
        /// un peu devant et en dessous) de la part `rapprochement` du chemin restant. Le clip porte déjà la main vers la
        /// tête (Use_Item_Boire) ; ceci comble ce qui manque selon la silhouette du héros, sur tous les postes.
        void MajChope(CatalogueEmotes cat, float t)
        {
            var tr = m_Chope.transform;
            tr.localPosition = cat.position;
            tr.localRotation = Quaternion.Euler(cat.rotation);
            if (m_Tete == null || cat.instantVide <= cat.instantBouche) return;
            float fondu = Mathf.Max(0.01f, cat.fondu);
            float w = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(cat.instantBouche - fondu, cat.instantBouche, t))
                * (1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(cat.instantVide, cat.instantVide + fondu, t)));
            if (w <= 0.001f) return;
            Vector3 bouche = m_Tete.position + transform.forward * cat.boucheAvant + Vector3.up * cat.boucheHaut;
            Vector3 versBouche = bouche - tr.position;
            if (versBouche.sqrMagnitude > 0.0001f && Mathf.Abs(cat.bascule) > 0.01f)
            {
                Vector3 axe = Vector3.Cross(tr.up, versBouche.normalized);
                if (axe.sqrMagnitude > 0.0001f) tr.rotation = Quaternion.AngleAxis(cat.bascule * w, axe.normalized) * tr.rotation;
            }
            if (cat.rapprochement > 0f)
            {
                Vector3 bord = tr.position + tr.up * ((cat.hauteurBord > 0f ? cat.hauteurBord : m_HauteurBord) * tr.lossyScale.y);
                tr.position += (bouche - bord) * (cat.rapprochement * w);
            }
        }

        void CacherArmes()
        {
            m_Armes.Clear();
            foreach (Transform enfant in m_Main)
            {
                if (enfant.gameObject == m_Chope) continue;
                m_Armes.Add((enfant.gameObject, enfant.gameObject.activeSelf));
                enfant.gameObject.SetActive(false);
            }
        }

        void RendreArmes()
        {
            foreach (var (go, actif) in m_Armes) if (go != null && go.transform.parent == m_Main) go.SetActive(actif);
            m_Armes.Clear();
        }

        void OnDisable()
        {
            if (m_ChopeVisible) { m_ChopeVisible = false; RendreArmes(); if (m_Chope != null) m_Chope.SetActive(false); }
        }
    }
}
