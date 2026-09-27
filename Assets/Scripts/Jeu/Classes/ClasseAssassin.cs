using System.Collections.Generic;
using UnityEngine;

namespace Deathless.Jeu
{
    /// Assassin (dague, arbalète dans le dos). Passifs (wiki) : hors combat, marcher sans sprinter fait passer en marche
    /// discrète et en mode furtif ; courir, attaquer ou être repéré en fait sortir ; dans la fumée d'une grenade, il
    /// redevient furtif même en combat. Coup non détecté ×2, dans le dos ×3, les deux ×5 (meilleur critique).
    /// Dague (RT), arbalète en main et visée (LT maintenu, RT tire ; critique seulement à la tête ; recharge 6 s),
    /// grenade fumigène (LB : une, qui revient 20 s après, nuage d'environ 5 s). RB : Pas de l'ombre (27/09/2026,
    /// wiki : classe-assassin.md) : bond de 7 m en 0,15 s vers la visée, invulnérable, à travers les ennemis (pas les
    /// murs), qui s'arrête 1 m derrière l'ennemi visé face à son dos ; il ne fait pas sortir du mode furtif. Passif
    /// Exécution : un coup de dague sur un ennemi commun sous 30 % de vie l'achève net (élite ou boss : ×3) et remet la
    /// recharge du bond à zéro.
    public class ClasseAssassin : ClasseHeros
    {
        enum Action { Aucune, Dague, Tir, Grenade, Bond }

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
        float m_RechargeArbalete, m_RechargeGrenade, m_RechargePasOmbre;
        bool m_Arbalete;
        bool m_GrenadeTenue, m_GrenadeLancee;
        Vector3 m_CibleGrenade;
        ModeFurtif m_Visuel;
        Fumigene m_Fumigene;
        WeaponStyle m_Style;
        GameObject m_Modele;
        // Pas de l'ombre : départ, arrivée, durée réelle du bond, ennemi visé (face à son dos à l'arrivée).
        Vector3 m_BondDepart, m_BondArrivee;
        float m_BondDuree;
        Squelette m_BondCible;
        GemmesVolantes m_Gemmes;

        static readonly List<Fumigene> s_Fumees = new List<Fumigene>();
        static readonly int P_Sneaking = Animator.StringToHash("Sneaking");
        static readonly int P_Stab = Animator.StringToHash("Stab");
        static readonly int P_Crossbow = Animator.StringToHash("Crossbow");
        static readonly int P_Shoot = Animator.StringToHash("Shoot");
        static readonly int P_Throw = Animator.StringToHash("Throw");
        static readonly int P_PasOmbre = Animator.StringToHash("PasOmbre");
        static readonly RaycastHit[] s_Hits = new RaycastHit[24];

        /// Thème Ombre (traînée du bond, éclat de l'exécution) : base, vif, cœur.
        static Color[] PaletteOmbre => VfxPalette.Cache("ClasseAssassin.Ombre", () => new[]
        {
            VfxPalette.Couleur(VfxTheme.Ombre, VfxRole.Base, new Color(0.17f, 0.09f, 0.25f)),
            VfxPalette.Couleur(VfxTheme.Ombre, VfxRole.Vif, new Color(0.36f, 0.23f, 0.54f)),
            VfxPalette.Couleur(VfxTheme.Ombre, VfxRole.Coeur, new Color(0.65f, 0.54f, 0.84f)),
        });

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
            if (EffetsJeu.Gemmes != null) m_Gemmes = GemmesVolantes.Creer("Ombre_" + name, EffetsJeu.Gemmes, 160);
        }

        void OnDestroy()
        {
            if (m_Fumigene != null) { s_Fumees.Remove(m_Fumigene); Destroy(m_Fumigene.gameObject); }
            if (m_Gemmes != null) Destroy(m_Gemmes.gameObject);
        }

        public override bool Occupe => m_Action != Action.Aucune;
        public override bool PeutEsquiver => m_Action != Action.Bond;
        public override float FacteurVitesse => m_Action == Action.Grenade ? 0.3f : m_Action != Action.Aucune ? 0.4f : m_Arbalete ? 0.5f : 1f;
        public override bool BloqueSprint => m_Arbalete;
        public override bool FaceVisee => m_Arbalete || (m_Action != Action.Aucune && m_Action != Action.Bond);
        public override bool HautDuCorps => m_Arbalete;
        public override bool Furtif => m_Furtif;
        public override float FacteurAnimation => m_Furtif ? 1f : 1f;

        Transform m_Arbal;
        Transform m_Main;   // socket handslot.r (grenade tenue puis lancée), cherché une fois

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
        // Effets diffusés aux autres postes (ClasseHeros.Diffuser).
        const int E_Dague = 1, E_DagueVide = 2, E_Arbalete = 3, E_PasOmbre = 4, E_Execution = 5;

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
                case "Skill2": PasDeLOmbre(); break;
            }
        }

        // ----------------------------------------------------------------- Pas de l'ombre (RB, 27/09/2026)

        /// Bond de pasOmbreDistance m en pasOmbreDuree s vers la visée. Réticule sur un ennemi à portée : arrivée à
        /// pasOmbreArret m derrière lui, face à son dos. Un mur (SphereCast, ennemis et héros ignorés) arrête le bond
        /// avant. Invulnérable pendant, à travers les ennemis (TraverserEnnemis). Ne fait pas sortir du mode furtif.
        void PasDeLOmbre()
        {
            if (!H.PeutAgir || m_RechargePasOmbre > 0f || m_Arbalete) return;
            var b = B;
            Vector3 origine = transform.position;
            Vector3 dir = H.AvantCamera; dir.y = 0f;
            if (dir.sqrMagnitude < 0.01f) dir = transform.forward;
            dir.Normalize();
            float distance = b.pasOmbreDistance;
            m_BondCible = null;
            Combat.PointVise(H.CameraJeu, transform, b.pasOmbrePorteeCible, out var vise);
            var sq = vise != null ? vise.GetComponent<Squelette>() : null;
            if (sq != null && sq.Vivant)
            {
                Vector3 avantCible = sq.transform.forward; avantCible.y = 0f;
                if (avantCible.sqrMagnitude < 0.01f) avantCible = -dir;
                Vector3 arrivee = sq.transform.position - avantCible.normalized * b.pasOmbreArret;
                Vector3 d = arrivee - origine; d.y = 0f;
                if (d.magnitude <= b.pasOmbreDistance + b.pasOmbreArret && d.sqrMagnitude > 0.04f)
                {
                    m_BondCible = sq;
                    dir = d.normalized;
                    distance = Mathf.Min(d.magnitude, b.pasOmbreDistance);
                }
            }
            // Murs : premier obstacle qui n'est ni un ennemi ni un héros, le bond s'arrête 0,5 m avant.
            int n = Physics.SphereCastNonAlloc(origine + Vector3.up * 1f, 0.35f, dir, s_Hits, distance, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                var c = s_Hits[i].collider;
                if (c.transform.IsChildOf(transform) || c.GetComponentInParent<Sante>() != null) continue;
                distance = Mathf.Min(distance, Mathf.Max(0f, s_Hits[i].distance - 0.5f));
            }
            m_BondDepart = origine;
            m_BondArrivee = origine + dir * distance;
            m_BondDuree = Mathf.Max(0.05f, b.pasOmbreDuree * Mathf.Clamp01(distance / Mathf.Max(0.1f, b.pasOmbreDistance)));
            m_RechargePasOmbre = b.pasOmbreRecharge * Facteur(3);
            m_Action = Action.Bond;
            m_Depuis = 0f;
            H.Tourner(dir);
            H.Invulnerable(m_BondDuree + 0.05f);
            H.TraverserEnnemis(true);
            if (Anim != null) H.Declencher(P_PasOmbre);
            EffetPasOmbre(m_BondDepart, m_BondArrivee, m_BondDuree);
            Diffuser(E_PasOmbre, m_BondDepart, m_BondArrivee, m_BondDuree);
            if (H.Partie != null) H.Partie.Journal("Pas de l'ombre : " + distance.ToString("F1") + " m" + (m_BondCible != null ? " derrière " + m_BondCible.type : ""));
        }

        void FinBond()
        {
            H.TraverserEnnemis(false);
            if (m_BondCible != null && m_BondCible.Vivant)
            {
                // Face à son dos : le corps et la caméra se tournent vers lui (sinon le coup suivant, donné vers la
                // visée, partirait dans l'autre sens).
                Vector3 vers = m_BondCible.transform.position - transform.position; vers.y = 0f;
                H.Tourner(vers);
                if (H.CameraEpaule != null && vers.sqrMagnitude > 0.01f) H.CameraEpaule.lacet = Mathf.Atan2(vers.x, vers.z) * Mathf.Rad2Deg;
            }
            m_BondCible = null;
            m_Action = Action.Aucune;
        }

        /// Traînée de gemmes Ombre semées le long du bond, nées au fil du trajet (retard), qui dérivent vers l'arrière et
        /// s'éteignent ; jouée ici et sur la marionnette (E_PasOmbre).
        void EffetPasOmbre(Vector3 a, Vector3 b, float duree)
        {
            AudioBank.Jouer(SonsDuJeu.Esquive, a + Vector3.up, 0.7f);
            AudioBank.Jouer(SonsDuJeu.FurtifEntree, a + Vector3.up, 0.5f);
            if (m_Gemmes == null) return;
            var pal = PaletteOmbre;
            Vector3 arriere = (a - b).normalized;
            int n = Mathf.Clamp(Mathf.RoundToInt(Vector3.Distance(a, b) * 7f), 10, 70);
            for (int i = 0; i < n; i++)
            {
                float k = (i + 0.5f) / n;
                Vector3 p = Vector3.Lerp(a, b, k) + Vector3.up * Random.Range(0.25f, 1.55f) + Random.insideUnitSphere * 0.28f;
                Vector3 v = arriere * Random.Range(0.3f, 1.2f) + Random.insideUnitSphere * 0.6f;
                m_Gemmes.Emettre(p, v, Random.Range(0.06f, 0.15f), Random.Range(0.35f, 0.65f), pal[Random.Range(0, pal.Length)], 0f, 2.5f, 0.03f, 0.3f, default, k * duree);
            }
        }

        /// Petit éclat Ombre d'une exécution, au point du coup.
        void EffetExecution(Vector3 point)
        {
            AudioBank.Jouer(SonsDuJeu.FurtifSortie, point, 0.8f);
            if (m_Gemmes == null) return;
            var pal = PaletteOmbre;
            for (int i = 0; i < 22; i++)
            {
                Vector3 v = Random.onUnitSphere * Random.Range(1.5f, 3.5f); v.y = Mathf.Abs(v.y) + 0.5f;
                m_Gemmes.Emettre(point + Random.insideUnitSphere * 0.15f, v, Random.Range(0.07f, 0.16f), Random.Range(0.4f, 0.7f), pal[Random.Range(0, pal.Length)], 6f, 1.5f, 0.03f, 0.4f);
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
            var cibles = Cibles(transform.position, H.AvantCamera, B.daguePortee + 0.8f, 70f);
            H.Tourner(cibles.Count > 0 ? cibles[0].transform.position - transform.position : H.AvantCamera);
            if (Anim != null) H.Declencher(P_Stab);
        }

        void PorterDague()
        {
            m_CoupPorte = true;
            var b = B;
            var cibles = Cibles(transform.position, transform.forward, b.daguePortee, b.dagueDemiAngle);
            if (cibles.Count > 0)
            {
                var s = cibles[0];
                bool dos = Combat.DansLeDos(s.transform, transform.position, b.angleDos);
                bool furtif = m_FurtifAuCoup;
                float mult = furtif && dos ? b.critiqueFurtifDos : dos ? b.critiqueDos : furtif ? b.critiqueFurtif : 1f;
                bool critique = furtif || dos;
                Vector3 point = s.transform.position + Vector3.up * 1.1f;
                Vector3 dir = (s.transform.position - transform.position).normalized;
                float degats = b.dagueDegats * Facteur(0) * mult;
                // Exécution (27/09/2026) : ennemi commun sous le seuil achevé net (l'hôte le vérifie sur sa vie), élite ou
                // boss ×executionElite (le meilleur des deux facteurs, pas le produit) ; la recharge du bond repart à zéro.
                var sq = s.GetComponent<Squelette>();
                bool execution = sq != null && sq.Vivant && s.Ratio < b.executionSeuil;
                if (execution)
                {
                    if (sq.EliteOuBoss) degats = b.dagueDegats * Facteur(0) * Mathf.Max(mult, b.executionElite);
                    else degats = Mathf.Max(degats, s.Pv + 1f);
                    critique = true;
                }
                if (critique) Critique(point, -dir, furtif && dos);
                H.Frapper(s, degats, critique, point, dir, false, false, execution);
                AudioBank.Jouer(SonsDuJeu.Dague, point, 0.9f);
                Diffuser(E_Dague, point);
                if (execution)
                {
                    m_RechargePasOmbre = 0f;
                    EffetExecution(point);
                    Diffuser(E_Execution, point);
                }
                if (H.Partie != null && critique) H.Partie.Journal("Dague : " + (execution ? "exécution" + (sq.EliteOuBoss ? " ×" + b.executionElite + " (élite ou boss)" : " (achevé)") + ", " : "") + (furtif && dos ? "meilleur critique ×" : "critique ×") + mult + (dos ? " (dos)" : "") + (furtif ? " (furtif)" : ""));
            }
            else { AudioBank.Jouer(SonsDuJeu.EpeeElan, transform.position + Vector3.up, 0.5f); Diffuser(E_DagueVide); }
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
            m_RechargeArbalete = b.arbaleteRecharge * Facteur(1);
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
                if (tete) Critique(point, -dir, false);
                H.Frapper(s, b.arbaleteDegats * (tete ? b.arbaleteTete : 1f), tete, point, dir);
            });
            AudioBank.Jouer(SonsDuJeu.ArbaleteTir, depart, 1f);
            Invoke(nameof(SonRecharge), 0.6f);
            Diffuser(E_Arbalete, depart);
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
            m_RechargeGrenade = b.grenadeRecharge * Facteur(2);
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
            m_RechargePasOmbre = Mathf.Max(0f, m_RechargePasOmbre - dt);
            var b = B;
            // Arbalète en main tant que LT est maintenu (bascule dague ↔ arbalète).
            bool arbalete = H.Vivant && H.EnJeu && H.Entrees.GardeMaintenue && m_Action != Action.Grenade && m_Action != Action.Dague && m_Action != Action.Bond;
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
                    if (m_Main == null) m_Main = MannequinEquip.Trouver(transform, "handslot.r");
                    var main = m_Main;
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
                case Action.Bond:
                    if (m_Depuis >= m_BondDuree) FinBond();
                    break;
            }
        }

        /// Bond du Pas de l'ombre : trajet rectiligne à vitesse constante entre le départ et l'arrivée (ce qui reste à
        /// parcourir, image par image) ; le décor arrête le CharacterController (SurCollisionCote).
        public override bool DeplacementImpose(float dt, out Vector3 vitesse)
        {
            vitesse = Vector3.zero;
            if (m_Action != Action.Bond) return false;
            float k = Mathf.Clamp01(m_Depuis / Mathf.Max(0.01f, m_BondDuree));
            Vector3 voulu = Vector3.Lerp(m_BondDepart, m_BondArrivee, k);
            Vector3 d = voulu - transform.position; d.y = 0f;
            vitesse = d / Mathf.Max(dt, 0.001f);
            return true;
        }

        public override void SurCollisionCote() { if (m_Action == Action.Bond && m_Depuis > 0.03f) FinBond(); }

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
            m_CibleGrenade = cible;
            Invoke(nameof(SonFumee), duree);
        }

        public override void EffetDistant(int effet, Vector3 a, Vector3 b, float v)
        {
            switch (effet)
            {
                case E_Dague: AudioBank.Jouer(SonsDuJeu.Dague, a, 0.9f); break;
                case E_DagueVide: AudioBank.Jouer(SonsDuJeu.EpeeElan, transform.position + Vector3.up, 0.5f); break;
                case E_Arbalete: AudioBank.Jouer(SonsDuJeu.ArbaleteTir, a, 1f); Invoke(nameof(SonRecharge), 0.6f); break;
                case E_PasOmbre: EffetPasOmbre(a, b, v); break;
                case E_Execution: EffetExecution(a); break;
                default: base.EffetDistant(effet, a, b, v); break;
            }
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
            if (m_Action == Action.Bond) { H.TraverserEnnemis(false); m_BondCible = null; }
            m_Action = Action.Aucune;
        }

        public override EtatEmplacement Emplacement(int i, out float restant, out float total)
        {
            restant = total = 0f;
            switch (i)
            {
                case 0: return m_Action == Action.Dague ? EtatEmplacement.Actif : EtatEmplacement.Pret;
                case 1: return Recharge(m_RechargeArbalete, B.arbaleteRecharge * Facteur(1), out restant, out total, m_Arbalete && m_RechargeArbalete <= 0f);
                case 2: return Recharge(m_RechargeGrenade, B.grenadeRecharge * Facteur(2), out restant, out total, m_Action == Action.Grenade);
                case 3: return Recharge(m_RechargePasOmbre, B.pasOmbreRecharge * Facteur(3), out restant, out total, m_Action == Action.Bond);
                default: return EtatEmplacement.Vide;
            }
        }
    }
}
