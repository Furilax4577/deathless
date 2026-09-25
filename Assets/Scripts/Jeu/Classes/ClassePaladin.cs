using System.Collections.Generic;
using UnityEngine;

namespace Deathless.Jeu
{
    /// Paladin (épée et bouclier) : épée (RT), garde et parade (LT maintenu), charge bélier (LB), soin sur soi (RB).
    /// Code de la version 0.1, déplacé de Heros sans changement de comportement.
    public class ClassePaladin : ClasseHeros
    {
        enum Action { Aucune, Attaque, ChargeAnticipation, Charge, Soin }

        [Tooltip("Instance de l'aura de soin (enfant, racine à y = 0 sous les pieds).")]
        public AuraSoin aura;

        public override string Id => "paladin";
        public override float PvMax => B.herosPV;

        Action m_Action;
        float m_Depuis;
        bool m_Garde;
        float m_GardeDepuis = -99f;
        float m_DerniereAttaque = -99f;
        bool m_CoupPorte;
        int m_Combo;
        float m_RechargeCharge, m_RechargeSoin;
        Vector3 m_Dir;
        ChargeBelier m_ChargeVisuel;
        Squelette m_CibleCharge;
        float m_DistanceCharge, m_ParcouruCharge;
        Vector3 m_DepartCharge;
        readonly HashSet<Squelette> m_Repousses = new HashSet<Squelette>();
        bool m_SoinDonne;

        static readonly int P_Guard = Animator.StringToHash("Guard");
        static readonly int P_Attack1 = Animator.StringToHash("Attack1");
        static readonly int P_Attack2 = Animator.StringToHash("Attack2");
        static readonly int P_Charge = Animator.StringToHash("Charge");
        static readonly int P_Heal = Animator.StringToHash("Heal");
        static readonly int P_BlockHit = Animator.StringToHash("BlockHit");

        public bool EnGarde => m_Garde;

        public override void Initialiser(Heros heros)
        {
            base.Initialiser(heros);
            if (aura == null) aura = GetComponentInChildren<AuraSoin>(true);
            var fx = EffetsJeu.Instance;
            if (fx != null && fx.prefabChargeBelier != null)
            {
                var go = Instantiate(fx.prefabChargeBelier);
                go.name = "ChargeBelier_" + name;
                m_ChargeVisuel = go.GetComponent<ChargeBelier>();
            }
        }

        void OnDestroy() { if (m_ChargeVisuel != null) Destroy(m_ChargeVisuel.gameObject); }

        public override bool Occupe => m_Action != Action.Aucune;
        public override bool PeutEsquiver => m_Action == Action.Aucune || m_Action == Action.Attaque;
        public override float FacteurVitesse => m_Action == Action.Attaque ? 0.25f : m_Action == Action.Soin ? 0f : m_Garde ? B.gardeVitesse : 1f;
        public override bool BloqueSprint => m_Garde;
        public override bool FaceVisee => m_Garde;
        public override bool HautDuCorps => m_Garde || m_Action == Action.ChargeAnticipation;

        public override void SurAction(string action)
        {
            switch (action)
            {
                case "AttackPrimary": Attaquer(); break;
                case "AttackSecondary": m_GardeDepuis = Time.time; break;   // la parade se juge depuis l'appui
                case "Skill1": Charger(); break;
                case "Skill2": Soigner(); break;
            }
        }

        void Attaquer()
        {
            if (m_Action != Action.Aucune || Time.time - m_DerniereAttaque < B.epeeIntervalle) return;
            m_Action = Action.Attaque;
            m_Depuis = 0f;
            m_DerniereAttaque = Time.time;
            m_CoupPorte = false;
            m_Combo = 1 - m_Combo;
            H.Tourner(H.AvantCamera);
            if (Anim != null) H.Declencher(m_Combo == 0 ? P_Attack1 : P_Attack2);
            AudioBank.Jouer(SonsDuJeu.EpeeElan, transform.position + Vector3.up, 0.7f);
        }

        void PorterCoup()
        {
            m_CoupPorte = true;
            var cibles = Combat.Ennemis(transform.position, transform.forward, B.epeePortee, B.epeeDemiAngle);
            for (int i = 0; i < cibles.Count && i < B.epeeCiblesParCoup; i++)
            {
                H.Frapper(cibles[i], B.epeeDegats);
                AudioBank.Jouer(SonsDuJeu.EpeeImpact, cibles[i].transform.position + Vector3.up, 0.9f);
            }
        }

        /// Charge bélier : ruée de 7 m au plus vers la visée ; les ennemis sur le chemin sont repoussés sur le côté et
        /// brièvement étourdis ; la cible au bout est frappée (dégâts selon la distance parcourue) et longuement étourdie.
        void Charger()
        {
            if (!H.PeutAgir || m_RechargeCharge > 0f) return;
            var b = B;
            m_Dir = H.AvantCamera;
            H.Tourner(m_Dir);
            m_CibleCharge = null;
            float meilleur = float.MaxValue;
            var dv = DirecteurVagues.Instance;
            if (dv != null)
                foreach (var s in dv.Vivants)
                {
                    if (s == null || !s.Vivant) continue;
                    Vector3 d = s.transform.position - transform.position; d.y = 0f;
                    float le = Vector3.Dot(d, m_Dir);
                    if (le < 1f || le > b.chargeDistance + 1.2f) continue;
                    float lat = (d - m_Dir * le).magnitude;
                    if (lat > 1.6f) continue;
                    float score = le + lat * 3f;
                    if (lat < 0.9f) score = -le;   // bien aligné et plus loin : c'est la cible au bout
                    if (score < meilleur) { meilleur = score; m_CibleCharge = s; }
                }
            m_DistanceCharge = b.chargeDistance;
            if (m_CibleCharge != null)
            {
                float le = Vector3.Dot(m_CibleCharge.transform.position - transform.position, m_Dir);
                m_DistanceCharge = Mathf.Clamp(le - 1.1f, 0.3f, b.chargeDistance);
            }
            m_Action = Action.ChargeAnticipation;
            m_Depuis = 0f;
            m_RechargeCharge = b.chargeRecharge;
            m_Repousses.Clear();
            m_ParcouruCharge = 0f;
            if (Anim != null) H.Declencher(P_Charge);
            if (m_ChargeVisuel != null) m_ChargeVisuel.Jouer(transform, m_Dir, 99f, b.chargeDistance);
            AudioBank.Jouer(SonsDuJeu.Charge, transform.position + Vector3.up, 1f);
        }

        void Soigner()
        {
            if (!H.PeutAgir || m_RechargeSoin > 0f) return;
            m_Action = Action.Soin;
            m_Depuis = 0f;
            m_SoinDonne = false;
            m_RechargeSoin = B.soinRecharge;
            if (Anim != null) H.Declencher(P_Heal);
            AudioBank.Jouer(SonsDuJeu.Soin, transform.position + Vector3.up, 0.9f);
        }

        public override void Temps(float dt)
        {
            m_RechargeCharge = Mathf.Max(0f, m_RechargeCharge - dt);
            m_RechargeSoin = Mathf.Max(0f, m_RechargeSoin - dt);
        }

        public override void Maj(float dt, Vector3 dir)
        {
            var b = B;
            m_Depuis += dt;

            bool garde = H.Entrees.GardeMaintenue && m_Action == Action.Aucune && H.Endurance > 0f;
            if (garde && !m_Garde && Time.time - m_GardeDepuis > 0.3f) m_GardeDepuis = Time.time;
            m_Garde = garde;
            if (Anim != null) Anim.SetBool(P_Guard, m_Garde);

            switch (m_Action)
            {
                case Action.Attaque:
                    if (!m_CoupPorte && m_Depuis >= b.epeeInstant) PorterCoup();
                    if (m_Depuis >= b.epeeIntervalle) m_Action = Action.Aucune;
                    break;
                case Action.ChargeAnticipation:
                    if (m_Depuis >= b.chargeAnticipation) { m_Action = Action.Charge; m_Depuis = 0f; m_DepartCharge = transform.position; TraverserEnnemis(true); }
                    break;
                case Action.Soin:
                    if (!m_SoinDonne && m_Depuis >= b.soinIncantation)
                    {
                        m_SoinDonne = true;
                        H.Sante.Soigner(H.Sante.pvMax * b.soinPart);
                        if (aura != null) aura.Jouer();
                    }
                    if (m_Depuis >= b.soinIncantation + 0.5f) m_Action = Action.Aucune;
                    break;
            }
        }

        public override bool DeplacementImpose(float dt, out Vector3 vitesse)
        {
            vitesse = Vector3.zero;
            if (m_Action == Action.ChargeAnticipation) return true;
            if (m_Action != Action.Charge) return false;
            var b = B;
            float duree = b.chargeDuree * m_DistanceCharge / b.chargeDistance;
            float k = Mathf.Clamp01(m_Depuis / Mathf.Max(0.05f, duree));
            float voulu = m_DistanceCharge * (1f - (1f - k) * (1f - k) * (1f - 0.3f * k));   // départ franc, léger freinage
            m_ParcouruCharge = Vector3.Dot(transform.position - m_DepartCharge, m_Dir);
            vitesse = m_Dir * Mathf.Max(0f, voulu - m_ParcouruCharge) / Mathf.Max(dt, 0.001f);
            var dv = DirecteurVagues.Instance;
            if (dv != null)
                foreach (var s in dv.Vivants)
                {
                    if (s == null || !s.Vivant || s == m_CibleCharge || m_Repousses.Contains(s)) continue;
                    Vector3 d = s.transform.position - transform.position; d.y = 0f;
                    float le = Vector3.Dot(d, m_Dir);
                    if (le < -0.3f || le > 1.4f) continue;
                    Vector3 lat = d - m_Dir * le;
                    if (lat.magnitude > b.chargeLargeur) continue;
                    m_Repousses.Add(s);
                    Vector3 cote = m_ChargeVisuel != null ? m_ChargeVisuel.Repousser(s.transform, b.chargeEtourdiRepousses) : Vector3.zero;
                    if (cote.sqrMagnitude < 0.01f) cote = lat.sqrMagnitude > 0.01f ? lat.normalized : Vector3.Cross(Vector3.up, m_Dir);
                    s.Repousser(cote.normalized * b.chargeRepoussement, b.chargeEtourdiRepousses, H.Id);
                }
            if (k >= 1f) FinCharge();
            return true;
        }

        public override void SurCollisionCote() { if (m_Action == Action.Charge && m_Depuis > 0.08f) FinCharge(); }

        void FinCharge()
        {
            if (m_Action != Action.Charge) return;
            var b = B;
            m_ParcouruCharge = Mathf.Max(0f, Vector3.Dot(transform.position - m_DepartCharge, m_Dir));
            float force = Mathf.Clamp01(m_ParcouruCharge / b.chargeDistance);
            float degats = Mathf.Lerp(b.chargeDegatsMin, b.chargeDegatsMax, force);
            var p = H.Partie;
            if (m_CibleCharge != null && m_CibleCharge.Vivant && Vector3.Distance(m_CibleCharge.transform.position, transform.position) < 2.6f)
            {
                H.Frapper(m_CibleCharge.Sante, degats);
                if (m_CibleCharge.Vivant)
                {
                    m_CibleCharge.Etourdir(b.chargeEtourdiCible, H.Id);
                    if (m_CibleCharge.Repoussable) m_CibleCharge.Repousser(m_Dir * 0.6f, b.chargeEtourdiCible, H.Id);
                    if (m_ChargeVisuel != null) m_ChargeVisuel.EtourdirCible(m_CibleCharge.transform, b.chargeEtourdiCible * m_CibleCharge.FacteurEtourdissement);
                }
                if (p != null) p.Journal("Charge : " + m_ParcouruCharge.ToString("F1") + " m, force " + force.ToString("F2") + ", " + degats.ToString("F0") + " dégâts sur " + m_CibleCharge.type + ", " + m_Repousses.Count + " repoussés");
            }
            else if (p != null) p.Journal("Charge : " + m_ParcouruCharge.ToString("F1") + " m sans cible, " + m_Repousses.Count + " repoussés");
            if (m_ChargeVisuel != null) m_ChargeVisuel.Impact(force);
            TraverserEnnemis(false);
            AudioBank.Jouer(SonsDuJeu.ChargeImpact, transform.position + transform.forward, 0.8f * (0.5f + force));
            m_Action = Action.Aucune;
            m_CibleCharge = null;
        }

        void TraverserEnnemis(bool traverser) => H.TraverserEnnemis(traverser, m_CibleCharge);

        public override void Interrompre()
        {
            if (m_Action == Action.Charge) FinCharge();
            m_Action = Action.Aucune;
            m_Garde = false;
            if (Anim != null) Anim.SetBool(P_Guard, false);
        }

        // ----------------------------------------------------------------- Garde et parade

        public override Interception Intercepter(InfoDegats info)
        {
            if (!m_Garde || info.source == null) return Interception.Passe;
            Vector3 vers = info.source.transform.position - transform.position; vers.y = 0f;
            if (Vector3.Angle(transform.forward, vers) > B.gardeDemiAngle) return Interception.Passe;
            var sq = info.source.GetComponent<Squelette>();
            if (Time.time - m_GardeDepuis <= B.paradeFenetre)
            {
                if (sq != null) sq.Etourdir(B.paradeEtourdi, H.Id);
                return Interception.Pare;
            }
            float cout = info.montant * B.gardeCoutParDegat;
            if (H.Depenser(cout)) return Interception.Bloque;
            // Garde brisée : le coup passe et le héros est déséquilibré.
            H.ViderEndurance();
            H.Etourdir(B.gardeBriseeEtourdi);
            return Interception.Passe;
        }

        public override void SurIntercepte(InfoDegats info, Interception r)
        {
            if (H.EstInvulnerable && !m_Garde) return;   // coup esquivé
            if (Anim != null) H.Declencher(P_BlockHit);
            H.HautDuCorpsPendant(0.5f);
            AudioBank.Jouer(r == Interception.Pare ? SonsDuJeu.Parade : SonsDuJeu.Blocage, transform.position + Vector3.up * 1.2f, 1f);
            if (r == Interception.Pare && H.Partie != null) H.Partie.Journal("Parade !");
        }

        // ----------------------------------------------------------------- HUD

        public override EtatEmplacement Emplacement(int i, out float restant, out float total)
        {
            restant = total = 0f;
            switch (i)
            {
                case 0: return m_Action == Action.Attaque ? EtatEmplacement.Actif : EtatEmplacement.Pret;
                case 1: return m_Garde ? EtatEmplacement.Actif : H.Endurance <= 0f ? EtatEmplacement.Indisponible : EtatEmplacement.Pret;
                case 2: return Recharge(m_RechargeCharge, B.chargeRecharge, out restant, out total, m_Action == Action.Charge || m_Action == Action.ChargeAnticipation);
                case 3: return Recharge(m_RechargeSoin, B.soinRecharge, out restant, out total, m_Action == Action.Soin);
                default: return EtatEmplacement.Vide;
            }
        }
    }
}
