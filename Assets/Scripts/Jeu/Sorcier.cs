using UnityEngine;
using UnityEngine.AI;

namespace Deathless.Jeu
{
    /// Villageois sorcier (wiki : village, nyxessa ; sans combat, on ne lui parle pas pour l'instant). Le jour, il reste
    /// dans sa maison : debout à sa place (Ancre_Villageois_Sorcier, intérieurs de InterieursBuilder), visible par la porte
    /// ouverte ; sans intérieur, invisible devant la porte. Au crépuscule, il traverse la pièce, passe la porte, puis sort, marche jusqu'à Nyxessa, lève son bâton et incante
    /// toute la nuit (sort en boucle, son `sorcier_incantation`) : il lève le bouclier (BouclierNyxessa) à la tombée de la
    /// nuit. À l'aube, le bouclier redescend et il rentre chez lui. Bouclier brisé : il meurt (dissolution, son énergie
    /// retourne à Nyxessa : MortAllie) et réapparaît chez lui le jour suivant. Les squelettes peuvent le frapper comme
    /// Nyxessa ; le bouclier levé encaisse ces coups à sa place. Autorité : l'hôte (solo : ce poste). Rien ne tourne tant
    /// que la partie n'est pas lancée (menu). Réaction de coup (Quentin, retour 0.5.2) : chaque coup encaissé par le
    /// bouclier (BouclierNyxessa.Touche, qu'il protège Nyxessa ou lui) lui fait jouer une courte réaction (Hit_A, socle
    /// commun à toutes les classes) sans perdre sa pose d'incantation plus d'un instant ; fréquence limitée
    /// (sorcierReactionCoupIntervalle), vue de tous (PartieReseau.SorcierTouche).
    [RequireComponent(typeof(NavMeshAgent), typeof(Sante))]
    public class Sorcier : MonoBehaviour
    {
        public enum Etat { Maison, Sortie, Incante, Retour, Mort }

        public static Sorcier Instance { get; private set; }

        public Animator animator;

        public Etat EtatCourant => m_Etat;
        public Sante Sante { get; private set; }
        public NavMeshAgent Agent { get; private set; }
        /// Vivant et dehors : les squelettes peuvent le viser.
        public bool Ciblable => m_Etat == Etat.Sortie || m_Etat == Etat.Incante || m_Etat == Etat.Retour;

        Etat m_Etat = Etat.Maison;
        Vector3 m_Porte, m_Place;
        // Intérieur (maison ouverte) : sa place et le passage de la porte (place -> milieu de la pièce -> seuil -> dehors),
        // marché sans NavMesh (les pièces sont hors NavMesh : les squelettes n'y entrent pas).
        bool m_Interieur;
        Vector3 m_Place0; Quaternion m_RotationPlace = Quaternion.identity;
        Vector3[] m_Chemin, m_Couloir; int m_Pas;
        float m_VitesseCouloir;
        /// Vitesse de marche actuelle (NavMesh ou passage de la porte), pour l'animation des clients.
        public float Vitesse => m_Couloir != null ? m_VitesseCouloir : Agent != null && Agent.enabled ? Agent.velocity.magnitude : 0f;
        Quaternion m_RotationPorte = Quaternion.identity;
        AudioSource m_Boucle;
        int m_CoucheHaut = -1;
        float m_PoidsHaut;
        bool m_Pret;
        // Réaction de coup (bouclier frappé, wiki : Bouclier) : le déclencheur "Hit" (Socle : Hit_A) est commun à toutes
        // les classes ; fréquence limitée ici pour ne pas trembler en continu sous une pluie de coups
        // (GameBalance.sorcierReactionCoupIntervalle).
        float m_DernierHit = -99f;

        static readonly int P_Speed = Animator.StringToHash("Speed");
        static readonly int P_Grounded = Animator.StringToHash("Grounded");
        static readonly int P_Cone = Animator.StringToHash("Cone");
        static readonly int P_Dead = Animator.StringToHash("Dead");
        static readonly int P_Respawn = Animator.StringToHash("Respawn");
        static readonly int P_Hit = Animator.StringToHash("Hit");
        static readonly int P_Invoque = Animator.StringToHash("Invoque");

        GameBalance B => GameBalance.Courant;
        Partie P => Partie.Instance;
        BouclierNyxessa Bouclier => BouclierNyxessa.Instance;

        void Awake()
        {
            Instance = this;
            Sante = GetComponent<Sante>();
            Agent = GetComponent<NavMeshAgent>();
            // Agent coupé dans la scène : allumé seulement à la sortie (Sortir), une fois le NavMesh chargé ; sinon le build
            // signale « Failed to create agent because there is no valid NavMesh » au chargement du village.
            Agent.enabled = false;
            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (animator != null) m_CoucheHaut = animator.GetLayerIndex("HautDuCorps");
        }

        void OnDestroy() { if (Instance == this) Instance = null; }

        void Start()
        {
            Sante.equipe = Equipe.Relique;
            Sante.Initialiser(B.sorcierPV);
            Sante.Tue += _ => Mourir("tué");
            // Coup qui passe malgré tout (bouclier baissé ou brisé sur ce coup) : même réaction que le bouclier frappé.
            Sante.Touche += (i, r) => { if (r > 0f) JouerReactionCoup(); };
            Agent.speed = B.sorcierVitesse;
            Agent.stoppingDistance = 0.1f;
            Calculer();
            if (P != null)
            {
                P.PhaseChangee += OnPhase;
                Sante.absorbeur = i => Bouclier != null ? Bouclier.Absorber(i) : i.montant;
            }
            if (Bouclier != null)
            {
                Bouclier.Brise += () => Mourir("bouclier brisé");
                // Le bouclier encaisse un coup (pour Nyxessa ou pour lui) : il « prend » le coup à sa place (Quentin,
                // retour 0.5.2), une courte réaction sans perdre sa pose d'incantation plus d'un instant.
                Bouclier.Touche += _ => JouerReactionCoup();
            }
            Rentrer(true);
        }

        /// Porte de sa maison (face avant, au pied du mur) et place d'incantation (près de Nyxessa, de son côté).
        void Calculer()
        {
            var maisons = GameObject.Find("Maisons");
            Transform maison = maisons != null ? maisons.transform.Find(B.sorcierMaison) : null;
            Vector3 nyx = P != null && P.nyxessa != null ? P.nyxessa.transform.position : Vector3.zero;
            if (maison != null)
            {
                Vector3 avant = maison.forward; avant.y = 0f; avant.Normalize();
                // Bord avant de la maison : le rayon part du centre vers l'avant jusqu'à sortir de ses rendus.
                var bornes = new Bounds(maison.position, Vector3.zero);
                foreach (var r in maison.GetComponentsInChildren<Renderer>()) bornes.Encapsulate(r.bounds);
                float d = 1f;
                while (d < 12f && bornes.Contains(new Vector3(maison.position.x, bornes.center.y, maison.position.z) + avant * d)) d += 0.25f;
                m_Porte = maison.position + avant * (d + 0.6f);
                m_RotationPorte = Quaternion.LookRotation(avant);
            }
            else m_Porte = nyx + new Vector3(-12f, 0f, 0f);
            if (NavMesh.SamplePosition(m_Porte, out var h1, 4f, NavMesh.AllAreas)) m_Porte = h1.position;
            // Maison ouverte : sa place à l'intérieur et le passage de la porte, dans l'axe du linteau (repère de l'intérieur).
            var it = GameObject.Find("VillageBlockout/Interieurs/Interieur_Sorcier");
            var ancre = it != null ? it.transform.Find("Ancre_Villageois_Sorcier") : null;
            Transform linteau = null;
            if (it != null) foreach (var c in it.GetComponentsInChildren<Transform>(true)) if (c.name == "Linteau") { linteau = c; break; }
            m_Interieur = ancre != null && linteau != null;
            if (m_Interieur)
            {
                var t = it.transform;
                Vector3 l = t.InverseTransformPoint(linteau.position);
                float yIn = ancre.position.y;
                m_Place0 = ancre.position;
                Vector3 milieu = t.TransformPoint(new Vector3(l.x, 0f, l.z - 1.0f)); milieu.y = yIn;
                Vector3 seuil = t.TransformPoint(new Vector3(l.x, 0f, l.z)); seuil.y = yIn;
                Vector3 dehors = t.TransformPoint(new Vector3(l.x, 0f, l.z + 1.0f)); dehors.y = m_Porte.y;
                if (NavMesh.SamplePosition(dehors, out var h3, 1.5f, NavMesh.AllAreas)) dehors = h3.position;
                m_Chemin = new[] { m_Place0, milieu, seuil, dehors };
                m_Porte = dehors;   // le NavMesh prend le relais au pied du perron
                Vector3 face = milieu - m_Place0; face.y = 0f;
                m_RotationPlace = face.sqrMagnitude > 0.01f ? Quaternion.LookRotation(face) : ancre.rotation;
            }
            Vector3 cote = m_Porte - nyx; cote.y = 0f;
            m_Place = nyx + (cote.sqrMagnitude > 0.01f ? cote.normalized : Vector3.back) * B.sorcierDistanceNyxessa;
            if (NavMesh.SamplePosition(m_Place, out var h2, 3f, NavMesh.AllAreas)) m_Place = h2.position;
            m_Pret = true;
        }

        void OnPhase(Phase avant, Phase apres)
        {
            if (!Deathless.Reseau.ReseauJeu.Autorite) return;
            switch (apres)
            {
                case Phase.Crepuscule:
                case Phase.Nuit:
                    if (m_Etat == Etat.Maison) Sortir();
                    if (apres == Phase.Nuit && m_Etat == Etat.Incante && Bouclier != null) Bouclier.Lever();
                    break;
                case Phase.Aube:
                    if (Bouclier != null) Bouclier.Baisser();
                    if (m_Etat == Etat.Sortie || m_Etat == Etat.Incante) Retour();
                    break;
                case Phase.Jour:
                    // Mort : il réapparaît chez lui ; blessé mais vivant : il repart aussi de ses PV pleins, sinon il
                    // gardait ses blessures d'une nuit à l'autre.
                    if (m_Etat == Etat.Mort) Reapparaitre();
                    else Sante.Remplir();
                    break;
                case Phase.Terminee:
                    if (Bouclier != null) Bouclier.Baisser();
                    ArreterBoucle();
                    break;
            }
        }

        void Sortir()
        {
            Visible(true);
            if (m_Interieur) { Couloir(true); m_Etat = Etat.Sortie; P?.Journal("Sorcier : quitte sa place et passe la porte"); return; }
            SortirDehors();
        }

        /// Dehors, devant la porte : le NavMesh prend le relais jusqu'à la place d'incantation.
        void SortirDehors()
        {
            Agent.enabled = true;
            Agent.Warp(m_Porte);
            Agent.isStopped = false;
            Agent.SetDestination(m_Place);
            m_Etat = Etat.Sortie;
            P?.Journal("Sorcier : sort de sa maison");
        }

        /// Passage de la porte, marché pas à pas : sortie (place -> dehors) ou entrée (dehors -> place).
        void Couloir(bool sortie)
        {
            if (Agent.enabled) { Agent.isStopped = true; Agent.enabled = false; }
            m_Couloir = new Vector3[m_Chemin.Length];
            for (int i = 0; i < m_Chemin.Length; i++) m_Couloir[i] = m_Chemin[sortie ? i : m_Chemin.Length - 1 - i];
            m_Pas = 1;
            if (sortie) transform.position = m_Couloir[0];
        }

        /// Une image de marche dans le passage de la porte ; vrai quand il est au bout.
        bool Marcher(float dt)
        {
            Vector3 cible = m_Couloir[m_Pas];
            float v = B.sorcierVitesse * 0.8f;
            Vector3 d = cible - transform.position;
            Vector3 plat = new Vector3(d.x, 0f, d.z);
            if (plat.sqrMagnitude > 0.0004f)
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(plat), 360f * dt);
            transform.position = Vector3.MoveTowards(transform.position, cible, v * dt);
            m_VitesseCouloir = v;
            if ((transform.position - cible).sqrMagnitude < 0.0004f && ++m_Pas >= m_Couloir.Length) { m_Couloir = null; m_VitesseCouloir = 0f; return true; }
            return false;
        }

        void Retour()
        {
            ArreterBoucle();
            if (animator != null) animator.SetBool(P_Cone, false);
            if (m_Couloir != null) { Couloir(false); m_Etat = Etat.Retour; P?.Journal("Sorcier : rentre chez lui"); return; }   // pas encore sorti : demi-tour
            Agent.enabled = true;
            Agent.isStopped = false;
            Agent.SetDestination(m_Porte);
            m_Etat = Etat.Retour;
            P?.Journal("Sorcier : rentre chez lui");
        }

        /// Chez lui : à sa place dans la pièce, visible par la porte ouverte (sans intérieur : invisible, à la porte).
        void Rentrer(bool debut)
        {
            ArreterBoucle();
            m_Couloir = null;
            if (Agent.enabled) { Agent.Warp(m_Porte); Agent.isStopped = true; }
            if (m_Interieur) transform.SetPositionAndRotation(m_Place0, m_RotationPlace);
            else transform.SetPositionAndRotation(m_Porte, m_RotationPorte);
            Agent.enabled = false;
            Visible(m_Interieur);
            m_Etat = Etat.Maison;
            if (!debut) P?.Journal("Sorcier : chez lui");
        }

        void Mourir(string cause)
        {
            if (m_Etat == Etat.Mort || m_Etat == Etat.Maison) return;
            m_Etat = Etat.Mort;
            ArreterBoucle();
            m_Couloir = null;
            if (Bouclier != null && Bouclier.Incantation) Bouclier.Baisser();
            if (Agent.enabled) { Agent.isStopped = true; Agent.enabled = false; }
            if (animator != null) { animator.SetBool(P_Cone, false); animator.SetBool(P_Dead, true); }
            AudioBank.Jouer(SonsDuJeu.JoueurMort, transform.position + Vector3.up, 0.8f);
            P?.Journal("Sorcier mort (" + cause + ") : plus de bouclier jusqu'à la nuit suivante");
            Invoke(nameof(Dissoudre), 1.8f);   // il tombe en arrière (Death_A), reste étendu un instant, puis se dissout
        }

        void Dissoudre()
        {
            if (m_Etat != Etat.Mort) return;
            AudioBank.Jouer(SonsDuJeu.EnergieMort, transform.position + Vector3.up, 0.8f);
            if (MortAllie.Instance != null) MortAllie.Instance.Mourir(gameObject, Nyxessa.Instance);
            else Visible(false);
        }

        /// Le jour suivant : de nouveau chez lui, en vie.
        void Reapparaitre()
        {
            CancelInvoke(nameof(Dissoudre));
            Sante.Ranimer();
            if (animator != null) { animator.SetBool(P_Dead, false); animator.SetTrigger(P_Respawn); }
            Rentrer(false);
            P?.Journal("Sorcier : réapparaît chez lui");
        }

        void Update()
        {
            if (!m_Pret || P == null) return;
            if (Partie.ClientReseau) { SuivreHote(); return; }
            float vitesse = 0f;
            switch (m_Etat)
            {
                case Etat.Sortie:
                    if (m_Couloir != null) { vitesse = m_VitesseCouloir; if (Marcher(Time.deltaTime)) SortirDehors(); break; }
                    vitesse = Agent.velocity.magnitude;
                    if (!Agent.pathPending && Agent.remainingDistance <= 0.25f)
                    {
                        Agent.isStopped = true;
                        m_Etat = Etat.Incante;
                        m_Pivot = 0f;
                        // Il lève son bâton (invocation, le temps que le bouclier monte), puis incante en boucle.
                        if (animator != null) { animator.SetTrigger(P_Invoque); animator.SetBool(P_Cone, true); }
                        Deathless.Reseau.PartieReseau.Instance?.SorcierInvoque();
                        m_Boucle = AudioBank.Boucle(SonsDuJeu.SorcierIncantation, transform, 0.35f);
                        if (P.Etat.phase == Phase.Nuit && Bouclier != null) Bouclier.Lever();
                        P.Journal("Sorcier : incante près de Nyxessa");
                    }
                    break;
                case Etat.Incante:
                    Orienter(Time.deltaTime);
                    break;
                case Etat.Retour:
                    if (m_Couloir != null) { vitesse = m_VitesseCouloir; if (Marcher(Time.deltaTime)) Rentrer(false); break; }
                    vitesse = Agent.velocity.magnitude;
                    if (!Agent.pathPending && Agent.remainingDistance <= 0.3f)
                    {
                        if (m_Interieur) { Couloir(false); vitesse = 0f; }   // devant la porte : il entre et regagne sa place
                        else Rentrer(false);
                    }
                    break;
            }
            if (animator != null)
            {
                animator.SetFloat(P_Speed, vitesse / Mathf.Max(0.1f, B.sorcierVitesse) * 0.62f, 0.1f, Time.deltaTime);
                animator.SetBool(P_Grounded, true);
                if (m_CoucheHaut >= 0)
                {
                    m_PoidsHaut = Mathf.MoveTowards(m_PoidsHaut, m_Etat == Etat.Incante ? 1f : 0f, Time.deltaTime * 4f);
                    animator.SetLayerWeight(m_CoucheHaut, m_PoidsHaut);
                }
            }
        }

        float m_Pivot;

        /// Dos à Nyxessa, face à l'extérieur (là où arrivent les squelettes) ; il se tourne doucement vers l'ennemi le plus
        /// proche devant lui, de ±sorcierPivotMax degrés au plus, et garde toujours Nyxessa dans le dos.
        void Orienter(float dt)
        {
            Vector3 dehors = transform.position - (P.nyxessa != null ? P.nyxessa.transform.position : Vector3.zero); dehors.y = 0f;
            if (dehors.sqrMagnitude < 0.01f) return;
            dehors.Normalize();
            float voulu = 0f, dmin = 18f;
            var dv = DirecteurVagues.Instance;
            if (dv != null)
                foreach (var s in dv.Vivants)
                {
                    if (s == null || !s.Vivant) continue;
                    Vector3 d = s.transform.position - transform.position; d.y = 0f;
                    float dist = d.magnitude;
                    if (dist > dmin || dist < 0.1f) continue;
                    float a = Vector3.SignedAngle(dehors, d, Vector3.up);
                    if (Mathf.Abs(a) > 90f) continue;
                    dmin = dist;
                    voulu = Mathf.Clamp(a, -B.sorcierPivotMax, B.sorcierPivotMax);
                }
            m_Pivot = Mathf.MoveTowards(m_Pivot, voulu, 30f * dt);
            Quaternion cible = Quaternion.LookRotation(Quaternion.Euler(0f, m_Pivot, 0f) * dehors);
            // En arrivant, il se retourne (demi-tour en un peu plus d'une seconde), puis ne bouge plus que doucement.
            transform.rotation = Quaternion.RotateTowards(transform.rotation, cible, 160f * dt);
        }

        // ----------------------------------------------------------------- Client d'une partie réseau (l'hôte fait foi)

        Etat m_EtatVu = Etat.Maison;

        /// Client : marionnette du sorcier de l'hôte (état, position, orientation, vitesse de marche : PartieReseau).
        void SuivreHote()
        {
            var r = Deathless.Reseau.PartieReseau.Instance;
            if (r == null) return;
            var e = (Etat)r.SorcierEtat.Value;
            if (e != m_EtatVu)
            {
                var avant = m_EtatVu;
                m_EtatVu = e;
                m_Etat = e;
                switch (e)
                {
                    case Etat.Maison:
                        ArreterBoucle();
                        if (animator != null) { animator.SetBool(P_Cone, false); if (avant == Etat.Mort) { animator.SetBool(P_Dead, false); animator.SetTrigger(P_Respawn); } }
                        CancelInvoke(nameof(Dissoudre));
                        if (m_Interieur) transform.SetPositionAndRotation(m_Place0, m_RotationPlace);
                        Visible(m_Interieur);
                        break;
                    case Etat.Sortie:
                    case Etat.Retour:
                        ArreterBoucle();
                        if (animator != null) animator.SetBool(P_Cone, false);
                        transform.position = r.SorcierPosition.Value;
                        Visible(true);
                        break;
                    case Etat.Incante:
                        Visible(true);
                        if (animator != null) animator.SetBool(P_Cone, true);
                        if (m_Boucle == null) m_Boucle = AudioBank.Boucle(SonsDuJeu.SorcierIncantation, transform, 0.35f);
                        break;
                    case Etat.Mort:
                        ArreterBoucle();
                        if (animator != null) { animator.SetBool(P_Cone, false); animator.SetBool(P_Dead, true); }
                        AudioBank.Jouer(SonsDuJeu.JoueurMort, transform.position + Vector3.up, 0.8f);
                        Invoke(nameof(Dissoudre), 1.8f);
                        break;
                }
            }
            if (m_Etat != Etat.Maison && m_Etat != Etat.Mort)
            {
                transform.position = Vector3.Lerp(transform.position, r.SorcierPosition.Value, 1f - Mathf.Exp(-Time.deltaTime * 12f));
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(0f, r.SorcierLacet.Value, 0f), 1f - Mathf.Exp(-Time.deltaTime * 10f));
            }
            if (animator != null)
            {
                animator.SetFloat(P_Speed, r.SorcierVitesse.Value / Mathf.Max(0.1f, B.sorcierVitesse) * 0.62f, 0.1f, Time.deltaTime);
                animator.SetBool(P_Grounded, true);
                if (m_CoucheHaut >= 0)
                {
                    m_PoidsHaut = Mathf.MoveTowards(m_PoidsHaut, m_Etat == Etat.Incante ? 1f : 0f, Time.deltaTime * 4f);
                    animator.SetLayerWeight(m_CoucheHaut, m_PoidsHaut);
                }
            }
        }

        /// Client : il lève son bâton (invocation) comme chez l'hôte.
        public void InvoquerDistant() { if (animator != null) animator.SetTrigger(P_Invoque); }

        /// Hôte (et poste solo) : le bouclier vient d'encaisser un coup, ou un coup est passé jusqu'à lui. Fréquence
        /// limitée (sorcierReactionCoupIntervalle) pour ne pas trembler en continu sous une pluie de coups ; vu de tous
        /// (PartieReseau.SorcierTouche). Réutilise le déclencheur "Hit" commun à toutes les classes (Socle : Hit_A), qui
        /// revient à Vide puis, tant que le bouclier tient (Cone), repart aussitôt vers l'incantation.
        void JouerReactionCoup()
        {
            if (Partie.ClientReseau || m_Etat == Etat.Mort || m_Etat == Etat.Maison) return;
            if (Time.time - m_DernierHit < B.sorcierReactionCoupIntervalle) return;
            m_DernierHit = Time.time;
            if (animator != null) animator.SetTrigger(P_Hit);
            if (Deathless.Reseau.ReseauJeu.EnPartie && Deathless.Reseau.ReseauJeu.Autorite) Deathless.Reseau.PartieReseau.Instance?.SorcierTouche();
        }

        /// Client : réaction de coup du sorcier (bouclier touché), comme chez l'hôte.
        public void ToucherDistant()
        {
            if (animator == null || m_Etat == Etat.Mort || m_Etat == Etat.Maison) return;
            animator.SetTrigger(P_Hit);
        }

        void ArreterBoucle()
        {
            if (m_Boucle != null) { m_Boucle.Stop(); Destroy(m_Boucle); m_Boucle = null; }
        }

        void Visible(bool oui)
        {
            foreach (var r in GetComponentsInChildren<Renderer>(true)) r.enabled = oui;
        }
    }
}
