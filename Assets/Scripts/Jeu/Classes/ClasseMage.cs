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
    /// Mana (wiki) : 100, remonte de 3 par seconde (pas pendant le cône) ; la boule de feu ne rend plus de mana depuis le
    /// 01/10/2026 (GameBalance.manaParTouche = 0, décision de Quentin) ; le cône en consomme tant qu'il est maintenu, la
    /// grande boule et le mur ont un coût fixe et une recharge.
    public class ClasseMage : ClasseHeros
    {
        enum Action { Aucune, Boule, Cone, GrandeBoule, Mur, Viser }
        /// Sorts à visée au sol (VisiereZone.Sort) : le lancement part à la confirmation (clic gauche, RT), pas à l'appui sur la compétence.
        const int V_Grande = 1, V_Mur = 2;

        public Transform pointeBaton;

        public override string Id => "mage";
        public override float PvMax => B.magePV;
        public override float Vitesse => B.mageVitesse;
        /// Attributs : les sorts du Mage sont des coups à distance (Perception) ; il tire ses critiques lui-même.
        public override bool CoupADistance => true;
        public override bool CritiquePropre => true;

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
        Vector3 m_PointGrande, m_CentreMur, m_AxeMur;   // point et axe confirmés par la visée

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
            m_Mana = JaugeMax;
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
                    case Action.Viser: return B.viseeZoneVitesse;
                    default: return 1f;
                }
            }
        }
        public override bool BloqueSprint => m_Action != Action.Aucune;
        public override bool FaceVisee => m_Action != Action.Aucune;
        public override bool HautDuCorps => m_Action != Action.Aucune;
        public override void RemplirJauge() { m_Mana = JaugeMax; }
        public override JaugeClasse Jauge => JaugeClasse.Mana;
        public override float ValeurJauge => m_Mana;
        public override float JaugeMax => B.manaMax * FacteurJauge;   // Esprit gagné : jauge plus grande

        /// Améliorations (ArbreCompetences, une par action) : 0 boule, 1 cône, 2 grande boule, 3 mur.
        float RechargeGrandeTotale => B.grandeBouleRecharge * Facteur(2) * RechargeEsprit;
        float DureeMur => B.murDuree * Facteur(3);

        public override void SurAction(string action)
        {
            if (ViseeSurAction(action)) return;   // visée ouverte : clic gauche / RT confirme, clic droit / LT annule
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
            if (m_Action != Action.Aucune || Time.time - m_DerniereBoule < B.bouleIntervalle / VitesseAttaque) return;
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
            // Attributs (01/10/2026) : + Chance et Perception gagnées (sorts du Mage : coups à distance).
            bool crit = !H.Distant && Random.value < B.mageCritiqueChance + Attributs.ChanceCritique(EtatJ, true);
            if (crit) Critique(direct != null && !direct.Mort ? direct.transform.position + Vector3.up * 1.1f : point, -dir, false);
            return crit;
        }

        void Toucher(Sante s, float degats, Vector3 point, Vector3 dir, bool critique)
        {
            float reel = H.Frapper(s, degats, critique, point, dir, true);
            // Regain par ennemi touché : 0 depuis le 01/10/2026 (seule la régénération passive reste) ; réglage gardé dans GameBalance.
            if (reel > 0f && B.manaParTouche > 0f) m_Mana = Mathf.Min(JaugeMax, m_Mana + B.manaParTouche);
            Brulure.Allumer(s, H, B.brulureRemplissageBoule);   // brûlure en paliers : la boule remplit beaucoup d'un coup
        }

        // ----------------------------------------------------------------- Grande boule de feu (LB, 01/10/2026)

        /// Peut lancer (ou ouvrir la visée de) un sort à coût : libre d'agir, pas d'autre action en cours (sauf une visée, qu'on
        /// peut changer pour une autre), recharge finie, assez de mana.
        bool PeutLancer(float recharge, float cout)
            => H.EtatCourant == Heros.Etat.Libre && H.Vivant && H.EnJeu && !H.EnTransit
               && (m_Action == Action.Aucune || m_Action == Action.Viser) && recharge <= 0f && m_Mana >= cout;

        /// Ouvre la visée au sol d'un sort (02/10/2026) : ni mana ni recharge avant la confirmation. Le geste de visée est
        /// la pose d'incantation du cône (couche haut du corps), sans particules ; le mage marche au ralenti.
        void Viser(int sort, ZoneVisee.Forme forme, float a, float b, float portee)
        {
            if (m_Action == Action.Viser) AnnulerVisee(false);   // autre sort visé : on change, sans bruit d'annulation
            m_Action = Action.Viser;
            m_Depuis = 0f;
            if (Anim != null) Anim.SetBool(P_Cone, true);
            CommencerVisee(sort, forme, VfxTheme.Feu, a, b, portee);
        }

        public override string LibelleVisee => SortVise == V_Mur ? "Mur de flammes" : "Grande boule de feu";

        protected override void ViseeAnnulee(int sort)
        {
            if (m_Action == Action.Viser)
            {
                m_Action = Action.Aucune;
                if (Anim != null) Anim.SetBool(P_Cone, false);
            }
        }

        protected override bool ViseeConfirmee(int sort, Vector3 point, Vector3 axe)
        {
            if (m_Action != Action.Viser) return false;
            return sort == V_Grande ? ConfirmerGrande(point) : ConfirmerMur(point, axe);
        }

        void GrandeBoule()
        {
            var b = B;
            if (EnVisee && SortVise == V_Grande) return;   // déjà en visée de ce sort
            if (!PeutLancer(m_RechargeGrande, b.grandeBouleMana)) return;
            Viser(V_Grande, ZoneVisee.Forme.Cercle, b.grandeBouleRayon, 0f, b.grandeBoulePortee);
        }

        /// Confirmation de la visée : le mana et la recharge partent ici (pas avant), la boule sera lancée sur ce point.
        bool ConfirmerGrande(Vector3 point)
        {
            var b = B;
            if (m_RechargeGrande > 0f || m_Mana < b.grandeBouleMana) return false;
            m_Mana -= b.grandeBouleMana;
            m_RechargeGrande = RechargeGrandeTotale;
            m_Action = Action.GrandeBoule;
            m_Depuis = 0f;
            m_GrandeLancee = false;
            m_PointGrande = point;
            if (Anim != null) { Anim.SetBool(P_Cone, false); H.Declencher(P_GrandeBoule); }
            AudioBank.Jouer(SonsDuJeu.GrandeBouleLancer, transform.position + Vector3.up * 1.5f, 0.9f);
            Diffuser(E_GrandeBoule);
            return true;
        }

        void LancerGrandeBoule()
        {
            m_GrandeLancee = true;
            var b = B;
            if (H.Partie != null) H.Partie.Journal("Grande boule de feu : lancée sur le point visé, à " + Vector3.Distance(transform.position, m_PointGrande).ToString("F1") + " m");
            // Tir en cloche sur le point confirmé (ProjectileJeu : rien n'arrête la boule en route, elle tombe au centre du cercle).
            ProjectileJeu.Tirer(ProjectileJeu.Genre.GrandeBouleDeFeu, DepartSort, m_PointGrande, b.grandeBouleVitesse, b.grandeBoulePortee + 5f, transform, ExploserGrande);
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
            m_TuesGrande = 0;
            // La boule tombe sur un point, pas sur un ennemi : le « coup direct » va à l'ennemi le plus proche du centre, s'il est
            // dans le cœur de l'explosion (GameBalance.grandeBouleCoeur) ; viser juste rapporte les gros dégâts.
            if (direct == null)
                foreach (var s in Cibles(point, dir, b.grandeBouleRayon, 180f))
                {
                    Vector3 e = s.transform.position - point; e.y = 0f;
                    if (e.magnitude <= b.grandeBouleCoeur) direct = s;
                    break;   // la liste est triée du plus proche au plus loin
                }
            bool crit = TirerCritique(point, dir, direct);
            float k = crit ? b.mageCritiqueMultiplicateur : 1f;
            if (direct != null && !direct.Mort) { ToucherGrande(direct, b.grandeBouleDegats * k, point, dir, crit); n++; }
            foreach (var s in Cibles(point, dir, b.grandeBouleRayon, 180f))
            {
                if (s == direct) continue;
                ToucherGrande(s, b.grandeBouleDegatsZone * k, point, dir, crit);
                n++;
            }
            Deathless.Succes.ServiceSucces.GrandeBoule(H, m_TuesGrande);   // succès « Grande boule, grand ménage »
            if (H.Partie != null) H.Partie.Journal("Grande boule de feu : explose à " + Vector3.Distance(transform.position, point).ToString("F1") + " m" + (direct != null ? ", coup direct" : "") + ", " + n + " touchés" + (crit ? ", critique" : "") + ", mana " + m_Mana.ToString("F0"));
        }

        int m_TuesGrande;   // ennemis achevés par la grande boule en cours d'explosion (succès)

        void ToucherGrande(Sante s, float degats, Vector3 point, Vector3 dir, bool critique)
        {
            float pvAvant = s.Pv;
            if (Deathless.Succes.ServiceSucces.Acheve(s, pvAvant, H.Frapper(s, degats, critique, point, dir, false))) m_TuesGrande++;
            Brulure.Monter(s, H);   // +1 palier de brûlure d'un coup (décidé le 01/10/2026)
        }

        // ----------------------------------------------------------------- Mur de flammes (RB, 01/10/2026)

        void Mur()
        {
            var b = B;
            if (EnVisee && SortVise == V_Mur) return;   // déjà en visée de ce sort
            if (!PeutLancer(m_RechargeMur, b.murMana)) return;
            // Visée au sol (02/10/2026) : la ligne de 8 m se pose en travers de la ligne mage → point visé, au plus à murPortee.
            Viser(V_Mur, ZoneVisee.Forme.Ligne, b.murLongueur, b.murEpaisseur, b.murPortee);
        }

        /// Confirmation de la visée du mur : le mana et la recharge partent ici ; le mur se posera à l'instant du geste.
        bool ConfirmerMur(Vector3 point, Vector3 axe)
        {
            var b = B;
            if (m_RechargeMur > 0f || m_Mana < b.murMana) return false;
            m_Mana -= b.murMana;
            m_RechargeMur = b.murRecharge * RechargeEsprit;
            m_Action = Action.Mur;
            m_Depuis = 0f;
            m_MurPose = false;
            m_CentreMur = point;
            m_AxeMur = axe;
            if (Anim != null) { Anim.SetBool(P_Cone, false); H.Declencher(P_Mur); }
            return true;
        }

        void PoserMur()
        {
            m_MurPose = true;
            var b = B;
            Vector3 centre = m_CentreMur;   // le point confirmé, déjà au sol (Combat.PointViseSol)
            Vector3 axe = m_AxeMur;         // en travers de la ligne mage → point visé
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
                Deathless.Succes.ServiceSucces.MurDeFlammes(H, prochainPalier.Count);   // ennemis entrés dans ce mur (succès)
                yield return new WaitForSeconds(0.25f);
            }
        }

        // ----------------------------------------------------------------- Boucle

        public override void Temps(float dt)
        {
            m_RechargeGrande = Mathf.Max(0f, m_RechargeGrande - dt);
            m_RechargeMur = Mathf.Max(0f, m_RechargeMur - dt);
            if (m_Action != Action.Cone) m_Mana = Mathf.Min(JaugeMax, m_Mana + B.manaRegen * dt);
        }

        public override void Maj(float dt, Vector3 dir)
        {
            var b = B;
            m_Depuis += dt;
            ViseeMaj();   // visée au sol : annulations (menu, étourdi…), indicateur
            bool tenu = H.Entrees.GardeMaintenue && GardeLibre;   // LT qui vient d'annuler une visée ne lance pas le cône
            if (m_Action == Action.Aucune && tenu && m_Mana >= b.coneMana * 0.25f) CommencerCone();
            switch (m_Action)
            {
                case Action.Boule:
                    if (!m_BouleLancee && m_Depuis >= b.bouleInstant / VitesseAttaque) LancerBoule();   // Agilité gagnée : lancer plus tôt
                    if (m_Depuis >= Mathf.Min(b.bouleIntervalle, 0.7f) / VitesseAttaque) m_Action = Action.Aucune;
                    break;
                case Action.GrandeBoule:
                    if (!m_GrandeLancee && m_Depuis >= b.grandeBouleInstant) LancerGrandeBoule();
                    if (m_Depuis >= Mathf.Max(b.grandeBouleDuree, b.grandeBouleInstant + 0.05f)) m_Action = Action.Aucune;
                    break;
                case Action.Mur:
                    if (!m_MurPose && m_Depuis >= b.murInstant) PoserMur();
                    if (m_Depuis >= Mathf.Max(b.murGeste, b.murInstant + 0.05f)) m_Action = Action.Aucune;
                    break;
                case Action.Viser:
                    break;   // la visée est tenue par ViseeMaj ; le lancement part à la confirmation
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
            AnnulerVisee(false);   // visée coupée (esquive, étourdissement, mort, menu) : rien n'a été dépensé
            // Sort coupé avant de partir (esquive, étourdissement, mort) : mana et recharge rendus.
            if (m_Action == Action.GrandeBoule && !m_GrandeLancee) { m_Mana = Mathf.Min(JaugeMax, m_Mana + B.grandeBouleMana); m_RechargeGrande = 0f; }
            if (m_Action == Action.Mur && !m_MurPose) { m_Mana = Mathf.Min(JaugeMax, m_Mana + B.murMana); m_RechargeMur = 0f; }
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
                    var e = Recharge(m_RechargeGrande, RechargeGrandeTotale, out restant, out total, m_Action == Action.GrandeBoule || (m_Action == Action.Viser && SortVise == V_Grande));
                    return e == EtatEmplacement.Pret && m_Mana < b.grandeBouleMana ? EtatEmplacement.Indisponible : e;
                }
                case 3:
                {
                    var e = Recharge(m_RechargeMur, b.murRecharge * RechargeEsprit, out restant, out total, m_Action == Action.Mur || (m_Action == Action.Viser && SortVise == V_Mur));
                    return e == EtatEmplacement.Pret && m_Mana < b.murMana ? EtatEmplacement.Indisponible : e;
                }
                default: return EtatEmplacement.Vide;
            }
        }
    }
}
