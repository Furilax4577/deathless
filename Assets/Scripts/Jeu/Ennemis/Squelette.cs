using System;
using UnityEngine;
using UnityEngine.AI;

namespace Deathless.Jeu
{
    /// Squelette (IA côté autorité, version 0.1) : sort de terre, marche vers Nyxessa (une place autour du plateau, dans
    /// son angle d'arrivée), poursuit un joueur proche puis revient, frappe au contact (préparation lisible et parable),
    /// peut être étourdi ou repoussé (parade, charge bélier), meurt puis se désintègre en gemmes couleur os. Le Golem, le
    /// Nécromancien, le Voleur et le Mage en dérivent (coup de zone ; distance, missile, invocation ; chasse des isolés ;
    /// tir à distance).
    [RequireComponent(typeof(NavMeshAgent), typeof(Sante))]
    public class Squelette : MonoBehaviour
    {
        /// Esquive (30/09/2026) : bond latéral ou arrière devant un héros qui arme un coup (ajouté en fin de liste).
        public enum Etat { SortieDeTerre, Marche, Poursuite, Preparation, Recuperation, Etourdi, Mort, Esquive }

        [Header("Réglages (posés par le directeur des vagues)")]
        public TypeEnnemi type;
        public bool elite;
        [Tooltip("Yeux rouges de l'élite, posés à l'apparition sur les maillages « *_Eyes ».")]
        public Material yeuxElite;
        [Tooltip("Modèle (enfant) : c'est lui qui sort de terre.")]
        public Transform modele;
        public Animator animator;
        [Tooltip("Durée de la sortie de terre (clip Skeletons_Spawn_Ground à la vitesse de GameBalance).")]
        public float dureeSortie = 1.6f;
        [Tooltip("Instant du coup dans le clip d'attaque (s, vitesse 1).")]
        public float instantCoupClip = 0.58f;

        public int Id { get; private set; }
        public Etat EtatCourant => m_Etat;
        /// Statuts (brûlure, étourdissement, ralenti, provocation ; Statuts.cs), sur tous les postes.
        public Statuts Statuts { get; private set; }
        public Sante Sante { get; private set; }
        public NavMeshAgent Agent { get; private set; }
        public bool Vivant => m_Etat != Etat.Mort;
        /// Vrai si ce squelette frappe Nyxessa (coup porté récemment ou coup en préparation contre elle).
        public bool SurNyxessa => m_Etat != Etat.Mort && ((m_Etat == Etat.Preparation && m_CibleNyxessa) || Time.time - m_DernierCoupNyxessa < GameBalance.Courant.surNyxessaDepuis);
        public float DegatsNyxessa => m_Stats.degatsNyxessa;
        public float Intervalle => m_Stats.intervalle;
        /// Élite, mini-boss ou boss : l'exécution de l'assassin (27/09/2026) fait ×3 au lieu d'achever.
        public bool EliteOuBoss => elite || type == TypeEnnemi.Golem || type == TypeEnnemi.Necromancien;
        /// Ennemi commun sous le seuil d'exécution (GameBalance.executionSeuil) : un coup de dague l'achève net.
        public bool Executable => Vivant && !EliteOuBoss && Sante != null && Sante.Ratio < GameBalance.Courant.executionSeuil;
        /// Progression du coup en préparation (0 hors préparation, vers 1 à l'instant de l'impact). Lisibilité du coup
        /// (piste « yeux qui s'intensifient », prototype temps 1, PreparationLisible.cs) : purement visuel, ne change
        /// rien à l'équité ou au réseau (Frapper reste seul juge de l'impact).
        public float PreparationProgress => m_Etat == Etat.Preparation ? Mathf.Clamp01(m_EtatDepuis / Mathf.Max(0.01f, m_Stats.preparation)) : 0f;
        public event Action<Squelette> Retire;     // désintégré (mort ou aube)
        /// Joueur poursuivi (null : Nyxessa ou personne) et coup en préparation : succès « Tir ami… presque » (Deathless.Succes).
        public Heros CibleJoueur => m_Cible;
        public bool EnPreparation => m_Etat == Etat.Preparation;

        protected StatsSquelette m_Stats = new StatsSquelette();
        protected Etat m_Etat = Etat.SortieDeTerre;
        protected float m_EtatDepuis;
        protected Heros m_Cible;             // joueur poursuivi
        protected bool m_CibleNyxessa;
        protected float m_DernierCoup = -99f, m_DernierCoupNyxessa = -99f;
        protected float m_SansFrapper;
        protected Vector3 m_Place;
        protected float m_Etourdi;
        // Riposte au contact de Nyxessa (wiki : ennemis.md, décidé 27/09/2026) : compteur des coups reçus d'un même héros
        // à moins de riposteDistance ; puis poursuite bornée (riposteDuree s ou riposteAttaques coups) avant le retour.
        int m_RiposteHerosId;
        int m_RiposteCoups;
        float m_RiposteDernierCoup = -99f;
        float m_RiposteJusque = -99f;
        int m_RiposteAttaques;
        protected bool m_EnRiposte;
        /// Tests : ce squelette poursuit un héros par riposte (au lieu de frapper Nyxessa).
        public bool EnRiposte => m_EnRiposte;
        Vector3 m_Pousse; float m_PousseReste;
        bool m_Desintegre;
        static int s_Ids;

        protected static readonly int P_Speed = Animator.StringToHash("Speed");
        protected static readonly int P_Attack = Animator.StringToHash("Attack");
        protected static readonly int P_VitesseAttaque = Animator.StringToHash("VitesseAttaque");
        protected static readonly int P_Hit = Animator.StringToHash("Hit");
        protected static readonly int P_Stun = Animator.StringToHash("Stun");
        protected static readonly int P_Dead = Animator.StringToHash("Dead");
        protected static readonly int P_Dodge = Animator.StringToHash("Dodge");
        /// Direction du clip d'esquive par rapport à la face (0 Avant, 1 Droite, 2 Arrière, 3 Gauche ; comme le héros).
        protected static readonly int P_DodgeDir = Animator.StringToHash("DodgeDir");

        protected Partie P => Partie.Instance;
        protected GameBalance B => GameBalance.Courant;
        protected Transform Nyxessa => P != null && P.nyxessa != null ? P.nyxessa.transform : null;

        /// Gardien du butin au donjon : il reste à son poste, poursuit les joueurs qui approchent, n'attaque jamais
        /// Nyxessa et ne rapporte pas d'or (le butin du donjon est dans les coffres).
        public bool Gardien { get; private set; }
        public void Garder(Vector3 poste) { Gardien = true; m_Place = poste; m_AvecPassage = false; m_FacteurPas = 1f; }
        /// Poste d'un gardien (sa place ; pour les autres, la place autour de Nyxessa).
        public Vector3 Poste => m_Place;

        protected virtual void Awake()
        {
            Id = ++s_Ids;
            Sante = GetComponent<Sante>();
            Statuts = Statuts.De(this);
            Agent = GetComponent<NavMeshAgent>();
            if (animator == null) animator = GetComponentInChildren<Animator>();
            Sante.equipe = Equipe.Ennemis;
            Sante.Touche += OnTouche;
            Sante.Tue += OnTue;
            m_Reseau = GetComponent<Deathless.Reseau.EnnemiReseau>();
            if (modele != null) Tete = MannequinEquip.Trouver(modele, "head");
            if (modele != null)
                foreach (var r in modele.GetComponentsInChildren<Renderer>())
                    if (r.name.EndsWith("_Head")) m_RenduTete = r;
            // Yeux qui s'intensifient pendant la préparation du coup (lisibilité, décidé le 26/09/2026, planche
            // lisibilite_cam_planche.png) : posé en code pour couvrir tous les squelettes (sbire, guerrier, élite, Golem,
            // Necromancien, Morgrim) sans éditer chaque prefab à la main ; purement visuel (PreparationLisible.cs).
            if (GetComponent<PreparationLisible>() == null) gameObject.AddComponent<PreparationLisible>();
        }

        Renderer m_RenduTete;
        /// Rendu du crâne (null si absent) : la rangée des statuts n'est montrée que s'il est affiché.
        public Renderer RenduTete => m_RenduTete;

        /// Os de la tête (base du crâne) ; le centre et le rayon de la zone de tête (tirs à la tête du rôdeur et de
        /// l'arbalète) viennent du maillage de la tête (gros crâne des squelettes KayKit).
        public Transform Tete { get; private set; }
        public Vector3 CentreTete => m_RenduTete != null ? m_RenduTete.bounds.center : (Tete != null ? Tete.position + Vector3.up * 0.3f : transform.position + Vector3.up * 1.4f);
        public float RayonTete => m_RenduTete != null ? m_RenduTete.bounds.extents.y * 1.05f : 0.3f;

        // ----------------------------------------------------------------- Vue : furtif, fumée, provocation

        Heros m_Provocateur;
        float m_ProvoqueJusque;

        // ----------------------------------------------------------------- Multijoueur (marionnette chez un client)

        protected Deathless.Reseau.EnnemiReseau m_Reseau;
        /// Client : ce squelette est tenu par l'hôte ; les actions des héros de ce poste lui sont envoyées.
        protected bool Distant => m_Reseau != null && m_Reseau.Distant;
        protected bool RelaiEtourdir(float duree, int sourceId) { if (!Distant) return false; m_Reseau.DemanderEtourdir(duree, sourceId); return true; }
        protected bool RelaiRepousser(Vector3 deplacement, float etourdi) { if (!Distant) return false; m_Reseau.DemanderRepousser(deplacement, etourdi); return true; }

        /// Rugissement du viking : ce squelette le prend pour cible pendant `duree` s (priorité sur Nyxessa).
        public virtual void Provoquer(Heros h, float duree)
        {
            if (m_Etat == Etat.Mort || h == null) return;
            if (Distant) { m_Reseau.DemanderProvoquer(duree); return; }
            m_Provocateur = h;
            m_ProvoqueJusque = Time.time + duree;
            m_EnRiposte = false;   // la provocation prend la place d'une riposte en cours
            if (Statuts != null) Statuts.Ajouter(TypeStatut.Provoque, duree, 1f, OrigineStatut.Joueur, h.Id);
            m_Cible = h;
            m_SansFrapper = 0f;
            if (m_Etat == Etat.Marche) m_Etat = Etat.Poursuite;
        }

        protected bool Provoque => m_Provocateur != null && m_Provocateur.Vivant && Time.time < m_ProvoqueJusque;
        /// Héros qui a provoqué ce squelette (rugissement), valable tant que Provoque est vrai.
        protected Heros Provocateur => m_Provocateur;

        /// Ce squelette voit-il le héros ? Personne n'est vu dans la fumée d'une grenade ; l'assassin furtif n'est repéré
        /// que dans le cône de vue (wiki : environ 6 m) ou tout près dans le dos (1,5 m). Un repérage le fait sortir du
        /// mode furtif.
        protected bool Voit(Heros h)
        {
            if (h == null) return false;
            if (ClasseAssassin.DansLaFumee(h.transform.position)) return false;
            var a = h.Classe as ClasseAssassin;
            if (a == null || !a.Furtif) return true;
            Vector3 d = h.transform.position - transform.position; d.y = 0f;
            float dist = d.magnitude;
            bool vu = dist <= B.assassinDetectionDos || (dist <= B.assassinDetectionVue && Vector3.Angle(transform.forward, d) <= B.assassinDetectionAngle);
            if (vu) a.Reperer();
            return vu;
        }

        /// Pose le squelette : stats, PV (multiplicateur de la nuit), place autour de Nyxessa.
        public virtual void Initialiser(StatsSquelette stats, float multiplicateurPV)
        {
            m_Stats = stats;
            Sante.Initialiser(stats.pv * multiplicateurPV);
            Agent.speed = stats.vitesse;
            Agent.stoppingDistance = 0.2f;
            Agent.acceleration = 12f;
            Agent.angularSpeed = 540f;
            var n = Nyxessa;
            Vector3 c = n != null ? n.position : Vector3.zero;
            Vector3 dir = transform.position - c; dir.y = 0f;
            if (dir.sqrMagnitude < 0.01f) dir = Vector3.forward;
            // Trajets variés (01/10/2026) : couloir tiré, place d'arrivée décalée du même côté, pas propre à chacun.
            bool varie = TrajetVarie;
            int couloirs = Mathf.Max(1, B.trajetCouloirs);
            int couloir = varie ? UnityEngine.Random.Range(0, couloirs) : 0;
            float cote = couloirs > 1 ? couloir / (float)(couloirs - 1) * 2f - 1f : 0f;   // -1 … 1
            float angle = varie ? Mathf.Clamp(cote * B.trajetAngleArrivee * 0.5f + UnityEngine.Random.Range(-0.5f, 0.5f) * B.trajetAngleArrivee, -B.trajetAngleArrivee, B.trajetAngleArrivee)
                                : UnityEngine.Random.Range(-35f, 35f);
            // Côté du couloir vu depuis la clairière (droite = +) ; la place est vue depuis Nyxessa : même côté = angle inverse.
            dir = Quaternion.Euler(0f, -angle, 0f) * dir.normalized;
            m_Place = c + dir * B.rayonPlacesNyxessa;
            if (NavMesh.SamplePosition(m_Place, out var hit, 2f, NavMesh.AllAreas)) m_Place = hit.position;
            m_FacteurPas = varie ? 1f + UnityEngine.Random.Range(-1f, 1f) * Mathf.Clamp(B.trajetEcartVitesse, 0f, 0.5f) : 1f;
            m_AvecPassage = varie && ChoisirPassage(cote);
            CommencerSortie();
        }

        // ----------------------------------------------------------------- Trajets variés (Quentin, 30/09/2026)

        float m_FacteurPas = 1f;
        Vector3 m_Passage;
        bool m_AvecPassage;
        static NavMeshPath s_Chemin;

        /// Tests : point de passage (couloir) de ce squelette vers Nyxessa, s'il en a un et ne l'a pas encore atteint.
        public bool AvecPassage => m_AvecPassage;
        public Vector3 Passage => m_Passage;
        public float FacteurPas => m_FacteurPas;

        /// Sbire, guerrier, voleur (élites compris) en marche vers Nyxessa ; pas les boss, le mage (MajDistance), les gardiens.
        protected virtual bool TrajetVarie => Agile && !Gardien;

        /// Pas de marche de ce squelette (pas de base × son facteur individuel, en marche seulement).
        float PasDeBase => m_Stats.vitesse * (m_Etat == Etat.Marche ? m_FacteurPas : 1f);

        static float LongueurChemin(Vector3 a, Vector3 b)
        {
            if (s_Chemin == null) s_Chemin = new NavMeshPath();
            if (!NavMesh.CalculatePath(a, b, NavMesh.AllAreas, s_Chemin) || s_Chemin.status != NavMeshPathStatus.PathComplete) return -1f;
            var coins = s_Chemin.corners;
            float l = 0f;
            for (int i = 1; i < coins.Length; i++) l += Vector3.Distance(coins[i - 1], coins[i]);
            return l;
        }

        /// Point de passage sur le couloir `cote` (-1 … 1) : à mi-trajet de l'axe clairière → place, décalé sur le côté, posé
        /// sur le NavMesh ; refusé si le chemin par lui dépasse le chemin direct de plus de trajetDetourMax (rivière, forêt
        /// impraticable) : le décalage est alors réduit de moitié, puis abandonné.
        bool ChoisirPassage(float cote)
        {
            Vector3 depart = transform.position;
            Vector3 axe = m_Place - depart; axe.y = 0f;
            float d = axe.magnitude;
            if (d < Mathf.Max(5f, B.trajetDistanceMin)) return false;
            float direct = LongueurChemin(depart, m_Place);
            if (direct <= 0f) return false;
            axe /= d;
            Vector3 droite = Vector3.Cross(Vector3.up, axe);
            float part = UnityEngine.Random.Range(B.trajetPartPassage.x, B.trajetPartPassage.y);
            float flou = B.trajetFlou;
            float lateral = cote * B.trajetLargeur * 0.5f + UnityEngine.Random.Range(-flou, flou);
            float long_ = UnityEngine.Random.Range(-flou, flou);
            for (int essai = 0; essai < 3; essai++)
            {
                Vector3 p = depart + axe * (d * part + long_) + droite * lateral;
                if (NavMesh.SamplePosition(p, out var h, 3f, NavMesh.AllAreas))
                {
                    float l1 = LongueurChemin(depart, h.position), l2 = l1 > 0f ? LongueurChemin(h.position, m_Place) : -1f;
                    if (l2 > 0f && l1 + l2 <= direct * (1f + B.trajetDetourMax)) { m_Passage = h.position; return true; }
                }
                lateral *= 0.5f;
            }
            return false;
        }

        /// Destination de la marche : le point de passage tant qu'il n'est pas atteint (ou dépassé, après une poursuite),
        /// puis la place autour de Nyxessa.
        Vector3 DestinationMarche()
        {
            if (!m_AvecPassage) return m_Place;
            Vector3 v = m_Passage - transform.position; v.y = 0f;
            Vector3 c = Nyxessa != null ? Nyxessa.position : m_Place;
            Vector3 pc = m_Passage - c, sc = transform.position - c; pc.y = 0f; sc.y = 0f;
            if (v.sqrMagnitude < 9f || sc.sqrMagnitude < pc.sqrMagnitude) m_AvecPassage = false;
            return m_AvecPassage ? m_Passage : m_Place;
        }

        protected virtual void CommencerSortie()
        {
            m_Etat = Etat.SortieDeTerre;
            m_EtatDepuis = 0f;
            Agent.isStopped = true;
            if (modele != null) modele.localPosition = Vector3.down * 1.9f;
            if (EffetsJeu.Terre != null) DirtBurst.Spawn(transform.position, EffetsJeu.Terre, 1f);
            AudioBank.Jouer(SonsDuJeu.SqueletteSortie, transform.position, 0.8f, 0.15f);
            m_TerreSeconde = false;
        }
        bool m_TerreSeconde;

        protected virtual void Update()
        {
            float dt = Time.deltaTime;
            m_EtatDepuis += dt;
            if (m_PousseReste > 0f && Agent.enabled)
            {
                float k = Mathf.Min(dt, m_PousseReste);
                Agent.Move(m_Pousse * (k / 0.2f));
                m_PousseReste -= k;
            }
            // Bouclier levé : jamais dans l'enceinte (01/10/2026), le squelette glisse le long de la paroi.
            var bouclier = BouclierNyxessa.Instance;
            if (bouclier != null && Agent.enabled && Agent.isOnNavMesh && m_Etat != Etat.Mort)
            {
                Vector3 dehors = bouclier.Repousser(transform.position, Agent.radius * 0.5f);
                if (dehors != Vector3.zero) Agent.Move(dehors);
            }
            // Eau du donjon (bassin) : ralentit (le NavMesh la contourne déjà quand c'est plus court en temps).
            // Statut Ralenti : même facteur, par-dessus l'eau.
            // Course (30/09/2026) : le pas de base devient une course (VitesseCourse) quand la cible est loin (Court).
            if (Agent.enabled && m_Stats.vitesse > 0f) Agent.speed = (Court ? VitesseCourse : PasDeBase) * Deathless.Donjon.ZoneEau.FacteurEn(transform.position + Vector3.up * 0.2f)
                * (Statuts != null ? Statuts.FacteurVitesse : 1f);
            if (animator != null) animator.SetFloat(P_Speed, Agent.enabled && !Agent.isStopped ? VitesseAnimation(Agent.velocity.magnitude) : 0f);
            // Invulnérable au début du bond d'esquive (comme le héros), recalculé à chaque image : rien ne reste bloqué.
            Sante.invulnerable = m_Etat == Etat.Esquive && m_EtatDepuis < B.esquiveEnnemiInvulnerable;
            if (P != null && (P.Etat.phase == Phase.Terminee || P.Etat.nyxessa.detruite) && m_Etat != Etat.Mort)
            {
                Agent.isStopped = true;
                return;
            }
            if (PeutEsquiverMaintenant) GuetterAttaques();
            GarderLaMarche(dt);
            switch (m_Etat)
            {
                case Etat.Esquive: MajEsquive(dt); break;
                case Etat.SortieDeTerre: MajSortie(); break;
                case Etat.Marche: MajMarche(dt); break;
                case Etat.Poursuite: MajPoursuite(dt); break;
                case Etat.Preparation: MajPreparation(); break;
                // Riposte décidée pendant un coup sur Nyxessa : la récupération est écourtée, il se retourne vite.
                case Etat.Recuperation: if (m_EtatDepuis >= RecuperationDuree || (m_EnRiposte && m_EtatDepuis >= 0.25f)) Reprendre(); else if (!OrientationFigee) Tourner(CiblePosition()); break;
                case Etat.Etourdi:
                    m_Etourdi -= dt;
                    if (m_Etourdi <= 0f) { if (animator != null) animator.SetBool(P_Stun, false); Reprendre(); }
                    break;
            }
        }

        /// Vrai pendant un coup qui garde sa direction (Charge écrasante de Morgrim massue) : pas de pivot vers la cible.
        protected virtual bool OrientationFigee => false;

        protected virtual float RecuperationDuree => Mathf.Max(0.2f, m_Stats.intervalle - m_Stats.preparation);

        void MajSortie()
        {
            float k = Mathf.Clamp01(m_EtatDepuis / Mathf.Max(0.1f, dureeSortie * 0.75f));
            float montee = 1f - (1f - k) * (1f - k);
            if (modele != null) modele.localPosition = Vector3.down * 1.9f * (1f - montee);
            if (!m_TerreSeconde && m_EtatDepuis >= dureeSortie * 0.35f)
            {
                m_TerreSeconde = true;
                if (EffetsJeu.Terre != null) DirtBurst.Spawn(transform.position, EffetsJeu.Terre, 0.55f);
            }
            if (m_EtatDepuis >= dureeSortie)
            {
                if (modele != null) modele.localPosition = Vector3.zero;
                Reprendre();
            }
        }

        /// Retour au comportement de base après une action.
        protected virtual void Reprendre()
        {
            m_Etat = m_Cible != null && m_Cible.Vivant ? Etat.Poursuite : Etat.Marche;
            m_EtatDepuis = 0f;
            if (Agent.enabled) Agent.isStopped = false;
        }

        // ----------------------------------------------------------------- Course et esquive (Quentin, 30/09/2026)

        bool m_Court;
        float m_EsquivePossible;          // Time.time à partir duquel une nouvelle esquive est permise (recharge)
        float m_AttaqueJugee = -99f;      // dernière attaque de héros déjà jugée (une seule chance par attaque)
        Vector3 m_EsquiveDir;
        Heros m_EsquiveDe;
        /// Tests : esquives faites par ce squelette.
        public int Esquives { get; private set; }

        /// Sbire, guerrier, voleur (élites compris) : courent en poursuite et esquivent. Le mage garde ses distances,
        /// Morgrim et le Nécromancien ont leur propre comportement.
        protected virtual bool Agile => type == TypeEnnemi.Sbire || type == TypeEnnemi.Guerrier || type == TypeEnnemi.Voleur;

        /// Court en ce moment : poursuite d'un héros loin de lui (jamais en marche vers Nyxessa).
        public bool Court => m_Court && m_Etat == Etat.Poursuite && Agile;

        /// Vitesse de course : vitesse × courseFacteur, plafonnée à courseVitesseMax (jamais sous le pas de base).
        protected float VitesseCourse => Mathf.Max(m_Stats.vitesse, Mathf.Min(m_Stats.vitesse * B.courseFacteur, B.courseVitesseMax));

        /// Paramètre Speed du blend tree : 0 arrêt, 0,5 pas de base (Walking_A), 1 course (Running_A, Squelette_Jeu).
        float VitesseAnimation(float v)
        {
            float vb = Mathf.Max(0.1f, PasDeBase);
            if (v <= vb || !Agile) return Mathf.Clamp01(v / vb) * 0.5f;
            float vc = VitesseCourse;
            return vc <= vb + 0.05f ? 0.5f : 0.5f + 0.5f * Mathf.Clamp01((v - vb) / (vc - vb));
        }

        /// Hystérésis de la course : au-delà de courseDistance il court, en deçà de courseDistanceArret il reprend le pas.
        void MajCourse(float distanceCible)
        {
            m_Court = m_Court ? distanceCible > B.courseDistanceArret : distanceCible > B.courseDistance;
        }

        /// Chance d'esquiver une attaque armée (GameBalance, par type ; 0 : jamais).
        protected virtual float ChanceEsquive
        {
            get
            {
                if (!Agile) return 0f;
                switch (type)
                {
                    case TypeEnnemi.Voleur: return B.esquiveChanceVoleur;
                    case TypeEnnemi.Guerrier: return B.esquiveChanceGuerrier;
                    default: return B.esquiveChanceSbire;
                }
            }
        }

        /// Hôte seulement (Update ne tourne pas chez les clients) ; jamais pendant son propre coup, étourdi, en sortie de
        /// terre ou mort ; recharge par squelette.
        bool PeutEsquiverMaintenant => (m_Etat == Etat.Marche || m_Etat == Etat.Poursuite || m_Etat == Etat.Recuperation)
            && Time.time >= m_EsquivePossible && Agent.enabled && ChanceEsquive > 0f
            && (Statuts == null || Statuts.FacteurVitesse > 0.01f);

        /// Un héros proche, tourné vers ce squelette, vient d'armer une attaque (Heros.DerniereAttaque, répliqué à l'hôte
        /// par l'effet commun EffetAttaque) : une chance d'esquiver par attaque.
        void GuetterAttaques()
        {
            if (P == null) return;
            var tous = P.TousLesHeros;
            float r2 = B.esquiveEnnemiDetection * B.esquiveEnnemiDetection;
            for (int i = 0; i < tous.Count; i++)
            {
                var h = tous[i];
                if (h == null || !h.Vivant || (h.Classe != null && h.Classe.Furtif)) continue;   // furtif : coup non vu venir
                float t = h.DerniereAttaque;
                if (t <= m_AttaqueJugee || Time.time - t > B.esquiveEnnemiFenetre) continue;
                Vector3 d = transform.position - h.transform.position; d.y = 0f;
                if (d.sqrMagnitude > r2 || Mathf.Abs(transform.position.y - h.transform.position.y) > 1.5f) continue;
                Vector3 f = h.transform.forward; f.y = 0f;
                if (d.sqrMagnitude > 0.01f && Vector3.Angle(f, d) > B.esquiveEnnemiAngle) continue;
                m_AttaqueJugee = t;
                if (UnityEngine.Random.value < ChanceEsquive) { Esquiver(h); return; }
            }
        }

        /// Bond d'esquive (comme le héros) : sur le côté ou en arrière par rapport au héros, vers le côté le plus dégagé
        /// du NavMesh ; clip Dodge_* selon la direction par rapport à la face ; invulnérable au début.
        protected void Esquiver(Heros h)
        {
            Vector3 loin = transform.position - h.transform.position; loin.y = 0f;
            loin = loin.sqrMagnitude > 0.01f ? loin.normalized : -transform.forward;
            Vector3 cote = Vector3.Cross(Vector3.up, loin) * (UnityEngine.Random.value < 0.5f ? 1f : -1f);
            bool lateral = UnityEngine.Random.value < B.esquiveEnnemiLaterale;
            Vector3[] essais = lateral ? new[] { cote, -cote, loin } : new[] { loin, cote, -cote };
            Vector3 dir = essais[0]; float meilleur = -1f;
            float l = B.esquiveEnnemiDistance;
            foreach (var e in essais)
            {
                float libre = NavMesh.Raycast(transform.position, transform.position + e * l, out var hit, NavMesh.AllAreas) ? hit.distance : l;
                if (libre >= l * 0.8f) { dir = e; meilleur = libre; break; }
                if (libre > meilleur) { meilleur = libre; dir = e; }
            }
            if (meilleur < 0.8f) return;   // coincé : pas d'esquive
            m_EsquiveDir = dir;
            m_EsquiveDe = h;
            m_Etat = Etat.Esquive;
            m_EtatDepuis = 0f;
            m_EsquivePossible = Time.time + UnityEngine.Random.Range(B.esquiveEnnemiRecharge.x, B.esquiveEnnemiRecharge.y);
            Esquives++;
            if (Agent.enabled) Agent.isStopped = true;
            // Face au héros pendant le bond : le clip dépend de la direction par rapport à cette face.
            Vector3 face = -loin;
            transform.rotation = Quaternion.LookRotation(face);
            float a = Vector3.SignedAngle(face, dir, Vector3.up);
            int clip = a > -45f && a <= 45f ? 0 : a > 45f && a <= 135f ? 1 : a < -45f && a >= -135f ? 3 : 2;
            if (animator != null) { animator.SetInteger(P_DodgeDir, clip); animator.SetTrigger(P_Dodge); }
            AudioBank.Jouer(SonsDuJeu.Esquive, transform.position + Vector3.up, 0.6f, 0.1f);
            // Le héros qui l'arme devient la cible (sauf provocation ou riposte en cours) : l'esquive mène à la contre-attaque.
            if (!Provoque && !m_EnRiposte && (Gardien || DistanceNyxessa() > RayonContact)) { m_Cible = h; m_SansFrapper = 0f; }
        }

        void MajEsquive(float dt)
        {
            float duree = Mathf.Max(0.05f, B.esquiveEnnemiDuree);
            float k = Mathf.Clamp01(m_EtatDepuis / duree);
            float v = 2f * B.esquiveEnnemiDistance / duree * (1f - k);   // départ franc, freinage (intégrale = distance)
            if (Agent.enabled && k < 1f) Agent.Move(m_EsquiveDir * v * dt);
            if (m_EsquiveDe != null) Tourner(m_EsquiveDe.transform.position);
            if (m_EtatDepuis >= duree + 0.1f) Reprendre();
        }

        protected Heros JoueurProche(float rayon)
        {
            if (P == null) return null;
            Heros meilleur = null;
            float d = rayon * rayon;
            var tous = P.TousLesHeros;
            for (int i = 0; i < tous.Count; i++)
            {
                var h = tous[i];
                if (h == null || !h.Vivant) continue;
                float dd = (h.transform.position - transform.position).sqrMagnitude;
                if (dd >= d) continue;
                // Gardien (30/09/2026) : seulement un héros en ligne de vue (murs) et dans la laisse de son poste.
                if (Gardien && (!DansLaLaisse(h) || !LigneDeVue(h))) continue;
                if (Voit(h)) { d = dd; meilleur = h; }
            }
            return meilleur;
        }

        // ----------------------------------------------------------------- Gardiens du donjon (30/09/2026)

        /// Le héros est à moins de gardienLaisse m du poste de ce gardien.
        protected bool DansLaLaisse(Heros h) => h != null && (h.transform.position - m_Place).sqrMagnitude <= B.gardienLaisse * B.gardienLaisse;

        static readonly RaycastHit[] s_Vue = new RaycastHit[16];

        /// Rien de solide entre les yeux du squelette et le buste du héros : les personnages (tout ce qui porte une Sante)
        /// ne bloquent pas ; murs, sols, plafonds et gros décors bloquent.
        protected bool LigneDeVue(Heros h)
        {
            Vector3 a = transform.position + Vector3.up * 1.5f, b = h.transform.position + Vector3.up * 1.2f;
            Vector3 v = b - a;
            float l = v.magnitude;
            if (l < 0.05f) return true;
            int n = Physics.RaycastNonAlloc(a, v / l, s_Vue, l, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
                if (s_Vue[i].collider.GetComponentInParent<Sante>() == null) return false;
            return true;
        }

        /// Alerte (gardiens) : les gardiens à moins de gardienAlerte m qui ne poursuivent personne prennent `h` pour cible.
        /// Une seule vague d'alerte (un gardien alerté ne relaie pas).
        protected void AlerterGardiens(Heros h)
        {
            var dv = DirecteurVagues.Instance;
            if (!Gardien || dv == null || h == null) return;
            float r2 = B.gardienAlerte * B.gardienAlerte;
            int n = 0;
            foreach (var s in dv.Vivants)
            {
                if (s == null || s == this || !s.Gardien || !s.Vivant || s.m_Cible != null) continue;
                if ((s.transform.position - transform.position).sqrMagnitude > r2) continue;
                if (s.m_Etat != Etat.Marche && s.m_Etat != Etat.Recuperation) continue;
                s.m_Cible = h; s.m_SansFrapper = 0f; s.m_Etat = Etat.Poursuite; s.m_EtatDepuis = 0f;
                if (s.Agent.enabled) s.Agent.isStopped = false;
                n++;
            }
            if (n > 0 && P != null) P.Journal("Donjon : gardien " + Id + " alerte " + n + " gardien(s) contre le joueur " + h.Id);
        }

        /// Joueur pris pour cible pendant la marche (MajMarche) : le plus proche vu à moins de `rayon` m. Le voleur
        /// (Voleur.cs, 28/09/2026) préfère un joueur isolé ; les autres types gardent ce choix.
        protected virtual Heros ChoisirCible(float rayon) => JoueurProche(rayon);

        /// Distance de frappe de Nyxessa : bouclier levé, les squelettes frappent la paroi (un peu au-delà de son rayon).
        protected float RayonContact
        {
            get
            {
                var bo = BouclierNyxessa.Instance;
                return bo != null && bo.Leve ? Mathf.Max(B.rayonContactNyxessa, bo.Rayon + 0.4f) : B.rayonContactNyxessa;
            }
        }

        protected float DistanceNyxessa()
        {
            var n = Nyxessa;
            if (n == null || Gardien) return 999f;
            Vector3 d = n.position - transform.position; d.y = 0f;
            return d.magnitude;
        }

        protected virtual void MajMarche(float dt)
        {
            if (Provoque) { m_Cible = m_Provocateur; m_Etat = Etat.Poursuite; return; }
            // Nyxessa au contact : on la frappe (priorité).
            if (DistanceNyxessa() <= RayonContact)
            {
                if (Time.time - m_DernierCoup >= m_Stats.intervalle) CommencerAttaque(null);
                else { Agent.isStopped = true; Tourner(Nyxessa.position); }
                return;
            }
            var j = ChoisirCible(Gardien ? B.gardienDetection : B.detectionJoueur);
            if (j != null)
            {
                m_Cible = j;
                m_SansFrapper = 0f;
                m_Etat = Etat.Poursuite;
                AlerterGardiens(j);
                return;
            }
            Agent.isStopped = false;
            Vector3 but = DestinationMarche();
            m_But = but;
            // Pas de nouvelle demande pendant qu'un chemin se calcule (01/10/2026) : la relancer à chaque image l'empêchait d'aboutir.
            if ((!Agent.hasPath && !Agent.pathPending) || (Agent.destination - but).sqrMagnitude > 0.5f) { Agent.SetDestination(but); OublierDestination(); }
        }

        // Garde-fou de la marche (01/10/2026) : position de référence, temps sans avancer, blocages de suite.
        Vector3 m_GardeFouPos;
        Vector3 m_But;   // destination voulue (Agent.destination devient le bout d'un chemin partiel)
        float m_GardeFouDepuis;
        int m_GardeFouFois;

        /// En Marche, agent lancé (pas arrêté exprès, comme le mage dans sa bande de tir) et loin de sa destination : s'il
        /// n'avance pas d'1 m en GameBalance.marcheBloqueeDelai s, il abandonne son point de passage et relance son chemin ;
        /// à la deuxième fois de suite (ou hors du NavMesh), loin de Nyxessa, il est replacé sur le NavMesh 2 m plus près d'elle.
        void GarderLaMarche(float dt)
        {
            Vector3 pos = transform.position;
            Vector3 reste = m_But - pos; reste.y = 0f;
            if (m_Etat != Etat.Marche || !Agent.enabled || Agent.isStopped || reste.sqrMagnitude < 2.25f)
            {
                m_GardeFouPos = pos; m_GardeFouDepuis = 0f;
                if (m_Etat != Etat.Marche) m_GardeFouFois = 0;
                return;
            }
            Vector3 fait = pos - m_GardeFouPos; fait.y = 0f;
            if (fait.sqrMagnitude > 1f) { m_GardeFouPos = pos; m_GardeFouDepuis = 0f; m_GardeFouFois = 0; return; }
            m_GardeFouDepuis += dt;
            if (m_GardeFouDepuis < Mathf.Max(1f, B.marcheBloqueeDelai)) return;
            m_GardeFouDepuis = 0f;
            m_GardeFouFois++;
            m_AvecPassage = false;
            bool replace = false;
            var n = Nyxessa;
            if (n != null && (m_GardeFouFois >= 2 || !Agent.isOnNavMesh) && DistanceNyxessa() > 15f)
            {
                Vector3 vers = n.position - pos; vers.y = 0f;
                if (NavMesh.SamplePosition(pos + vers.normalized * 2f, out var h, 4f, NavMesh.AllAreas)) { Agent.Warp(h.position); replace = true; }
            }
            if (Agent.isOnNavMesh) Agent.ResetPath();
            OublierDestination();
            if (P != null) P.Journal("Garde-fou : " + type + " " + Id + " bloqué en marche à " + DistanceNyxessa().ToString("F0") + " m de Nyxessa, "
                + (replace ? "replacé sur le NavMesh" : "chemin relancé"));
        }

        Vector3 m_DestinationSuivie;
        float m_DestinationQuand = -99f;

        /// Suit une cible mobile sans recalculer le chemin à chaque image (jusqu'à 60 squelettes) : le chemin n'est
        /// relancé que si la cible s'est éloignée de plus de 0,5 m de la dernière destination demandée, toutes les
        /// 0,2 s au plus tard, ou si l'agent n'a plus de chemin. Le premier appel après OublierDestination est immédiat.
        protected void Poursuivre(Vector3 p)
        {
            // Chemin en cours de calcul vers presque la même cible : on attend (01/10/2026). Relancé toutes les 0,2 s, le
            // calcul d'un long chemin (mage de la clairière sud-ouest vers Nyxessa, ×3 au banc) n'aboutissait jamais.
            m_But = p;
            if (Agent.pathPending && (p - m_DestinationSuivie).sqrMagnitude <= 4f) return;
            if (Time.time - m_DestinationQuand < 0.2f && (p - m_DestinationSuivie).sqrMagnitude <= 0.25f
                && (Agent.hasPath || Agent.pathPending)) return;
            Agent.SetDestination(p);
            m_DestinationSuivie = p;
            m_DestinationQuand = Time.time;
        }

        /// À appeler après tout autre SetDestination : le prochain Poursuivre relance le chemin tout de suite.
        protected void OublierDestination() { m_DestinationQuand = -99f; m_DestinationSuivie = Vector3.one * 1e6f; }

        protected virtual void MajPoursuite(float dt)
        {
            if (Provoque) { m_Cible = m_Provocateur; m_SansFrapper = 0f; }
            else if (m_Cible != null && !Voit(m_Cible)) { m_EnRiposte = false; m_Cible = null; m_Etat = Etat.Marche; return; }
            // Fin de la riposte (temps écoulé, coups portés, héros mort) : retour à Nyxessa.
            if (m_EnRiposte && (Time.time >= m_RiposteJusque || m_RiposteAttaques >= B.riposteAttaques || m_Cible == null || !m_Cible.Vivant))
            {
                m_EnRiposte = false;
                m_Cible = null;
                m_Etat = Etat.Marche;
                if (P != null) P.Journal("Riposte terminée : " + type + " revient à Nyxessa");
                return;
            }
            if (!Provoque && !m_EnRiposte && DistanceNyxessa() <= RayonContact && (m_Cible == null || !m_Cible.Vivant || Distance(m_Cible.transform.position) > PorteeEngagement(m_Cible)))
            {
                m_Cible = null;
                m_Etat = Etat.Marche;
                return;
            }
            if (m_Cible == null || !m_Cible.Vivant) { m_Cible = null; m_Etat = Etat.Marche; return; }
            float d = Distance(m_Cible.transform.position);
            m_SansFrapper += dt;
            // Gardien (30/09/2026) : pas d'abandon au temps ; il lâche quand la cible sort de sa laisse (poste), puis y retourne.
            bool abandon = Gardien && !Provoque ? !DansLaLaisse(m_Cible) : d > B.abandonPoursuite || m_SansFrapper > B.abandonApres;
            if (abandon) { m_Cible = null; m_Court = false; m_Etat = Etat.Marche; return; }
            MajCourse(d);
            if (d <= PorteeEngagement(m_Cible))
            {
                Agent.isStopped = true;
                Tourner(m_Cible.transform.position);
                if (Time.time - m_DernierCoup >= m_Stats.intervalle) CommencerAttaque(m_Cible);
            }
            else
            {
                Agent.isStopped = false;
                Poursuivre(m_Cible.transform.position);
            }
        }

        /// Distance à laquelle le squelette s'arrête pour frapper cette cible. Par défaut la portée de son coup ; un
        /// ennemi à plusieurs compétences (Morgrim) renvoie la portée de celle qu'il choisirait maintenant, pour ne
        /// pas s'arrêter hors de portée du coup qui partira.
        protected virtual float PorteeEngagement(Heros cible) => m_Stats.portee;

        protected float Distance(Vector3 p) { Vector3 d = p - transform.position; d.y = 0f; return d.magnitude; }

        protected void Tourner(Vector3 vers)
        {
            Vector3 d = vers - transform.position; d.y = 0f;
            if (d.sqrMagnitude < 0.001f) return;
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(d), 540f * Time.deltaTime);
        }

        protected Vector3 CiblePosition() => m_CibleNyxessa || m_Cible == null ? (Nyxessa != null ? Nyxessa.position : transform.position + transform.forward) : m_Cible.transform.position;

        /// Préparation d'un coup (lisible : le joueur peut parer ou esquiver).
        protected virtual void CommencerAttaque(Heros cible)
        {
            m_Cible = cible;
            m_CibleNyxessa = cible == null;
            if (m_EnRiposte && cible != null) m_RiposteAttaques++;
            m_Etat = Etat.Preparation;
            m_EtatDepuis = 0f;
            m_DernierCoup = Time.time;
            m_SansFrapper = 0f;
            Agent.isStopped = true;
            if (animator != null)
            {
                animator.SetFloat(P_VitesseAttaque, instantCoupClip / Mathf.Max(0.1f, m_Stats.preparation));
                animator.SetTrigger(P_Attack);
            }
            AudioBank.Jouer(SonsDuJeu.SquelettePreparation, transform.position, 0.6f, 0.1f);
            AnnoncerCoup(cible);
        }

        // ----------------------------------------------------------------- Annonce du coup (jauge de parade)

        Heros m_AnnonceCible;
        float m_AnnonceImpact = -99f, m_AnnonceDuree;

        /// Autorité : le coup en préparation vise `cible` ; son poste l'affiche (jauge de parade du paladin) et peut juger
        /// une parade parfaite sur l'instant d'impact prévu (TelegraphieCoups, Docs/reseau.md « Parade parfaite »).
        void AnnoncerCoup(Heros cible)
        {
            m_AnnonceCible = cible;
            m_AnnonceDuree = m_Stats.preparation;
            m_AnnonceImpact = Time.time + m_AnnonceDuree;
            if (cible == null) return;
            if (!cible.Distant) { TelegraphieCoups.Annoncer(this, cible, m_AnnonceDuree); return; }
            var r = cible.GetComponent<Deathless.Reseau.HerosReseau>();
            if (r != null && m_Reseau != null && m_Reseau.IsSpawned) r.AnnoncerCoup(m_Reseau.NetworkObjectId, m_AnnonceDuree);
        }

        /// Autorité : dernier coup annoncé contre `h` (impact prévu en temps de l'hôte, durée de la préparation). Sert à
        /// valider la parade parfaite demandée par un client (ParadeParfaite.Demande).
        public bool CoupAnnonceSur(Heros h, out float impact, out float duree)
        {
            impact = m_AnnonceImpact;
            duree = m_AnnonceDuree;
            return h != null && m_AnnonceCible == h;
        }

        protected virtual void MajPreparation()
        {
            Tourner(CiblePosition());
            if (m_EtatDepuis < m_Stats.preparation) return;
            Frapper();
            // Paré pendant le coup : l'étourdissement posé par la parade reste.
            if (m_Etat != Etat.Preparation) return;
            m_Etat = Etat.Recuperation;
            m_EtatDepuis = 0f;
        }

        protected virtual void Frapper()
        {
            if (m_CibleNyxessa)
            {
                // Le sorcier à portée, sans bouclier levé : il prend le coup (les squelettes le visent comme Nyxessa).
                var so = Sorcier.Instance;
                var bo = BouclierNyxessa.Instance;
                if (so != null && so.Ciblable && (bo == null || !bo.Leve) && Distance(so.transform.position) <= m_Stats.portee + 0.8f)
                {
                    so.Sante.Encaisser(new InfoDegats { montant = m_Stats.degatsJoueur, equipeSource = Equipe.Ennemis, source = gameObject, point = so.transform.position + Vector3.up, direction = transform.forward });
                    return;
                }
                if (P != null && P.nyxessa != null && DistanceNyxessa() <= RayonContact + 0.6f)
                {
                    m_DernierCoupNyxessa = Time.time;
                    Vector3 point = P.nyxessa.transform.position + Vector3.up * 1.2f + (transform.position - P.nyxessa.transform.position).normalized * 1.3f;
                    P.nyxessa.Encaisser(new InfoDegats { montant = m_Stats.degatsNyxessa, equipeSource = Equipe.Ennemis, source = gameObject, point = point, direction = transform.forward });
                }
                return;
            }
            if (m_Cible == null || !m_Cible.Vivant) return;
            Vector3 vers = m_Cible.transform.position - transform.position; vers.y = 0f;
            if (vers.magnitude > m_Stats.portee + 0.6f || Vector3.Angle(transform.forward, vers) > 70f) return;   // esquivé
            m_Cible.Sante.Encaisser(new InfoDegats
            {
                montant = m_Stats.degatsJoueur, equipeSource = Equipe.Ennemis, source = gameObject, parable = true,
                point = m_Cible.transform.position + Vector3.up, direction = vers.normalized
            });
        }

        // ----------------------------------------------------------------- Réactions

        void OnTouche(InfoDegats info, float reel)
        {
            if (info.continu) return;
            AudioBank.Jouer(SonsDuJeu.SqueletteTouche, transform.position + Vector3.up, 0.8f, 0.05f);
            if (animator != null && m_Etat != Etat.Preparation && m_Etat != Etat.SortieDeTerre) animator.SetTrigger(P_Hit);
            // Un joueur qui frappe attire l'attention s'il est proche.
            if (info.sourceId > 0 && P != null && m_Etat != Etat.Mort && m_Etat != Etat.SortieDeTerre)
            {
                var h = P.HerosDe(info.sourceId);
                if (h == null || ClasseAssassin.DansLaFumee(h.transform.position)) return;
                float d = Distance(h.transform.position);
                // Gardien (30/09/2026) : frappé de n'importe où dans sa laisse (flèche de loin comprise), il se retourne et
                // alerte les gardiens voisins.
                if (Gardien)
                {
                    if (m_Cible == null && DansLaLaisse(h))
                    {
                        m_Cible = h; m_SansFrapper = 0f;
                        if (m_Etat == Etat.Marche) m_Etat = Etat.Poursuite;
                        if (h.Classe is ClasseAssassin ag) ag.Reperer();
                    }
                    AlerterGardiens(h);
                }
                else if (m_Etat == Etat.Marche && d < B.detectionJoueur && DistanceNyxessa() > RayonContact)
                {
                    m_Cible = h; m_Etat = Etat.Poursuite; m_SansFrapper = 0f;
                    if (h.Classe is ClasseAssassin a) a.Reperer();
                }
                else if (Riposteur && DistanceNyxessa() <= RayonContact && d <= B.riposteDistance) CompterRiposte(h);
            }
        }

        /// Riposte au contact de Nyxessa (27/09/2026) : sbires, guerriers et voleurs (élites compris) ; Morgrim, le
        /// Nécromancien et le mage (qui ne vient pas au contact) gardent leur propre comportement.
        protected virtual bool Riposteur => type == TypeEnnemi.Sbire || type == TypeEnnemi.Guerrier || type == TypeEnnemi.Voleur;

        /// Compte les coups reçus de `h` (autorité). Au riposteCoups-ième coup de suite du même héros, le squelette se
        /// retourne vers lui : poursuite tout de suite s'il marche ou récupère ; sinon (coup en préparation, étourdi) dès
        /// que Reprendre le rend au comportement de base, m_Cible étant déjà posé.
        void CompterRiposte(Heros h)
        {
            if (h.Id != m_RiposteHerosId || Time.time - m_RiposteDernierCoup > 4f) { m_RiposteHerosId = h.Id; m_RiposteCoups = 0; }
            m_RiposteCoups++;
            m_RiposteDernierCoup = Time.time;
            if (m_RiposteCoups < B.riposteCoups || m_EnRiposte || Provoque) return;
            m_RiposteCoups = 0;
            m_EnRiposte = true;
            m_RiposteJusque = Time.time + B.riposteDuree;
            m_RiposteAttaques = 0;
            m_Cible = h;
            m_SansFrapper = 0f;
            if (m_Etat == Etat.Marche || m_Etat == Etat.Recuperation)
            {
                m_Etat = Etat.Poursuite;
                m_EtatDepuis = 0f;
                if (Agent.enabled) Agent.isStopped = false;
            }
            if (h.Classe is ClasseAssassin a) a.Reperer();
            if (P != null) P.Journal("Riposte : " + type + (elite ? " élite" : "") + " se retourne vers le joueur " + h.Id);
        }

        /// Étourdissement (parade, charge). Crédite au joueur les coups empêchés contre Nyxessa (score).
        public virtual void Etourdir(float duree, int sourceId = 0)
        {
            if (RelaiEtourdir(duree, sourceId)) return;
            if (m_Etat == Etat.Mort || m_Etat == Etat.SortieDeTerre) return;
            if (sourceId > 0 && SurNyxessa && P != null) P.CompterDegatsEvites(sourceId, m_Stats.degatsNyxessa * duree / Mathf.Max(0.1f, m_Stats.intervalle));
            m_Etat = Etat.Etourdi;
            m_EtatDepuis = 0f;
            m_Etourdi = Mathf.Max(m_Etourdi, duree);
            if (Statuts != null) Statuts.Ajouter(TypeStatut.Etourdi, m_Etourdi, 1f, sourceId > 0 ? OrigineStatut.Joueur : OrigineStatut.Inconnue, sourceId);
            if (Agent.enabled) Agent.isStopped = true;
            if (animator != null) { animator.SetTrigger(P_Hit); animator.SetBool(P_Stun, true); }
        }

        /// Repoussé sur le côté (charge bélier) puis étourdi.
        public virtual void Repousser(Vector3 deplacement, float etourdi, int sourceId)
        {
            if (RelaiRepousser(deplacement, etourdi)) return;
            if (m_Etat == Etat.Mort || m_Etat == Etat.SortieDeTerre) return;
            m_Pousse = deplacement; m_Pousse.y = 0f;
            m_PousseReste = 0.2f;
            Etourdir(etourdi, sourceId);
        }

        public virtual bool Repoussable => true;

        /// Échelle d'un élite (wiki : ennemis.md, environ 1,3 fois plus grand) : posée par l'hôte à l'apparition
        /// (DirecteurVagues), rejouée chez les clients (EnnemiReseau) car l'échelle ne passe pas par le NetworkTransform.
        public const float EchelleElite = 1.3f;

        /// Élite : yeux rouges et légère aura rouge (wiki : ennemis, Élites ; sans éclat de Nyx). Appelé à l'apparition, chez l'hôte comme chez les clients (purement visuel).
        public void MarquerElite()
        {
            if (!elite) return;
            if (yeuxElite == null) { if (GetComponent<AuraElite>() == null) gameObject.AddComponent<AuraElite>(); return; }
            foreach (var r in GetComponentsInChildren<Renderer>(true)) if (r.name.EndsWith("_Eyes")) r.sharedMaterial = yeuxElite;
            if (GetComponent<AuraElite>() == null) gameObject.AddComponent<AuraElite>();   // légère aura de gemmes rouges
        }

        /// Or rapporté à sa mort (GameBalance, wiki : ennemis) ; rien s'il est désintégré à l'aube.
        public int OrRapporte
        {
            get
            {
                var b = B;
                if (Gardien) return 0;
                if (type == TypeEnnemi.Golem) return b.orMorgrim;
                if (type == TypeEnnemi.Necromancien) return b.orNyxar;
                if (elite) return b.orElite;
                switch (type)
                {
                    case TypeEnnemi.Guerrier: return b.orGuerrier;
                    case TypeEnnemi.Voleur: return b.orVoleur;
                    case TypeEnnemi.Mage: return b.orMage;
                    default: return b.orSbire;
                }
            }
        }
        public virtual float FacteurEtourdissement => 1f;

        void OnTue(InfoDegats info)
        {
            if (m_Etat == Etat.Mort) return;
            bool surNyx = SurNyxessa;
            m_Etat = Etat.Mort;
            if (Statuts != null) Statuts.Vider();
            // Or des vagues (règle provisoire sans donjon) : à la caisse commune, attribué à qui porte le coup fatal.
            if (P != null) P.GagnerOr(OrRapporte, info.sourceId, transform.position + Vector3.up * 1.6f);
            if (info.sourceId > 0 && P != null)
            {
                P.CompterTue(info.sourceId);
                if (surNyx) P.CompterDegatsEvites(info.sourceId, m_Stats.degatsNyxessa * B.fenetreDegatsEvites / Mathf.Max(0.1f, m_Stats.intervalle));
            }
            Deathless.Succes.ServiceSucces.EnnemiTue(this, info);   // succès (Morgrim, voleur, missile de Nyxessa…)
            if (Agent.enabled) { Agent.isStopped = true; Agent.enabled = false; }
            foreach (var c in GetComponentsInChildren<Collider>()) c.enabled = false;
            if (animator != null) { animator.SetBool(P_Stun, false); animator.SetBool(P_Dead, true); }
            AudioBank.Jouer(SonsDuJeu.SqueletteMort, transform.position + Vector3.up, 0.9f, 0.05f);
            Invoke(nameof(DesintegrerMort), 0.8f);
        }

        void DesintegrerMort() => Desintegrer(false);

        /// Désintégration (mort ou aube) : gemmes couleur os qui montent, rendus coupés, puis retrait.
        public void Desintegrer(bool aube)
        {
            if (m_Desintegre) return;
            m_Desintegre = true;
            if (aube)
            {
                m_Etat = Etat.Mort;
                AudioBank.Jouer(SonsDuJeu.SqueletteAube, transform.position + Vector3.up, 0.7f, 0.12f);
            }
            var gemmes = EffetsJeu.Gemmes;
            if (gemmes != null) GemBurst.Rise(EffetsJeu.Volume(gameObject), gemmes);
            foreach (var r in GetComponentsInChildren<Renderer>()) r.enabled = false;
            Retire?.Invoke(this);
            Destroy(gameObject, 0.1f);
        }
    }
}
