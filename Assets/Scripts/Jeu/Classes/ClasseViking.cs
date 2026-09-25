using Deathless.UI.Donnees;
using UnityEngine;

namespace Deathless.Jeu
{
    /// Viking (hache à deux mains) : hache (RT, toutes les cibles de l'arc), attaque tournante maintenue (LT, consomme la
    /// rage en continu, s'arrête quand elle est vide), rugissement (LB : provoque les squelettes proches), saut percutant
    /// (RB : bond de 5 m, onde de terre à l'impact). Rage (wiki) : monte quand il frappe, redescend lentement hors combat.
    public class ClasseViking : ClasseHeros
    {
        enum Action { Aucune, Attaque, Tournante, Rugissement, Saut }

        public Transform teteHache;

        public override string Id => "viking";
        public override float PvMax => B.vikingPV;
        public override float Vitesse => B.vikingVitesse;

        Action m_Action;
        float m_Depuis;
        bool m_CoupPorte;
        int m_Combo;
        float m_DerniereAttaque = -99f;
        float m_Rage;
        float m_DernierCoup = -99f;
        float m_ProchainTic;
        bool m_VfxTournante;
        float m_RechargeRugir, m_RechargeSaut;
        bool m_Crie, m_VfxCri, m_Impact;
        Vector3 m_DirSaut, m_DepartSaut;
        AttaqueTournante m_Tournante;
        Rugissement m_Rugissement;
        AudioSource m_SonTournante;

        static readonly int P_Attack1 = Animator.StringToHash("Attack1");
        static readonly int P_Attack2 = Animator.StringToHash("Attack2");
        static readonly int P_Tourne = Animator.StringToHash("Tourne");
        static readonly int P_Rugir = Animator.StringToHash("Rugir");
        static readonly int P_Saut = Animator.StringToHash("Saut");

        // Instants du geste (clips à vitesse 1, mesurés par VfxBench) et vitesses de lecture du contrôleur.
        const float CriClip = 1.63f, VitesseCri = 1.6f;
        const float DecollageClip = 0.19f, AtterrissageClip = 0.69f, ImpactClip = 0.79f, VitesseSaut = 1.2f;

        public override void Initialiser(Heros heros)
        {
            base.Initialiser(heros);
            var fx = EffetsJeu.Instance;
            if (teteHache == null)
            {
                var axe = MannequinEquip.Trouver(transform, "axe_2handed");
                if (axe != null)
                {
                    teteHache = new GameObject("TeteHache").transform;
                    teteHache.SetParent(axe, false);
                    teteHache.localPosition = new Vector3(0f, 0.92f, 0f);
                }
            }
            if (fx != null && fx.prefabTournante != null) m_Tournante = Instantiate(fx.prefabTournante).GetComponent<AttaqueTournante>();
            if (fx != null && fx.prefabRugissement != null)
            {
                var r = Instantiate(fx.prefabRugissement, transform);
                r.transform.localPosition = Vector3.up * 1f;
                r.transform.localRotation = Quaternion.identity;
                m_Rugissement = r.GetComponent<Rugissement>();
            }
        }

        void OnDestroy() { if (m_Tournante != null) Destroy(m_Tournante.gameObject); }

        public override bool Occupe => m_Action != Action.Aucune;
        public override bool PeutEsquiver => m_Action == Action.Aucune || m_Action == Action.Attaque || m_Action == Action.Tournante;
        public override float FacteurVitesse => m_Action == Action.Tournante ? B.tournanteVitesse : m_Action == Action.Attaque ? 0.25f : m_Action == Action.Aucune ? 1f : 0f;
        public override void RemplirJauge() { m_Rage = B.rageMax; }
        public override JaugeClasse Jauge => JaugeClasse.Rage;
        public override float ValeurJauge => m_Rage;
        public override float JaugeMax => B.rageMax;

        public override void SurAction(string action)
        {
            switch (action)
            {
                case "AttackPrimary": Attaquer(); break;
                case "Skill1": Rugir(); break;
                case "Skill2": Sauter(); break;
            }
        }

        void Attaquer()
        {
            if (m_Action != Action.Aucune || Time.time - m_DerniereAttaque < B.hacheIntervalle) return;
            m_Action = Action.Attaque;
            m_Depuis = 0f;
            m_DerniereAttaque = Time.time;
            m_CoupPorte = false;
            m_Combo = 1 - m_Combo;
            H.Tourner(H.AvantCamera);
            if (Anim != null) Anim.SetTrigger(m_Combo == 0 ? P_Attack1 : P_Attack2);
            AudioBank.Jouer(SonsDuJeu.EpeeElan, transform.position + Vector3.up, 0.6f);
        }

        void Rugir()
        {
            if (!H.PeutAgir || m_RechargeRugir > 0f || m_Rage < B.rugissementRage) return;
            m_Rage -= B.rugissementRage;
            m_RechargeRugir = B.rugissementRecharge;
            m_Action = Action.Rugissement;
            m_Depuis = 0f;
            m_Crie = m_VfxCri = false;
            if (Anim != null) Anim.SetTrigger(P_Rugir);
        }

        void Sauter()
        {
            if (!H.PeutAgir || m_RechargeSaut > 0f || m_Rage < B.sautRage) return;
            m_Rage -= B.sautRage;
            m_RechargeSaut = B.sautRecharge;
            m_Action = Action.Saut;
            m_Depuis = 0f;
            m_Impact = false;
            m_DirSaut = H.AvantCamera;
            m_DepartSaut = transform.position;
            H.TraverserEnnemis(true);
            H.Tourner(m_DirSaut);
            if (Anim != null) Anim.SetTrigger(P_Saut);
        }

        public override void Temps(float dt)
        {
            m_RechargeRugir = Mathf.Max(0f, m_RechargeRugir - dt);
            m_RechargeSaut = Mathf.Max(0f, m_RechargeSaut - dt);
            if (Time.time - m_DernierCoup > B.rageDelaiBaisse && m_Action != Action.Tournante)
                m_Rage = Mathf.Max(0f, m_Rage - B.rageBaisse * dt);
        }

        public override void SurCoupDonne(Sante cible, float reel, bool parBoule)
        {
            m_Rage = Mathf.Min(B.rageMax, m_Rage + B.rageParTouche);
            m_DernierCoup = Time.time;
        }

        public override void Maj(float dt, Vector3 dir)
        {
            var b = B;
            m_Depuis += dt;
            // Tournante : tenue tant que LT est maintenu et qu'il reste de la rage.
            bool tenue = H.Entrees.GardeMaintenue;
            if (m_Action == Action.Aucune && tenue && m_Rage >= b.tournanteRageMin) Commencer();
            switch (m_Action)
            {
                case Action.Attaque:
                    if (!m_CoupPorte && m_Depuis >= b.hacheInstant)
                    {
                        m_CoupPorte = true;
                        bool touche = false;
                        foreach (var s in Combat.Ennemis(transform.position, transform.forward, b.hachePortee, b.hacheDemiAngle))
                        {
                            H.Frapper(s, b.hacheDegats);
                            touche = true;
                        }
                        if (touche) AudioBank.Jouer(SonsDuJeu.Hache, transform.position + transform.forward + Vector3.up, 1f);
                    }
                    if (m_Depuis >= b.hacheIntervalle) m_Action = Action.Aucune;
                    break;
                case Action.Tournante:
                    m_Rage -= b.tournanteRage * dt;
                    if (!m_VfxTournante && m_Depuis >= 0.35f && m_Tournante != null && teteHache != null) { m_Tournante.Commencer(transform, teteHache); m_VfxTournante = true; }
                    if (m_Depuis >= 0.35f && Time.time >= m_ProchainTic)
                    {
                        m_ProchainTic = Time.time + b.tournanteIntervalle;
                        bool touche = false;
                        foreach (var s in Combat.Ennemis(transform.position, transform.forward, b.tournanteRayon, 180f))
                        {
                            H.Frapper(s, b.tournanteDegats, false, s.transform.position + Vector3.up, (s.transform.position - transform.position).normalized, false, true);
                            touche = true;
                        }
                        if (touche) AudioBank.Jouer(SonsDuJeu.Hache, transform.position + Vector3.up, 0.6f, 0.25f);
                    }
                    if (!tenue || m_Rage <= 0f) Arreter();
                    break;
                case Action.Rugissement:
                {
                    float cri = CriClip / VitesseCri;
                    if (!m_VfxCri && m_Depuis >= cri - 0.32f) { m_VfxCri = true; if (m_Rugissement != null) m_Rugissement.Jouer(); }
                    if (!m_Crie && m_Depuis >= cri)
                    {
                        m_Crie = true;
                        AudioBank.Jouer(SonsDuJeu.Rugissement, transform.position + Vector3.up * 1.6f, 1f);
                        int n = 0;
                        var dv = DirecteurVagues.Instance;
                        if (dv != null)
                            foreach (var s in dv.Vivants)
                                if (s != null && s.Vivant && (s.transform.position - transform.position).sqrMagnitude <= b.rugissementRayon * b.rugissementRayon) { s.Provoquer(H, b.rugissementProvocation); n++; }
                        if (H.Partie != null) H.Partie.Journal("Rugissement : " + n + " squelettes provoqués");
                    }
                    if (m_Depuis >= cri + 0.45f) m_Action = Action.Aucune;
                    break;
                }
                case Action.Saut:
                    if (!m_Impact && m_Depuis >= ImpactClip / VitesseSaut) Atterrir();
                    if (m_Depuis >= ImpactClip / VitesseSaut + 0.35f) m_Action = Action.Aucune;
                    break;
            }
        }

        void Commencer()
        {
            m_Action = Action.Tournante;
            m_Depuis = 0f;
            m_ProchainTic = 0f;
            m_VfxTournante = false;
            if (Anim != null) Anim.SetBool(P_Tourne, true);
            m_SonTournante = AudioBank.Boucle(SonsDuJeu.Tournante, transform, 0.8f);
        }

        void Arreter()
        {
            if (m_Action != Action.Tournante) return;
            m_Action = Action.Aucune;
            if (Anim != null) Anim.SetBool(P_Tourne, false);
            if (m_Tournante != null && m_VfxTournante) m_Tournante.Arreter();
            m_VfxTournante = false;
            if (m_SonTournante != null) { Destroy(m_SonTournante); m_SonTournante = null; }
            m_Rage = Mathf.Max(0f, m_Rage);
        }

        public override bool DeplacementImpose(float dt, out Vector3 vitesse)
        {
            vitesse = Vector3.zero;
            if (m_Action != Action.Saut && m_Action != Action.Rugissement) return false;
            if (m_Action == Action.Rugissement) return true;
            float t0 = DecollageClip / VitesseSaut, t1 = AtterrissageClip / VitesseSaut;
            if (m_Depuis < t0 || m_Depuis > t1) return true;
            float duree = t1 - t0;
            float k = (m_Depuis - t0) / duree;
            // 5 m à vitesse horizontale constante et un arc vertical de 0,6 m (comme au banc).
            vitesse = m_DirSaut * (B.sautDistance / duree) + Vector3.up * (0.6f * 4f * (1f - 2f * k) / duree);
            return true;
        }

        void Atterrir()
        {
            m_Impact = true;
            H.TraverserEnnemis(false);
            var b = B;
            Vector3 point = transform.position + m_DirSaut * 1f;
            var fx = EffetsJeu.Instance;
            if (fx != null && fx.prefabOndeSaut != null)
            {
                var onde = Instantiate(fx.prefabOndeSaut, point + Vector3.up, Quaternion.LookRotation(m_DirSaut));
                var o = onde.GetComponent<OndeDeChoc>();
                if (o != null) o.Jouer();
                Destroy(onde, 3f);
            }
            AudioBank.Jouer(SonsDuJeu.SautPercutant, point, 1f);
            int n = 0;
            foreach (var s in Combat.Ennemis(point, m_DirSaut, b.sautRayon, 180f))
            {
                H.Frapper(s, b.sautDegats);
                var sq = s.GetComponent<Squelette>();
                if (sq != null && sq.Vivant) sq.Etourdir(b.sautEtourdi, H.Id);
                n++;
            }
            if (H.Partie != null) H.Partie.Journal("Saut percutant : " + Vector3.Distance(m_DepartSaut, transform.position).ToString("F1") + " m, " + n + " touchés");
        }

        public override void Interrompre()
        {
            if (m_Action == Action.Saut && !m_Impact) H.TraverserEnnemis(false);
            Arreter();
            m_Action = Action.Aucune;
        }

        public override EtatEmplacement Emplacement(int i, out float restant, out float total)
        {
            restant = total = 0f;
            switch (i)
            {
                case 0: return m_Action == Action.Attaque ? EtatEmplacement.Actif : EtatEmplacement.Pret;
                case 1: return m_Action == Action.Tournante ? EtatEmplacement.Actif : m_Rage < B.tournanteRageMin ? EtatEmplacement.Indisponible : EtatEmplacement.Pret;
                case 2:
                {
                    var e = Recharge(m_RechargeRugir, B.rugissementRecharge, out restant, out total, m_Action == Action.Rugissement);
                    return e == EtatEmplacement.Pret && m_Rage < B.rugissementRage ? EtatEmplacement.Indisponible : e;
                }
                case 3:
                {
                    var e = Recharge(m_RechargeSaut, B.sautRecharge, out restant, out total, m_Action == Action.Saut);
                    return e == EtatEmplacement.Pret && m_Rage < B.sautRage ? EtatEmplacement.Indisponible : e;
                }
                default: return EtatEmplacement.Vide;
            }
        }
    }
}
