using System.Collections;
using UnityEngine;

namespace Deathless.Jeu
{
    /// Rôdeur (arc et carquois) : visée (LT / clic droit maintenu, sans zoom de caméra) ; pendant la visée, bander et tirer
    /// (RT / clic gauche maintenu puis relâché : charge 1,2 s, 10 à 40 dégâts, tir à la tête ×2 et critique) ; lâcher la
    /// visée pendant qu'il bande repose la flèche sans tirer (wiki : classe-rodeur, Viser puis bander, 26/09/2026) ;
    /// nuée de flèches (LB), roulade arrière et salve (RB). Flèches non magiques (modèle
    /// KayKit, traînée d'air) ; cercle de charge et éclat à 100 % par l'effet ArcBande. Arc repos / visée, flèche encochée
    /// et blendshape `Draw` pilotés ici (mêmes valeurs que BowStance).
    public class ClasseRodeur : ClasseHeros
    {
        enum Action { Aucune, Bander, Lacher, Nuee }

        public override string Id => "rodeur";
        public override float PvMax => B.rodeurPV;
        public override float Vitesse => B.rodeurVitesse;

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
        public override float FacteurVitesse => m_Action == Action.Nuee ? 0f : m_Action != Action.Aucune ? B.arcVitesseBander : m_Visee ? B.viseeVitesse : 1f;
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
            switch (action)
            {
                case "AttackPrimary": if (m_Visee) Bander(); break;   // RT ne bande que pendant la visée
                case "Skill1": Nuee(); break;
                case "Skill2": Roulade(); break;
            }
        }

        void Bander()
        {
            if (m_Action != Action.Aucune || Time.time - m_DernierTir < B.arcIntervalle) return;
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
            float degats = Mathf.Lerp(b.arcDegatsMin, b.arcDegatsMax, charge);
            // Vitesse selon la charge : tir rapide lent (retombe vite), charge complète rapide (file loin et tendu).
            TirerFleche(depart, cible, Mathf.Lerp(b.arcVitesseMin, b.arcVitesseMax, charge), degats, b.arcTete);
            AudioBank.Jouer(charge >= 0.999f ? SonsDuJeu.ArcTirCharge : SonsDuJeu.ArcTir, depart, 0.9f);
            Diffuser(E_Tir, depart, default, charge);
        }

        void TirerFleche(Vector3 depart, Vector3 cible, float vitesse, float degats, float multTete)
        {
            ProjectileJeu.Tirer(ProjectileJeu.Genre.Fleche, depart, cible, vitesse, B.arcPortee, transform, (point, dir, s) =>
            {
                AudioBank.Jouer(SonsDuJeu.FlecheImpact, point, 0.7f, 0.05f);
                if (s == null) return;
                bool tete = Combat.ALaTete(s.GetComponent<Squelette>(), point, dir);
                if (tete) Critique(point, -dir, false);
                H.Frapper(s, degats * (tete ? multTete : 1f), tete, point, dir);
            });
        }

        void Nuee()
        {
            if (!H.PeutAgir || m_RechargeNuee > 0f) return;
            var b = B;
            Vector3 p = Combat.PointVise(H.CameraJeu, transform, b.nueePortee, out _);
            Vector3 d = p - transform.position; d.y = 0f;
            if (d.magnitude > b.nueePortee) p = transform.position + d.normalized * b.nueePortee;
            if (Physics.Raycast(p + Vector3.up * 5f, Vector3.down, out var hit, 20f, ~0, QueryTriggerInteraction.Ignore)) p = hit.point;
            m_CentreNuee = p;
            m_RechargeNuee = b.nueeRecharge;
            m_Action = Action.Nuee;
            m_Depuis = 0f;
            m_NueeLancee = false;
            H.Tourner(d);
            if (Anim != null) H.Declencher(P_TirHaut);
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
                foreach (var s in Combat.Ennemis(centre, Vector3.forward, b.nueeRayon, 180f))
                    H.Frapper(s, b.nueeDegatsSalve, false, s.transform.position + Vector3.up, Vector3.down);
                yield return new WaitForSeconds(1.2f / b.nueeSalves);
            }
        }

        /// Roulade arrière (RB) : le rôdeur roule en arrière pour reprendre ses distances et tire en même temps une salve
        /// de flèches en éventail devant lui (flèches normales, traînée d'air, pas d'effet dédié).
        void Roulade()
        {
            if (!H.PeutAgir || m_RechargeRoulade > 0f || !H.Depenser(B.rouladeCout)) return;
            var b = B;
            m_RechargeRoulade = b.rouladeRecharge;
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
            for (int i = 0; i < b.salveFleches; i++)
            {
                float a = b.salveFleches > 1 ? Mathf.Lerp(-b.salveEcart, b.salveEcart, i / (float)(b.salveFleches - 1)) : 0f;
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
            m_Visee = H.Vivant && H.EnJeu && H.Entrees.GardeMaintenue;
            if (H.CameraEpaule != null) H.CameraEpaule.viseeVoulue = 0f;   // visée sans zoom (décision de Quentin, 26/09/2026)
        }

        public override void Maj(float dt, Vector3 dir)
        {
            var b = B;
            m_Depuis += dt;
            switch (m_Action)
            {
                case Action.Bander:
                    m_Charge = Mathf.Clamp01(m_Depuis / b.arcCharge);
                    if (m_Cercle != null) m_Cercle.Charge = m_Charge;
                    if (!m_Visee) Reposer();                               // visée lâchée : pas de tir
                    else if (!H.Entrees.AttaqueMaintenue) Lacher();
                    break;
                case Action.Aucune:
                    // RT déjà maintenu quand la visée commence : il bande aussitôt.
                    if (m_Visee && H.Entrees.AttaqueMaintenue && Time.time - m_DernierTir >= b.arcIntervalle + 0.15f) Bander();
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
            bool vise = m_Action == Action.Bander || m_Action == Action.Nuee;
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
                case 2: return Recharge(m_RechargeNuee, B.nueeRecharge, out restant, out total, m_Action == Action.Nuee);
                case 3: return Recharge(m_RechargeRoulade, B.rouladeRecharge, out restant, out total);
                default: return EtatEmplacement.Vide;
            }
        }
    }
}
