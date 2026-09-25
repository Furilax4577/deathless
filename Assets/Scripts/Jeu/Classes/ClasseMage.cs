using Deathless.UI.Donnees;
using UnityEngine;

namespace Deathless.Jeu
{
    /// Mage (bâton), style de magie feu (le seul pour l'instant ; le style est une donnée de la classe, ClassesJeu.element,
    /// pas son identité) : boule de feu (RT) qui explose à l'impact et allume la brûlure, cône de flammes maintenu (LT)
    /// tant qu'il reste du mana. Mana (wiki) : 100, remonte d'environ 1 par seconde, bonus à chaque ennemi touché par la
    /// boule ; le cône en consomme tant qu'il est maintenu. LB et RB restent vides (décision de Quentin).
    public class ClasseMage : ClasseHeros
    {
        enum Action { Aucune, Boule, Cone }

        public Transform pointeBaton;

        public override string Id => "mage";
        public override float PvMax => B.magePV;
        public override float Vitesse => B.mageVitesse;

        Action m_Action;
        float m_Depuis;
        float m_Mana;
        float m_DerniereBoule = -99f;
        bool m_BouleLancee;
        Vector3 m_Vise;
        ParticleSystem[] m_Cone;
        GameObject m_ConeGo;
        AudioSource m_SonCone;
        float m_TicCone;

        // Effets diffusés aux autres postes (ClasseHeros.Diffuser).
        const int E_Boule = 1, E_ConeDebut = 2, E_ConeFin = 3;
        bool m_ConeDistant;

        static readonly int P_Attack1 = Animator.StringToHash("Attack1");
        static readonly int P_Cone = Animator.StringToHash("Cone");

        public override void Initialiser(Heros heros)
        {
            base.Initialiser(heros);
            m_Mana = B.manaMax;
            if (pointeBaton == null)
            {
                var baton = MannequinEquip.Trouver(transform, "staff");
                if (baton != null)
                {
                    pointeBaton = new GameObject("PointeBaton").transform;
                    pointeBaton.SetParent(baton, false);
                    pointeBaton.localPosition = new Vector3(0f, 1.2f, 0f);
                }
            }
            var fx = EffetsJeu.Instance;
            if (fx != null && fx.prefabCone != null && pointeBaton != null)
            {
                m_ConeGo = Instantiate(fx.prefabCone, pointeBaton);
                m_ConeGo.transform.localPosition = Vector3.zero;
                m_Cone = m_ConeGo.GetComponentsInChildren<ParticleSystem>();
                foreach (var ps in m_Cone) ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }

        public override bool Occupe => m_Action != Action.Aucune;
        public override bool PeutEsquiver => true;
        public override float FacteurVitesse => m_Action == Action.Cone ? B.coneVitesse : m_Action == Action.Boule ? 0.6f : 1f;
        public override bool BloqueSprint => m_Action != Action.Aucune;
        public override bool FaceVisee => m_Action != Action.Aucune;
        public override bool HautDuCorps => m_Action != Action.Aucune;
        public override void RemplirJauge() { m_Mana = B.manaMax; }
        public override JaugeClasse Jauge => JaugeClasse.Mana;
        public override float ValeurJauge => m_Mana;
        public override float JaugeMax => B.manaMax;

        public override void SurAction(string action)
        {
            if (action == "AttackPrimary") Boule();
        }

        void Boule()
        {
            if (m_Action != Action.Aucune || Time.time - m_DerniereBoule < B.bouleIntervalle) return;
            m_Action = Action.Boule;
            m_Depuis = 0f;
            m_DerniereBoule = Time.time;
            m_BouleLancee = false;
            H.Tourner(H.AvantCamera);
            if (Anim != null) H.Declencher(P_Attack1);
            AudioBank.Jouer(SonsDuJeu.BouleLancer, transform.position + Vector3.up * 1.5f, 0.8f);
            Diffuser(E_Boule);
        }

        void LancerBoule()
        {
            m_BouleLancee = true;
            var b = B;
            Vector3 cible = Combat.PointVise(H.CameraJeu, transform, b.boulePortee, out Sante visee);
            if (H.Partie != null) H.Partie.Journal("Boule de feu : visée à " + Vector3.Distance(transform.position, cible).ToString("F1") + " m" + (visee != null ? " sur un ennemi" : ""));
            Vector3 depart = pointeBaton != null ? pointeBaton.position : transform.position + Vector3.up * 1.6f + transform.forward * 0.6f;
            ProjectileJeu.Tirer(ProjectileJeu.Genre.BouleDeFeu, depart, cible, b.bouleVitesse, b.boulePortee + 5f, transform, Exploser);
        }

        void Exploser(Vector3 point, Vector3 dir, Sante direct)
        {
            var b = B;
            var fx = EffetsJeu.Instance;
            if (fx != null && fx.gemmes != null) ExplosionFeu.Jouer(point, 2.5f, fx.gemmes);
            AudioBank.Jouer(SonsDuJeu.BouleExplosion, point, 1f);
            int n = 0;
            if (direct != null && !direct.Mort) { Toucher(direct, b.bouleDegats * Facteur(0), point, dir); n++; }
            foreach (var s in Combat.Ennemis(point, dir, b.bouleRayon, 180f))
            {
                if (s == direct) continue;
                Toucher(s, b.bouleDegatsZone * Facteur(0), point, dir);
                n++;
            }
            if (H.Partie != null) H.Partie.Journal("Boule de feu : explose à " + Vector3.Distance(transform.position, point).ToString("F1") + " m" + (direct != null ? ", coup direct" : "") + ", " + n + " touchés, mana " + m_Mana.ToString("F0"));
        }

        void Toucher(Sante s, float degats, Vector3 point, Vector3 dir)
        {
            float reel = H.Frapper(s, degats, false, point, dir, true);
            if (reel > 0f) m_Mana = Mathf.Min(B.manaMax, m_Mana + B.manaParTouche);   // wiki : bonus par ennemi touché
            Brulure.Allumer(s, H);
        }

        public override void Temps(float dt)
        {
            if (m_Action != Action.Cone) m_Mana = Mathf.Min(B.manaMax, m_Mana + B.manaRegen * Facteur(2) * dt);
        }

        public override void Maj(float dt, Vector3 dir)
        {
            var b = B;
            m_Depuis += dt;
            bool tenu = H.Entrees.GardeMaintenue;
            if (m_Action == Action.Aucune && tenu && m_Mana >= b.coneMana * 0.25f) CommencerCone();
            switch (m_Action)
            {
                case Action.Boule:
                    if (!m_BouleLancee && m_Depuis >= b.bouleInstant) LancerBoule();
                    if (m_Depuis >= Mathf.Min(b.bouleIntervalle, 0.7f)) m_Action = Action.Aucune;
                    break;
                case Action.Cone:
                    m_Mana -= b.coneMana * Facteur(1) * dt;
                    if (pointeBaton != null && m_ConeGo != null)
                    {
                        // Le cône part de la pointe vers la visée (horizontale).
                        Vector3 f = H.AvantCamera;
                        m_ConeGo.transform.rotation = Quaternion.LookRotation(f);
                    }
                    if (m_Depuis >= 0.25f && Time.time >= m_TicCone)
                    {
                        m_TicCone = Time.time + 0.25f;
                        Vector3 o = pointeBaton != null ? pointeBaton.position : transform.position;
                        o.y = transform.position.y;
                        foreach (var s in Combat.Ennemis(o, H.AvantCamera, b.conePortee, b.coneDemiAngle))
                        {
                            H.Frapper(s, b.coneDegats * 0.25f, false, s.transform.position + Vector3.up, H.AvantCamera, false, true);
                            Brulure.Allumer(s, H);
                        }
                    }
                    if (!tenu || m_Mana <= 0f) ArreterCone();
                    break;
            }
        }

        void CommencerCone()
        {
            m_Action = Action.Cone;
            m_Depuis = 0f;
            m_TicCone = 0f;
            if (Anim != null) Anim.SetBool(P_Cone, true);
            AllumerCone(H.AvantCamera);
            Diffuser(E_ConeDebut);
        }

        void AllumerCone(Vector3 f)
        {
            if (m_ConeGo != null && f.sqrMagnitude > 0.001f) m_ConeGo.transform.rotation = Quaternion.LookRotation(f);
            if (m_Cone != null) foreach (var ps in m_Cone) ps.Play(false);
            if (m_SonCone != null) Destroy(m_SonCone);
            m_SonCone = AudioBank.Boucle(SonsDuJeu.Cone, pointeBaton != null ? pointeBaton : transform, 0.8f);
        }

        void EteindreCone()
        {
            if (m_Cone != null) foreach (var ps in m_Cone) ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            if (m_SonCone != null) { Destroy(m_SonCone); m_SonCone = null; }
        }

        void ArreterCone()
        {
            if (m_Action != Action.Cone) return;
            m_Action = Action.Aucune;
            m_Mana = Mathf.Max(0f, m_Mana);
            if (Anim != null) Anim.SetBool(P_Cone, false);
            EteindreCone();
            Diffuser(E_ConeFin);
        }

        // ----------------------------------------------------------------- Multijoueur (marionnette)

        public override void EffetDistant(int effet, Vector3 a, Vector3 b, float v)
        {
            switch (effet)
            {
                case E_Boule: AudioBank.Jouer(SonsDuJeu.BouleLancer, transform.position + Vector3.up * 1.5f, 0.8f); break;
                case E_ConeDebut: m_ConeDistant = true; AllumerCone(transform.forward); break;
                case E_ConeFin: m_ConeDistant = false; EteindreCone(); break;
                default: base.EffetDistant(effet, a, b, v); break;
            }
        }

        /// Marionnette : le cône suit l'orientation du héros (tourné vers la visée de son propriétaire).
        void LateUpdate()
        {
            if (m_ConeDistant && m_ConeGo != null) m_ConeGo.transform.rotation = Quaternion.LookRotation(transform.forward);
        }

        public override void Interrompre()
        {
            ArreterCone();
            m_Action = Action.Aucune;
        }

        public override EtatEmplacement Emplacement(int i, out float restant, out float total)
        {
            restant = total = 0f;
            switch (i)
            {
                case 0: return m_Action == Action.Boule ? EtatEmplacement.Actif : EtatEmplacement.Pret;
                case 1: return m_Action == Action.Cone ? EtatEmplacement.Actif : m_Mana < B.coneMana * 0.25f ? EtatEmplacement.Indisponible : EtatEmplacement.Pret;
                default: return EtatEmplacement.Vide;
            }
        }
    }
}
