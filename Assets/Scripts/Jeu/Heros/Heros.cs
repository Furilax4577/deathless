using UnityEngine;

namespace Deathless.Jeu
{
    /// Héros commun à toutes les classes : vie et endurance, déplacement relatif à la caméra, sprint, saut, esquive,
    /// mort (dissolution vers Nyxessa) et réapparition, visière (si le modèle en a une), paramètres d'animation communs.
    /// Tout ce qui est propre à une classe (attaques, compétences, maintiens, jauge, emplacements) est dans le composant
    /// ClasseHeros du même objet (ClassePaladin, ClasseMage…). Les entrées viennent de HerosEntrees (InputChordResolver) ;
    /// l'état qui fait foi est recopié dans l'EtatJoueur de Partie.
    [RequireComponent(typeof(CharacterController), typeof(Sante), typeof(HerosEntrees))]
    public class Heros : MonoBehaviour
    {
        /// État commun : Classe = une action de la classe est en cours (voir ClasseHeros.Occupe).
        /// Renverse (26/09/2026) : chute à la renverse sans contrôle (Death_A), un instant au sol, puis relevé
        /// (Lie_StandUp) — voir Renverser ci-dessous et statuts.md.
        public enum Etat { Libre, Esquive, Etourdi, Renverse, Mort, Reapparition }

        public Animator animator;
        public HelmetVisor visiere;

        public Sante Sante { get; private set; }
        public ClasseHeros Classe { get; private set; }
        public HerosEntrees Entrees { get; private set; }
        /// Roue à emotes et emotes (composant ajouté dans Awake, sur tous les héros).
        public EmotesHeros Emotes { get; private set; }
        /// Statuts du héros (ralenti, étourdi, ivresse… ; Statuts.cs) : l'hôte fait foi, le propriétaire prédit.
        public Statuts Statuts { get; private set; }
        public CharacterController CC { get; private set; }
        public Partie Partie { get; private set; }
        /// Multijoueur : héros d'un autre poste (marionnette). Position et animations arrivent par le réseau
        /// (NetworkTransform, NetworkAnimator) ; ni entrées, ni caméra, ni logique de classe, ni déplacement ici.
        public bool Distant { get; private set; }
        public int Id => m_Etat != null ? m_Etat.id : 0;
        /// État du joueur de ce héros (points et rangs de compétence…).
        public EtatJoueur EtatJoueur => m_Etat;
        public bool Vivant => !Sante.Mort && m_EtatCourant != Etat.Mort && m_EtatCourant != Etat.Reapparition;
        public Etat EtatCourant => m_EtatCourant;
        public bool AuSol => m_AuSol;
        public float Endurance => m_Endurance;
        /// Renversé (26/09/2026, statuts.md) : sans contrôle, en cours de chute/relevé.
        public bool EnRenverse => m_EtatCourant == Etat.Renverse;
        /// Progression du Renversé (0 à 1 : chute, au sol, relevé), martelage compris — pour la jauge de relevé (HUD).
        public float RenverseProgression => EnRenverse && m_RenverseDureeTotale > 0.01f ? Mathf.Clamp01((m_EtatDepuis + m_RenverseMartelement) / m_RenverseDureeTotale) : 0f;
        /// Part du plafond de martelage déjà atteinte (0 à 1) : jauge de relevé (HUD).
        public float RenverseMartelementRatio => EnRenverse && m_RenverseDureeTotale > 0.01f ? Mathf.Clamp01(m_RenverseMartelement / (m_RenverseDureeTotale * B.renverseMartelementPlafond)) : 0f;
        /// Dernier martelage (Time.time) : la jauge de relevé (HUD) en tire un petit tremblement.
        public float RenverseDernierMartelement => m_RenverseDerniereReduction;
        public bool EnJeu => Partie != null && Partie.EnCours;
        /// Libre de lancer une action (vivant, en jeu, ni esquive ni étourdissement, pas d'action de classe en cours).
        public bool PeutAgir => m_EtatCourant == Etat.Libre && Vivant && EnJeu && !EnTransit && (Classe == null || !Classe.Occupe);
        /// Passage d'un portail (donjon) : immobile, invisible, sans action.
        public bool EnTransit { get; set; }

        CameraEpaule m_Camera;
        EtatJoueur m_Etat;
        Etat m_EtatCourant = Etat.Libre;
        float m_EtatDepuis;
        float m_VitesseY;
        bool m_AuSol = true;
        float m_Endurance, m_EnduranceUtilisee = -99f;
        float m_RechargeEsquive;
        Vector3 m_DirEsquive;
        float m_Etourdi;
        // Renversé (26/09/2026) : durées effectives de la séquence en cours (chute + au sol + relevé, secondes), martelage
        // cumulé (retiré du temps écoulé, plafonné), relevé déjà déclenché (le trigger de l'Animator ne part qu'une fois).
        float m_RenverseChute, m_RenverseAuSol, m_RenverseReleve, m_RenverseDureeTotale;
        float m_RenverseMartelement, m_RenverseDerniereReduction = -99f;
        bool m_RenverseReleveDeclenche;
        Deathless.Reseau.HerosReseau m_ReseauHeros;
        float m_InvulnerableJusque;
        bool m_EsquiveArriere;
        float m_Pas;
        int m_CoucheHaut = -1;
        float m_PoidsHaut;
        float m_HautJusque;
        bool m_Sprint;
        Unity.Netcode.Components.NetworkAnimator m_AnimReseau;
        // Alignement de l'arme sur la visée (arc, arbalète) : décalage de lacet de la pose de tir, direction voulue, penché
        // du buste (appliqué après l'Animator).
        float m_DecalageVisee, m_PencheBuste;
        Vector3 m_DirVisee;
        bool m_Aligne;
        float m_AligneDepuis = -99f;
        Transform m_Buste;
        // Penché du corps entier (charge bélier…) : rotation du modèle (enfant) autour des pieds, capsule et caméra intactes.
        Transform m_Modele;
        Quaternion m_RotModele;
        float m_Penche;

        static readonly int P_Speed = Animator.StringToHash("Speed");
        static readonly int P_Grounded = Animator.StringToHash("Grounded");
        static readonly int P_Dead = Animator.StringToHash("Dead");
        static readonly int P_Dodge = Animator.StringToHash("Dodge");
        static readonly int P_DodgeBack = Animator.StringToHash("DodgeBack");
        /// Direction du clip d'esquive par rapport à la face (0 Avant, 1 Droite, 2 Arrière, 3 Gauche ; DodgeDirection).
        static readonly int P_DodgeDir = Animator.StringToHash("DodgeDir");
        static readonly int P_Jump = Animator.StringToHash("Jump");
        static readonly int P_Hit = Animator.StringToHash("Hit");
        static readonly int P_Respawn = Animator.StringToHash("Respawn");
        static readonly int P_RenverseChute = Animator.StringToHash("RenverseChute");
        static readonly int P_RenverseRelevage = Animator.StringToHash("RenverseRelevage");
        static readonly int P_RenverseVitesse = Animator.StringToHash("RenverseVitesse");
        static readonly int P_PortailArrivee = Animator.StringToHash(PortailAnim.ParamDeclencheur);
        static readonly int P_PortailAir = Animator.StringToHash(PortailAnim.ParamAir);

        GameBalance B => GameBalance.Courant;

        void Awake()
        {
            CC = GetComponent<CharacterController>();
            Entrees = GetComponent<HerosEntrees>();
            Sante = GetComponent<Sante>();
            Sante.equipe = Equipe.Heros;
            Statuts = Statuts.De(this);
            Classe = GetComponent<ClasseHeros>();
            Emotes = GetComponent<EmotesHeros>();
            if (Emotes == null) Emotes = gameObject.AddComponent<EmotesHeros>();
            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (visiere == null) visiere = GetComponentInChildren<HelmetVisor>();
            if (animator != null) m_CoucheHaut = animator.GetLayerIndex("HautDuCorps");
            m_AnimReseau = GetComponent<Unity.Netcode.Components.NetworkAnimator>();
            m_ReseauHeros = GetComponent<Deathless.Reseau.HerosReseau>();
            if (animator != null) foreach (var t in animator.GetComponentsInChildren<Transform>(true)) if (t.name == "chest") { m_Buste = t; break; }
            if (animator != null && animator.transform != transform) { m_Modele = animator.transform; m_RotModele = m_Modele.localRotation; }
        }

        /// Multijoueur : ce héros appartient à un autre poste (avant Initialiser).
        public void DevenirDistant()
        {
            Distant = true;
            Entrees.enabled = false;
        }

        /// Place le héros sans passer par le déplacement (CharacterController coupé le temps du saut de position).
        public void Teleporter(Vector3 point)
        {
            bool actif = CC.enabled;
            CC.enabled = false;
            transform.position = point;
            m_VitesseY = 0f;
            m_SommetChute = point.y;   // une téléportation n'est pas une chute
            Physics.SyncTransforms();
            CC.enabled = actif;
        }

        /// Marionnette (multijoueur) : le propriétaire vient de mourir ou de réapparaître (état tenu par l'hôte). La mort
        /// (animation) arrive par le NetworkAnimator ; ici l'état (les squelettes ne la visent plus) et la dissolution.
        public void MortDistante(bool mort)
        {
            if (!Distant) return;
            if (mort)
            {
                m_EtatCourant = Etat.Mort;
                Invoke(nameof(Dissoudre), 1.0f);
            }
            else
            {
                CancelInvoke(nameof(Dissoudre));
                m_EtatCourant = Etat.Libre;
                Invoke(nameof(RendreVisible), 0.35f);
            }
        }

        void RendreVisible() { foreach (var r in GetComponentsInChildren<Renderer>(true)) r.enabled = true; }

        /// Déclencheur d'animation : par le NetworkAnimator en réseau (les autres postes le jouent aussi), sinon direct.
        public void Declencher(int hash)
        {
            if (animator == null) return;
            if (m_AnimReseau != null && m_AnimReseau.IsSpawned) m_AnimReseau.SetTrigger(hash);
            else animator.SetTrigger(hash);
        }

        public void Declencher(string nom) => Declencher(Animator.StringToHash(nom));

        /// Portail (DonjonJeu.Transit) : joue le clip d'arrivée en entier, sans contrôle (EnTransit), par le
        /// NetworkAnimator comme les emotes — les autres postes voient la même séquence sur la marionnette.
        /// `air` : vrai (Spawn_Air, au donjon), faux (Spawn_Ground, au village ou au rappel de Nyxessa).
        public void DeclencherPortail(bool air)
        {
            if (animator == null) return;
            animator.SetBool(P_PortailAir, air);
            Declencher(P_PortailArrivee);
        }

        /// Annule un déclencheur resté armé (pas encore consommé), ici et chez les autres postes.
        public void AnnulerDeclencheur(int hash)
        {
            if (animator == null) return;
            if (m_AnimReseau != null && m_AnimReseau.IsSpawned) m_AnimReseau.ResetTrigger(hash);
            else animator.ResetTrigger(hash);
        }

        public void Initialiser(Partie partie, EtatJoueur etat)
        {
            Partie = partie;
            m_Etat = etat;
            m_Camera = Distant ? null : partie.cameraJeu;
            if (Classe != null) Classe.Initialiser(this);
            Sante.Initialiser(Classe != null ? Classe.PvMax : B.herosPV);
            Sante.invulnerable = B.joueurInvincible;
            Sante.intercepteur = Intercepter;
            Sante.Touche += OnTouche;
            Sante.Intercepte += (i, r) => { if (Classe != null) Classe.SurIntercepte(i, r); };
            Sante.Tue += OnTue;
            Sante.Soigne += reel => { if (Partie != null) Partie.CompterSoins(Id, reel); };
            m_Endurance = B.endurance;
            if (!Distant) Entrees.Action += OnAction;
        }

        /// Recopie l'état qui fait foi dans l'EtatJoueur (lu par le HUD et, plus tard, par le réseau).
        public void EcrireEtat(EtatJoueur j)
        {
            j.pv = Sante.Pv;
            j.pvMax = Sante.pvMax;
            j.endurance = m_Endurance;
            j.enduranceMax = B.endurance;
            if (Classe != null)
            {
                j.jauge = Classe.ValeurJauge;
                j.jaugeMax = Classe.JaugeMax;
                j.furtif = Classe.Furtif;
            }
        }

        // ----------------------------------------------------------------- Services pour les classes

        public Camera CameraJeu => m_Camera != null ? m_Camera.GetComponent<Camera>() : Camera.main;
        public CameraEpaule CameraEpaule => m_Camera;
        public Vector3 AvantCamera => m_Camera != null ? m_Camera.AvantPlat : transform.forward;

        public Vector3 DirectionEntree()
        {
            Vector2 d = Entrees.Deplacement;
            if (d.sqrMagnitude < 0.01f) return Vector3.zero;
            Vector3 avant = AvantCamera;
            Vector3 droite = Vector3.Cross(Vector3.up, avant);
            return avant * d.y + droite * d.x;
        }

        public bool Depenser(float cout)
        {
            if (m_Endurance < cout) return false;
            m_Endurance -= cout;
            m_EnduranceUtilisee = Time.time;
            return true;
        }

        /// Endurance vidée (garde brisée).
        public void ViderEndurance() { m_Endurance = 0f; m_EnduranceUtilisee = Time.time; }

        public void Invulnerable(float duree) { m_InvulnerableJusque = Mathf.Max(m_InvulnerableJusque, Time.time + duree); }
        public bool EstInvulnerable => Time.time < m_InvulnerableJusque;

        /// Couche « haut du corps » forcée un instant (coup bloqué, touché…).
        public void HautDuCorpsPendant(float duree) { m_HautJusque = Mathf.Max(m_HautJusque, Time.time + duree); }

        public void Tourner(Vector3 vers)
        {
            vers.y = 0f;
            if (vers.sqrMagnitude > 0.0001f) transform.rotation = Quaternion.LookRotation(vers.normalized);
        }

        /// Étourdit le héros (garde brisée).
        public void Etourdir(float duree)
        {
            if (Classe != null) Classe.Interrompre();
            m_EtatCourant = Etat.Etourdi;
            m_Etourdi = duree;
            if (Statuts != null) Statuts.Ajouter(TypeStatut.Etourdi, duree, 1f, OrigineStatut.Ennemi);
        }

        /// Renverse le héros (statut Renversé, 26/09/2026 : chute à la renverse, un instant au sol, puis relevé, sans
        /// contrôle) : charge écrasante de Morgrim massue, onde de choc non sautée, grosse chute. Ne s'applique que sur
        /// le vrai héros du propriétaire (Heros.Update ne tourne que là) : chez l'hôte, pour le héros d'un autre poste,
        /// la demande part par HerosReseau (RPC hôte → propriétaire) plutôt que de changer l'état localement ici.
        public void Renverser()
        {
            if (m_ReseauHeros != null && m_ReseauHeros.IsSpawned && !m_ReseauHeros.IsOwner) { m_ReseauHeros.Renverser(); return; }
            RenverserLocal();
        }

        /// Propriétaire (ou solo) : déroule vraiment la séquence. Appelé directement (Renverser ci-dessus) ou par
        /// HerosReseau.RenverserRpc quand l'hôte l'a décidé pour ce joueur.
        public void RenverserLocal()
        {
            // Invulnérable (réapparition, joueurInvincible) : ni dégâts ni chute, même pour l'onde du Fracas qui ignore
            // la parade et la roulade (elle renversait un héros qui ne prenait pourtant aucun dégât).
            if (!Vivant || Sante.invulnerable) return;
            if (Classe != null) Classe.Interrompre();
            var b = B;
            m_EtatCourant = Etat.Renverse;
            m_EtatDepuis = 0f;
            m_RenverseChute = Mathf.Max(0.1f, b.renverseChuteDuree);
            m_RenverseAuSol = Mathf.Max(0f, b.renverseAuSolDuree);
            m_RenverseReleve = b.renverseReleveDuree / Mathf.Max(0.1f, b.renverseReleveVitesse);
            m_RenverseDureeTotale = m_RenverseChute + m_RenverseAuSol + m_RenverseReleve;
            m_RenverseMartelement = 0f;
            m_RenverseDerniereReduction = -99f;
            m_RenverseReleveDeclenche = false;
            if (animator != null)
            {
                animator.SetFloat(P_RenverseVitesse, b.renverseReleveVitesse);
                Declencher(P_RenverseChute);
            }
            if (Statuts != null) Statuts.Ajouter(TypeStatut.Renverse, m_RenverseDureeTotale, 1f, OrigineStatut.Ennemi);
        }

        /// Martelage de Saut pendant le Renversé (touche unique, accessibilité « maintenir » : OptionsJoueur) : raccourcit
        /// le temps au sol puis le relevé, jamais plus de moitié moins (GameBalance.renverseMartelementPlafond).
        void Marteler()
        {
            if (m_EtatCourant != Etat.Renverse) return;
            var b = B;
            m_RenverseDerniereReduction = Time.time;
            float plafond = m_RenverseDureeTotale * b.renverseMartelementPlafond;
            m_RenverseMartelement = Mathf.Min(m_RenverseMartelement + b.renverseMartelementReduction, plafond);
        }

        /// Applique un coup du héros : dégâts, score, retour à la classe (rage, mana). Renvoie les dégâts réels.
        public float Frapper(Sante s, float degats, bool critique, Vector3 point, Vector3 direction, bool parBoule = false, bool continu = false)
        {
            if (s == null || s.Mort) return 0f;
            float reel = s.Encaisser(new InfoDegats
            {
                montant = degats, sourceId = Id, equipeSource = Equipe.Heros, source = gameObject, critique = critique,
                point = point, direction = direction, continu = continu
            });
            if (reel > 0f)
            {
                if (Partie != null) Partie.CompterDegats(Id, reel, critique);
                if (Classe != null) Classe.SurCoupDonne(s, reel, parBoule);
            }
            return reel;
        }

        public float Frapper(Sante s, float degats, bool critique = false)
        {
            Vector3 d = s.transform.position - transform.position;
            return Frapper(s, degats, critique, s.transform.position + Vector3.up, d.sqrMagnitude > 0.0001f ? d.normalized : transform.forward);
        }

        /// Esquive déclenchée par une classe (roulade arrière du rôdeur) : même mouvement que l'esquive commune.
        /// `arriere` : roulade imposée (toujours Dodge_Backward). Sinon, `clipDir` choisit le clip directionnel
        /// (0 Avant, 1 Droite, 2 Arrière, 3 Gauche ; DirectionClip) de l'esquive commune.
        public void EsquiveImposee(Vector3 direction, bool arriere, int clipDir = 0)
        {
            m_DirEsquive = direction.normalized;
            m_EsquiveArriere = arriere;
            m_EtatCourant = Etat.Esquive;
            m_EtatDepuis = 0f;
            Invulnerable(B.esquiveInvulnerable);
            if (animator == null) return;
            if (arriere) { Declencher(P_DodgeBack); return; }
            animator.SetInteger(P_DodgeDir, clipDir);
            Declencher(P_Dodge);
        }

        /// Pendant une ruée ou un bond, le héros traverse les squelettes (sauf `sauf`) ; seul le décor l'arrête.
        readonly System.Collections.Generic.List<Collider> m_Ignores = new System.Collections.Generic.List<Collider>();
        public void TraverserEnnemis(bool traverser, Squelette sauf = null)
        {
            foreach (var c in m_Ignores) if (c != null) Physics.IgnoreCollision(CC, c, false);
            m_Ignores.Clear();
            if (!traverser) return;
            var dv = DirecteurVagues.Instance;
            if (dv == null) return;
            foreach (var s in dv.Vivants)
            {
                if (s == null || s == sauf) continue;
                foreach (var c in s.GetComponentsInChildren<Collider>()) { Physics.IgnoreCollision(CC, c, true); m_Ignores.Add(c); }
            }
        }

        // ----------------------------------------------------------------- Entrées

        void OnAction(string action)
        {
            if (Partie == null) return;
            if (action == "Ready") { Partie.BasculerPret(Id); return; }
            if (EnTransit) return;
            if (action == "CharacterMenu") { if (Partie.EnCours) (Deathless.UI.Donnees.DonneesUI.Personnage as MenuPersonnage)?.Ouvrir(); return; }
            if (!Vivant || !Partie.EnCours) return;
            if (Emotes != null && Emotes.SurAction(action)) return;   // roue à emotes ; toute autre action l'interrompt
            // Renversé (26/09/2026) : sans contrôle, seul Saut compte (martelage, accélère le relevé) ; il ne fait pas sauter.
            if (m_EtatCourant == Etat.Renverse) { if (action == "Jump") Marteler(); return; }
            switch (action)
            {
                case "Interact": PointInteraction.InteragirIci(this); break;
                case "Jump": Sauter(); break;
                case "Dodge": Esquiver(); break;
                default:
                    if (m_EtatCourant == Etat.Libre && Classe != null) Classe.SurAction(action);
                    break;
            }
        }

        void Sauter()
        {
            if (!PeutAgir || !m_AuSol || !Depenser(B.sautCout)) return;
            m_VitesseY = Mathf.Sqrt(2f * B.gravite * B.hauteurSaut);
            m_AuSol = false;
            if (animator != null) Declencher(P_Jump);
            AudioBank.Jouer(SonsDuJeu.Saut, transform.position, 0.5f);
            if (Classe != null) Classe.DiffuserCommun(ClasseHeros.EffetSaut);
        }

        /// Esquive directionnelle (décision de Quentin, 26/09/2026) : le héros ne pivote plus avant d'esquiver, il garde
        /// sa face vers la visée, la caméra ou la cible (`FaceVisee`). Le déplacement suit la direction réelle du stick
        /// (relative à la caméra, comme avant) ; le clip joué (Dodge_Forward/Right/Backward/Left) dépend de cette
        /// direction par rapport à la face du héros. Sans direction au stick : esquive arrière (réflexe classique).
        void Esquiver()
        {
            if (m_EtatCourant != Etat.Libre || m_RechargeEsquive > 0f || (Classe != null && !Classe.PeutEsquiver) || !Depenser(B.esquiveCout)) return;
            if (Classe != null) Classe.Interrompre();
            Vector3 face = Classe != null && Classe.FaceVisee ? FaceDeVisee() : transform.forward;
            Vector3 d = DirectionEntree();
            Vector3 dir = d.sqrMagnitude > 0.01f ? d.normalized : -face;
            m_RechargeEsquive = B.esquiveRecharge;
            EsquiveImposee(dir, false, DirectionClip(dir, face));
            AudioBank.Jouer(SonsDuJeu.Esquive, transform.position + Vector3.up, 0.8f);
            if (Classe != null) Classe.DiffuserCommun(ClasseHeros.EffetEsquive);
        }

        /// Quadrant du clip d'esquive par rapport à la face (0 Avant, 1 Droite, 2 Arrière, 3 Gauche), au plus proche.
        static int DirectionClip(Vector3 dir, Vector3 face)
        {
            float a = Vector3.SignedAngle(face, dir, Vector3.up);
            if (a > -45f && a <= 45f) return 0;     // Avant
            if (a > 45f && a <= 135f) return 1;      // Droite
            if (a < -45f && a >= -135f) return 3;    // Gauche
            return 2;                                // Arrière
        }

        Interception Intercepter(InfoDegats info)
        {
            if (EstInvulnerable) return Interception.Bloque;   // esquive : coup évité
            return Classe != null ? Classe.Intercepter(info) : Interception.Passe;
        }

        void OnTouche(InfoDegats info, float reel)
        {
            if (reel <= 0f) return;
            AudioBank.Jouer(SonsDuJeu.JoueurTouche, transform.position + Vector3.up, 0.9f, 0.2f);
            if (animator != null && !Sante.Mort) { Declencher(P_Hit); HautDuCorpsPendant(0.6f); }
            if (Classe != null) Classe.SurTouche(info, reel);
        }

        void OnTue(InfoDegats info)
        {
            if (Classe != null) Classe.Interrompre();
            m_EtatCourant = Etat.Mort;
            m_EtatDepuis = 0f;
            CC.enabled = false;
            if (animator != null) animator.SetBool(P_Dead, true);
            AudioBank.Jouer(SonsDuJeu.JoueurMort, transform.position + Vector3.up, 1f);
            if (Partie != null) Partie.SignalerMort(Id);
            Invoke(nameof(Dissoudre), 1.0f);
        }

        void Dissoudre()
        {
            if (m_EtatCourant != Etat.Mort) return;
            AudioBank.Jouer(SonsDuJeu.EnergieMort, transform.position + Vector3.up, 0.8f);
            if (MortAllie.Instance != null) MortAllie.Instance.Mourir(gameObject, Nyxessa.Instance);
            else foreach (var r in GetComponentsInChildren<Renderer>()) r.enabled = false;
        }

        /// Réapparition près de Nyxessa (appelée par Partie).
        public void Reapparaitre(Vector3 point)
        {
            CancelInvoke(nameof(Dissoudre));
            m_EtatCourant = Etat.Reapparition;
            m_EtatDepuis = 0f;
            Sante.Ranimer();
            m_Endurance = B.endurance;
            CC.enabled = false;
            transform.position = point;
            Vector3 vers = -new Vector3(point.x, 0f, point.z);
            if (vers.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(-vers.normalized);
            if (animator != null) { animator.SetBool(P_Dead, false); Declencher(P_Respawn); }
            System.Action fin = () =>
            {
                m_EtatCourant = Etat.Libre;
                CC.enabled = true;
                Invulnerable(B.reapparitionInvulnerable);
                AudioBank.Jouer(SonsDuJeu.Reapparition, transform.position + Vector3.up, 1f);
            };
            if (MortAllie.Instance != null) MortAllie.Instance.Reapparaitre(gameObject, point, Nyxessa.Instance, fin);
            else { foreach (var r in GetComponentsInChildren<Renderer>()) r.enabled = true; fin(); }
        }

        // ----------------------------------------------------------------- Boucle

        void Update()
        {
            float dt = Time.deltaTime;
            var b = B;
            m_EtatDepuis += dt;
            m_RechargeEsquive = Mathf.Max(0f, m_RechargeEsquive - dt);
            Sante.invulnerable = b.joueurInvincible || EstInvulnerable && m_EtatCourant != Etat.Esquive;

            // Visière : abaissée la nuit (crépuscule compris), relevée le jour.
            if (visiere != null && Partie != null)
            {
                var ph = Partie.Etat.phase;
                visiere.open = !(ph == Phase.Crepuscule || ph == Phase.Nuit);
            }
            if (Distant) return;
            // Filet de sécurité : un héros passé sous le sol revient au point de réapparition le plus proche.
            if (transform.position.y < -25f && Partie != null)
            {
                Debug.LogWarning("[Héros] " + name + " sous le sol (" + transform.position + "), replacé");
                Teleporter(Partie.PointReapparition(transform.position) + Vector3.up * 0.1f);
            }

            if (m_Camera != null && m_EtatCourant != Etat.Mort && (Emotes == null || !Emotes.RoueOuverte)) { Vector2 r = Entrees.Regard(); m_Camera.Tourner(r.x, r.y); }

            if (m_EtatCourant == Etat.Mort || m_EtatCourant == Etat.Reapparition || !CC.enabled)
            {
                m_SommetChute = transform.position.y;
                MajAnimation(0f);
                return;
            }

            bool enJeu = EnJeu;
            Vector3 dir = enJeu ? DirectionEntree() : Vector3.zero;
            if (Emotes != null && m_EtatCourant == Etat.Libre) dir = Emotes.FiltrerDeplacement(dir);   // emote : immobile, se relève
            Vector3 deplacement = Vector3.zero;
            float vitesseAnim = 0f;
            bool impose = false;

            if (Classe != null) Classe.Temps(dt);
            if (m_EtatCourant == Etat.Libre && Classe != null && enJeu) Classe.Maj(dt, dir);

            switch (m_EtatCourant)
            {
                case Etat.Libre:
                {
                    if (EnTransit) { deplacement = Vector3.zero; break; }
                    if (Classe != null && Classe.DeplacementImpose(dt, out Vector3 v)) { deplacement = v; impose = true; break; }
                    // Ivresse (taverne) : démarche hésitante, la direction de marche ondule (pas pendant une action de classe).
                    if (Ivresse.Active && !Distant && dir.sqrMagnitude > 0.01f && (Classe == null || !Classe.Occupe)) dir = Quaternion.Euler(0f, Ivresse.Deviation, 0f) * dir;
                    float facteur = Classe != null ? Classe.FacteurVitesse : 1f;
                    bool occupe = Classe != null && Classe.Occupe;
                    m_Sprint = Entrees.SprintMaintenu && dir.sqrMagnitude > 0.01f && m_Endurance > 0f && !occupe && facteur >= 0.99f && (Classe == null || !Classe.BloqueSprint);
                    // Statut Ralenti (chute…) : vitesse et cadence de marche réduites.
                    float ralenti = Statuts != null ? Statuts.FacteurVitesse : 1f;
                    float vitesse = (Classe != null ? Classe.Vitesse : b.vitesse) * (m_Sprint ? b.sprintMultiplicateur : 1f) * facteur
                        * Deathless.Donjon.ZoneEau.FacteurEn(transform.position + Vector3.up * 0.2f)   // eau du donjon : × 0,6
                        * ralenti;
                    if (m_Sprint) { m_Endurance = Mathf.Max(0f, m_Endurance - b.sprintCout * dt); m_EnduranceUtilisee = Time.time; }
                    deplacement = dir * vitesse;
                    Vector3 face = Classe != null && Classe.FaceVisee ? FaceDeVisee() : dir;
                    if (face.sqrMagnitude > 0.01f)
                        transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(face), 720f * dt);
                    vitesseAnim = dir.magnitude * (m_Sprint ? 1f : 0.62f) * (Classe != null ? Classe.FacteurAnimation : 1f);
                    if (facteur < 0.99f) vitesseAnim *= Mathf.Max(0.5f, facteur);
                    if (ralenti < 0.99f) vitesseAnim *= Mathf.Max(0.5f, ralenti);
                    break;
                }
                case Etat.Esquive:
                {
                    float k = Mathf.Clamp01(m_EtatDepuis / b.esquiveDuree);
                    float v = 2f * b.esquiveDistance / b.esquiveDuree * (1f - k);   // départ franc, freinage (intégrale = distance)
                    deplacement = m_DirEsquive * v;
                    if (m_EtatDepuis >= b.esquiveDuree + 0.1f) m_EtatCourant = Etat.Libre;
                    break;
                }
                case Etat.Etourdi:
                    m_Etourdi -= dt;
                    if (m_Etourdi <= 0f) m_EtatCourant = Etat.Libre;
                    break;
                case Etat.Renverse:
                {
                    // Accessibilité « maintenir » (OptionsJoueur) : Saut maintenu martèle tout seul, au même rythme maximal.
                    if (!Distant && OptionsJoueur.RelevageMaintenir && Entrees.SautMaintenu
                        && Time.time - m_RenverseDerniereReduction >= b.renverseMartelementIntervalleMaintenir)
                        Marteler();
                    float ecoule = m_EtatDepuis + m_RenverseMartelement;
                    if (!m_RenverseReleveDeclenche && ecoule >= m_RenverseChute + m_RenverseAuSol)
                    {
                        m_RenverseReleveDeclenche = true;
                        if (animator != null) Declencher(P_RenverseRelevage);
                    }
                    if (ecoule >= m_RenverseDureeTotale) m_EtatCourant = Etat.Libre;
                    break;
                }
            }

            // Endurance : régénération après un court délai sans dépense.
            if (Time.time - m_EnduranceUtilisee > b.enduranceDelai)
                m_Endurance = Mathf.Min(b.endurance, m_Endurance + b.enduranceRegen * dt);

            // Gravité et saut.
            bool etaitAuSol = m_AuSol;
            if (m_AuSol && m_VitesseY < 0f) m_VitesseY = -2f;
            m_VitesseY -= b.gravite * dt;
            if (impose) m_VitesseY = Mathf.Min(m_VitesseY, -2f);
            var flags = CC.Move((deplacement + Vector3.up * m_VitesseY) * dt);
            m_AuSol = (flags & CollisionFlags.Below) != 0 || CC.isGrounded;
            SuivreChute(etaitAuSol, impose || EnTransit);
            if (m_AuSol && !etaitAuSol && m_VitesseY < -6f) AudioBank.Jouer(SonsDuJeu.Reception, transform.position, 0.6f);
            if (impose && (flags & CollisionFlags.Sides) != 0 && Classe != null) Classe.SurCollisionCote();

            // Pas.
            if (m_AuSol && deplacement.sqrMagnitude > 1f && m_EtatCourant == Etat.Libre && !impose)
            {
                m_Pas -= dt * deplacement.magnitude / 5f;
                if (m_Pas <= 0f) { m_Pas = 0.42f; AudioBank.Jouer(SonsDuJeu.Pas, transform.position, 0.35f); }
            }
            MajAnimation(vitesseAnim);
        }

        public bool Sprinte => m_Sprint;

        // ----------------------------------------------------------------- Chute (wiki : statuts.md)

        float m_SommetChute = float.NegativeInfinity;   // point le plus haut depuis le dernier contact avec le sol

        /// Hauteur de chute : du point le plus haut atteint en l'air jusqu'au sol. Au-delà de GameBalance.chuteSeuil,
        /// dégâts proportionnels à la hauteur, puis statut Ralenti quelques secondes. Un saut sur place (1,2 m) reste bien
        /// en dessous. Une téléportation (Teleporter, réapparition, portail) et un déplacement imposé par la classe (ruée,
        /// bond du saut percutant) remettent la mesure à zéro.
        void SuivreChute(bool etaitAuSol, bool ignorer)
        {
            float y = transform.position.y;
            if (ignorer) { m_SommetChute = y; return; }
            if (!m_AuSol) { m_SommetChute = Mathf.Max(m_SommetChute, y); return; }
            if (!etaitAuSol)
            {
                float h = m_SommetChute - y;
                if (h > B.chuteSeuil) Chuter(h);
            }
            m_SommetChute = y;
        }

        /// Chute au-delà du seuil (propriétaire du héros) : dégâts, puis Ralenti (demandé à l'hôte en réseau, appliqué ici
        /// tout de suite).
        void Chuter(float hauteur)
        {
            var b = B;
            float degats = Mathf.Min(b.chuteDegatsMax, (hauteur - b.chuteSeuil) * b.chuteDegatsParMetre);
            if (Partie != null) Partie.Journal("Chute de " + hauteur.ToString("F1") + " m : " + degats.ToString("F0") + " dégâts, ralenti " + b.chuteRalentiDuree.ToString("F1") + " s");
            if (degats > 0f)
                Sante.Encaisser(new InfoDegats { montant = degats, equipeSource = Equipe.Ennemis, point = transform.position + Vector3.up * 0.2f, direction = Vector3.down });
            if (!Vivant) return;
            // Grosse chute (26/09/2026) : renversé avant le Ralenti habituel (statuts.md).
            if (hauteur > b.chuteRenverseSeuil) RenverserLocal();
            if (Statuts != null) Statuts.Ajouter(TypeStatut.Ralenti, b.chuteRalentiDuree, b.chuteRalentiForce, OrigineStatut.Chute);
        }

        /// Direction du corps en visée : vers la visée, corrigée du décalage de lacet de la pose de tir (l'arme regarde le
        /// réticule, pas le nez du personnage).
        Vector3 FaceDeVisee()
        {
            // Juste après un tir (lâcher), le décalage est gardé un instant : le corps ne pivote pas d'un coup.
            bool recent = Time.time - m_AligneDepuis < 0.45f;
            if ((!m_Aligne && !recent) || m_DirVisee.sqrMagnitude < 0.01f) return AvantCamera;
            return Quaternion.Euler(0f, -m_DecalageVisee, 0f) * m_DirVisee;
        }

        /// Après l'Animator : mesure la ligne de tir posée par l'animation (ClasseHeros.AxeDeTir), en tire le décalage de
        /// lacet (utilisé par Update au tour suivant) et penche le buste pour que l'arme suive le tangage de la visée.
        void LateUpdate()
        {
            AppliquerPenche();
            if (Distant || Classe == null) return;
            float k = 1f - Mathf.Exp(-Time.deltaTime * 14f);
            Vector3 o = Vector3.zero, axe = Vector3.zero;
            bool aligne = Classe.FaceVisee && Vivant && Classe.AxeDeTir(out o, out axe);
            if (aligne)
            {
                var cam = CameraJeu;
                Vector3 point = cam != null ? Combat.PointVise(cam, transform, 80f, out _) : o + AvantCamera * 30f;
                Vector3 voulu = point - o;
                Vector3 plat = new Vector3(voulu.x, 0f, voulu.z);
                Vector3 axePlat = new Vector3(axe.x, 0f, axe.z);
                if (plat.sqrMagnitude > 0.25f && axePlat.sqrMagnitude > 0.0001f)
                {
                    m_DirVisee = plat.normalized;
                    float decalage = Vector3.SignedAngle(new Vector3(transform.forward.x, 0f, transform.forward.z), axePlat, Vector3.up);
                    m_DecalageVisee = m_Aligne ? Mathf.LerpAngle(m_DecalageVisee, decalage, k) : decalage;
                    float tangageVoulu = Mathf.Atan2(voulu.y, plat.magnitude) * Mathf.Rad2Deg;
                    float tangageArme = Mathf.Atan2(axe.y, axePlat.magnitude) * Mathf.Rad2Deg;
                    m_PencheBuste = Mathf.Lerp(m_PencheBuste, Mathf.Clamp(tangageVoulu - tangageArme, -40f, 40f), k);
                }
                else aligne = false;
            }
            if (!aligne) m_PencheBuste = Mathf.Lerp(m_PencheBuste, 0f, k);
            m_Aligne = aligne;
            if (aligne) m_AligneDepuis = Time.time;
            // Buste : rotation autour de l'axe horizontal perpendiculaire à la visée (positif : l'arme monte).
            if (m_Buste != null && Mathf.Abs(m_PencheBuste) > 0.05f)
            {
                Vector3 d = m_DirVisee.sqrMagnitude > 0.01f ? m_DirVisee : transform.forward;
                Vector3 droite = Vector3.Cross(Vector3.up, d).normalized;
                m_Buste.rotation = Quaternion.AngleAxis(-m_PencheBuste, droite) * m_Buste.rotation;
            }
        }

        /// Penché voulu par la classe (ClasseHeros.Penche), lissé, appliqué au modèle autour de ses pieds. Aussi sur les
        /// marionnettes : la classe le déduit de l'état de l'Animator, répliqué par le NetworkAnimator.
        void AppliquerPenche()
        {
            if (m_Modele == null) return;
            float voulu = Classe != null && Vivant ? Classe.Penche : 0f;
            if (voulu == 0f && m_Penche == 0f) return;
            m_Penche = Mathf.MoveTowards(m_Penche, voulu, Time.deltaTime * 90f);
            m_Modele.localRotation = m_RotModele * Quaternion.Euler(m_Penche, 0f, 0f);
        }

        void MajAnimation(float vitesse)
        {
            if (animator == null) return;
            animator.SetFloat(P_Speed, vitesse, 0.1f, Time.deltaTime);
            animator.SetBool(P_Grounded, m_AuSol);
            if (m_CoucheHaut >= 0)
            {
                float voulu = (Classe != null && Classe.HautDuCorps && Vivant) || Time.time < m_HautJusque ? 1f : 0f;
                m_PoidsHaut = Mathf.MoveTowards(m_PoidsHaut, voulu, Time.deltaTime * 8f);
                animator.SetLayerWeight(m_CoucheHaut, m_PoidsHaut);
            }
        }
    }
}
