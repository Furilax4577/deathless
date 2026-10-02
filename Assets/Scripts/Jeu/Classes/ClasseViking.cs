using Deathless.UI.Donnees;
using UnityEngine;

namespace Deathless.Jeu
{
    /// Viking (hache à deux mains) : hache (RT, toutes les cibles de l'arc, léger recul), attaque tournante maintenue (LT,
    /// 3 s au plus puis recharge), rugissement (LB : provoque les squelettes proches), saut percutant (RB : bond de 5 m,
    /// onde de terre à l'impact). Refonte du 03/10/2026 (décidée par Quentin) : les compétences sont GRATUITES, limitées
    /// par leur recharge ; la rage monte au combat (il frappe, il est touché), redescend lentement hors combat, et à 100
    /// le joueur peut DÉCLENCHER la FURIE, l'ultime (action Ultimate : R3 / G ; décision de Quentin, 03/10/2026) : la jauge se vide à vitesse fixe pendant GameBalance.
    /// furieDuree, le modèle grossit, il court, tape et recharge plus vite, a plus de recul et fait plus de dégâts. Le
    /// rugissement pose Peau de fer (−35 % de dégâts subis 6 s) au moment du cri et se joue sur le haut du corps : le
    /// viking continue de marcher (lissage du 27/09/2026).
    public class ClasseViking : ClasseHeros
    {
        enum Action { Aucune, Attaque, Tournante, Rugissement, Saut }

        public Transform teteHache;

        public override string Id => "viking";
        public override float PvMax => B.vikingPV;
        public override float Vitesse => B.vikingVitesse * (m_Furie ? B.furieVitesse : 1f);
        public override float FacteurDegats => m_Furie ? B.furieDegats : 1f;

        Action m_Action;
        float m_Depuis;
        bool m_CoupPorte;
        int m_Combo;
        float m_DerniereAttaque = -99f;
        float m_Rage;
        float m_DernierCoup = -99f;
        float m_ProchainTic;
        bool m_VfxTournante;
        int m_ToursVent;
        float m_RechargeRugir, m_RechargeSaut, m_RechargeTournante;
        // Furie (03/10/2026) : état tenu par le propriétaire (répliqué par HerosReseau vers les marionnettes), échelle du
        // modèle lissée (appliquée au seul modèle visuel : ni capsule, ni caméra, ni ancres), aura de gemmes.
        bool m_Furie;
        float m_Echelle = 1f;
        Transform m_Modele;
        Vector3 m_EchelleModele = Vector3.one;
        bool m_ModeleTrouve;
        AuraFurie m_Aura;
        float m_CadenceAnim = 1f;
        bool m_ParamCadence;
        float m_CriFurieJusque;
        bool m_PretVu;
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
        static readonly int P_VitesseAttaque = Animator.StringToHash("VitesseAttaque");

        // Effets diffusés aux autres postes (ClasseHeros.Diffuser).
        const int E_Elan = 1, E_Hache = 2, E_TournanteDebut = 3, E_TournanteVfx = 4, E_TournanteTic = 5, E_TournanteFin = 6,
            E_RugirVfx = 7, E_RugirCri = 8, E_Saut = 9, E_TournanteVent = 10, E_FurieDebut = 11, E_FurieFin = 12;

        // Instants du geste (clips à vitesse 1, mesurés par VfxBench) et vitesses de lecture du contrôleur.
        const float CriClip = 1.63f, VitesseCri = 1.6f;
        const float DecollageClip = 0.19f, AtterrissageClip = 0.69f, ImpactClip = 0.79f, VitesseSaut = 1.2f;

        public override void Initialiser(Heros heros)
        {
            base.Initialiser(heros);
            m_Rage = B.rageMin;
            m_Aura = GetComponent<AuraFurie>() ?? gameObject.AddComponent<AuraFurie>();
            if (Anim != null) foreach (var p in Anim.parameters) if (p.nameHash == P_VitesseAttaque) m_ParamCadence = true;
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

        // ----------------------------------------------------------------- Furie (03/10/2026)

        /// En Furie (ultime) : propriétaire ou marionnette.
        public bool EnFurie => m_Furie;
        public override bool UltimeActif => m_Furie;
        /// Rage à 100 : la Furie est prête, le joueur peut la déclencher (Ultimate : R3, G).
        public override bool UltimePret => !m_Furie && m_Rage >= JaugeMax - 0.01f;
        /// La rage approche de la pleine jauge (signal du HUD) : GameBalance.furieSignal.
        public override bool UltimeProche => !m_Furie && m_Rage < JaugeMax - 0.01f && m_Rage >= JaugeMax * B.furieSignal;
        public override string NomUltime => "Furie";
        /// Échelle actuelle du modèle (1 normal, GameBalance.furieEchelle en Furie), pour les tests.
        public float EchelleModele => m_Echelle;
        /// Cadence de la hache : Agilité gagnée, puis Furie.
        float Cadence => VitesseAttaque * (m_Furie ? B.furieCadence : 1f);

        /// Ultimate (R3, G) : entre en Furie si la rage est pleine, sinon rien (pas de coût, pas de refus sonore). Possible
        /// pendant la hache ou la tournante (l'action continue), pas pendant le rugissement ni le saut percutant ; le cri
        /// (haut du corps) n'est joué que si le viking est libre de ses gestes.
        void DeclencherFurie()
        {
            if (!UltimePret || !H.Vivant || !H.EnJeu) return;
            if (m_Action == Action.Rugissement || m_Action == Action.Saut) return;
            EntrerFurie();
            if (m_Action == Action.Aucune && Anim != null) { H.Declencher(P_Rugir); m_CriFurieJusque = Time.time + 1.5f; }
        }

        void EntrerFurie()
        {
            if (m_Furie) return;
            m_Furie = true;
            m_Rage = JaugeMax;
            m_DernierCoup = Time.time;
            if (m_Aura != null) { m_Aura.Echelle(B.furieEchelle); m_Aura.Commencer(); }
            AudioBank.Jouer(SonsDuJeu.Rugissement, transform.position + Vector3.up * 1.6f, 0.9f);
            Diffuser(E_FurieDebut);
            if (H.Partie != null) H.Partie.Journal("Furie : entrée (rage pleine), " + B.furieDuree.ToString("F0") + " s");
        }

        /// Fin de la Furie (jauge vide, ou mort : `calme` faux, rien à jouer) : retour à la taille normale (lissé), aura éteinte.
        void SortirFurie(bool calme = true)
        {
            if (!m_Furie) return;
            m_Furie = false;
            m_Rage = B.rageMin;
            m_DernierCoup = Time.time;
            if (m_Aura != null) m_Aura.Arreter(calme);
            if (calme)
            {
                AudioBank.Jouer(SonsDuJeu.TournanteVent, transform.position + Vector3.up, 0.5f);   // souffle : retour au calme
                Diffuser(E_FurieFin);
            }
            if (H != null && H.Partie != null) H.Partie.Journal("Furie : fin");
        }

        /// Marionnette (HerosReseau) : état de Furie du propriétaire ; grossit et rougeoie ici aussi (sons par l'effet diffusé).
        public void ForcerFurieDistante(bool furie)
        {
            if (m_Furie == furie) return;
            m_Furie = furie;
            if (m_Aura != null) { m_Aura.Echelle(furie ? B.furieEchelle : 1f); if (furie) m_Aura.Commencer(); else m_Aura.Arreter(); }
        }

        public override void SurMort() { if (m_Furie) SortirFurie(false); m_Rage = B.rageMin; }

        /// Échelle du modèle visuel, lissée (toutes les instances : la marionnette suit l'état répliqué). Seul le modèle
        /// (enfant portant l'Animator) grossit : la capsule, la caméra et les ancres de la racine ne bougent pas.
        void LateUpdate()
        {
            if (!m_ModeleTrouve && Anim != null)
            {
                m_ModeleTrouve = true;
                if (Anim.transform != transform) { m_Modele = Anim.transform; m_EchelleModele = m_Modele.localScale; }
            }
            if (m_Modele == null) return;
            float voulu = m_Furie ? B.furieEchelle : 1f;
            if (Mathf.Approximately(m_Echelle, voulu)) return;
            m_Echelle = Mathf.Abs(m_Echelle - voulu) < 0.0015f ? voulu : Mathf.Lerp(m_Echelle, voulu, 1f - Mathf.Exp(-Time.deltaTime * 7f));
            m_Modele.localScale = m_EchelleModele * m_Echelle;
            if (m_Aura != null) m_Aura.Echelle(m_Echelle);
        }

        public override bool Occupe => m_Action != Action.Aucune;
        public override bool PeutEsquiver => m_Action == Action.Aucune || m_Action == Action.Attaque || m_Action == Action.Tournante;
        public override float FacteurVitesse => m_Action == Action.Tournante ? B.tournanteVitesse : m_Action == Action.Attaque ? 0.25f : m_Action == Action.Aucune || m_Action == Action.Rugissement ? 1f : 0f;
        /// Rugissement sur la couche haute (comme la charge du paladin) : les jambes marchent pendant le cri.
        public override bool HautDuCorps => m_Action == Action.Rugissement || Time.time < m_CriFurieJusque;
        public override void RemplirJauge() { m_Rage = JaugeMax; m_DernierCoup = Time.time; }   // tests : compte comme du combat (pas de baisse tout de suite)
        public override JaugeClasse Jauge => JaugeClasse.Rage;
        public override float ValeurJauge => m_Rage;
        public override float JaugeMax => B.rageMax * FacteurJauge;   // Esprit gagné : jauge plus grande

        public override void SurAction(string action)
        {
            switch (action)
            {
                case "AttackPrimary": Attaquer(); break;
                case "Skill1": Rugir(); break;
                case "Skill2": Sauter(); break;
                case "Ultimate": DeclencherFurie(); break;
            }
        }

        void Attaquer()
        {
            if (m_Action != Action.Aucune || Time.time - m_DerniereAttaque < B.hacheIntervalle / Cadence) return;
            m_Action = Action.Attaque;
            m_Depuis = 0f;
            m_DerniereAttaque = Time.time;
            m_CoupPorte = false;
            m_Combo = 1 - m_Combo;
            H.Tourner(H.AvantCamera);
            if (Anim != null)
            {
                // Cadence (Agilité, Furie) : l'animation de la hache suit (paramètre VitesseAttaque des états d'attaque).
                if (m_ParamCadence && Mathf.Abs(Cadence - m_CadenceAnim) > 0.001f) { m_CadenceAnim = Cadence; Anim.SetFloat(P_VitesseAttaque, m_CadenceAnim); }
                H.Declencher(m_Combo == 0 ? P_Attack1 : P_Attack2);
            }
            AudioBank.Jouer(SonsDuJeu.EpeeElan, transform.position + Vector3.up, 0.6f);
            Diffuser(E_Elan);
        }

        void Rugir()
        {
            if (!H.PeutAgir || m_RechargeRugir > 0f) return;   // gratuit, limité par sa recharge (03/10/2026)
            m_RechargeRugir = B.rugissementRecharge * Facteur(2) * RechargeEsprit;
            m_Action = Action.Rugissement;
            m_Depuis = 0f;
            m_Crie = m_VfxCri = false;
            if (Anim != null) H.Declencher(P_Rugir);
        }

        void Sauter()
        {
            if (!H.PeutAgir || m_RechargeSaut > 0f) return;   // gratuit, limité par sa recharge (03/10/2026)
            m_RechargeSaut = B.sautRecharge * RechargeEsprit;
            m_Action = Action.Saut;
            m_Depuis = 0f;
            m_Impact = false;
            m_DirSaut = H.AvantCamera;
            m_DepartSaut = transform.position;
            H.TraverserEnnemis(true);
            H.Tourner(m_DirSaut);
            if (Anim != null) H.Declencher(P_Saut);
        }

        public override void Temps(float dt)
        {
            // Furie : les recharges s'écoulent plus vite (furieRecharge).
            float dr = dt * (m_Furie ? B.furieRecharge : 1f);
            m_RechargeRugir = Mathf.Max(0f, m_RechargeRugir - dr);
            m_RechargeSaut = Mathf.Max(0f, m_RechargeSaut - dr);
            m_RechargeTournante = Mathf.Max(0f, m_RechargeTournante - dr);
            if (m_Furie)
            {
                // Ultime : la jauge se vide à vitesse fixe (pleine → vide en furieDuree), elle ne monte plus.
                m_Rage -= JaugeMax / Mathf.Max(0.5f, B.furieDuree) * dt;
                if (m_Rage <= 0f) SortirFurie();
                return;
            }
            // Rage pleine : la Furie est prête (le joueur la déclenche : Ultimate). Petit signal sonore, une fois par montée.
            bool pret = m_Rage >= JaugeMax - 0.01f;
            if (pret && !m_PretVu && !H.Distant) AudioBank.Jouer2D(SonsDuJeu.Pret, 0.6f);
            m_PretVu = pret;
            // Hors combat (ni coup donné ni coup reçu depuis rageDelaiBaisse), la rage redescend vers rageMin.
            if (Time.time - m_DernierCoup > B.rageDelaiBaisse)
                m_Rage = Mathf.MoveTowards(m_Rage, B.rageMin, B.rageBaisse * dt);
        }

        public override void SurCoupDonne(Sante cible, float reel, bool parBoule, bool continu)
        {
            m_DernierCoup = Time.time;
            if (m_Furie) return;   // pendant la Furie la jauge ne monte plus
            // Chaque coup de hache rend rageParTouche par ennemi touché ; les tics de l'attaque tournante (continu) rendent
            // tournanteRageParTic (une petite rage : la tournante est gratuite, elle ne doit pas remplir la jauge seule).
            m_Rage = Mathf.Min(JaugeMax, m_Rage + (continu ? B.tournanteRageParTic : B.rageParTouche));
        }

        /// Encaisser fait monter la rage (03/10/2026) : rageParDegatRecu par point de dégât subi (après Peau de fer),
        /// plafonné par coup (rageRecuMax). Ni la chute ni les dégâts continus (brûlure) n'en donnent. Un coup reçu compte
        /// comme du combat : la baisse hors combat attend rageDelaiBaisse.
        public override void SurTouche(InfoDegats info, float reel)
        {
            if (info.continu || info.direction.y < -0.99f) return;
            m_DernierCoup = Time.time;
            if (m_Furie) return;
            m_Rage = Mathf.Min(JaugeMax, m_Rage + Mathf.Min(reel * B.rageParDegatRecu, B.rageRecuMax));
        }

        public override void Maj(float dt, Vector3 dir)
        {
            var b = B;
            m_Depuis += dt;
            // Tournante : tenue tant que LT est maintenu, au plus tournanteDureeMax, puis recharge (gratuite, 03/10/2026).
            bool tenue = H.Entrees.GardeMaintenue;
            if (m_Action == Action.Aucune && tenue && m_RechargeTournante <= 0f) Commencer();
            switch (m_Action)
            {
                case Action.Attaque:
                    if (!m_CoupPorte && m_Depuis >= b.hacheInstant / Cadence)   // Agilité gagnée, Furie : coup plus tôt
                    {
                        m_CoupPorte = true;
                        bool touche = false;
                        foreach (var s in Cibles(transform.position, transform.forward, b.hachePortee, b.hacheDemiAngle))
                        {
                            H.Frapper(s, b.hacheDegats * Facteur(0));
                            Reculer(s);
                            touche = true;
                        }
                        if (touche) { AudioBank.Jouer(SonsDuJeu.Hache, transform.position + transform.forward + Vector3.up, 1f); Diffuser(E_Hache); }
                    }
                    if (m_Depuis >= b.hacheIntervalle / Cadence) m_Action = Action.Aucune;
                    break;
                case Action.Tournante:
                    if (!m_VfxTournante && m_Depuis >= 0.35f && m_Tournante != null && teteHache != null) { m_Tournante.Commencer(transform, teteHache); m_VfxTournante = true; Diffuser(E_TournanteVfx); }
                    // Souffle de la hache : un whoosh à chaque tour complet de la tête (AttaqueTournante.Tours), en plus
                    // de la boucle whirlwind_loop déjà lancée par Commencer().
                    if (m_VfxTournante && m_Tournante != null && m_Tournante.Tours > m_ToursVent)
                    {
                        m_ToursVent = m_Tournante.Tours;
                        AudioBank.Jouer(SonsDuJeu.TournanteVent, transform.position + Vector3.up, 0.75f);
                        Diffuser(E_TournanteVent);
                    }
                    if (m_Depuis >= 0.35f && Time.time >= m_ProchainTic)
                    {
                        m_ProchainTic = Time.time + b.tournanteIntervalle;
                        bool touche = false;
                        foreach (var s in Cibles(transform.position, transform.forward, b.tournanteRayon, 180f))
                        {
                            H.Frapper(s, b.tournanteDegats, false, s.transform.position + Vector3.up, (s.transform.position - transform.position).normalized, false, true);
                            touche = true;
                            m_TouchesTournante.Add(s);
                        }
                        Deathless.Succes.ServiceSucces.Tournante(H, m_TouchesTournante.Count);   // succès « Berserk »
                        if (touche) { AudioBank.Jouer(SonsDuJeu.Hache, transform.position + Vector3.up, 0.6f, 0.25f); Diffuser(E_TournanteTic); }
                    }
                    if (!tenue || m_Depuis >= b.tournanteDureeMax) Arreter();
                    break;
                case Action.Rugissement:
                {
                    float cri = CriClip / VitesseCri;
                    if (!m_VfxCri && m_Depuis >= cri - 0.32f) { m_VfxCri = true; if (m_Rugissement != null) m_Rugissement.Jouer(); Diffuser(E_RugirVfx); }
                    if (!m_Crie && m_Depuis >= cri)
                    {
                        m_Crie = true;
                        AudioBank.Jouer(SonsDuJeu.Rugissement, transform.position + Vector3.up * 1.6f, 1f);
                        Diffuser(E_RugirCri);
                        // Peau de fer (27/09/2026) : posée sur lui au cri, le temps de la provocation plus une seconde ;
                        // chemin des statuts sur son propre héros (prédit chez un client, confirmé par l'hôte).
                        if (H.Statuts != null) H.Statuts.Ajouter(TypeStatut.PeauDeFer, b.peauDeFerDuree, b.peauDeFerReduction, OrigineStatut.Joueur, H.Id);
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

        /// Ennemis différents touchés par la tournante en cours (succès « Berserk »).
        readonly System.Collections.Generic.HashSet<Sante> m_TouchesTournante = new System.Collections.Generic.HashSet<Sante>();

        void Commencer()
        {
            m_Action = Action.Tournante;
            m_TouchesTournante.Clear();
            m_Depuis = 0f;
            m_ProchainTic = 0f;
            m_VfxTournante = false;
            m_ToursVent = 0;
            if (Anim != null) Anim.SetBool(P_Tourne, true);
            m_SonTournante = AudioBank.Boucle(SonsDuJeu.Tournante, transform, 0.8f);
            Diffuser(E_TournanteDebut);
        }

        void Arreter()
        {
            if (m_Action != Action.Tournante) return;
            m_Action = Action.Aucune;
            m_RechargeTournante = B.tournanteRecharge * Facteur(1) * RechargeEsprit;   // recharge depuis la fin du tourbillon
            if (Anim != null) Anim.SetBool(P_Tourne, false);
            FinTournante();
            Diffuser(E_TournanteFin);
        }

        /// Recul de la hache (03/10/2026) : pousse l'ennemi touché de hacheRecul m (Force gagnée, ×furieRecul en Furie), sans
        /// étourdissement ; les ennemis non repoussables (Morgrim) restent en place.
        void Reculer(Sante cible)
        {
            if (cible == null || B.hacheRecul <= 0f) return;
            var sq = cible.GetComponent<Squelette>();
            if (sq == null || !sq.Vivant) return;
            Vector3 d = cible.transform.position - transform.position; d.y = 0f;
            d = d.sqrMagnitude > 0.01f ? d.normalized : transform.forward;
            sq.Pousser(d * B.hacheRecul * Attributs.FacteurRecul(EtatJ) * (m_Furie ? B.furieRecul : 1f));
        }

        void FinTournante()
        {
            if (m_Tournante != null && m_VfxTournante) m_Tournante.Arreter();
            m_VfxTournante = false;
            if (m_SonTournante != null) { Destroy(m_SonTournante); m_SonTournante = null; }
        }

        public override bool DeplacementImpose(float dt, out Vector3 vitesse)
        {
            vitesse = Vector3.zero;
            if (m_Action != Action.Saut) return false;   // le rugissement laisse le déplacement libre (27/09/2026)
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
            EffetSautPercutant(point, m_DirSaut);
            Diffuser(E_Saut, point, m_DirSaut);
            int n = 0;
            foreach (var s in Cibles(point, m_DirSaut, b.sautRayon, 180f))
            {
                H.Frapper(s, b.sautDegats * Facteur(3));
                var sq = s.GetComponent<Squelette>();
                if (sq != null && sq.Vivant) sq.Etourdir(b.sautEtourdi, H.Id);
                n++;
            }
            if (H.Partie != null) H.Partie.Journal("Saut percutant : " + Vector3.Distance(m_DepartSaut, transform.position).ToString("F1") + " m, " + n + " touchés");
        }

        void EffetSautPercutant(Vector3 point, Vector3 dir)
        {
            var fx = EffetsJeu.Instance;
            if (fx != null && fx.prefabOndeSaut != null)
            {
                var onde = Instantiate(fx.prefabOndeSaut, point + Vector3.up, Quaternion.LookRotation(dir));
                var o = onde.GetComponent<OndeDeChoc>();
                if (o != null) o.Jouer();
                Destroy(onde, 3f);
            }
            AudioBank.Jouer(SonsDuJeu.SautPercutant, point, 1f);
        }

        // ----------------------------------------------------------------- Multijoueur (marionnette)

        public override void EffetDistant(int effet, Vector3 a, Vector3 b, float v)
        {
            switch (effet)
            {
                case E_Elan: AudioBank.Jouer(SonsDuJeu.EpeeElan, transform.position + Vector3.up, 0.6f); break;
                case E_Hache: AudioBank.Jouer(SonsDuJeu.Hache, transform.position + transform.forward + Vector3.up, 1f); break;
                case E_TournanteDebut:
                    FinTournante();
                    m_SonTournante = AudioBank.Boucle(SonsDuJeu.Tournante, transform, 0.8f);
                    break;
                case E_TournanteVfx:
                    if (m_Tournante != null && teteHache != null && !m_VfxTournante) { m_Tournante.Commencer(transform, teteHache); m_VfxTournante = true; }
                    break;
                case E_TournanteTic: AudioBank.Jouer(SonsDuJeu.Hache, transform.position + Vector3.up, 0.6f, 0.25f); break;
                case E_TournanteVent: AudioBank.Jouer(SonsDuJeu.TournanteVent, transform.position + Vector3.up, 0.75f); break;
                case E_TournanteFin: FinTournante(); break;
                case E_RugirVfx: if (m_Rugissement != null) m_Rugissement.Jouer(); break;
                case E_RugirCri: AudioBank.Jouer(SonsDuJeu.Rugissement, transform.position + Vector3.up * 1.6f, 1f); break;
                case E_Saut: EffetSautPercutant(a, b); break;
                case E_FurieDebut: AudioBank.Jouer(SonsDuJeu.Rugissement, transform.position + Vector3.up * 1.6f, 0.9f); break;
                case E_FurieFin: AudioBank.Jouer(SonsDuJeu.TournanteVent, transform.position + Vector3.up, 0.5f); break;
                default: base.EffetDistant(effet, a, b, v); break;
            }
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
                // Compétences gratuites (03/10/2026) : seule la recharge les limite, montrée comme chez les autres classes.
                case 1: return Recharge(m_RechargeTournante, B.tournanteRecharge * Facteur(1) * RechargeEsprit, out restant, out total, m_Action == Action.Tournante);
                case 2: return Recharge(m_RechargeRugir, B.rugissementRecharge * Facteur(2) * RechargeEsprit, out restant, out total, m_Action == Action.Rugissement);
                case 3: return Recharge(m_RechargeSaut, B.sautRecharge * RechargeEsprit, out restant, out total, m_Action == Action.Saut);
                default: return EtatEmplacement.Vide;
            }
        }
    }
}
