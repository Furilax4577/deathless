using System.Collections.Generic;
using UnityEngine;

namespace Deathless.Jeu
{
    /// Le Paladin (version 0.1) : déplacement relatif à la caméra, sprint, saut, esquive-roulade, attaque à l'épée, garde
    /// et parade, charge bélier (compétence 1), soin sur soi (compétence 2), vie et endurance, mort (dissolution vers
    /// Nyxessa) et réapparition. Les entrées viennent de HerosEntrees (InputChordResolver), l'état qui fait foi est recopié
    /// dans l'EtatJoueur de Partie ; ce composant est le seul à écrire dans l'Animator du héros.
    [RequireComponent(typeof(CharacterController), typeof(Sante), typeof(HerosEntrees))]
    public class Heros : MonoBehaviour
    {
        public enum Action { Libre, Attaque, Esquive, ChargeAnticipation, Charge, Soin, Etourdi, Mort, Reapparition }

        public Animator animator;
        public HelmetVisor visiere;
        [Tooltip("Instance de l'aura de soin (enfant, racine à y = 0 sous les pieds).")]
        public AuraSoin aura;

        public Sante Sante { get; private set; }
        public int Id => m_Etat != null ? m_Etat.id : 0;
        public bool Vivant => !Sante.Mort && m_Action != Action.Mort && m_Action != Action.Reapparition;
        public bool EnGarde => m_Garde;
        public Action ActionCourante => m_Action;
        public float RechargeCharge => m_RechargeCharge;
        public float RechargeSoin => m_RechargeSoin;
        public float Endurance => m_Endurance;

        CharacterController m_CC;
        HerosEntrees m_Entrees;
        Partie m_Partie;
        EtatJoueur m_Etat;
        CameraEpaule m_Camera;
        ChargeBelier m_ChargeVisuel;

        Action m_Action = Action.Libre;
        float m_ActionDepuis;
        float m_VitesseY;
        bool m_AuSol = true;
        float m_Endurance, m_EnduranceUtilisee = -99f;
        bool m_Garde;
        float m_GardeDepuis = -99f;
        float m_DerniereAttaque = -99f;
        bool m_CoupPorte;
        int m_Combo;
        float m_RechargeCharge, m_RechargeSoin, m_RechargeEsquive;
        Vector3 m_DirAction;
        float m_Etourdi;
        float m_InvulnerableJusque;
        // Charge bélier
        Squelette m_CibleCharge;
        float m_DistanceCharge, m_ParcouruCharge;
        Vector3 m_DepartCharge;
        readonly HashSet<Squelette> m_Repousses = new HashSet<Squelette>();
        bool m_SoinDonne;
        float m_Pas;

        static readonly int P_Speed = Animator.StringToHash("Speed");
        static readonly int P_Grounded = Animator.StringToHash("Grounded");
        static readonly int P_Guard = Animator.StringToHash("Guard");
        static readonly int P_Dead = Animator.StringToHash("Dead");
        static readonly int P_Attack1 = Animator.StringToHash("Attack1");
        static readonly int P_Attack2 = Animator.StringToHash("Attack2");
        static readonly int P_Dodge = Animator.StringToHash("Dodge");
        static readonly int P_Jump = Animator.StringToHash("Jump");
        static readonly int P_Charge = Animator.StringToHash("Charge");
        static readonly int P_Heal = Animator.StringToHash("Heal");
        static readonly int P_BlockHit = Animator.StringToHash("BlockHit");
        static readonly int P_Hit = Animator.StringToHash("Hit");
        static readonly int P_Respawn = Animator.StringToHash("Respawn");
        int m_CoucheHaut = -1;
        float m_PoidsHaut;
        float m_HautJusque;

        GameBalance B => GameBalance.Courant;

        void Awake()
        {
            m_CC = GetComponent<CharacterController>();
            m_Entrees = GetComponent<HerosEntrees>();
            Sante = GetComponent<Sante>();
            Sante.equipe = Equipe.Heros;
            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (visiere == null) visiere = GetComponentInChildren<HelmetVisor>();
            if (aura == null) aura = GetComponentInChildren<AuraSoin>(true);
            if (animator != null) m_CoucheHaut = animator.GetLayerIndex("HautDuCorps");
        }

        public void Initialiser(Partie partie, EtatJoueur etat)
        {
            m_Partie = partie;
            m_Etat = etat;
            m_Camera = partie.cameraJeu;
            Sante.Initialiser(B.herosPV);
            Sante.invulnerable = B.joueurInvincible;
            Sante.intercepteur = Intercepter;
            Sante.Touche += OnTouche;
            Sante.Intercepte += OnIntercepte;
            Sante.Tue += OnTue;
            Sante.Soigne += reel => { if (m_Partie != null) m_Partie.CompterSoins(Id, reel); };
            m_Endurance = B.endurance;
            m_Entrees.Action += OnAction;
            var fx = EffetsJeu.Instance;
            if (fx != null && fx.prefabChargeBelier != null)
            {
                var go = Instantiate(fx.prefabChargeBelier);
                go.name = "ChargeBelier_" + name;
                m_ChargeVisuel = go.GetComponent<ChargeBelier>();
            }
        }

        /// Recopie l'état qui fait foi dans l'EtatJoueur (lu par le HUD et, plus tard, par le réseau).
        public void EcrireEtat(EtatJoueur j)
        {
            j.pv = Sante.Pv;
            j.pvMax = Sante.pvMax;
            j.endurance = m_Endurance;
            j.enduranceMax = B.endurance;
            j.rechargeCharge = m_RechargeCharge;
            j.rechargeSoin = m_RechargeSoin;
        }

        // ----------------------------------------------------------------- Entrées

        bool PeutAgir => m_Action == Action.Libre && Vivant && m_Partie != null && m_Partie.EnCours;

        void OnAction(string action)
        {
            if (m_Partie == null) return;
            if (action == "Ready") { m_Partie.BasculerPret(Id); return; }
            if (!Vivant || !m_Partie.EnCours) return;
            switch (action)
            {
                case "AttackPrimary": Attaquer(); break;
                case "AttackSecondary": m_GardeDepuis = Time.time; break;   // la parade se juge depuis l'appui
                case "Jump": Sauter(); break;
                case "Dodge": Esquiver(); break;
                case "Skill1": Charger(); break;
                case "Skill2": Soigner(); break;
            }
        }

        Vector3 DirectionEntree()
        {
            Vector2 d = m_Entrees.Deplacement;
            if (d.sqrMagnitude < 0.01f) return Vector3.zero;
            Vector3 avant = m_Camera != null ? m_Camera.AvantPlat : transform.forward;
            Vector3 droite = Vector3.Cross(Vector3.up, avant);
            return (avant * d.y + droite * d.x);
        }

        Vector3 AvantCamera => m_Camera != null ? m_Camera.AvantPlat : transform.forward;

        bool Depenser(float cout)
        {
            if (m_Endurance < cout) return false;
            m_Endurance -= cout;
            m_EnduranceUtilisee = Time.time;
            return true;
        }

        void Attaquer()
        {
            if (m_Action != Action.Libre || Time.time - m_DerniereAttaque < B.epeeIntervalle) return;
            m_Action = Action.Attaque;
            m_ActionDepuis = 0f;
            m_DerniereAttaque = Time.time;
            m_CoupPorte = false;
            m_Combo = 1 - m_Combo;
            // L'attaque part vers la caméra (visée à l'épaule).
            transform.rotation = Quaternion.LookRotation(AvantCamera);
            if (animator != null) animator.SetTrigger(m_Combo == 0 ? P_Attack1 : P_Attack2);
            AudioBank.Jouer(SonsDuJeu.EpeeElan, transform.position + Vector3.up, 0.7f);
        }

        void PorterCoup()
        {
            m_CoupPorte = true;
            var b = B;
            var cibles = new List<(Sante, float)>();
            foreach (var c in Physics.OverlapSphere(transform.position + Vector3.up, b.epeePortee + 0.6f, ~0, QueryTriggerInteraction.Ignore))
            {
                var s = c.GetComponentInParent<Sante>();
                if (s == null || s.equipe != Equipe.Ennemis || s.Mort) continue;
                Vector3 d = s.transform.position - transform.position; d.y = 0f;
                float dist = d.magnitude - 0.4f;
                if (dist > b.epeePortee || Vector3.Angle(transform.forward, d) > b.epeeDemiAngle) continue;
                bool deja = false;
                foreach (var t in cibles) if (t.Item1 == s) deja = true;
                if (!deja) cibles.Add((s, dist));
            }
            cibles.Sort((a, c) => a.Item2.CompareTo(c.Item2));
            int n = 0;
            foreach (var t in cibles)
            {
                if (n++ >= b.epeeCiblesParCoup) break;
                Frapper(t.Item1, b.epeeDegats, false);
                AudioBank.Jouer(SonsDuJeu.EpeeImpact, t.Item1.transform.position + Vector3.up, 0.9f);
            }
        }

        float Frapper(Sante s, float degats, bool critique)
        {
            float reel = s.Encaisser(new InfoDegats
            {
                montant = degats, sourceId = Id, equipeSource = Equipe.Heros, source = gameObject, critique = critique,
                point = s.transform.position + Vector3.up, direction = (s.transform.position - transform.position).normalized
            });
            if (m_Partie != null && reel > 0f) m_Partie.CompterDegats(Id, reel, critique);
            return reel;
        }

        void Sauter()
        {
            if (m_Action != Action.Libre || !m_AuSol || !Depenser(B.sautCout)) return;
            m_VitesseY = Mathf.Sqrt(2f * B.gravite * B.hauteurSaut);
            m_AuSol = false;
            if (animator != null) animator.SetTrigger(P_Jump);
            AudioBank.Jouer(SonsDuJeu.Saut, transform.position, 0.5f);
        }

        void Esquiver()
        {
            if ((m_Action != Action.Libre && m_Action != Action.Attaque) || m_RechargeEsquive > 0f || !Depenser(B.esquiveCout)) return;
            Vector3 d = DirectionEntree();
            m_DirAction = d.sqrMagnitude > 0.01f ? d.normalized : transform.forward;
            transform.rotation = Quaternion.LookRotation(m_DirAction);
            m_Action = Action.Esquive;
            m_ActionDepuis = 0f;
            m_RechargeEsquive = B.esquiveRecharge;
            m_InvulnerableJusque = Time.time + B.esquiveInvulnerable;
            if (animator != null) animator.SetTrigger(P_Dodge);
            AudioBank.Jouer(SonsDuJeu.Esquive, transform.position + Vector3.up, 0.8f);
        }

        /// Charge bélier : ruée de 7 m au plus vers la visée ; les ennemis sur le chemin sont repoussés sur le côté et
        /// brièvement étourdis ; la cible au bout est frappée (dégâts selon la distance parcourue) et longuement étourdie.
        void Charger()
        {
            if (!PeutAgir || m_RechargeCharge > 0f) return;
            var b = B;
            m_DirAction = AvantCamera;
            transform.rotation = Quaternion.LookRotation(m_DirAction);
            // Cible : l'ennemi le mieux aligné avec la visée, à portée de charge.
            m_CibleCharge = null;
            float meilleur = float.MaxValue;
            var dv = DirecteurVagues.Instance;
            if (dv != null)
                foreach (var s in dv.Vivants)
                {
                    if (s == null || !s.Vivant) continue;
                    Vector3 d = s.transform.position - transform.position; d.y = 0f;
                    float le = Vector3.Dot(d, m_DirAction);
                    if (le < 1f || le > b.chargeDistance + 1.2f) continue;
                    float lat = (d - m_DirAction * le).magnitude;
                    if (lat > 1.6f) continue;
                    float score = le + lat * 3f;
                    // Plus loin mais bien aligné : c'est la cible au bout ; ceux d'avant sont repoussés.
                    if (lat < 0.9f) score = -le;
                    if (score < meilleur) { meilleur = score; m_CibleCharge = s; }
                }
            m_DistanceCharge = b.chargeDistance;
            if (m_CibleCharge != null)
            {
                float le = Vector3.Dot(m_CibleCharge.transform.position - transform.position, m_DirAction);
                m_DistanceCharge = Mathf.Clamp(le - 1.1f, 0.3f, b.chargeDistance);
            }
            m_Action = Action.ChargeAnticipation;
            m_ActionDepuis = 0f;
            m_RechargeCharge = b.chargeRecharge;
            m_Repousses.Clear();
            m_ParcouruCharge = 0f;
            if (animator != null) animator.SetTrigger(P_Charge);
            if (m_ChargeVisuel != null) m_ChargeVisuel.Jouer(transform, m_DirAction, 99f, b.chargeDistance);
            AudioBank.Jouer(SonsDuJeu.Charge, transform.position + Vector3.up, 1f);
        }

        void Soigner()
        {
            if (!PeutAgir || m_RechargeSoin > 0f) return;
            m_Action = Action.Soin;
            m_ActionDepuis = 0f;
            m_SoinDonne = false;
            m_RechargeSoin = B.soinRecharge;
            if (animator != null) animator.SetTrigger(P_Heal);
            AudioBank.Jouer(SonsDuJeu.Soin, transform.position + Vector3.up, 0.9f);
        }

        // ----------------------------------------------------------------- Garde et parade

        Interception Intercepter(InfoDegats info)
        {
            if (Time.time < m_InvulnerableJusque) return Interception.Bloque;   // esquive : coup évité
            if (!m_Garde || info.source == null) return Interception.Passe;
            Vector3 vers = info.source.transform.position - transform.position; vers.y = 0f;
            if (Vector3.Angle(transform.forward, vers) > B.gardeDemiAngle) return Interception.Passe;
            var sq = info.source.GetComponent<Squelette>();
            if (Time.time - m_GardeDepuis <= B.paradeFenetre)
            {
                if (sq != null) sq.Etourdir(B.paradeEtourdi, Id);
                return Interception.Pare;
            }
            float cout = info.montant * B.gardeCoutParDegat;
            if (m_Endurance >= cout)
            {
                Depenser(cout);
                return Interception.Bloque;
            }
            // Garde brisée : le coup passe et le héros est déséquilibré.
            m_Endurance = 0f;
            m_EnduranceUtilisee = Time.time;
            m_Garde = false;
            m_Action = Action.Etourdi;
            m_Etourdi = B.gardeBriseeEtourdi;
            return Interception.Passe;
        }

        void OnIntercepte(InfoDegats info, Interception r)
        {
            if (Time.time < m_InvulnerableJusque && !m_Garde) return;   // coup esquivé
            if (animator != null) animator.SetTrigger(P_BlockHit);
            m_HautJusque = Time.time + 0.5f;
            AudioBank.Jouer(r == Interception.Pare ? SonsDuJeu.Parade : SonsDuJeu.Blocage, transform.position + Vector3.up * 1.2f, 1f);
            if (r == Interception.Pare && m_Partie != null) m_Partie.Journal("Parade !");
        }

        void OnTouche(InfoDegats info, float reel)
        {
            if (reel <= 0f) return;
            AudioBank.Jouer(SonsDuJeu.JoueurTouche, transform.position + Vector3.up, 0.9f, 0.2f);
            if (animator != null && !Sante.Mort) { animator.SetTrigger(P_Hit); m_HautJusque = Time.time + 0.6f; }
        }

        void OnTue(InfoDegats info)
        {
            m_Action = Action.Mort;
            m_ActionDepuis = 0f;
            m_Garde = false;
            m_CC.enabled = false;
            if (animator != null) { animator.SetBool(P_Guard, false); animator.SetBool(P_Dead, true); }
            AudioBank.Jouer(SonsDuJeu.JoueurMort, transform.position + Vector3.up, 1f);
            if (m_Partie != null) m_Partie.SignalerMort(Id);
            Invoke(nameof(Dissoudre), 1.0f);
        }

        void Dissoudre()
        {
            if (m_Action != Action.Mort) return;
            AudioBank.Jouer(SonsDuJeu.EnergieMort, transform.position + Vector3.up, 0.8f);
            if (MortAllie.Instance != null) MortAllie.Instance.Mourir(gameObject, Nyxessa.Instance);
            else foreach (var r in GetComponentsInChildren<Renderer>()) r.enabled = false;
        }

        /// Réapparition près de Nyxessa (appelée par Partie).
        public void Reapparaitre(Vector3 point)
        {
            CancelInvoke(nameof(Dissoudre));
            m_Action = Action.Reapparition;
            m_ActionDepuis = 0f;
            Sante.Ranimer();
            m_Endurance = B.endurance;
            m_CC.enabled = false;
            transform.position = point;
            Vector3 vers = -new Vector3(point.x, 0f, point.z);
            if (vers.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(-vers.normalized);
            if (animator != null) { animator.SetBool(P_Dead, false); animator.SetTrigger(P_Respawn); }
            System.Action fin = () =>
            {
                m_Action = Action.Libre;
                m_CC.enabled = true;
                m_InvulnerableJusque = Time.time + B.reapparitionInvulnerable;
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
            m_ActionDepuis += dt;
            m_RechargeCharge = Mathf.Max(0f, m_RechargeCharge - dt);
            m_RechargeSoin = Mathf.Max(0f, m_RechargeSoin - dt);
            m_RechargeEsquive = Mathf.Max(0f, m_RechargeEsquive - dt);
            Sante.invulnerable = b.joueurInvincible || Time.time < m_InvulnerableJusque && m_Action != Action.Esquive;

            // Visière : abaissée la nuit (crépuscule compris), relevée le jour.
            if (visiere != null && m_Partie != null)
            {
                var ph = m_Partie.Etat.phase;
                visiere.open = !(ph == Phase.Crepuscule || ph == Phase.Nuit);
            }

            if (m_Camera != null && m_Action != Action.Mort) { Vector2 r = m_Entrees.Regard(); m_Camera.Tourner(r.x, r.y); }

            if (m_Action == Action.Mort || m_Action == Action.Reapparition || !m_CC.enabled)
            {
                MajAnimation(0f);
                return;
            }

            bool enJeu = m_Partie != null && m_Partie.EnCours;
            Vector3 dir = enJeu ? DirectionEntree() : Vector3.zero;
            Vector3 deplacement = Vector3.zero;
            float vitesse = 0f;

            // Garde : maintenue, en action libre, avec de l'endurance.
            bool garde = enJeu && m_Entrees.GardeMaintenue && m_Action == Action.Libre && m_Endurance > 0f;
            if (garde && !m_Garde && Time.time - m_GardeDepuis > 0.3f) m_GardeDepuis = Time.time;
            m_Garde = garde;

            switch (m_Action)
            {
                case Action.Libre:
                {
                    bool sprint = m_Entrees.SprintMaintenu && dir.sqrMagnitude > 0.01f && !m_Garde && m_Endurance > 0f;
                    vitesse = b.vitesse * (sprint ? b.sprintMultiplicateur : 1f) * (m_Garde ? b.gardeVitesse : 1f);
                    if (sprint) { m_Endurance = Mathf.Max(0f, m_Endurance - b.sprintCout * dt); m_EnduranceUtilisee = Time.time; }
                    deplacement = dir * vitesse;
                    Vector3 face = m_Garde ? AvantCamera : dir;
                    if (face.sqrMagnitude > 0.01f)
                        transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(face), 720f * dt);
                    vitesse = dir.magnitude * (sprint ? 1f : 0.62f);
                    break;
                }
                case Action.Attaque:
                    deplacement = dir * b.vitesse * 0.25f;
                    if (!m_CoupPorte && m_ActionDepuis >= b.epeeInstant) PorterCoup();
                    if (m_ActionDepuis >= b.epeeIntervalle) m_Action = Action.Libre;
                    break;
                case Action.Esquive:
                {
                    float k = Mathf.Clamp01(m_ActionDepuis / b.esquiveDuree);
                    float v = 2f * b.esquiveDistance / b.esquiveDuree * (1f - k);   // départ franc, freinage (intégrale = distance)
                    deplacement = m_DirAction * v;
                    if (m_ActionDepuis >= b.esquiveDuree + 0.1f) m_Action = Action.Libre;
                    break;
                }
                case Action.ChargeAnticipation:
                    if (m_ActionDepuis >= b.chargeAnticipation) { m_Action = Action.Charge; m_ActionDepuis = 0f; m_DepartCharge = transform.position; TraverserEnnemis(true); }
                    break;
                case Action.Charge:
                    deplacement = MajCharge(dt);
                    break;
                case Action.Soin:
                    if (!m_SoinDonne && m_ActionDepuis >= b.soinIncantation)
                    {
                        m_SoinDonne = true;
                        Sante.Soigner(Sante.pvMax * b.soinPart);
                        if (aura != null) aura.Jouer();
                    }
                    if (m_ActionDepuis >= b.soinIncantation + 0.5f) m_Action = Action.Libre;
                    break;
                case Action.Etourdi:
                    m_Etourdi -= dt;
                    if (m_Etourdi <= 0f) m_Action = Action.Libre;
                    break;
            }

            // Endurance : régénération après un court délai sans dépense.
            if (Time.time - m_EnduranceUtilisee > b.enduranceDelai && !m_Garde)
                m_Endurance = Mathf.Min(b.endurance, m_Endurance + b.enduranceRegen * dt);

            // Gravité et saut.
            bool etaitAuSol = m_AuSol;
            if (m_AuSol && m_VitesseY < 0f) m_VitesseY = -2f;
            m_VitesseY -= b.gravite * dt;
            if (m_Action == Action.Charge) m_VitesseY = Mathf.Min(m_VitesseY, -2f);
            var flags = m_CC.Move((deplacement + Vector3.up * m_VitesseY) * dt);
            m_AuSol = (flags & CollisionFlags.Below) != 0 || m_CC.isGrounded;
            if (m_AuSol && !etaitAuSol && m_VitesseY < -6f) AudioBank.Jouer(SonsDuJeu.Reception, transform.position, 0.6f);
            if (m_Action == Action.Charge && (flags & CollisionFlags.Sides) != 0 && m_ActionDepuis > 0.08f) FinCharge();

            // Pas.
            if (m_AuSol && deplacement.sqrMagnitude > 1f && m_Action == Action.Libre)
            {
                m_Pas -= dt * deplacement.magnitude / 5f;
                if (m_Pas <= 0f) { m_Pas = 0.42f; AudioBank.Jouer(SonsDuJeu.Pas, transform.position, 0.35f); }
            }
            MajAnimation(vitesse);
        }

        Vector3 MajCharge(float dt)
        {
            var b = B;
            float duree = b.chargeDuree * m_DistanceCharge / b.chargeDistance;
            float k = Mathf.Clamp01(m_ActionDepuis / Mathf.Max(0.05f, duree));
            float voulu = m_DistanceCharge * (1f - (1f - k) * (1f - k) * (1f - 0.3f * k));   // départ franc, léger freinage
            m_ParcouruCharge = Vector3.Dot(transform.position - m_DepartCharge, m_DirAction);
            Vector3 dep = m_DirAction * Mathf.Max(0f, voulu - m_ParcouruCharge) / Mathf.Max(dt, 0.001f);
            // Ennemis sur le chemin (bulle 1,2 m devant) : repoussés sur le côté, étourdis brièvement.
            var dv = DirecteurVagues.Instance;
            if (dv != null)
                foreach (var s in dv.Vivants)
                {
                    if (s == null || !s.Vivant || s == m_CibleCharge || m_Repousses.Contains(s)) continue;
                    Vector3 d = s.transform.position - transform.position; d.y = 0f;
                    float le = Vector3.Dot(d, m_DirAction);
                    if (le < -0.3f || le > 1.4f) continue;
                    Vector3 lat = d - m_DirAction * le;
                    if (lat.magnitude > b.chargeLargeur) continue;
                    m_Repousses.Add(s);
                    Vector3 cote = m_ChargeVisuel != null ? m_ChargeVisuel.Repousser(s.transform, b.chargeEtourdiRepousses) : Vector3.zero;
                    if (cote.sqrMagnitude < 0.01f) cote = lat.sqrMagnitude > 0.01f ? lat.normalized : Vector3.Cross(Vector3.up, m_DirAction);
                    s.Repousser(cote.normalized * b.chargeRepoussement, b.chargeEtourdiRepousses, Id);
                }
            if (k >= 1f) FinCharge();
            return dep;
        }

        void FinCharge()
        {
            if (m_Action != Action.Charge) return;
            var b = B;
            m_ParcouruCharge = Mathf.Max(0f, Vector3.Dot(transform.position - m_DepartCharge, m_DirAction));
            float force = Mathf.Clamp01(m_ParcouruCharge / b.chargeDistance);
            float degats = Mathf.Lerp(b.chargeDegatsMin, b.chargeDegatsMax, force);
            if (m_CibleCharge != null && m_CibleCharge.Vivant && Vector3.Distance(m_CibleCharge.transform.position, transform.position) < 2.6f)
            {
                Frapper(m_CibleCharge.Sante, degats, false);
                if (m_CibleCharge.Vivant)
                {
                    m_CibleCharge.Etourdir(b.chargeEtourdiCible, Id);
                    if (m_CibleCharge.Repoussable) m_CibleCharge.Repousser(m_DirAction * 0.6f, b.chargeEtourdiCible, Id);
                    if (m_ChargeVisuel != null) m_ChargeVisuel.EtourdirCible(m_CibleCharge.transform, b.chargeEtourdiCible * m_CibleCharge.FacteurEtourdissement);
                }
                if (m_Partie != null) m_Partie.Journal("Charge : " + m_ParcouruCharge.ToString("F1") + " m, force " + force.ToString("F2") + ", " + degats.ToString("F0") + " dégâts sur " + m_CibleCharge.type + ", " + m_Repousses.Count + " repoussés");
            }
            else if (m_Partie != null) m_Partie.Journal("Charge : " + m_ParcouruCharge.ToString("F1") + " m sans cible, " + m_Repousses.Count + " repoussés");
            if (m_ChargeVisuel != null) m_ChargeVisuel.Impact(force);
            TraverserEnnemis(false);
            AudioBank.Jouer(SonsDuJeu.ChargeImpact, transform.position + transform.forward, 0.8f * (0.5f + force));
            m_Action = Action.Libre;
            m_CibleCharge = null;
        }

        /// Pendant la ruée, le héros traverse les squelettes (ils sont repoussés sur le côté) ; seuls le décor et la cible
        /// au bout l'arrêtent.
        readonly List<Collider> m_Ignores = new List<Collider>();
        void TraverserEnnemis(bool traverser)
        {
            if (!traverser)
            {
                foreach (var c in m_Ignores) if (c != null) Physics.IgnoreCollision(m_CC, c, false);
                m_Ignores.Clear();
                return;
            }
            var dv = DirecteurVagues.Instance;
            if (dv == null) return;
            foreach (var s in dv.Vivants)
            {
                if (s == null || s == m_CibleCharge) continue;
                foreach (var c in s.GetComponentsInChildren<Collider>()) { Physics.IgnoreCollision(m_CC, c, true); m_Ignores.Add(c); }
            }
        }

        void MajAnimation(float vitesse)
        {
            if (animator == null) return;
            animator.SetFloat(P_Speed, vitesse, 0.1f, Time.deltaTime);
            animator.SetBool(P_Grounded, m_AuSol);
            animator.SetBool(P_Guard, m_Garde);
            if (m_CoucheHaut >= 0)
            {
                float voulu = m_Garde || Time.time < m_HautJusque || m_Action == Action.ChargeAnticipation ? 1f : 0f;
                m_PoidsHaut = Mathf.MoveTowards(m_PoidsHaut, voulu, Time.deltaTime * 8f);
                animator.SetLayerWeight(m_CoucheHaut, m_PoidsHaut);
            }
        }

        void OnDestroy()
        {
            if (m_ChargeVisuel != null) Destroy(m_ChargeVisuel.gameObject);
        }
    }
}
