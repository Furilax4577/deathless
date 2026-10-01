using System.Collections;
using System.Collections.Generic;
using Deathless.UI.Donnees;
using UnityEngine;

namespace Deathless.Jeu
{
    /// Mage (bâton), style de magie feu (le seul pour l'instant ; le style est une donnée de la classe, ClassesJeu.element,
    /// pas son identité) : boule de feu (RT) qui explose à l'impact et allume la brûlure, cône de flammes maintenu (LT)
    /// tant qu'il reste du mana, qui ralentit ce qu'il touche. Kit refondu le 01/10/2026 (décision de Quentin, refonte (c)
    /// de Docs/equilibrage-classes.md) : grande boule de feu (LB : lente, explosion de 5 m, +1 palier de brûlure à tous les
    /// touchés) et mur de flammes (RB : ligne de feu de 8 m posée devant le mage, 5 s, brûle et ralentit qui le traverse).
    /// Mana (wiki) : 100, remonte de 3 par seconde (pas pendant le cône), +4 à chaque ennemi touché par la boule ; le cône
    /// en consomme tant qu'il est maintenu, la grande boule et le mur ont un coût fixe et une recharge.
    public class ClasseMage : ClasseHeros
    {
        enum Action { Aucune, Boule, Cone, GrandeBoule, Mur }

        public Transform pointeBaton;

        public override string Id => "mage";
        public override float PvMax => B.magePV;
        public override float Vitesse => B.mageVitesse;

        Action m_Action;
        float m_Depuis;
        float m_Mana;
        float m_DerniereBoule = -99f;
        bool m_BouleLancee;
        ParticleSystem[] m_Cone;
        GameObject m_ConeGo;
        AudioSource m_SonCone;
        float m_TicCone;
        float m_RechargeGrande, m_RechargeMur;
        bool m_GrandeLancee, m_MurPose;

        // Effets diffusés aux autres postes (ClasseHeros.Diffuser).
        const int E_Boule = 1, E_ConeDebut = 2, E_ConeFin = 3, E_GrandeBoule = 4, E_Mur = 5;
        bool m_ConeDistant;

        static readonly int P_Attack1 = Animator.StringToHash("Attack1");
        static readonly int P_Cone = Animator.StringToHash("Cone");
        static readonly int P_GrandeBoule = Animator.StringToHash("GrandeBoule");
        static readonly int P_Mur = Animator.StringToHash("Mur");

        /// Tests (ScenariosClasses « mage_kit ») : dernier mur posé (centre au sol, axe de la ligne).
        public Vector3 DernierMurCentre { get; private set; }
        public Vector3 DernierMurAxe { get; private set; }

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
        public override float FacteurVitesse
        {
            get
            {
                switch (m_Action)
                {
                    case Action.Cone: return B.coneVitesse;
                    case Action.Boule: return 0.6f;
                    case Action.GrandeBoule: return 0.35f;
                    case Action.Mur: return 0.3f;
                    default: return 1f;
                }
            }
        }
        public override bool BloqueSprint => m_Action != Action.Aucune;
        public override bool FaceVisee => m_Action != Action.Aucune;
        public override bool HautDuCorps => m_Action != Action.Aucune;
        public override void RemplirJauge() { m_Mana = B.manaMax; }
        public override JaugeClasse Jauge => JaugeClasse.Mana;
        public override float ValeurJauge => m_Mana;
        public override float JaugeMax => B.manaMax;

        /// Améliorations (ArbreCompetences, une par action) : 0 boule, 1 cône, 2 grande boule, 3 mur.
        float RechargeGrandeTotale => B.grandeBouleRecharge * Facteur(2);
        float DureeMur => B.murDuree * Facteur(3);

        public override void SurAction(string action)
        {
            switch (action)
            {
                case "AttackPrimary": Boule(); break;
                case "Skill1": GrandeBoule(); break;   // LB
                case "Skill2": Mur(); break;           // RB
            }
        }

        // ----------------------------------------------------------------- Boule de feu (RT)

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

        Vector3 DepartSort => pointeBaton != null ? pointeBaton.position : transform.position + Vector3.up * 1.6f + transform.forward * 0.6f;

        void LancerBoule()
        {
            m_BouleLancee = true;
            var b = B;
            Vector3 cible = Combat.PointVise(H.CameraJeu, transform, b.boulePortee, out Sante visee);
            if (H.Partie != null) H.Partie.Journal("Boule de feu : visée à " + Vector3.Distance(transform.position, cible).ToString("F1") + " m" + (visee != null ? " sur un ennemi" : ""));
            ProjectileJeu.Tirer(ProjectileJeu.Genre.BouleDeFeu, DepartSort, cible, b.bouleVitesse, b.boulePortee + 5f, transform, Exploser);
        }

        void Exploser(Vector3 point, Vector3 dir, Sante direct)
        {
            var b = B;
            var fx = EffetsJeu.Instance;
            if (fx != null && fx.gemmes != null) ExplosionFeu.Jouer(point, 2.5f, fx.gemmes);
            AudioBank.Jouer(SonsDuJeu.BouleExplosion, point, 1f);
            int n = 0;
            bool crit = TirerCritique(point, dir, direct);
            float k = crit ? b.mageCritiqueMultiplicateur : 1f;
            if (direct != null && !direct.Mort) { Toucher(direct, b.bouleDegats * Facteur(0) * k, point, dir, crit); n++; }
            foreach (var s in Cibles(point, dir, b.bouleRayon, 180f))
            {
                if (s == direct) continue;
                Toucher(s, b.bouleDegatsZone * Facteur(0) * k, point, dir, crit);
                n++;
            }
            if (H.Partie != null) H.Partie.Journal("Boule de feu : explose à " + Vector3.Distance(transform.position, point).ToString("F1") + " m" + (direct != null ? ", coup direct" : "") + ", " + n + " touchés" + (crit ? ", critique" : "") + ", mana " + m_Mana.ToString("F0"));
        }

        /// Coup critique du Mage (décidé le 01/10/2026) : GameBalance.mageCritiqueChance par boule (ou grande boule), tiré une
        /// fois à l'explosion, par le poste qui tient ce héros (comme les critiques des autres classes ; l'hôte applique les
        /// dégâts). Marque et son communs (Combat.Critique, rejoués chez les autres par Diffuser), sur la cible du coup direct
        /// ou au point d'explosion. La brûlure ne critique pas (Brulure.Allumer / Monter inchangés).
        bool TirerCritique(Vector3 point, Vector3 dir, Sante direct)
        {
            bool crit = !H.Distant && Random.value < B.mageCritiqueChance;
            if (crit) Critique(direct != null && !direct.Mort ? direct.transform.position + Vector3.up * 1.1f : point, -dir, false);
            return crit;
        }

        void Toucher(Sante s, float degats, Vector3 point, Vector3 dir, bool critique)
        {
            float reel = H.Frapper(s, degats, critique, point, dir, true);
            if (reel > 0f) m_Mana = Mathf.Min(B.manaMax, m_Mana + B.manaParTouche);   // wiki : bonus par ennemi touché
            Brulure.Allumer(s, H, B.brulureRemplissageBoule);   // brûlure en paliers : la boule remplit beaucoup d'un coup
        }

        // ----------------------------------------------------------------- Grande boule de feu (LB, 01/10/2026)

        bool PeutLancer(float recharge, float cout) => H.PeutAgir && m_Action == Action.Aucune && recharge <= 0f && m_Mana >= cout;

        void GrandeBoule()
        {
            var b = B;
            if (!PeutLancer(m_RechargeGrande, b.grandeBouleMana)) return;
            m_Mana -= b.grandeBouleMana;
            m_RechargeGrande = RechargeGrandeTotale;
            m_Action = Action.GrandeBoule;
            m_Depuis = 0f;
            m_GrandeLancee = false;
            H.Tourner(H.AvantCamera);
            if (Anim != null) H.Declencher(P_GrandeBoule);
            AudioBank.Jouer(SonsDuJeu.GrandeBouleLancer, transform.position + Vector3.up * 1.5f, 0.9f);
            Diffuser(E_GrandeBoule);
        }

        void LancerGrandeBoule()
        {
            m_GrandeLancee = true;
            var b = B;
            Vector3 cible = Combat.PointVise(H.CameraJeu, transform, b.boulePortee, out Sante visee);
            if (H.Partie != null) H.Partie.Journal("Grande boule de feu : visée à " + Vector3.Distance(transform.position, cible).ToString("F1") + " m" + (visee != null ? " sur un ennemi" : ""));
            ProjectileJeu.Tirer(ProjectileJeu.Genre.GrandeBouleDeFeu, DepartSort, cible, b.grandeBouleVitesse, b.boulePortee + 5f, transform, ExploserGrande);
        }

        /// Visuel et son de l'explosion de la grande boule (tous postes : ici et ProjectileJeu.TirerVisuel) : l'explosion de
        /// la boule de feu (ExplosionFeu, gemmes Feu) à l'échelle du rayon, doublée d'un cœur plus dense, et une secousse.
        public static void ExplosionGrandeBoule(Vector3 point)
        {
            var b = GameBalance.Courant;
            var fx = EffetsJeu.Instance;
            if (fx != null && fx.gemmes != null)
            {
                ExplosionFeu.Jouer(point, b.grandeBouleRayon, fx.gemmes);
                ExplosionFeu.Jouer(point, b.grandeBouleRayon * 0.5f, fx.gemmes);
            }
            AudioBank.Jouer(SonsDuJeu.GrandeBouleExplosion, point, 1f);
            var p = Partie.Instance;
            var h = p != null ? p.HerosLocal : null;
            var cam = h != null ? h.CameraJeu : null;
            if (cam != null && Vector3.Distance(cam.transform.position, point) < 25f) SecousseCamera.Jouer(cam, 0.08f, 0.25f);
        }

        void ExploserGrande(Vector3 point, Vector3 dir, Sante direct)
        {
            var b = B;
            ExplosionGrandeBoule(point);
            int n = 0;
            bool crit = TirerCritique(point, dir, direct);
            float k = crit ? b.mageCritiqueMultiplicateur : 1f;
            if (direct != null && !direct.Mort) { ToucherGrande(direct, b.grandeBouleDegats * k, point, dir, crit); n++; }
            foreach (var s in Cibles(point, dir, b.grandeBouleRayon, 180f))
            {
                if (s == direct) continue;
                ToucherGrande(s, b.grandeBouleDegatsZone * k, point, dir, crit);
                n++;
            }
            if (H.Partie != null) H.Partie.Journal("Grande boule de feu : explose à " + Vector3.Distance(transform.position, point).ToString("F1") + " m" + (direct != null ? ", coup direct" : "") + ", " + n + " touchés" + (crit ? ", critique" : "") + ", mana " + m_Mana.ToString("F0"));
        }

        void ToucherGrande(Sante s, float degats, Vector3 point, Vector3 dir, bool critique)
        {
            H.Frapper(s, degats, critique, point, dir, false);
            Brulure.Monter(s, H);   // +1 palier de brûlure d'un coup (décidé le 01/10/2026)
        }

        // ----------------------------------------------------------------- Mur de flammes (RB, 01/10/2026)

        void Mur()
        {
            var b = B;
            if (!PeutLancer(m_RechargeMur, b.murMana)) return;
            m_Mana -= b.murMana;
            m_RechargeMur = b.murRecharge;
            m_Action = Action.Mur;
            m_Depuis = 0f;
            m_MurPose = false;
            H.Tourner(H.AvantCamera);
            if (Anim != null) H.Declencher(P_Mur);
        }

        void PoserMur()
        {
            m_MurPose = true;
            var b = B;
            Vector3 f = H.AvantCamera; f.y = 0f;
            if (f.sqrMagnitude < 0.0001f) f = transform.forward;
            f.Normalize();
            Vector3 centre = MurDeFlammes.Sol(transform.position + f * b.murDistance, transform.position.y);
            Vector3 axe = Vector3.Cross(Vector3.up, f).normalized;   // perpendiculaire à la visée
            float duree = DureeMur;
            DernierMurCentre = centre;
            DernierMurAxe = axe;
            JouerMur(centre, axe, duree);
            StartCoroutine(Brasier(centre, axe, duree));
            Diffuser(E_Mur, centre, axe, duree);
            if (H.Partie != null) H.Partie.Journal("Mur de flammes : posé à " + Vector3.Distance(transform.position, centre).ToString("F1") + " m, " + b.murLongueur.ToString("F0") + " m de long, " + duree.ToString("F1") + " s, mana " + m_Mana.ToString("F0"));
        }

        /// Visuel et son du mur (ici et chez les autres postes).
        void JouerMur(Vector3 centre, Vector3 axe, float duree)
        {
            var b = B;
            var fx = EffetsJeu.Instance;
            var mur = fx != null ? MurDeFlammes.Jouer(centre, axe, b.murLongueur, duree, fx.gemmes) : null;
            AudioBank.Jouer(SonsDuJeu.MurPose, centre + Vector3.up, 1f);
            if (mur != null)
            {
                var son = AudioBank.Boucle(SonsDuJeu.MurBoucle, mur.transform, 0.7f);
                if (son != null) Destroy(son, duree + 0.2f);
            }
        }

        /// Poste du mage (comme la nuée du Rôdeur) : tant que le mur brûle, 4 tics par seconde, les ennemis dans la bande
        /// (longueur × épaisseur) sont ralentis (Ralenti court, relancé) ; en y entrant ils montent d'un palier de brûlure,
        /// puis d'un autre toutes les murIntervallePalier s s'ils y restent. Statuts relayés à l'hôte depuis un client.
        IEnumerator Brasier(Vector3 centre, Vector3 axe, float duree)
        {
            var b = B;
            var prochainPalier = new Dictionary<Sante, float>();
            Vector3 travers = Vector3.Cross(Vector3.up, axe);
            float demi = b.murLongueur * 0.5f;
            float fin = Time.time + duree;
            while (Time.time < fin)
            {
                foreach (var s in Cibles(centre, axe, demi + 1f, 180f))
                {
                    Vector3 d = s.transform.position - centre;
                    if (Mathf.Abs(Vector3.Dot(d, axe)) > demi + 0.4f || Mathf.Abs(Vector3.Dot(d, travers)) > b.murEpaisseur * 0.5f + 0.4f || Mathf.Abs(d.y) > 2.5f) continue;
                    Statuts.De(s)?.Ajouter(TypeStatut.Ralenti, b.murRalentiDuree, b.murRalenti, OrigineStatut.Joueur, H.Id);
                    if (!prochainPalier.TryGetValue(s, out float t) || Time.time >= t)
                    {
                        Brulure.Monter(s, H);
                        prochainPalier[s] = Time.time + b.murIntervallePalier;
                    }
                }
                yield return new WaitForSeconds(0.25f);
            }
        }

        // ----------------------------------------------------------------- Boucle

        public override void Temps(float dt)
        {
            m_RechargeGrande = Mathf.Max(0f, m_RechargeGrande - dt);
            m_RechargeMur = Mathf.Max(0f, m_RechargeMur - dt);
            if (m_Action != Action.Cone) m_Mana = Mathf.Min(B.manaMax, m_Mana + B.manaRegen * dt);
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
                case Action.GrandeBoule:
                    if (!m_GrandeLancee && m_Depuis >= b.grandeBouleInstant) LancerGrandeBoule();
                    if (m_Depuis >= Mathf.Max(b.grandeBouleDuree, b.grandeBouleInstant + 0.05f)) m_Action = Action.Aucune;
                    break;
                case Action.Mur:
                    if (!m_MurPose && m_Depuis >= b.murInstant) PoserMur();
                    if (m_Depuis >= Mathf.Max(b.murGeste, b.murInstant + 0.05f)) m_Action = Action.Aucune;
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
                        foreach (var s in Cibles(o, H.AvantCamera, b.conePortee, b.coneDemiAngle))
                        {
                            H.Frapper(s, b.coneDegats * 0.25f, false, s.transform.position + Vector3.up, H.AvantCamera, false, true);
                            Brulure.Allumer(s, H, b.brulureRemplissageCone);   // chaque tic remplit la jauge de brûlure
                            // Le cône ralentit (décidé le 01/10/2026) : Ralenti court, relancé à chaque tic tant qu'on y est.
                            if (b.coneRalenti > 0f && !s.Mort) Statuts.De(s)?.Ajouter(TypeStatut.Ralenti, b.coneRalentiDuree, b.coneRalenti, OrigineStatut.Joueur, H.Id);
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
                // La grande boule en vol et son explosion arrivent par ProjectileJeu.TirerVisuel (HerosReseau.Tir).
                case E_GrandeBoule: AudioBank.Jouer(SonsDuJeu.GrandeBouleLancer, transform.position + Vector3.up * 1.5f, 0.9f); break;
                case E_Mur: JouerMur(a, b, v); break;
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
            // Sort coupé avant de partir (esquive, étourdissement, mort) : mana et recharge rendus.
            if (m_Action == Action.GrandeBoule && !m_GrandeLancee) { m_Mana = Mathf.Min(B.manaMax, m_Mana + B.grandeBouleMana); m_RechargeGrande = 0f; }
            if (m_Action == Action.Mur && !m_MurPose) { m_Mana = Mathf.Min(B.manaMax, m_Mana + B.murMana); m_RechargeMur = 0f; }
            m_Action = Action.Aucune;
        }

        public override EtatEmplacement Emplacement(int i, out float restant, out float total)
        {
            restant = total = 0f;
            var b = B;
            switch (i)
            {
                case 0: return m_Action == Action.Boule ? EtatEmplacement.Actif : EtatEmplacement.Pret;
                case 1: return m_Action == Action.Cone ? EtatEmplacement.Actif : m_Mana < b.coneMana * 0.25f ? EtatEmplacement.Indisponible : EtatEmplacement.Pret;
                case 2:
                {
                    var e = Recharge(m_RechargeGrande, RechargeGrandeTotale, out restant, out total, m_Action == Action.GrandeBoule);
                    return e == EtatEmplacement.Pret && m_Mana < b.grandeBouleMana ? EtatEmplacement.Indisponible : e;
                }
                case 3:
                {
                    var e = Recharge(m_RechargeMur, b.murRecharge, out restant, out total, m_Action == Action.Mur);
                    return e == EtatEmplacement.Pret && m_Mana < b.murMana ? EtatEmplacement.Indisponible : e;
                }
                default: return EtatEmplacement.Vide;
            }
        }
    }
}
