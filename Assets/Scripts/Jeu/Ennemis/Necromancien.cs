using UnityEngine;
using UnityEngine.AI;

namespace Deathless.Jeu
{
    /// Nyxar, le Nécromancien : boss final de la nuit 12 (wiki : ennemis.md, section Nyxar ; kit complet codé le
    /// 30/09/2026, valeurs GameBalance necro* et nyxar*, à équilibrer). Ses yeux brillent en vert Nyxessa.
    ///
    /// Deux éclats de Nyx (EclatNyx), dans le crâne de sa couronne et dans celui de son grimoire à la ceinture, sont
    /// ses points faibles (décidé le 01/10/2026) : son corps prend des dégâts normalement dès le début ; un coup sur un
    /// éclat est un critique garanti (×GameBalance.nyxarEclatMultiplicateur, effet et chiffre de critique) et abîme
    /// l'éclat (CoupSurEclat) ; un éclat se brise aussi de lui-même par tranche de vie (2/3, puis 1/3 des PV max :
    /// VerifierTranches) ; chaque éclat brisé lui retire une partie de son kit. Trois phases :
    /// - phase 1 (deux éclats) : garde ses distances, se téléporte quand on l'approche, tire des salves de crânes,
    ///   relève des squelettes autour de lui, fauche à la faux qui le serre de près ;
    /// - phase 2, couronne brisée : plus de téléportation ni de squelettes relevés ; grimoire brisé : plus de salves
    ///   (un seul crâne par tir) ;
    /// - phase 3 (deux éclats brisés) : enragé, il se bat au corps à corps à la faux (comportement de base d'un
    ///   squelette, plus rapide et plus fort).
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
            Sante.renvoi = CoupSurCorps;
            Sante.Touche += VerifierTranches;
        }

        /// Hôte (Touche n'est levé que là où le coup s'applique) : un éclat se brise de lui-même par tranche de vie
        /// (décidé le 01/10/2026) — le premier à 2/3 des PV max, le second à 1/3 — s'il n'a pas déjà été brisé par les
        /// coups. Le premier à céder : celui qui a perdu le plus de PV, sinon la couronne.
        void VerifierTranches(InfoDegats info, float reel)
        {
            if (Distant || !Vivant || Sante.pvMax <= 0f) return;
            float ratio = Sante.Pv / Sante.pvMax;
            int voulu = (ratio <= 2f / 3f ? 1 : 0) + (ratio <= 1f / 3f ? 1 : 0);
            for (int garde = 0; garde < 2 && Brises < voulu; garde++)
            {
                EclatNyx a = m_Eclats[0], g = m_Eclats[1];
                bool aOk = a != null && !a.Brise, gOk = g != null && !g.Brise;
                EclatNyx e = aOk && gOk ? (g.Sante.Ratio < a.Sante.Ratio ? g : a) : aOk ? a : gOk ? g : null;
                if (e == null) return;
                if (P != null) P.Journal("Nyxar : tranche de vie franchie (" + Mathf.RoundToInt(ratio * 100f) + " %), l'éclat cède");
                e.Abimer(e.Sante.Pv + 1f, info);
            }
        }

        int Brises => (CouronneBrisee ? 1 : 0) + (GrimoireBrise ? 1 : 0);

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
            Sante.invulnerable = false;
            m_ProchainTir = Time.time + 3f;
            m_ProchaineInvocation = Time.time + 6f;
            m_ProchaineTeleport = Time.time + 4f;
            if (P != null) P.Journal("Nyxar : " + Mathf.RoundToInt(Sante.pvMax) + " PV, éclats " + Mathf.RoundToInt(b.nyxarEclatPV * multiplicateurPV) + " PV chacun");
        }

        // ----------------------------------------------------------------- Éclats de Nyx

        /// Hôte : un éclat vient d'être brisé (part du kit perdue : CouronneBrisee, GrimoireBrise) ; les deux : enragé.
        /// Plus de perte de PV max à la rupture depuis le 01/10/2026 (le corps prend des dégâts dès le début).
        public void EclatBrise(EclatNyx e, InfoDegats info)
        {
            if (Distant || !Vivant) return;
            EffetsBoss.Diffuser(this, EffetBoss.EclatBrise, e.Position, 1f);
            if (P != null) P.Journal("Nyxar : éclat " + (e.Index == 0 ? "de la couronne" : "du grimoire") + " brisé (joueur " + info.sourceId + "), phase " + PhaseCombat);
            if (CouronneBrisee && GrimoireBrise) Enrager();
        }

        void Enrager()
        {
            if (m_Enrage) return;
            var b = B;
            m_Enrage = true;
            m_SalveRestante = 0; m_SalveDans = -1f;
            m_Stats.vitesse = b.nyxarEnrageVitesse;
            m_Stats.degatsJoueur = b.nyxarEnrageDegats;
            m_Stats.degatsNyxessa = b.nyxarEnrageDegatsNyxessa;
            m_Stats.preparation = b.nyxarEnragePreparation;
            m_Stats.intervalle = b.nyxarEnrageIntervalle;
            m_Stats.portee = b.nyxarEnragePortee;
            EffetsBoss.Diffuser(this, EffetBoss.Enrage, transform.position, 1f);
            if (P != null) P.Journal("Nyxar enragé : corps à corps (" +Mathf.RoundToInt(Sante.Pv) + " PV)");
        }

        // Un même geste (même héros, même image) ne frappe Nyxar qu'une fois : un coup de zone qui prend à la fois le
        // corps et un ou deux éclats (Combat.Ennemis les rend tous) ne compte que le premier touché (l'éclat, plus proche
        // pour la mêlée) ; les autres éclats s'usent seulement. Valeur : image du dernier coup, négative s'il venait d'un éclat.
        readonly System.Collections.Generic.Dictionary<int, int> m_DernierGeste = new System.Collections.Generic.Dictionary<int, int>();
        bool m_DepuisEclat;

        /// Tous les postes (Sante.renvoi du corps) : coup sur le corps, ignoré si le même geste vient de toucher un éclat.
        float CoupSurCorps(InfoDegats info)
        {
            if (!m_DepuisEclat && info.sourceId > 0)
            {
                if (m_DernierGeste.TryGetValue(info.sourceId, out int g) && g == -Time.frameCount) return 0f;
                m_DernierGeste[info.sourceId] = Time.frameCount;
            }
            return Sante.Encaisser(info);   // chemin ordinaire (relais chez un client, garde, événements)
        }

        /// Tous les postes (Sante.renvoi d'un éclat, 01/10/2026) : critique garanti sur Nyxar (×nyxarEclatMultiplicateur,
        /// sauf coup déjà critique ou continu) et usure de l'éclat. Chez l'hôte : usure et effet de critique diffusé ici ;
        /// chez un client : le corps part par son relais habituel (chiffre de critique estimé tout de suite), l'usure et
        /// l'effet par EnnemiReseau.RelayerEclat. Renvoie les dégâts faits à Nyxar (score, jauges de classe).
        public float CoupSurEclat(EclatNyx e, InfoDegats info)
        {
            if (e == null || e.Brise || !Vivant || info.montant <= 0f) return 0f;
            bool deja = false;
            if (info.sourceId > 0)
            {
                deja = m_DernierGeste.TryGetValue(info.sourceId, out int g) && (g == Time.frameCount || g == -Time.frameCount);
                if (!deja) m_DernierGeste[info.sourceId] = -Time.frameCount;
            }
            var crit = info;
            if (!info.critique && !info.continu) { crit.montant *= Mathf.Max(1f, B.nyxarEclatMultiplicateur); crit.critique = true; }
            float reel = 0f;
            if (!deja)
            {
                m_DepuisEclat = true;
                try { reel = Sante.Encaisser(crit); }
                finally { m_DepuisEclat = false; }
            }
            if (Distant) { if (m_Reseau != null) m_Reseau.RelayerEclat(e.Index, info, e.Sante.Pv); }
            else AbimerEclat(e.Index, info);
            return reel;
        }

        /// Hôte : usure d'un éclat (coup d'un héros d'ici, ou relayé par un client) et effet de critique vu de tous.
        public void AbimerEclat(int index, InfoDegats info)
        {
            if (Distant || !Vivant) return;
            var e = Eclat(index);
            if (e == null || e.Brise) return;
            if (!info.critique && !info.continu) EffetsBoss.Diffuser(this, EffetBoss.Critique, info.point);
            e.Abimer(info.montant, info);
        }

        /// Client : PV des éclats reçus de l'hôte ; les deux brisés : enragé.
        public void RecevoirEclats(Vector2 pv)
        {
            if (pv.x < 0f) return;
            float max = B.nyxarEclatPV * Mathf.Max(1f, GameBalance.ParNuit(B.multiplicateurPV, P != null ? P.Etat.nuit : 1, 1f));
            if (m_Eclats[0] != null) m_Eclats[0].Fixer(pv.x, Mathf.Max(max, pv.x));
            if (m_Eclats[1] != null) m_Eclats[1].Fixer(pv.y, Mathf.Max(max, pv.y));
            m_Enrage = pv.x <= 0f && pv.y <= 0f;
        }

        // ----------------------------------------------------------------- Comportement

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
            // Vers Nyxessa : sa place autour d'elle, pas son centre (îlot du NavMesh hors d'atteinte : chemin « en calcul » des
            // secondes durant, même cause que le mage squelette figé, 01/10/2026) ; l'aube attend sa chute, il ne doit pas se figer.
            if (d > b.necroDistance.y) { Agent.isStopped = false; Agent.speed = b.necroVitesse; Poursuivre(P != null && cible == P.nyxessa ? m_Place : c); }
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

        /// Salve de crânes en cours : un crâne toutes les nyxarSalveEcart s, départs décalés sur les côtés. Crânes
        /// esquivables (01/10/2026) : guidage faible (nyxarCraneGuidage), coupé près de la cible ou au-delà d'un angle.
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
            MissileCrane.Tirer(depart, m_CibleTir, b.necroDegats, b.necroVitesseMissile, b.nyxarCraneGuidage, Equipe.Ennemis, gameObject, false,
                b.nyxarCraneCoupureDistance, b.nyxarCraneCoupureAngle);
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

        public override void Pousser(Vector3 deplacement) => base.Pousser(deplacement * 0.6f);

        public override void Repousser(Vector3 deplacement, float etourdi, int sourceId) { if (RelaiRepousser(deplacement, etourdi)) return; base.Repousser(deplacement * 0.6f, etourdi, sourceId); }
    }
}
