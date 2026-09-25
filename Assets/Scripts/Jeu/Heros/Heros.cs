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
        public enum Etat { Libre, Esquive, Etourdi, Mort, Reapparition }

        public Animator animator;
        public HelmetVisor visiere;

        public Sante Sante { get; private set; }
        public ClasseHeros Classe { get; private set; }
        public HerosEntrees Entrees { get; private set; }
        public CharacterController CC { get; private set; }
        public Partie Partie { get; private set; }
        public int Id => m_Etat != null ? m_Etat.id : 0;
        public bool Vivant => !Sante.Mort && m_EtatCourant != Etat.Mort && m_EtatCourant != Etat.Reapparition;
        public Etat EtatCourant => m_EtatCourant;
        public bool AuSol => m_AuSol;
        public float Endurance => m_Endurance;
        public bool EnJeu => Partie != null && Partie.EnCours;
        /// Libre de lancer une action (vivant, en jeu, ni esquive ni étourdissement, pas d'action de classe en cours).
        public bool PeutAgir => m_EtatCourant == Etat.Libre && Vivant && EnJeu && (Classe == null || !Classe.Occupe);

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
        float m_InvulnerableJusque;
        bool m_EsquiveArriere;
        float m_Pas;
        int m_CoucheHaut = -1;
        float m_PoidsHaut;
        float m_HautJusque;
        bool m_Sprint;

        static readonly int P_Speed = Animator.StringToHash("Speed");
        static readonly int P_Grounded = Animator.StringToHash("Grounded");
        static readonly int P_Dead = Animator.StringToHash("Dead");
        static readonly int P_Dodge = Animator.StringToHash("Dodge");
        static readonly int P_Jump = Animator.StringToHash("Jump");
        static readonly int P_Hit = Animator.StringToHash("Hit");
        static readonly int P_Respawn = Animator.StringToHash("Respawn");

        GameBalance B => GameBalance.Courant;

        void Awake()
        {
            CC = GetComponent<CharacterController>();
            Entrees = GetComponent<HerosEntrees>();
            Sante = GetComponent<Sante>();
            Sante.equipe = Equipe.Heros;
            Classe = GetComponent<ClasseHeros>();
            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (visiere == null) visiere = GetComponentInChildren<HelmetVisor>();
            if (animator != null) m_CoucheHaut = animator.GetLayerIndex("HautDuCorps");
        }

        public void Initialiser(Partie partie, EtatJoueur etat)
        {
            Partie = partie;
            m_Etat = etat;
            m_Camera = partie.cameraJeu;
            if (Classe != null) Classe.Initialiser(this);
            Sante.Initialiser(Classe != null ? Classe.PvMax : B.herosPV);
            Sante.invulnerable = B.joueurInvincible;
            Sante.intercepteur = Intercepter;
            Sante.Touche += OnTouche;
            Sante.Intercepte += (i, r) => { if (Classe != null) Classe.SurIntercepte(i, r); };
            Sante.Tue += OnTue;
            Sante.Soigne += reel => { if (Partie != null) Partie.CompterSoins(Id, reel); };
            m_Endurance = B.endurance;
            Entrees.Action += OnAction;
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
        public void EsquiveImposee(Vector3 direction, bool arriere)
        {
            m_DirEsquive = direction.normalized;
            m_EsquiveArriere = arriere;
            m_EtatCourant = Etat.Esquive;
            m_EtatDepuis = 0f;
            Invulnerable(B.esquiveInvulnerable);
            if (animator != null) animator.SetTrigger(arriere ? "DodgeBack" : "Dodge");
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
            if (!Vivant || !Partie.EnCours) return;
            switch (action)
            {
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
            if (animator != null) animator.SetTrigger(P_Jump);
            AudioBank.Jouer(SonsDuJeu.Saut, transform.position, 0.5f);
        }

        void Esquiver()
        {
            if (m_EtatCourant != Etat.Libre || m_RechargeEsquive > 0f || (Classe != null && !Classe.PeutEsquiver) || !Depenser(B.esquiveCout)) return;
            if (Classe != null) Classe.Interrompre();
            Vector3 d = DirectionEntree();
            Vector3 dir = d.sqrMagnitude > 0.01f ? d.normalized : transform.forward;
            transform.rotation = Quaternion.LookRotation(dir);
            m_RechargeEsquive = B.esquiveRecharge;
            EsquiveImposee(dir, false);
            AudioBank.Jouer(SonsDuJeu.Esquive, transform.position + Vector3.up, 0.8f);
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
            if (animator != null && !Sante.Mort) { animator.SetTrigger(P_Hit); HautDuCorpsPendant(0.6f); }
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
            if (animator != null) { animator.SetBool(P_Dead, false); animator.SetTrigger(P_Respawn); }
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

            if (m_Camera != null && m_EtatCourant != Etat.Mort) { Vector2 r = Entrees.Regard(); m_Camera.Tourner(r.x, r.y); }

            if (m_EtatCourant == Etat.Mort || m_EtatCourant == Etat.Reapparition || !CC.enabled)
            {
                MajAnimation(0f);
                return;
            }

            bool enJeu = EnJeu;
            Vector3 dir = enJeu ? DirectionEntree() : Vector3.zero;
            Vector3 deplacement = Vector3.zero;
            float vitesseAnim = 0f;
            bool impose = false;

            if (Classe != null) Classe.Temps(dt);
            if (m_EtatCourant == Etat.Libre && Classe != null && enJeu) Classe.Maj(dt, dir);

            switch (m_EtatCourant)
            {
                case Etat.Libre:
                {
                    if (Classe != null && Classe.DeplacementImpose(dt, out Vector3 v)) { deplacement = v; impose = true; break; }
                    float facteur = Classe != null ? Classe.FacteurVitesse : 1f;
                    bool occupe = Classe != null && Classe.Occupe;
                    m_Sprint = Entrees.SprintMaintenu && dir.sqrMagnitude > 0.01f && m_Endurance > 0f && !occupe && facteur >= 0.99f && (Classe == null || !Classe.BloqueSprint);
                    float vitesse = (Classe != null ? Classe.Vitesse : b.vitesse) * (m_Sprint ? b.sprintMultiplicateur : 1f) * facteur;
                    if (m_Sprint) { m_Endurance = Mathf.Max(0f, m_Endurance - b.sprintCout * dt); m_EnduranceUtilisee = Time.time; }
                    deplacement = dir * vitesse;
                    Vector3 face = Classe != null && Classe.FaceVisee ? AvantCamera : dir;
                    if (face.sqrMagnitude > 0.01f)
                        transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(face), 720f * dt);
                    vitesseAnim = dir.magnitude * (m_Sprint ? 1f : 0.62f) * (Classe != null ? Classe.FacteurAnimation : 1f);
                    if (facteur < 0.99f) vitesseAnim *= Mathf.Max(0.5f, facteur);
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
