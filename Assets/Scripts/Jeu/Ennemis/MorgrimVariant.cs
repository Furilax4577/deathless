using UnityEngine;

namespace Deathless.Jeu
{
    /// Base commune aux deux versions de Morgrim, le mini-boss de la nuit 10 (wiki : ennemis.md, section Morgrim ;
    /// décidé le 26/09/2026). L'hôte choisit seul la compétence et ses effets (Docs/reseau.md) ; les autres postes ne
    /// tournent pas cette IA (le composant `Squelette` d'une marionnette est désactivé), mais reçoivent quand même la
    /// télégraphie et l'impact au sol par un chemin léger (`EnnemiReseau.DiffuserEffetMorgrim`, un octet de thème et
    /// une forme, pas un RPC par gemme) : c'est purement visuel, comme le reste des effets de squelette.
    /// Kit commun aux deux versions (30/09/2026) : Balayage, Coup écrasé et Cri, ci-dessous.
    /// Outils communs aux sous-classes : compte des joueurs proches (choix de compétence) et télégraphie / impact au
    /// sol dans le langage gemmes commun (MorgrimEffets : AnneauGemmes pour la préparation, EclatGemmes pour le coup).
    public abstract class MorgrimVariant : Golem
    {
        // ----------------------------------------------------------------- Kit commun (wiki : ennemis.md, 30/09/2026)
        // Balayage (arc de hache devant lui, parable, léger recul), Coup écrasé (frappe par-dessus au sol : onde de
        // choc lente autour de lui, à sauter comme le Fracas) et Cri (galvanise les squelettes proches : statut
        // Galvanisé). Les deux versions gardent en plus leurs trois compétences propres : CommencerAttaque choisit
        // d'abord une compétence commune prête (chance GameBalance.morgrimCommunChance), sinon passe la main à la
        // version (CommencerVariante / FrapperVariante). Le Cri part de lui-même dès que assez de squelettes sont
        // proches (Update), qu'il ait une cible ou non.

        enum Commune { Aucune, Balayage, Ecrase, Cri }

        Commune m_Commune;
        float m_ProchainBalayage = -99f, m_ProchainEcrase = -99f, m_ProchainCri;
        bool m_CriDemande;
        /// Morgrim a poussé son cri au moins une fois (succès « Pas le temps de crier »).
        public bool ACrie { get; private set; }

        /// Thème des compétences communes : Terre pour la massue, Rage pour la martache.
        protected virtual VfxTheme ThemeCommun => VfxTheme.Terre;

        public override void Initialiser(StatsSquelette stats, float multiplicateurPV)
        {
            base.Initialiser(stats, multiplicateurPV);
            m_ProchainCri = Time.time + 6f;   // pas de cri dès la sortie de terre
            // Plus offensif que le Golem d'origine (01/10/2026) : cadence et pas propres.
            m_Stats.intervalle = B.morgrimIntervalle;
            m_Stats.vitesse = B.morgrimVitesse;
            Agent.speed = m_Stats.vitesse;
        }

        // ----------------------------------------------------------------- Offensif (retour de Quentin, 01/10/2026)
        // Il chasse les joueurs au lieu de marcher surtout vers Nyxessa : proie = le joueur qui vient de le frapper, sinon
        // le plus proche vu à moins de morgrimDetection m ; Nyxessa seulement sans joueur à portée. Il court quand sa
        // proie est loin. Après un coup simple (CoupSimple : Fracas, Fauche), si une compétence est prête, il l'enchaîne
        // après morgrimEnchainementDelai s au lieu de la récupération ordinaire.

        Heros m_Agresseur;
        float m_AgresseurQuand = -99f, m_ProchainChoixProie;
        bool m_Enchainer;
        float m_EnchainerJusque;
        /// La prochaine attaque doit être une compétence (enchaînement) : lu par les versions dans leur choix.
        protected bool ForcerCompetence { get; private set; }

        protected override void Awake()
        {
            base.Awake();
            Sante.Touche += SurTouche;
        }

        void SurTouche(InfoDegats info, float reel)
        {
            if (info.sourceId <= 0 || P == null) return;
            var h = P.HerosDe(info.sourceId);
            if (h == null) return;
            m_Agresseur = h;
            m_AgresseurQuand = Time.time;
        }

        /// Joueur chassé : l'agresseur récent (jusqu'à morgrimDetection × 1,5 m), sinon le plus proche vu à moins de
        /// morgrimDetection m (la proie actuelle compte 3 m plus près, pour ne pas hésiter entre deux joueurs).
        Heros ChoisirProie()
        {
            if (P == null) return null;
            var b = B;
            if (m_Agresseur != null && m_Agresseur.Vivant && Time.time - m_AgresseurQuand <= b.morgrimAgressionDuree
                && Voit(m_Agresseur) && Distance(m_Agresseur.transform.position) <= b.morgrimDetection * 1.5f)
                return m_Agresseur;
            Heros meilleur = null;
            float dMin = b.morgrimDetection;
            var tous = P.TousLesHeros;
            for (int i = 0; i < tous.Count; i++)
            {
                var h = tous[i];
                if (h == null || !h.Vivant || !Voit(h)) continue;
                float d = Distance(h.transform.position) - (h == m_Cible ? 3f : 0f);
                if (d < dMin) { dMin = d; meilleur = h; }
            }
            return meilleur;
        }

        bool Pret => m_Enchainer || Time.time - m_DernierCoup >= m_Stats.intervalle;

        protected override void MajMarche(float dt)
        {
            if (!Gardien && !Provoque)
            {
                var proie = ChoisirProie();
                if (proie != null) { m_Cible = proie; m_SansFrapper = 0f; m_Etat = Etat.Poursuite; m_ProchainChoixProie = Time.time + 0.5f; return; }
            }
            base.MajMarche(dt);
        }

        protected override void MajPoursuite(float dt)
        {
            if (Gardien || Provoque) { base.MajPoursuite(dt); return; }
            if (m_Enchainer && Time.time > m_EnchainerJusque) m_Enchainer = false;
            if (Time.time >= m_ProchainChoixProie || m_Cible == null || !m_Cible.Vivant || !Voit(m_Cible))
            {
                m_ProchainChoixProie = Time.time + 0.5f;
                var proie = ChoisirProie();
                if (proie == null) { m_Cible = null; m_Enchainer = false; m_Etat = Etat.Marche; return; }
                m_Cible = proie;
            }
            m_SansFrapper = 0f;
            float d = Distance(m_Cible.transform.position);
            if (d <= PorteeEngagement(m_Cible))
            {
                Agent.isStopped = true;
                Tourner(m_Cible.transform.position);
                if (Pret) CommencerAttaque(m_Cible);
            }
            else
            {
                Agent.isStopped = false;
                // Squelette.Update vient de poser le pas (eau, statuts compris) : course quand la proie est loin.
                if (d > B.morgrimCourseDistance) Agent.speed *= B.morgrimVitesseCourse / Mathf.Max(0.1f, m_Stats.vitesse);
                Poursuivre(m_Cible.transform.position);
            }
        }

        /// Après un coup simple : courte récupération si une compétence va s'enchaîner.
        protected override float RecuperationDuree => m_Enchainer ? Mathf.Max(0.2f, B.morgrimEnchainementDelai) : base.RecuperationDuree;

        /// Vrai si le coup qui part est le coup simple de la version (Fracas, Fauche) : peut être suivi d'un enchaînement.
        protected abstract bool CoupSimple { get; }
        /// Une compétence propre à la version est prête (recharge écoulée).
        protected abstract bool CompetenceVariantePrete { get; }
        bool CommunePrete => Time.time >= m_ProchainBalayage || Time.time >= m_ProchainEcrase;

        protected override void Update()
        {
            base.Update();
            if ((m_Etat != Etat.Marche && m_Etat != Etat.Poursuite) || Time.time < m_ProchainCri) return;
            if (P != null && (P.Etat.phase == Phase.Terminee || P.Etat.nyxessa.detruite)) return;
            if (SquelettesProches(B.morgrimCriRayon) < B.morgrimCriSquelettesMin) return;
            m_CriDemande = true;
            CommencerAttaque(m_Etat == Etat.Poursuite ? m_Cible : null);
        }

        /// Squelettes ordinaires vivants (ni boss, ni sortant de terre) à moins de `rayon` m.
        int SquelettesProches(float rayon)
        {
            var dv = DirecteurVagues.Instance;
            if (dv == null) return 0;
            int n = 0;
            var vivants = dv.Vivants;
            for (int i = 0; i < vivants.Count; i++)
            {
                var s = vivants[i];
                if (!Galvanisable(s)) continue;
                Vector3 d = s.transform.position - transform.position; d.y = 0f;
                if (d.magnitude <= rayon) n++;
            }
            return n;
        }

        bool Galvanisable(Squelette s) => s != null && s != this && s.Vivant && s.EtatCourant != Etat.SortieDeTerre
            && s.type != TypeEnnemi.Golem && s.type != TypeEnnemi.Necromancien;

        Commune ChoisirCommune(Heros cible)
        {
            var b = B;
            if (cible == null) return Commune.Aucune;
            // Enchaînement : une commune prête, sans condition de distance ; une chance sur deux si la version en a une aussi.
            if (ForcerCompetence)
            {
                if (CompetenceVariantePrete && Random.value < 0.5f) return Commune.Aucune;
                if (Time.time >= m_ProchainBalayage && Distance(cible.transform.position) <= b.morgrimBalayageRayon) return Commune.Balayage;
                if (Time.time >= m_ProchainEcrase) return Commune.Ecrase;
                return Commune.Aucune;
            }
            if (Random.value > b.morgrimCommunChance) return Commune.Aucune;
            float d = Distance(cible.transform.position);
            if (Time.time >= m_ProchainEcrase && (JoueursProches(b.morgrimEcraseOndeRayonMax * 0.5f) >= 2 || d <= b.morgrimEcraseOndeRayonMax * 0.4f))
                return Commune.Ecrase;
            if (Time.time >= m_ProchainBalayage && d <= b.morgrimBalayageRayon) return Commune.Balayage;
            return Commune.Aucune;
        }

        protected sealed override void CommencerAttaque(Heros cible)
        {
            var b = B;
            ForcerCompetence = m_Enchainer && !m_CriDemande;
            m_Enchainer = false;
            m_Commune = m_CriDemande ? Commune.Cri : ChoisirCommune(cible);
            m_CriDemande = false;
            if (m_Commune == Commune.Aucune) { CommencerVariante(cible); ForcerCompetence = false; return; }
            ForcerCompetence = false;
            Vector3 sol = transform.position + Vector3.up * 0.05f;
            switch (m_Commune)
            {
                case Commune.Balayage:
                    m_Stats.preparation = b.morgrimBalayagePreparation;
                    m_ProchainBalayage = Time.time + b.morgrimBalayageRecharge;
                    DemarrerPreparation(cible);
                    Telegraphier(sol, ThemeCommun, b.morgrimBalayageRayon, m_Stats.preparation, b.morgrimBalayageAngle);
                    break;
                case Commune.Ecrase:
                    m_Stats.preparation = b.morgrimEcrasePreparation;
                    m_ProchainEcrase = Time.time + b.morgrimEcraseRecharge;
                    DemarrerPreparation(cible);
                    Telegraphier(sol, VfxTheme.Terre, b.morgrimEcraseOndeRayonMax * 0.35f, m_Stats.preparation);
                    break;
                default:
                    m_Stats.preparation = b.morgrimCriPreparation;
                    m_ProchainCri = Time.time + b.morgrimCriRecharge;
                    DemarrerPreparation(cible);
                    Telegraphier(sol, VfxTheme.Rage, b.morgrimCriRayon, m_Stats.preparation);
                    if (P != null) P.Journal("Morgrim : cri (" + SquelettesProches(b.morgrimCriRayon) + " squelettes proches)");
                    break;
            }
        }

        /// Préparation lisible commune (animation, annonce du coup, arrêt) : Golem puis Squelette.
        protected void DemarrerPreparation(Heros cible) => base.CommencerAttaque(cible);

        /// Compétence propre à la version (massue ou martache) : règle m_Stats.preparation, appelle DemarrerPreparation
        /// puis pose sa télégraphie.
        protected abstract void CommencerVariante(Heros cible);

        protected sealed override void Frapper()
        {
            switch (m_Commune)
            {
                case Commune.Balayage: FaireBalayage(); break;
                case Commune.Ecrase: FaireEcrase(); break;
                case Commune.Cri: FaireCri(); break;
                default: FrapperVariante(); break;
            }
            // Coup simple porté (01/10/2026) : enchaîne une compétence prête, après une récupération courte.
            if (m_Commune == Commune.Aucune && CoupSimple && m_Cible != null && (CommunePrete || CompetenceVariantePrete))
            {
                m_Enchainer = true;
                m_EnchainerJusque = Time.time + 3f;
            }
        }

        protected abstract void FrapperVariante();

        /// Balayage : arc de hache devant lui ; touche tous les joueurs dans l'arc (parable) et les repousse un peu.
        void FaireBalayage()
        {
            var b = B;
            Vector3 o = transform.position;
            Impact(o + Vector3.up, ThemeCommun, b.morgrimBalayageRayon, transform.forward, b.morgrimBalayageAngle);
            EffetsBoss.Diffuser(this, EffetBoss.CoupSon, o);
            float demi = b.morgrimBalayageAngle * 0.5f;
            if (P == null) return;
            var tous = P.TousLesHeros;
            for (int i = 0; i < tous.Count; i++)
            {
                var h = tous[i];
                if (h == null || !h.Vivant) continue;
                Vector3 d = h.transform.position - o; d.y = 0f;
                if (d.magnitude > b.morgrimBalayageRayon || (d.sqrMagnitude > 0.01f && Vector3.Angle(transform.forward, d) > demi)) continue;
                float reel = h.Sante.Encaisser(new InfoDegats
                {
                    montant = b.morgrimBalayageDegats, equipeSource = Equipe.Ennemis, source = gameObject, parable = true,
                    point = h.transform.position + Vector3.up, direction = d.sqrMagnitude > 0.01f ? d.normalized : transform.forward
                });
                if (reel > 0f && d.sqrMagnitude > 0.01f) h.Pousser(d.normalized * b.morgrimBalayageRecul);
            }
            if (P.nyxessa == null) return;
            Vector3 dn = P.nyxessa.transform.position - o; dn.y = 0f;
            var bo = BouclierNyxessa.Instance;
            if (dn.magnitude > (bo != null && bo.Leve ? RayonContact + 0.8f : b.morgrimBalayageRayon + 1.3f) || Vector3.Angle(transform.forward, dn) > demi) return;
            m_DernierCoupNyxessa = Time.time;
            P.nyxessa.Encaisser(new InfoDegats { montant = b.morgrimBalayageDegats, equipeSource = Equipe.Ennemis, source = gameObject, point = o + transform.forward * 2f + Vector3.up * 1.5f, direction = transform.forward });
        }

        /// Coup écrasé : frappe par-dessus, l'arme s'écrase au sol devant lui et une onde de choc lente part autour de
        /// lui (OndeChocLente, même règle que le Fracas : seul un saut au passage du front l'évite ; Renversé au sol).
        void FaireEcrase()
        {
            var b = B;
            Vector3 impact = transform.position + transform.forward * 1.2f;
            EffetsBoss.Diffuser(this, EffetBoss.Onde, impact, 1f);
            Impact(impact + Vector3.up * 0.2f, VfxTheme.Terre, 1.4f);
            LancerOnde(impact, b.morgrimEcraseOndeVitesse, b.morgrimEcraseOndeRayonMax, b.morgrimEcraseOndeLargeurBande, b.morgrimEcraseDegats);
            if (P == null || P.nyxessa == null) return;
            Vector3 dn = P.nyxessa.transform.position - impact; dn.y = 0f;
            var bo = BouclierNyxessa.Instance;
            if (dn.magnitude > (bo != null && bo.Leve ? RayonContact + 0.8f : b.morgrimEcraseOndeRayonMax * 0.5f)) return;
            m_DernierCoupNyxessa = Time.time;
            P.nyxessa.Encaisser(new InfoDegats { montant = b.morgrimEcraseDegatsNyxessa, equipeSource = Equipe.Ennemis, source = gameObject, point = impact + Vector3.up * 1.5f, direction = transform.forward });
        }

        /// Cri : galvanise les squelettes ordinaires proches (statut Galvanisé : dégâts et vitesse accrus). Statuts
        /// tenus par l'hôte et envoyés aux clients par le chemin habituel (StatutsReseau) ; son et gerbe par EffetsBoss.
        void FaireCri()
        {
            var b = B;
            ACrie = true;
            EffetsBoss.Diffuser(this, EffetBoss.Cri, transform.position, 1f);
            Impact(transform.position + Vector3.up * 0.1f, VfxTheme.Rage, b.morgrimCriRayon * 0.5f);
            var dv = DirecteurVagues.Instance;
            if (dv == null) return;
            int n = 0;
            var vivants = dv.Vivants;
            for (int i = 0; i < vivants.Count; i++)
            {
                var s = vivants[i];
                if (!Galvanisable(s)) continue;
                Vector3 d = s.transform.position - transform.position; d.y = 0f;
                if (d.magnitude > b.morgrimCriRayon || s.Statuts == null) continue;
                s.Statuts.Ajouter(TypeStatut.Galvanise, b.morgrimCriDuree, b.morgrimCriBonusDegats, OrigineStatut.Ennemi);
                n++;
            }
            if (P != null) P.Journal("Morgrim : cri, " + n + " squelettes galvanisés");
        }

        /// Hôte : onde de choc lente (Fracas de la massue, Coup écrasé commun) ; jouée ici et envoyée aux autres postes
        /// avec la même heure de départ réseau (Docs/reseau.md).
        protected void LancerOnde(Vector3 centre, float vitesse, float rayonMax, float largeurBande, float degats)
        {
            float depart = Deathless.Reseau.EnnemiReseau.TempsReseau();
            OndeChocLente.Creer(centre, vitesse, rayonMax, largeurBande, degats, depart, m_Reseau);
            if (m_Reseau != null && m_Reseau.IsSpawned && m_Reseau.IsServer)
                m_Reseau.DiffuserOndeMorgrim(centre, vitesse, rayonMax, largeurBande, degats, depart);
        }

        // ----------------------------------------------------------------- Outils communs

        /// Joueurs vivants à moins de `rayon` (m, horizontal) : sert au choix de compétence (zone vs cible isolée).
        protected int JoueursProches(float rayon)
        {
            if (P == null) return 0;
            int n = 0;
            var tous = P.TousLesHeros;
            for (int i = 0; i < tous.Count; i++)
            {
                var h = tous[i];
                if (h == null || !h.Vivant) continue;
                Vector3 d = h.transform.position - transform.position; d.y = 0f;
                if (d.magnitude <= rayon) n++;
            }
            return n;
        }

        protected bool BouclierLeve => BouclierNyxessa.Instance != null && BouclierNyxessa.Instance.Leve;

        /// Télégraphie au sol pendant la préparation (cercle ou cône de gemmes, langage commun VFX) : posée devant le
        /// personnage, orientée avec lui, détruite avec la préparation. Hôte seulement (appelée depuis CommencerAttaque) :
        /// diffusée aux autres postes par `EnnemiReseau.DiffuserEffetMorgrim`.
        protected void Telegraphier(Vector3 point, VfxTheme theme, float rayon, float duree, float angleDeg = 360f)
        {
            JouerTelegraphie(point, theme, rayon, duree, angleDeg);
            if (m_Reseau != null && m_Reseau.IsSpawned && m_Reseau.IsServer)
                m_Reseau.DiffuserEffetMorgrim(true, (byte)theme, rayon, duree, angleDeg);
        }

        /// Gerbe d'impact (coup qui part), même langage : boule (360°) ou gerbe dirigée dans un cône. Hôte seulement
        /// (appelée depuis Frapper) : diffusée comme la télégraphie.
        protected void Impact(Vector3 point, VfxTheme theme, float rayon, Vector3? direction = null, float coneDeg = 360f)
        {
            JouerImpact(point, theme, rayon, direction, coneDeg);
            if (m_Reseau != null && m_Reseau.IsSpawned && m_Reseau.IsServer)
                m_Reseau.DiffuserEffetMorgrim(false, (byte)theme, rayon, 0f, coneDeg);
        }

        /// Client (marionnette) : rejoue une télégraphie ou un impact reçus de l'hôte, devant le personnage comme
        /// chez lui (transform répliqué par NetworkTransform, léger décalage de latence accepté).
        public void RejouerEffetDistant(bool telegraphie, VfxTheme theme, float rayon, float duree, float angleDeg)
        {
            Vector3 point = transform.position + Vector3.up * 0.05f;
            if (telegraphie) JouerTelegraphie(point, theme, rayon, duree, angleDeg);
            else JouerImpact(point, theme, rayon, transform.forward, angleDeg);
        }

        /// Client (marionnette) : reçoit l'onde de choc lente du Fracas (Morgrim massue, 26/09/2026) et rejoue la même
        /// onde chez lui (visuel partout ; jugement des dégâts en plus sur son propre héros, OndeChocLente.Creer).
        public void RecevoirOndeDistante(Vector3 centre, float vitesse, float rayonMax, float largeurBande, float degats, float depart)
            => OndeChocLente.Creer(centre, vitesse, rayonMax, largeurBande, degats, depart, m_Reseau);

        void JouerTelegraphie(Vector3 point, VfxTheme theme, float rayon, float duree, float angleDeg)
        {
            var mat = EffetsJeu.GemmesMorgrim;
            if (mat == null) return;
            MorgrimEffets.MateriauGemmes = mat;
            var go = MorgrimEffets.AnneauGemmes(point, theme, Mathf.Max(0.3f, rayon), Mathf.Max(0.1f, duree), angleDeg, transform.forward, "Telegraphie_" + name);
            go.transform.SetParent(transform, true);
            Object.Destroy(go, duree + 0.3f);
        }

        void JouerImpact(Vector3 point, VfxTheme theme, float rayon, Vector3? direction, float coneDeg)
        {
            var mat = EffetsJeu.GemmesMorgrim;
            if (mat == null) return;
            MorgrimEffets.MateriauGemmes = mat;
            var go = MorgrimEffets.EclatGemmes(point, theme, Mathf.Max(0.3f, rayon), direction, coneDeg, "Impact_" + name);
            Object.Destroy(go, 1f);
        }
    }
}
