using UnityEngine;
using UnityEngine.AI;

namespace Deathless.Jeu
{
    /// Nyxar, le Nécromancien : boss final de la nuit 12 (wiki : ennemis.md, section Nyxar ; kit complet codé le
    /// 30/09/2026, valeurs GameBalance necro* et nyxar*, à équilibrer). Ses yeux brillent en vert Nyxessa.
    ///
    /// Deux éclats de Nyx (EclatNyx), dans le crâne de sa couronne et dans celui de son grimoire à la ceinture, sont
    /// ses points faibles : tant qu'un éclat tient, il est invulnérable (Sante.invulnerable) ; chaque éclat brisé lui
    /// retire un tiers de ses PV max et une partie de son kit. Trois phases :
    /// - phase 1 (deux éclats) : garde ses distances, se téléporte quand on l'approche, tire des salves de crânes,
    ///   relève des squelettes autour de lui, fauche à la faux qui le serre de près ;
    /// - phase 2, couronne brisée : plus de téléportation ni de squelettes relevés ; grimoire brisé : plus de salves
    ///   (un seul crâne par tir) ;
    /// - phase 3 (deux éclats brisés) : enragé, il se bat au corps à corps à la faux (comportement de base d'un
    ///   squelette, plus rapide et plus fort) ; il devient tuable.
    /// L'IA tourne chez l'hôte seulement ; les clients voient les crânes (PartieReseau.Missile), les sbires (apparition
    /// réseau), la téléportation et les éclats (EnnemiReseau, EffetsBoss).
    public class Necromancien : Squelette
    {
        static readonly int P_Shoot = Animator.StringToHash("Shoot");
        static readonly int P_Cast = Animator.StringToHash("Cast");

        [Tooltip("Grimoire à la ceinture (KayKit spellbook_closed), posé par Deathless > Jeu > 12. Boss. Sans lui, l'éclat du grimoire est montré seul.")]
        public GameObject modeleGrimoire;

        float m_ProchainTir, m_ProchaineInvocation, m_ProchaineTeleport;
        int m_Invoques;
        Sante m_CibleTir;
        int m_SalveRestante;
        float m_SalveDans = -1f;
        bool m_Enrage;

        readonly EclatNyx[] m_Eclats = new EclatNyx[2];

        public int Invoques { get => m_Invoques; set => m_Invoques = Mathf.Max(0, value); }
        public bool CouronneBrisee => m_Eclats[0] == null || m_Eclats[0].Brise;
        public bool GrimoireBrise => m_Eclats[1] == null || m_Eclats[1].Brise;
        /// Phase du combat (1 à 3) : nombre d'éclats brisés + 1.
        public int PhaseCombat => 1 + (CouronneBrisee ? 1 : 0) + (GrimoireBrise ? 1 : 0);
        public bool Enrage => m_Enrage;
        public EclatNyx Eclat(int index) => index >= 0 && index < m_Eclats.Length ? m_Eclats[index] : null;

        /// PV des deux éclats (x : couronne, y : grimoire), envoyés aux clients par EnnemiReseau.
        public Vector2 PvEclats => new Vector2(
            m_Eclats[0] != null && m_Eclats[0].Sante != null ? m_Eclats[0].Sante.Pv : 0f,
            m_Eclats[1] != null && m_Eclats[1].Sante != null ? m_Eclats[1].Sante.Pv : 0f);

        protected override void Awake()
        {
            base.Awake();
            CreerEclats();
        }

        /// Pose les deux éclats sur les os de la tête et du bassin, à partir de la pose de repos du modèle (KayKit
        /// Necromancer : crâne de la couronne devant, en haut ; grimoire à la ceinture, côté gauche).
        void CreerEclats()
        {
            Transform tete = modele != null ? MannequinEquip.Trouver(modele, "head") : null;
            Transform bassin = modele != null ? MannequinEquip.Trouver(modele, "hips") : null;
            // Points relevés sur le prefab (modèle à l'échelle necroEchelle, repère de Nyxar).
            Vector3 couronne = transform.TransformPoint(new Vector3(0f, 2.0f, 0.5f));
            Vector3 grimoire = transform.TransformPoint(new Vector3(-0.56f, 0.6f, 0.06f));
            m_Eclats[0] = EclatNyx.Creer(this, 0, tete, couronne, 0.2f);
            if (modeleGrimoire != null && bassin != null)
            {
                var livre = Instantiate(modeleGrimoire, bassin);
                livre.name = "Grimoire";
                livre.transform.position = transform.TransformPoint(new Vector3(-0.44f, 0.56f, 0.02f));
                livre.transform.rotation = transform.rotation * Quaternion.Euler(0f, 90f, 8f);
                livre.transform.localScale = Vector3.one * 0.55f / Mathf.Max(0.01f, bassin.lossyScale.y);
                foreach (var c in livre.GetComponentsInChildren<Collider>()) Destroy(c);
            }
            m_Eclats[1] = EclatNyx.Creer(this, 1, bassin, grimoire, 0.17f);
        }

        public override void Initialiser(StatsSquelette stats, float multiplicateurPV)
        {
            var b = GameBalance.Courant;
            // Faux au corps à corps (phases 1 et 2) : c'est le « coup » de base du squelette (parable).
            var s = new StatsSquelette
            {
                pv = b.necroPV, vitesse = b.necroVitesse, degatsJoueur = b.nyxarFauxDegats, degatsNyxessa = b.necroDegats,
                intervalle = b.nyxarFauxIntervalle, preparation = b.nyxarFauxPreparation, portee = b.nyxarFauxPortee
            };
            base.Initialiser(s, multiplicateurPV);
            for (int i = 0; i < m_Eclats.Length; i++) if (m_Eclats[i] != null) m_Eclats[i].Initialiser(b.nyxarEclatPV * multiplicateurPV);
            Sante.invulnerable = true;
            m_ProchainTir = Time.time + 3f;
            m_ProchaineInvocation = Time.time + 6f;
            m_ProchaineTeleport = Time.time + 4f;
            if (P != null) P.Journal("Nyxar : " + Mathf.RoundToInt(Sante.pvMax) + " PV, éclats " + Mathf.RoundToInt(b.nyxarEclatPV * multiplicateurPV) + " PV chacun");
        }

        // ----------------------------------------------------------------- Éclats de Nyx

        /// Hôte : un éclat vient d'être brisé. Un tiers des PV max en moins ; les deux brisés : enragé et tuable.
        public void EclatBrise(EclatNyx e, InfoDegats info)
        {
            if (Distant || !Vivant) return;
            EffetsBoss.Diffuser(this, EffetBoss.EclatBrise, e.Position, 1f);
            Sante.Fixer(Mathf.Max(1f, Sante.Pv - Sante.pvMax / 3f), Sante.pvMax);
            if (P != null) P.Journal("Nyxar : éclat " + (e.Index == 0 ? "de la couronne" : "du grimoire") + " brisé (joueur " + info.sourceId + "), phase " + PhaseCombat);
            if (CouronneBrisee && GrimoireBrise) Enrager();
        }

        void Enrager()
        {
            if (m_Enrage) return;
            var b = B;
            m_Enrage = true;
            Sante.invulnerable = false;
            m_SalveRestante = 0; m_SalveDans = -1f;
            m_Stats.vitesse = b.nyxarEnrageVitesse;
            m_Stats.degatsJoueur = b.nyxarEnrageDegats;
            m_Stats.degatsNyxessa = b.nyxarEnrageDegatsNyxessa;
            m_Stats.preparation = b.nyxarEnragePreparation;
            m_Stats.intervalle = b.nyxarEnrageIntervalle;
            m_Stats.portee = b.nyxarEnragePortee;
            EffetsBoss.Diffuser(this, EffetBoss.Enrage, transform.position, 1f);
            if (P != null) P.Journal("Nyxar enragé : corps à corps, tuable (" + Mathf.RoundToInt(Sante.Pv) + " PV)");
        }

        /// Client : relie les coups de ses héros sur les éclats à l'hôte (EnnemiReseau.OnNetworkSpawn).
        public void BrancherEclatsDistants(Deathless.Reseau.EnnemiReseau reseau)
        {
            for (int i = 0; i < m_Eclats.Length; i++)
            {
                var e = m_Eclats[i];
                if (e == null || e.Sante == null) continue;
                int index = i;
                e.Sante.relais = info => reseau.RelayerEclat(index, info, e.Sante.Pv);
            }
        }

        /// Client : PV des éclats reçus de l'hôte ; invulnérable tant qu'un éclat tient (pas de faux chiffres de dégâts).
        public void RecevoirEclats(Vector2 pv)
        {
            if (pv.x < 0f) return;
            float max = B.nyxarEclatPV * Mathf.Max(1f, GameBalance.ParNuit(B.multiplicateurPV, P != null ? P.Etat.nuit : 1, 1f));
            if (m_Eclats[0] != null) m_Eclats[0].Fixer(pv.x, Mathf.Max(max, pv.x));
            if (m_Eclats[1] != null) m_Eclats[1].Fixer(pv.y, Mathf.Max(max, pv.y));
            Sante.invulnerable = pv.x > 0f || pv.y > 0f;
            m_Enrage = !Sante.invulnerable;
        }

        // ----------------------------------------------------------------- Comportement

        protected override void Update()
        {
            base.Update();
            // Squelette.Update pose Sante.invulnerable pour l'esquive des squelettes : tant qu'un éclat tient, Nyxar
            // reste invulnérable quoi qu'il en soit.
            if (!m_Enrage && Vivant) Sante.invulnerable = true;
        }

        protected override void MajMarche(float dt) { if (m_Enrage) base.MajMarche(dt); else MajDistance(dt); }
        protected override void MajPoursuite(float dt) { if (m_Enrage) base.MajPoursuite(dt); else MajDistance(dt); }

        /// Phases 1 et 2 : à distance ; faux sur qui le serre, téléportation (couronne), salves (grimoire), sbires (couronne).
        void MajDistance(float dt)
        {
            var b = B;
            MajSalve(dt);
            var j = JoueurProche(b.necroPorteeTir + 4f);
            Sante cible = j != null ? j.Sante : (P != null ? P.nyxessa : null);
            if (cible == null) return;
            Vector3 c = cible.transform.position;
            float d = Distance(c);
            Tourner(c);

            // Serré de près : téléportation si la couronne tient et qu'elle est prête, sinon faux.
            var proche = JoueurProche(b.nyxarTeleportDeclencheur);
            if (proche != null)
            {
                if (!CouronneBrisee && Time.time >= m_ProchaineTeleport && Teleporter()) return;
                if (Distance(proche.transform.position) <= b.nyxarFauxPortee + 0.4f && Time.time - m_DernierCoup >= m_Stats.intervalle)
                {
                    m_SalveRestante = 0; m_SalveDans = -1f;
                    EffetsBoss.Diffuser(this, EffetBoss.Faux, transform.position + Vector3.up);
                    CommencerAttaque(proche);
                    return;
                }
            }

            // Garder ses distances : s'approcher au-delà de la distance haute, reculer en deçà de la distance basse.
            if (d > b.necroDistance.y) { Agent.isStopped = false; Agent.speed = b.necroVitesse; Poursuivre(c); }
            else if (d < b.necroDistance.x)
            {
                Vector3 fuite = transform.position + (transform.position - c).normalized * 5f;
                if (NavMesh.SamplePosition(fuite, out var hit, 3f, NavMesh.AllAreas)) { Agent.isStopped = false; Agent.speed = b.necroVitesse * 0.8f; Agent.SetDestination(hit.position); OublierDestination(); }
            }
            else Agent.isStopped = true;

            if (Time.time >= m_ProchainTir && d <= b.necroPorteeTir && m_SalveRestante <= 0)
            {
                m_ProchainTir = Time.time + b.necroIntervalleTir;
                m_CibleTir = cible;
                if (cible == (P != null ? P.nyxessa : null)) m_DernierCoupNyxessa = Time.time;
                m_SalveRestante = GrimoireBrise ? 1 : Mathf.Max(1, b.nyxarSalveCranes);
                m_SalveDans = 0.35f;
                if (animator != null) animator.SetTrigger(P_Shoot);
            }
            if (!CouronneBrisee && Time.time >= m_ProchaineInvocation)
            {
                m_ProchaineInvocation = Time.time + b.necroIntervalleInvocation;
                if (m_Invoques < b.necroInvoquesMax && DirecteurVagues.Instance != null)
                {
                    if (animator != null) animator.SetTrigger(P_Cast);
                    int n = Mathf.Min(b.necroSbiresParInvocation, b.necroInvoquesMax - m_Invoques);
                    m_Invoques += DirecteurVagues.Instance.Invoquer(this, n);
                    AudioBank.Jouer(SonsDuJeu.Invocation, transform.position, 1f);
                }
            }
        }

        /// Salve de crânes en cours : un crâne toutes les nyxarSalveEcart s, départs décalés sur les côtés.
        void MajSalve(float dt)
        {
            if (m_SalveRestante <= 0 || m_SalveDans < 0f) return;
            m_SalveDans -= dt;
            if (m_SalveDans >= 0f) return;
            var b = B;
            if (m_CibleTir == null || m_CibleTir.Mort) { m_SalveRestante = 0; m_SalveDans = -1f; return; }
            int total = GrimoireBrise ? 1 : Mathf.Max(1, b.nyxarSalveCranes);
            int rang = total - m_SalveRestante;
            float cote = total > 1 ? (rang - (total - 1) * 0.5f) * 0.55f : 0f;
            Vector3 depart = transform.position + Vector3.up * 1.6f + transform.forward * 0.6f + transform.right * cote;
            MissileCrane.Tirer(depart, m_CibleTir, b.necroDegats, b.necroVitesseMissile, 90f, Equipe.Ennemis, gameObject, false);
            m_SalveRestante--;
            m_SalveDans = m_SalveRestante > 0 ? b.nyxarSalveEcart : -1f;
        }

        /// Téléportation : réapparaît loin des joueurs (entre nyxarTeleportDistance.x et .y m de sa place), sur le
        /// NavMesh, sans s'éloigner de Nyxessa au-delà de nyxarTeleportRayonNyxessa. Faux si aucune place ne convient.
        bool Teleporter()
        {
            var b = B;
            Vector3 depart = transform.position;
            Vector3 centre = Nyxessa != null ? Nyxessa.position : depart;
            Vector3 meilleur = depart; float score = float.MinValue;
            for (int i = 0; i < 10; i++)
            {
                float a = i * 36f + Random.Range(-12f, 12f);
                Vector3 p = depart + Quaternion.Euler(0f, a, 0f) * Vector3.forward * Random.Range(b.nyxarTeleportDistance.x, b.nyxarTeleportDistance.y);
                Vector3 vc = p - centre; vc.y = 0f;
                if (vc.magnitude > b.nyxarTeleportRayonNyxessa) p = centre + vc.normalized * b.nyxarTeleportRayonNyxessa;
                if (!NavMesh.SamplePosition(p, out var hit, 3f, NavMesh.AllAreas)) continue;
                float s = DistanceJoueurs(hit.position);
                if (Distance(hit.position) < b.nyxarTeleportDistance.x * 0.6f) s -= 10f;
                if (s > score) { score = s; meilleur = hit.position; }
            }
            if (score == float.MinValue) return false;
            m_ProchaineTeleport = Time.time + b.nyxarTeleportRecharge;
            EffetsBoss.Diffuser(this, EffetBoss.TeleportDepart, depart, 1f);
            Agent.Warp(meilleur);
            OublierDestination();
            var vers = (Nyxessa != null ? Nyxessa.position : depart) - meilleur; vers.y = 0f;
            if (vers.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(vers);
            if (m_Reseau != null) m_Reseau.Teleporter();
            EffetsBoss.Diffuser(this, EffetBoss.TeleportArrivee, meilleur, 1f);
            if (P != null) P.Journal("Nyxar se téléporte (" + Vector3.Distance(depart, meilleur).ToString("F1") + " m)");
            return true;
        }

        /// Distance (horizontale) au joueur vivant le plus proche de `p`.
        float DistanceJoueurs(Vector3 p)
        {
            if (P == null) return 0f;
            float d = 999f;
            var tous = P.TousLesHeros;
            for (int i = 0; i < tous.Count; i++)
            {
                var h = tous[i];
                if (h == null || !h.Vivant) continue;
                Vector3 v = h.transform.position - p; v.y = 0f;
                d = Mathf.Min(d, v.magnitude);
            }
            return d;
        }

        /// Coup de faux : son sur tous les postes (l'animation part par le NetworkAnimator).
        protected override void Frapper()
        {
            if (m_Enrage) EffetsBoss.Diffuser(this, EffetBoss.Faux, transform.position + Vector3.up);
            base.Frapper();
        }

        public override void Repousser(Vector3 deplacement, float etourdi, int sourceId) { if (RelaiRepousser(deplacement, etourdi)) return; base.Repousser(deplacement * 0.6f, etourdi, sourceId); }
    }
}
