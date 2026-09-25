using System.Collections.Generic;
using UnityEngine;

namespace Deathless.Jeu
{
    /// Assassin (dague, arbalète dans le dos). Passifs (wiki) : hors combat, marcher sans sprinter fait passer en marche
    /// discrète et en mode furtif ; courir, attaquer ou être repéré en fait sortir ; dans la fumée d'une grenade, il
    /// redevient furtif même en combat. Coup non détecté ×2, dans le dos ×3, les deux ×5 (meilleur critique).
    /// Dague (RT), arbalète en main et visée (LT maintenu, RT tire ; critique seulement à la tête ; recharge 6 s),
    /// grenade fumigène (LB : une, qui revient 20 s après, nuage d'environ 5 s). RB vide.
    public class ClasseAssassin : ClasseHeros
    {
        enum Action { Aucune, Dague, Tir, Grenade }

        public override string Id => "assassin";
        public override float PvMax => B.assassinPV;
        public override float Vitesse => m_Furtif && !H.Sprinte ? B.marcheDiscrete : B.assassinVitesse;

        Action m_Action;
        float m_Depuis;
        bool m_Furtif;
        float m_DernierCombat = -99f;
        bool m_CoupPorte;
        bool m_FurtifAuCoup;
        float m_DerniereDague = -99f;
        float m_RechargeArbalete, m_RechargeGrenade;
        bool m_Arbalete;
        bool m_GrenadeTenue, m_GrenadeLancee;
        Vector3 m_CibleGrenade;
        ModeFurtif m_Visuel;
        Fumigene m_Fumigene;
        WeaponStyle m_Style;
        GameObject m_Modele;

        static readonly List<Fumigene> s_Fumees = new List<Fumigene>();
        static readonly int P_Sneaking = Animator.StringToHash("Sneaking");
        static readonly int P_Stab = Animator.StringToHash("Stab");
        static readonly int P_Crossbow = Animator.StringToHash("Crossbow");
        static readonly int P_Shoot = Animator.StringToHash("Shoot");
        static readonly int P_Throw = Animator.StringToHash("Throw");

        /// Un point est-il dans la fumée d'une grenade active ? (les ennemis ne voient personne dedans)
        public static bool DansLaFumee(Vector3 p)
        {
            for (int i = s_Fumees.Count - 1; i >= 0; i--)
            {
                var f = s_Fumees[i];
                if (f == null) { s_Fumees.RemoveAt(i); continue; }
                if (f.Actif && f.Contient(p)) return true;
            }
            return false;
        }

        public override void Initialiser(Heros heros)
        {
            base.Initialiser(heros);
            m_Visuel = GetComponent<ModeFurtif>();
            m_Modele = H.animator != null ? H.animator.gameObject : gameObject;
            m_Style = Resources.Load<ClassesJeu>("ClassesJeu")?.Trouver(Id)?.style;
            var fx = EffetsJeu.Instance;
            if (fx != null && fx.prefabFumigene != null)
            {
                m_Fumigene = Instantiate(fx.prefabFumigene).GetComponent<Fumigene>();
                var tenue = typeof(Fumigene).GetField("tenue", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (tenue != null) tenue.SetValue(m_Fumigene, Mathf.Max(0.5f, B.grenadeNuage - 0.5f));   // + 0,5 s d'éclosion : ~5 s (wiki)
                s_Fumees.Add(m_Fumigene);
            }
        }

        void OnDestroy() { if (m_Fumigene != null) { s_Fumees.Remove(m_Fumigene); Destroy(m_Fumigene.gameObject); } }

        public override bool Occupe => m_Action != Action.Aucune;
        public override float FacteurVitesse => m_Action == Action.Grenade ? 0.3f : m_Action != Action.Aucune ? 0.4f : m_Arbalete ? 0.5f : 1f;
        public override bool BloqueSprint => m_Arbalete;
        public override bool FaceVisee => m_Arbalete || m_Action != Action.Aucune;
        public override bool HautDuCorps => m_Arbalete;
        public override bool Furtif => m_Furtif;
        public override float FacteurAnimation => m_Furtif ? 1f : 1f;

        Transform m_Arbal;

        /// Arbalète en main : la ligne de tir est l'axe avant du modèle crossbow_1handed.
        public override bool AxeDeTir(out Vector3 origine, out Vector3 direction)
        {
            origine = direction = Vector3.zero;
            if (!m_Arbalete) return false;
            if (m_Arbal == null) m_Arbal = MannequinEquip.Trouver(transform, "crossbow_1handed");
            if (m_Arbal == null || !m_Arbal.gameObject.activeInHierarchy) return false;
            origine = m_Arbal.position;
            direction = m_Arbal.forward;
            return true;
        }

        /// Repéré par un squelette : sortie du mode furtif.
        public void Reperer()
        {
            // Multijoueur : repéré chez l'hôte (marionnette) ; c'est son propriétaire qui sort du mode furtif.
            if (H != null && H.Distant) { H.GetComponent<Deathless.Reseau.HerosReseau>()?.SignalerRepere(); return; }
            m_DernierCombat = Time.time;
            if (!m_Furtif) return;
            SortirFurtif();
            AudioBank.Jouer(SonsDuJeu.Repere, transform.position + Vector3.up * 1.6f, 0.9f);
            if (H.Partie != null) H.Partie.Journal("Assassin repéré");
        }

        void EntrerFurtif()
        {
            if (m_Furtif) return;
            m_Furtif = true;
            if (m_Visuel != null) m_Visuel.Entrer();
            if (Anim != null) Anim.SetBool(P_Sneaking, true);
            AudioBank.Jouer(SonsDuJeu.FurtifEntree, transform.position + Vector3.up, 0.6f);
        }

        void SortirFurtif()
        {
            if (!m_Furtif) return;
            m_Furtif = false;
            if (m_Visuel != null) m_Visuel.Sortir();
            if (Anim != null) Anim.SetBool(P_Sneaking, false);
            AudioBank.Jouer(SonsDuJeu.FurtifSortie, transform.position + Vector3.up, 0.5f);
        }

        public override void SurAction(string action)
        {
            switch (action)
            {
                case "AttackPrimary":
                    if (m_Arbalete) Tirer(); else Dague();
                    break;
                case "Skill1": Grenade(); break;
            }
        }

        void Dague()
        {
            if (m_Action != Action.Aucune || Time.time - m_DerniereDague < B.dagueIntervalle) return;
            m_Action = Action.Dague;
            m_Depuis = 0f;
            m_DerniereDague = Time.time;
            m_CoupPorte = false;
            m_FurtifAuCoup = m_Furtif;
            var cibles = Combat.Ennemis(transform.position, H.AvantCamera, B.daguePortee + 0.8f, 70f);
            H.Tourner(cibles.Count > 0 ? cibles[0].transform.position - transform.position : H.AvantCamera);
            if (Anim != null) H.Declencher(P_Stab);
        }

        void PorterDague()
        {
            m_CoupPorte = true;
            var b = B;
            var cibles = Combat.Ennemis(transform.position, transform.forward, b.daguePortee, b.dagueDemiAngle);
            if (cibles.Count > 0)
            {
                var s = cibles[0];
                bool dos = Combat.DansLeDos(s.transform, transform.position, b.angleDos);
                bool furtif = m_FurtifAuCoup;
                float mult = furtif && dos ? b.critiqueFurtifDos : dos ? b.critiqueDos : furtif ? b.critiqueFurtif : 1f;
                bool critique = furtif || dos;
                Vector3 point = s.transform.position + Vector3.up * 1.1f;
                Vector3 dir = (s.transform.position - transform.position).normalized;
                if (critique) Combat.Critique(point, -dir, furtif && dos);
                H.Frapper(s, b.dagueDegats * mult, critique, point, dir);
                AudioBank.Jouer(SonsDuJeu.Dague, point, 0.9f);
                if (H.Partie != null && critique) H.Partie.Journal("Dague : " + (furtif && dos ? "meilleur critique ×" : "critique ×") + mult + (dos ? " (dos)" : "") + (furtif ? " (furtif)" : ""));
            }
            else AudioBank.Jouer(SonsDuJeu.EpeeElan, transform.position + Vector3.up, 0.5f);
            // Attaquer fait sortir du mode furtif (wiki).
            m_DernierCombat = Time.time;
            SortirFurtif();
        }

        void Tirer()
        {
            if (m_Action != Action.Aucune || m_RechargeArbalete > 0f) return;
            var b = B;
            m_Action = Action.Tir;
            m_Depuis = 0f;
            m_RechargeArbalete = b.arbaleteRecharge;
            m_DernierCombat = Time.time;
            SortirFurtif();
            if (Anim != null) H.Declencher(P_Shoot);
            Vector3 cible = Combat.PointVise(H.CameraJeu, transform, b.arbaletePortee, out _);
            var arbalete = MannequinEquip.Trouver(transform, "crossbow_1handed");
            Vector3 depart = arbalete != null ? arbalete.position + H.AvantCamera * 0.4f : transform.position + Vector3.up * 1.4f;
            ProjectileJeu.Tirer(ProjectileJeu.Genre.Carreau, depart, cible, b.arbaleteVitesse, b.arbaletePortee, transform, (point, dir, s) =>
            {
                AudioBank.Jouer(SonsDuJeu.FlecheImpact, point, 0.7f, 0.05f);
                if (s == null) return;
                // Seul critique de l'arbalète : la tête (les passifs ne s'appliquent pas aux carreaux, wiki).
                bool tete = Combat.ALaTete(s.GetComponent<Squelette>(), point, dir);
                if (tete) Combat.Critique(point, -dir, false);
                H.Frapper(s, b.arbaleteDegats * (tete ? b.arbaleteTete : 1f), tete, point, dir);
            });
            AudioBank.Jouer(SonsDuJeu.ArbaleteTir, depart, 1f);
            Invoke(nameof(SonRecharge), 0.6f);
        }

        void SonRecharge() => AudioBank.Jouer(SonsDuJeu.ArbaleteRecharge, transform.position + Vector3.up * 1.3f, 0.7f);

        void Grenade()
        {
            if (!H.PeutAgir || m_RechargeGrenade > 0f || m_Fumigene == null) return;
            var b = B;
            Vector3 p = Combat.PointVise(H.CameraJeu, transform, b.grenadePortee + 20f, out _);
            Vector3 d = p - transform.position; d.y = 0f;
            if (d.magnitude > b.grenadePortee) p = transform.position + d.normalized * b.grenadePortee;
            if (d.magnitude < 2f) p = transform.position + H.AvantCamera * 3.5f;
            if (Physics.Raycast(p + Vector3.up * 5f, Vector3.down, out var hit, 20f, ~0, QueryTriggerInteraction.Ignore)) p = hit.point;
            m_CibleGrenade = p;
            m_RechargeGrenade = b.grenadeRecharge;
            m_Action = Action.Grenade;
            m_Depuis = 0f;
            m_GrenadeTenue = m_GrenadeLancee = false;
            H.Tourner(p - transform.position);
            if (Anim != null) H.Declencher(P_Throw);
        }

        public override void Temps(float dt)
        {
            m_RechargeArbalete = Mathf.Max(0f, m_RechargeArbalete - dt);
            m_RechargeGrenade = Mathf.Max(0f, m_RechargeGrenade - dt);
            var b = B;
            // Arbalète en main tant que LT est maintenu (bascule dague ↔ arbalète).
            bool arbalete = H.Vivant && H.EnJeu && H.Entrees.GardeMaintenue && m_Action != Action.Grenade && m_Action != Action.Dague;
            if (arbalete != m_Arbalete)
            {
                m_Arbalete = arbalete;
                if (m_Style != null) MannequinEquip.PoseAlternative(m_Modele, m_Style, arbalete);
                if (Anim != null) Anim.SetBool(P_Crossbow, arbalete);
                if (arbalete) { m_DernierCombat = Time.time; SortirFurtif(); }
            }
            if (H.CameraEpaule != null) H.CameraEpaule.viseeVoulue = m_Arbalete ? 1f : 0f;

            // Mode furtif : hors combat, marcher sans sprinter ; dans la fumée, même en combat.
            bool fumee = DansLaFumee(transform.position);
            if (fumee) m_DernierCombat = -99f;
            bool horsCombat = Time.time - m_DernierCombat > b.horsCombat;
            bool marche = H.Entrees.Deplacement.sqrMagnitude > 0.01f;
            if (!H.Vivant || H.Sprinte || m_Arbalete) { if (m_Furtif && H.Sprinte) m_DernierCombat = Time.time; SortirFurtif(); }
            else if ((horsCombat && marche) || fumee) EntrerFurtif();
        }

        public override void SurTouche(InfoDegats info, float reel)
        {
            m_DernierCombat = Time.time;
            SortirFurtif();
        }

        public override void Maj(float dt, Vector3 dir)
        {
            var b = B;
            m_Depuis += dt;
            switch (m_Action)
            {
                case Action.Dague:
                    if (!m_CoupPorte && m_Depuis >= b.dagueInstant) PorterDague();
                    if (m_Depuis >= b.dagueIntervalle) m_Action = Action.Aucune;
                    break;
                case Action.Tir:
                    if (m_Depuis >= 0.4f) m_Action = Action.Aucune;
                    break;
                case Action.Grenade:
                    var main = MannequinEquip.Trouver(transform, "handslot.r");
                    if (!m_GrenadeTenue && m_Depuis >= 0.1f) { m_GrenadeTenue = true; if (main != null) m_Fumigene.Tenir(main); }
                    if (!m_GrenadeLancee && m_Depuis >= 0.75f)
                    {
                        m_GrenadeLancee = true;
                        Vector3 depart = main != null ? main.position : transform.position + Vector3.up * 1.5f;
                        m_Fumigene.Lancer(depart, m_CibleGrenade, 0.6f);
                        Deathless.Reseau.HerosReseau.Local(H)?.Fumee(depart, m_CibleGrenade, 0.6f);
                        AudioBank.Jouer(SonsDuJeu.GrenadeLancer, depart, 0.8f);
                        Invoke(nameof(SonFumee), 0.6f);
                    }
                    if (m_Depuis >= 1.1f) m_Action = Action.Aucune;
                    break;
            }
        }

        // ----------------------------------------------------------------- Multijoueur (marionnette)

        /// Marionnette : mode furtif du propriétaire (visuel ici ; chez l'hôte, les squelettes en tiennent compte).
        public void ForcerFurtifDistant(bool furtif)
        {
            if (furtif == m_Furtif) return;
            if (furtif) EntrerFurtif(); else SortirFurtif();
        }

        /// Marionnette : grenade fumigène du propriétaire (le nuage cache aussi des squelettes de l'hôte).
        public void LancerFumeeDistante(Vector3 depart, Vector3 cible, float duree)
        {
            if (m_Fumigene == null) return;
            m_Fumigene.Lancer(depart, cible, duree);
            AudioBank.Jouer(SonsDuJeu.GrenadeLancer, depart, 0.8f);
        }

        void SonFumee() => AudioBank.Jouer(SonsDuJeu.Fumee, m_CibleGrenade, 1f);

        public override void Interrompre()
        {
            if (m_Action == Action.Grenade && m_GrenadeTenue && !m_GrenadeLancee && m_Fumigene != null)
            {
                // Esquive pendant le geste : la grenade part quand même, sous ses pieds.
                m_GrenadeLancee = true;
                m_Fumigene.Lancer(transform.position + Vector3.up, transform.position + transform.forward, 0.3f);
                Deathless.Reseau.HerosReseau.Local(H)?.Fumee(transform.position + Vector3.up, transform.position + transform.forward, 0.3f);
            }
            m_Action = Action.Aucune;
        }

        public override EtatEmplacement Emplacement(int i, out float restant, out float total)
        {
            restant = total = 0f;
            switch (i)
            {
                case 0: return m_Action == Action.Dague ? EtatEmplacement.Actif : EtatEmplacement.Pret;
                case 1: return Recharge(m_RechargeArbalete, B.arbaleteRecharge, out restant, out total, m_Arbalete && m_RechargeArbalete <= 0f);
                case 2: return Recharge(m_RechargeGrenade, B.grenadeRecharge, out restant, out total, m_Action == Action.Grenade);
                default: return EtatEmplacement.Vide;
            }
        }
    }
}
