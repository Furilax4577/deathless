using System.Collections;
using UnityEngine;

namespace Deathless.Jeu
{
    /// Rôdeur (arc et carquois) : visée (LT / clic droit maintenu) avec zoom de caméra (épaule serrée) ; bander et tirer
    /// (RT / clic gauche maintenu puis relâché : charge 1,2 s, 10 à 50 dégâts, tir à la tête ×2 et critique ; à pleine
    /// charge la flèche étourdit 1 s, et la zone de la nuée ralentit qui y reste : lissage du 27/09/2026) ; lâcher la
    /// sont indépendants de la visée : on peut bander sans viser, viser sans tirer, ou les deux (Quentin, 26/09/2026 :
    /// « le zoom et le tir sont deux actions distinctes, utilisables ou non ensemble ») ;
    /// nuée de flèches (LB), roulade arrière et salve (RB). Flèches non magiques (modèle
    /// KayKit, traînée d'air) ; cercle de charge et éclat à 100 % par l'effet ArcBande. Arc repos / visée, flèche encochée
    /// et blendshape `Draw` pilotés ici (mêmes valeurs que BowStance).
    public class ClasseRodeur : ClasseHeros
    {
        enum Action { Aucune, Bander, Lacher, Nuee, Viser }
        /// Sort à visée au sol (VisiereZone.Sort) : la nuée part à la confirmation (clic gauche, RT), pas à l'appui sur LB.
        const int V_Nuee = 1;

        public override string Id => "rodeur";
        public override float PvMax => B.rodeurPV;
        public override float Vitesse => B.rodeurVitesse;
        /// Attributs : flèches (tir, salve, nuée) = coups à distance (Perception).
        public override bool CoupADistance => true;

        Action m_Action;
        float m_Depuis;
        float m_Charge;
        float m_DernierTir = -99f;
        float m_RechargeNuee, m_RechargeRoulade;
        bool m_Visee;
        Transform m_Arc;
        GameObject m_Encochee;
        SkinnedMeshRenderer m_ArcRendu;
        float m_Pose;   // 0 repos, 1 visée
        ArcBande m_Cercle;
        NueeDeFleches m_Nuee;
        Vector3 m_CentreNuee;
        bool m_NueeLancee;
        bool m_AttenteRelache;   // RT a confirmé la visée : pas de nouvel arc bandé tant qu'il n'est pas relâché

        // Effets diffusés aux autres postes (ClasseHeros.Diffuser).
        const int E_Bander = 1, E_Tir = 2, E_Nuee = 3, E_Roulade = 4, E_Salve = 5;

        static readonly Vector3 ReposEuler = new Vector3(286f, 183f, 357f), ViseeEuler = new Vector3(0f, 0f, 180f);
        static readonly int P_Aiming = Animator.StringToHash("Aiming");
        static readonly int P_Shoot = Animator.StringToHash("Shoot");
        static readonly int P_TirHaut = Animator.StringToHash("TirHaut");

        public override void Initialiser(Heros heros)
        {
            base.Initialiser(heros);
            m_Arc = MannequinEquip.Trouver(transform, "bow_withString");
            var fl = MannequinEquip.Trouver(transform, "arrow_bow");
            m_Encochee = fl != null ? fl.gameObject : null;
            if (m_Arc != null) m_ArcRendu = m_Arc.GetComponentInChildren<SkinnedMeshRenderer>();
            var fx = EffetsJeu.Instance;
            if (fx != null && fx.prefabArcBande != null)
            {
                m_Cercle = Instantiate(fx.prefabArcBande).GetComponent<ArcBande>();
                // Son « coup prêt » joué par AudioBank (groupe Effets du mixer), pas par l'effet.
                var champ = typeof(ArcBande).GetField("sonPret", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (champ != null) champ.SetValue(m_Cercle, null);
                m_Cercle.Pret += () => AudioBank.Jouer(SonsDuJeu.ArcPret, transform.position + Vector3.up * 1.5f, 0.9f);
            }
            if (fx != null && fx.prefabNuee != null) m_Nuee = Instantiate(fx.prefabNuee).GetComponent<NueeDeFleches>();
            AppliquerPose();
        }

        void OnDestroy()
        {
            if (m_Cercle != null) Destroy(m_Cercle.gameObject);
            if (m_Nuee != null) Destroy(m_Nuee.gameObject);
        }

        public override bool Occupe => m_Action != Action.Aucune;
        public override bool PeutEsquiver => m_Action != Action.Nuee;
        public override float FacteurVitesse => m_Action == Action.Nuee ? 0f : m_Action == Action.Viser ? B.viseeZoneVitesse : m_Action != Action.Aucune ? B.arcVitesseBander : m_Visee ? B.viseeVitesse : 1f;
        public override bool BloqueSprint => m_Visee || m_Action != Action.Aucune;
        public override bool FaceVisee => m_Visee || m_Action != Action.Aucune;
        public override bool HautDuCorps => m_Action == Action.Bander || m_Action == Action.Lacher;

        Transform m_MainArc, m_MainCorde;

        /// Arc bandé : la ligne de tir va de la main de la corde (handslot.r) à la main de l'arc (handslot.l).
        public override bool AxeDeTir(out Vector3 origine, out Vector3 direction)
        {
            origine = direction = Vector3.zero;
            if (m_Action != Action.Bander) return false;
            if (m_MainArc == null) m_MainArc = MannequinEquip.Trouver(transform, "handslot.l");
            if (m_MainCorde == null) m_MainCorde = MannequinEquip.Trouver(transform, "handslot.r");
            if (m_MainArc == null || m_MainCorde == null) return false;
            origine = m_MainCorde.position;
            direction = m_MainArc.position - m_MainCorde.position;
            return direction.sqrMagnitude > 0.0001f;
        }

        public override void SurAction(string action)
        {
            if (ViseeSurAction(action)) return;   // visée de la nuée ouverte : clic gauche / RT confirme, clic droit / LT annule
            switch (action)
            {
                case "AttackPrimary": Bander(); break;   // RT bande, visée ou non
                case "Skill1": Nuee(); break;
                case "Skill2": Roulade(); break;
            }
        }

        void Bander()
        {
            if (m_Action != Action.Aucune || Time.time - m_DernierTir < B.arcIntervalle / VitesseAttaque) return;
            m_Action = Action.Bander;
            m_Depuis = 0f;
            m_Charge = 0f;
            if (Anim != null) Anim.SetBool(P_Aiming, true);
            if (m_Cercle != null && m_Encochee != null) m_Cercle.Bander(m_Encochee);
            AudioBank.Jouer(SonsDuJeu.ArcBander, transform.position + Vector3.up * 1.4f, 0.7f);
            Diffuser(E_Bander);
        }

        void Lacher()
        {
            var b = B;
            float charge = m_Charge;
            m_Action = Action.Lacher;
            m_Depuis = 0f;
            m_DernierTir = Time.time;
            if (Anim != null) { Anim.SetBool(P_Aiming, false); H.Declencher(P_Shoot); }
            if (m_Cercle != null) m_Cercle.Annuler();
            Vector3 cible = Combat.PointVise(H.CameraJeu, transform, b.arcPortee, out _);
            Vector3 depart = m_Encochee != null ? m_Encochee.transform.position : transform.position + Vector3.up * 1.4f + transform.forward * 0.5f;
            float degats = Mathf.Lerp(b.arcDegatsMin, b.arcDegatsMax, charge) * Facteur(0);
            // Vitesse selon la charge : tir rapide lent (retombe vite), charge complète rapide (file loin et tendu).
            // Pleine charge (27/09/2026) : la flèche étourdit l'ennemi touché (statut posé par le chemin de l'hôte).
            TirerFleche(depart, cible, Mathf.Lerp(b.arcVitesseMin, b.arcVitesseMax, charge), degats, b.arcTete, charge >= 0.999f ? b.arcEtourdiPleineCharge : 0f);
            AudioBank.Jouer(charge >= 0.999f ? SonsDuJeu.ArcTirCharge : SonsDuJeu.ArcTir, depart, 0.9f);
            Diffuser(E_Tir, depart, default, charge);
        }

        void TirerFleche(Vector3 depart, Vector3 cible, float vitesse, float degats, float multTete, float etourdi = 0f)
        {
            ProjectileJeu.Tirer(ProjectileJeu.Genre.Fleche, depart, cible, vitesse, B.arcPortee, transform, (point, dir, s) =>
            {
                AudioBank.Jouer(SonsDuJeu.FlecheImpact, point, 0.7f, 0.05f);
                if (s == null) return;
                var sq = s.GetComponent<Squelette>();
                bool tete = Combat.ALaTete(sq, point, dir);
                if (tete) Critique(point, -dir, false);
                float pvAvant = s.Pv;
                float reel = H.Frapper(s, degats * (tete ? multTete : 1f), tete, point, dir);
                if (etourdi > 0f && sq != null && sq.Vivant) sq.Etourdir(etourdi, H.Id);   // relayé à l'hôte depuis un client
                Deathless.Succes.ServiceSucces.FlecheRodeur(H, sq, tete, Deathless.Succes.ServiceSucces.Acheve(s, pvAvant, reel), etourdi);
            });
        }

        /// LB : ouvre la visée de la zone (02/10/2026, même mécanique que les sorts de zone du mage : VisiereZone). Le cercle
        /// de la zone (rayon de la pluie, thème Chasse) suit le réticule jusqu'à nueePortee ; clic gauche / RT confirme et lance
        /// la nuée sur ce point, clic droit / LT annule sans recharge. Avant, la nuée partait aussitôt sur le point visé.
        void Nuee()
        {
            if (EnVisee || !H.PeutAgir || m_RechargeNuee > 0f) return;
            var b = B;
            m_Action = Action.Viser;
            m_Depuis = 0f;
            CommencerVisee(V_Nuee, ZoneVisee.Forme.Cercle, VfxTheme.Chasse, b.nueeRayon, 0f, b.nueePortee);
        }

        public override string LibelleVisee => "Nuée de flèches";

        protected override void ViseeAnnulee(int sort)
        {
            if (m_Action == Action.Viser) m_Action = Action.Aucune;
        }

        /// Confirmation : la recharge part ici (pas avant) et la pluie tombera sur le point visé.
        protected override bool ViseeConfirmee(int sort, Vector3 point, Vector3 axe)
        {
            if (m_Action != Action.Viser || m_RechargeNuee > 0f) return false;
            var b = B;
            Vector3 d = point - transform.position; d.y = 0f;
            m_CentreNuee = point;
            m_RechargeNuee = b.nueeRecharge * Facteur(2) * RechargeEsprit;
            m_Action = Action.Nuee;
            m_Depuis = 0f;
            m_NueeLancee = false;
            m_AttenteRelache = H.Entrees.AttaqueMaintenue;   // le clic de confirmation ne rebande pas l'arc en traînant
            H.Tourner(d);
            if (Anim != null) H.Declencher(P_TirHaut);
            return true;
        }

        IEnumerator Pluie(Vector3 centre, bool degats = true)
        {
            var b = B;
            AudioBank.Jouer(SonsDuJeu.NueeMarqueur, centre, 0.8f);
            yield return new WaitForSeconds(0.35f);
            AudioBank.Jouer(SonsDuJeu.Nuee, centre, 1f);
            if (!degats) yield break;   // marionnette : visuel et sons seulement
            for (int i = 0; i < b.nueeSalves; i++)
            {
                // Une salve sur deux (toutes les 0,48 s), la zone ralentit qui y reste (27/09/2026, même règle que la
                // fissure du Fend-sol : Ralenti court relancé tant qu'on y est ; demande relayée à l'hôte depuis un client).
                bool ralentit = b.nueeRalentiForce > 0f && i % 2 == 0;
                foreach (var s in Cibles(centre, Vector3.forward, b.nueeRayon, 180f))
                {
                    H.Frapper(s, b.nueeDegatsSalve, false, s.transform.position + Vector3.up, Vector3.down);
                    if (ralentit && !s.Mort) Statuts.De(s)?.Ajouter(TypeStatut.Ralenti, b.nueeRalentiDuree, b.nueeRalentiForce, OrigineStatut.Joueur, H.Id);
                }
                yield return new WaitForSeconds(1.2f / b.nueeSalves);
            }
        }

        /// Roulade arrière (RB) : le rôdeur roule en arrière pour reprendre ses distances et tire en même temps une salve
        /// de flèches en éventail devant lui (flèches normales, traînée d'air, pas d'effet dédié).
        void Roulade()
        {
            if (!H.PeutAgir || m_RechargeRoulade > 0f || !H.Depenser(B.rouladeCout)) return;
            var b = B;
            m_RechargeRoulade = b.rouladeRecharge * RechargeEsprit;
            Vector3 avant = H.AvantCamera;
            H.Tourner(avant);
            H.EsquiveImposee(-avant, true);
            AudioBank.Jouer(SonsDuJeu.Esquive, transform.position + Vector3.up, 0.8f);
            Diffuser(E_Roulade);
            StartCoroutine(Salve(avant));
        }

        IEnumerator Salve(Vector3 avant)
        {
            yield return new WaitForSeconds(0.08f);
            var b = B;
            Vector3 cible = Combat.PointVise(H.CameraJeu, transform, b.arcPortee, out _);
            Vector3 depart = transform.position + Vector3.up * 1.3f + avant * 0.4f;
            Vector3 axe = cible - depart;
            int fleches = b.salveFleches + Mathf.RoundToInt(Facteur(3));
            for (int i = 0; i < fleches; i++)
            {
                float a = fleches > 1 ? Mathf.Lerp(-b.salveEcart, b.salveEcart, i / (float)(fleches - 1)) : 0f;
                Vector3 dir = Quaternion.AngleAxis(a, Vector3.up) * axe;
                TirerFleche(depart, depart + dir, b.salveVitesse, b.salveDegats, b.arcTete);
            }
            AudioBank.Jouer(SonsDuJeu.ArcTir, depart, 1f);
            Diffuser(E_Salve, depart);
        }

        public override void Temps(float dt)
        {
            m_RechargeNuee = Mathf.Max(0f, m_RechargeNuee - dt);
            m_RechargeRoulade = Mathf.Max(0f, m_RechargeRoulade - dt);
            AppliquerPose();
            m_Visee = H.Vivant && H.EnJeu && H.Entrees.GardeMaintenue && GardeLibre && !EnVisee;   // pas de zoom pendant la visée d'une zone : LT y annule
            if (H.CameraEpaule != null) H.CameraEpaule.viseeVoulue = m_Visee ? 1f : 0f;   // zoom pendant la visée (Quentin, 26/09/2026)
        }

        public override void Maj(float dt, Vector3 dir)
        {
            var b = B;
            m_Depuis += dt;
            ViseeMaj();   // visée au sol : annulations (menu, étourdi…), indicateur
            switch (m_Action)
            {
                case Action.Viser:
                    break;   // la visée est tenue par ViseeMaj ; la nuée part à la confirmation
                case Action.Bander:
                    m_Charge = Mathf.Clamp01(m_Depuis / (b.arcCharge * Facteur(1)));
                    if (m_Cercle != null) m_Cercle.Charge = m_Charge;
                    if (!H.Entrees.AttaqueMaintenue) Lacher();              // relâcher RT tire, visée ou non
                    break;
                case Action.Aucune:
                    // RT maintenu après un tir : il rebande dès que l'intervalle est passé (pas après la confirmation de la nuée,
                    // tant que RT n'est pas relâché).
                    if (m_AttenteRelache) { if (!H.Entrees.AttaqueMaintenue) m_AttenteRelache = false; }
                    else if (H.Entrees.AttaqueMaintenue && Time.time - m_DernierTir >= b.arcIntervalle / VitesseAttaque + 0.15f) Bander();
                    break;
                case Action.Lacher:
                    if (m_Depuis >= 0.3f) m_Action = Action.Aucune;
                    break;
                case Action.Nuee:
                    if (!m_NueeLancee && m_Depuis >= 0.55f)
                    {
                        m_NueeLancee = true;
                        if (m_Nuee != null) m_Nuee.Jouer(m_CentreNuee, b.nueeRayon);
                        StartCoroutine(Pluie(m_CentreNuee));
                        AudioBank.Jouer(SonsDuJeu.ArcTir, transform.position + Vector3.up * 1.6f, 0.8f);
                        Diffuser(E_Nuee, m_CentreNuee, default, b.nueeRayon);
                    }
                    if (m_Depuis >= 1.0f) m_Action = Action.Aucune;
                    break;
            }
        }

        // ----------------------------------------------------------------- Multijoueur (marionnette)

        public override void EffetDistant(int effet, Vector3 a, Vector3 b, float v)
        {
            switch (effet)
            {
                case E_Bander: AudioBank.Jouer(SonsDuJeu.ArcBander, transform.position + Vector3.up * 1.4f, 0.7f); break;
                case E_Tir: AudioBank.Jouer(v >= 0.999f ? SonsDuJeu.ArcTirCharge : SonsDuJeu.ArcTir, a, 0.9f); break;
                case E_Nuee:
                    if (m_Nuee != null) m_Nuee.Jouer(a, v);
                    StartCoroutine(Pluie(a, false));
                    AudioBank.Jouer(SonsDuJeu.ArcTir, transform.position + Vector3.up * 1.6f, 0.8f);
                    break;
                case E_Roulade: AudioBank.Jouer(SonsDuJeu.Esquive, transform.position + Vector3.up, 0.8f); break;
                case E_Salve: AudioBank.Jouer(SonsDuJeu.ArcTir, a, 1f); break;
                default: base.EffetDistant(effet, a, b, v); break;
            }
        }

        /// Arc repos / visée (rotation dans handslot.l), flèche encochée visible en bandant, tension du blendshape `Draw`.
        void AppliquerPose()
        {
            bool vise = m_Action == Action.Bander || m_Action == Action.Nuee || m_Action == Action.Viser;
            m_Pose = Mathf.MoveTowards(m_Pose, vise ? 1f : 0f, Time.deltaTime / 0.15f);
            float k = Mathf.SmoothStep(0f, 1f, m_Pose);
            if (m_Arc != null) m_Arc.localRotation = Quaternion.Slerp(Quaternion.Euler(ReposEuler), Quaternion.Euler(ViseeEuler), k);
            if (m_Encochee != null && m_Encochee.activeSelf != (m_Action == Action.Bander)) m_Encochee.SetActive(m_Action == Action.Bander);
            if (m_ArcRendu != null && m_ArcRendu.sharedMesh != null && m_ArcRendu.sharedMesh.blendShapeCount > 0)
                m_ArcRendu.SetBlendShapeWeight(0, m_Action == Action.Bander ? m_Charge * 100f : 0f);
        }

        /// Visée lâchée pendant qu'il bande : la flèche est reposée, sans tir.
        void Reposer()
        {
            if (m_Cercle != null) m_Cercle.Annuler();
            if (Anim != null) Anim.SetBool(P_Aiming, false);
            m_Action = Action.Aucune;
            m_DernierTir = Time.time;
        }

        public override void Interrompre()
        {
            AnnulerVisee(false);   // visée coupée (esquive, étourdissement, mort, menu) : rien n'a été dépensé
            if (m_Action == Action.Bander) Reposer();
            m_Action = Action.Aucune;
        }

        public override EtatEmplacement Emplacement(int i, out float restant, out float total)
        {
            restant = total = 0f;
            switch (i)
            {
                case 0: return m_Action == Action.Bander ? EtatEmplacement.Actif : EtatEmplacement.Pret;
                case 1: return m_Visee ? EtatEmplacement.Actif : EtatEmplacement.Pret;
                case 2: return Recharge(m_RechargeNuee, B.nueeRecharge * Facteur(2) * RechargeEsprit, out restant, out total, m_Action == Action.Nuee || m_Action == Action.Viser);
                case 3: return Recharge(m_RechargeRoulade, B.rouladeRecharge * RechargeEsprit, out restant, out total);
                default: return EtatEmplacement.Vide;
            }
        }
    }
}
