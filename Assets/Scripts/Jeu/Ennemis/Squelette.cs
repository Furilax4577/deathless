using System;
using UnityEngine;
using UnityEngine.AI;

namespace Deathless.Jeu
{
    /// Squelette (IA côté autorité, version 0.1) : sort de terre, marche vers Nyxessa (une place autour du plateau, dans
    /// son angle d'arrivée), poursuit un joueur proche puis revient, frappe au contact (préparation lisible et parable),
    /// peut être étourdi ou repoussé (parade, charge bélier), meurt puis se désintègre en gemmes couleur os. Le Golem et le
    /// Nécromancien en dérivent (coup de zone ; distance, missile, invocation).
    [RequireComponent(typeof(NavMeshAgent), typeof(Sante))]
    public class Squelette : MonoBehaviour
    {
        public enum Etat { SortieDeTerre, Marche, Poursuite, Preparation, Recuperation, Etourdi, Mort }

        [Header("Réglages (posés par le directeur des vagues)")]
        public TypeEnnemi type;
        public bool elite;
        [Tooltip("Modèle (enfant) : c'est lui qui sort de terre.")]
        public Transform modele;
        public Animator animator;
        [Tooltip("Durée de la sortie de terre (clip Skeletons_Spawn_Ground à la vitesse de GameBalance).")]
        public float dureeSortie = 1.6f;
        [Tooltip("Instant du coup dans le clip d'attaque (s, vitesse 1).")]
        public float instantCoupClip = 0.58f;

        public int Id { get; private set; }
        public Etat EtatCourant => m_Etat;
        public Sante Sante { get; private set; }
        public NavMeshAgent Agent { get; private set; }
        public bool Vivant => m_Etat != Etat.Mort;
        /// Vrai si ce squelette frappe Nyxessa (coup porté récemment ou coup en préparation contre elle).
        public bool SurNyxessa => m_Etat != Etat.Mort && ((m_Etat == Etat.Preparation && m_CibleNyxessa) || Time.time - m_DernierCoupNyxessa < GameBalance.Courant.surNyxessaDepuis);
        public float DegatsNyxessa => m_Stats.degatsNyxessa;
        public float Intervalle => m_Stats.intervalle;
        public event Action<Squelette> Retire;     // désintégré (mort ou aube)

        protected StatsSquelette m_Stats = new StatsSquelette();
        protected Etat m_Etat = Etat.SortieDeTerre;
        protected float m_EtatDepuis;
        protected Heros m_Cible;             // joueur poursuivi
        protected bool m_CibleNyxessa;
        protected float m_DernierCoup = -99f, m_DernierCoupNyxessa = -99f;
        protected float m_SansFrapper;
        protected Vector3 m_Place;
        protected float m_Etourdi;
        Vector3 m_Pousse; float m_PousseReste;
        bool m_Desintegre;
        static int s_Ids;

        protected static readonly int P_Speed = Animator.StringToHash("Speed");
        protected static readonly int P_Attack = Animator.StringToHash("Attack");
        protected static readonly int P_VitesseAttaque = Animator.StringToHash("VitesseAttaque");
        protected static readonly int P_Hit = Animator.StringToHash("Hit");
        protected static readonly int P_Stun = Animator.StringToHash("Stun");
        protected static readonly int P_Dead = Animator.StringToHash("Dead");

        protected Partie P => Partie.Instance;
        protected GameBalance B => GameBalance.Courant;
        protected Transform Nyxessa => P != null && P.nyxessa != null ? P.nyxessa.transform : null;

        protected virtual void Awake()
        {
            Id = ++s_Ids;
            Sante = GetComponent<Sante>();
            Agent = GetComponent<NavMeshAgent>();
            if (animator == null) animator = GetComponentInChildren<Animator>();
            Sante.equipe = Equipe.Ennemis;
            Sante.Touche += OnTouche;
            Sante.Tue += OnTue;
            if (modele != null) Tete = MannequinEquip.Trouver(modele, "head");
            if (modele != null)
                foreach (var r in modele.GetComponentsInChildren<Renderer>())
                    if (r.name.EndsWith("_Head")) m_RenduTete = r;
        }

        Renderer m_RenduTete;

        /// Os de la tête (base du crâne) ; le centre et le rayon de la zone de tête (tirs à la tête du rôdeur et de
        /// l'arbalète) viennent du maillage de la tête (gros crâne des squelettes KayKit).
        public Transform Tete { get; private set; }
        public Vector3 CentreTete => m_RenduTete != null ? m_RenduTete.bounds.center : (Tete != null ? Tete.position + Vector3.up * 0.3f : transform.position + Vector3.up * 1.4f);
        public float RayonTete => m_RenduTete != null ? m_RenduTete.bounds.extents.y * 1.05f : 0.3f;

        // ----------------------------------------------------------------- Vue : furtif, fumée, provocation

        Heros m_Provocateur;
        float m_ProvoqueJusque;

        /// Rugissement du viking : ce squelette le prend pour cible pendant `duree` s (priorité sur Nyxessa).
        public virtual void Provoquer(Heros h, float duree)
        {
            if (m_Etat == Etat.Mort || h == null) return;
            m_Provocateur = h;
            m_ProvoqueJusque = Time.time + duree;
            m_Cible = h;
            m_SansFrapper = 0f;
            if (m_Etat == Etat.Marche) m_Etat = Etat.Poursuite;
        }

        protected bool Provoque => m_Provocateur != null && m_Provocateur.Vivant && Time.time < m_ProvoqueJusque;

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
            dir = Quaternion.Euler(0f, UnityEngine.Random.Range(-35f, 35f), 0f) * dir.normalized;
            m_Place = c + dir * B.rayonPlacesNyxessa;
            if (NavMesh.SamplePosition(m_Place, out var hit, 2f, NavMesh.AllAreas)) m_Place = hit.position;
            CommencerSortie();
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
            if (animator != null) animator.SetFloat(P_Speed, Agent.enabled && !Agent.isStopped ? Mathf.Clamp01(Agent.velocity.magnitude / Mathf.Max(0.1f, m_Stats.vitesse)) * 0.5f : 0f);
            if (P != null && (P.Etat.phase == Phase.Terminee || P.Etat.nyxessa.detruite) && m_Etat != Etat.Mort)
            {
                Agent.isStopped = true;
                return;
            }
            switch (m_Etat)
            {
                case Etat.SortieDeTerre: MajSortie(); break;
                case Etat.Marche: MajMarche(dt); break;
                case Etat.Poursuite: MajPoursuite(dt); break;
                case Etat.Preparation: MajPreparation(); break;
                case Etat.Recuperation: if (m_EtatDepuis >= RecuperationDuree) Reprendre(); else Tourner(CiblePosition()); break;
                case Etat.Etourdi:
                    m_Etourdi -= dt;
                    if (m_Etourdi <= 0f) { if (animator != null) animator.SetBool(P_Stun, false); Reprendre(); }
                    break;
            }
        }

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

        protected Heros JoueurProche(float rayon)
        {
            if (P == null) return null;
            Heros meilleur = null;
            float d = rayon * rayon;
            foreach (var h in P.TousLesHeros)
            {
                if (h == null || !h.Vivant) continue;
                float dd = (h.transform.position - transform.position).sqrMagnitude;
                if (dd < d && Voit(h)) { d = dd; meilleur = h; }
            }
            return meilleur;
        }

        protected float DistanceNyxessa()
        {
            var n = Nyxessa;
            if (n == null) return 999f;
            Vector3 d = n.position - transform.position; d.y = 0f;
            return d.magnitude;
        }

        protected virtual void MajMarche(float dt)
        {
            if (Provoque) { m_Cible = m_Provocateur; m_Etat = Etat.Poursuite; return; }
            // Nyxessa au contact : on la frappe (priorité).
            if (DistanceNyxessa() <= B.rayonContactNyxessa)
            {
                if (Time.time - m_DernierCoup >= m_Stats.intervalle) CommencerAttaque(null);
                else { Agent.isStopped = true; Tourner(Nyxessa.position); }
                return;
            }
            var j = JoueurProche(B.detectionJoueur);
            if (j != null)
            {
                m_Cible = j;
                m_SansFrapper = 0f;
                m_Etat = Etat.Poursuite;
                return;
            }
            Agent.isStopped = false;
            if (!Agent.hasPath || (Agent.destination - m_Place).sqrMagnitude > 0.5f) Agent.SetDestination(m_Place);
        }

        protected virtual void MajPoursuite(float dt)
        {
            if (Provoque) { m_Cible = m_Provocateur; m_SansFrapper = 0f; }
            else if (m_Cible != null && !Voit(m_Cible)) { m_Cible = null; m_Etat = Etat.Marche; return; }
            if (!Provoque && DistanceNyxessa() <= B.rayonContactNyxessa && (m_Cible == null || !m_Cible.Vivant || Distance(m_Cible.transform.position) > m_Stats.portee))
            {
                m_Cible = null;
                m_Etat = Etat.Marche;
                return;
            }
            if (m_Cible == null || !m_Cible.Vivant) { m_Cible = null; m_Etat = Etat.Marche; return; }
            float d = Distance(m_Cible.transform.position);
            m_SansFrapper += dt;
            if (d > B.abandonPoursuite || m_SansFrapper > B.abandonApres) { m_Cible = null; m_Etat = Etat.Marche; return; }
            if (d <= m_Stats.portee)
            {
                Agent.isStopped = true;
                Tourner(m_Cible.transform.position);
                if (Time.time - m_DernierCoup >= m_Stats.intervalle) CommencerAttaque(m_Cible);
            }
            else
            {
                Agent.isStopped = false;
                Agent.SetDestination(m_Cible.transform.position);
            }
        }

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
                if (P != null && P.nyxessa != null && DistanceNyxessa() <= B.rayonContactNyxessa + 0.6f)
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
            if (info.sourceId > 0 && m_Etat == Etat.Marche && P != null)
            {
                var h = P.HerosDe(info.sourceId);
                if (h != null && Distance(h.transform.position) < B.detectionJoueur && DistanceNyxessa() > B.rayonContactNyxessa && !ClasseAssassin.DansLaFumee(h.transform.position))
                {
                    m_Cible = h; m_Etat = Etat.Poursuite; m_SansFrapper = 0f;
                    if (h.Classe is ClasseAssassin a) a.Reperer();
                }
            }
        }

        /// Étourdissement (parade, charge). Crédite au joueur les coups empêchés contre Nyxessa (score).
        public virtual void Etourdir(float duree, int sourceId = 0)
        {
            if (m_Etat == Etat.Mort || m_Etat == Etat.SortieDeTerre) return;
            if (sourceId > 0 && SurNyxessa && P != null) P.CompterDegatsEvites(sourceId, m_Stats.degatsNyxessa * duree / Mathf.Max(0.1f, m_Stats.intervalle));
            m_Etat = Etat.Etourdi;
            m_EtatDepuis = 0f;
            m_Etourdi = Mathf.Max(m_Etourdi, duree);
            if (Agent.enabled) Agent.isStopped = true;
            if (animator != null) { animator.SetTrigger(P_Hit); animator.SetBool(P_Stun, true); }
        }

        /// Repoussé sur le côté (charge bélier) puis étourdi.
        public virtual void Repousser(Vector3 deplacement, float etourdi, int sourceId)
        {
            if (m_Etat == Etat.Mort || m_Etat == Etat.SortieDeTerre) return;
            m_Pousse = deplacement; m_Pousse.y = 0f;
            m_PousseReste = 0.2f;
            Etourdir(etourdi, sourceId);
        }

        public virtual bool Repoussable => true;
        public virtual float FacteurEtourdissement => 1f;

        void OnTue(InfoDegats info)
        {
            if (m_Etat == Etat.Mort) return;
            bool surNyx = SurNyxessa;
            m_Etat = Etat.Mort;
            if (info.sourceId > 0 && P != null)
            {
                P.CompterTue(info.sourceId);
                if (surNyx) P.CompterDegatsEvites(info.sourceId, m_Stats.degatsNyxessa * B.fenetreDegatsEvites / Mathf.Max(0.1f, m_Stats.intervalle));
            }
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
