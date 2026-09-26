using UnityEngine;

namespace Deathless.Jeu
{
    /// Morgrim, version Martache (wiki : ennemis.md, Morgrim ; décidé le 26/09/2026) : colosse qui tranche et vise
    /// juste, thème Rage (accents fer), yeux bleu glacé (Yeux_Glace.mat) pour se distinguer de la Massue au premier
    /// coup d'œil. Trois compétences, choisies selon la présence du bouclier de Nyxessa, la distance à la cible et le
    /// nombre de joueurs proches (GameBalance, préfixe morgrimMartache*) : Fauche (compétence par défaut, cône
    /// tranchant devant lui), Fend-sol (ligne qui ralentit, thème Terre pour la fissure) et Coup de brèche (vise le
    /// bouclier de Nyxessa quand il est levé, dégâts renforcés contre lui). Chacune a sa propre recharge, sauf Fauche
    /// (cadence de base du Golem).
    public class MorgrimMartache : MorgrimVariant
    {
        enum Competence { Fauche, FendSol, Breche }

        Competence m_Competence;
        float m_ProchainFendSol = -99f, m_ProchaineBreche = -99f;

        public override void Initialiser(StatsSquelette stats, float multiplicateurPV)
        {
            base.Initialiser(stats, multiplicateurPV);
            // Porte d'engagement élargie pour Fend-sol (portée en ligne, plus longue que la Fauche au contact).
            m_Stats.portee = Mathf.Max(m_Stats.portee, B.morgrimMartacheFendSolLongueur);
        }

        protected override void CommencerAttaque(Heros cible)
        {
            var b = B;
            m_Competence = Choisir(cible);
            switch (m_Competence)
            {
                case Competence.FendSol:
                    m_Stats.preparation = b.morgrimMartacheFendSolPreparation;
                    m_ProchainFendSol = Time.time + b.morgrimMartacheFendSolRecharge;
                    break;
                case Competence.Breche:
                    m_Stats.preparation = b.morgrimMartacheBrechePreparation;
                    m_ProchaineBreche = Time.time + b.morgrimMartacheBrecheRecharge;
                    break;
                default:
                    m_Stats.preparation = b.morgrimMartacheFauchePreparation;
                    break;
            }
            base.CommencerAttaque(cible);
            PoserTelegraphie();
        }

        Competence Choisir(Heros cible)
        {
            var b = B;
            if (BouclierLeve && DistanceNyxessa() <= RayonContact + 1.5f && Time.time >= m_ProchaineBreche) return Competence.Breche;
            int proches = JoueursProches(b.morgrimJoueursProchesRayon);
            float distance = cible != null ? Distance(cible.transform.position) : 0f;
            if ((proches >= 2 || distance > b.morgrimMartacheFaucheRayon * 1.3f) && Time.time >= m_ProchainFendSol) return Competence.FendSol;
            return Competence.Fauche;
        }

        void PoserTelegraphie()
        {
            var b = B;
            switch (m_Competence)
            {
                case Competence.FendSol:
                    Telegraphier(transform.position + transform.forward * (b.morgrimMartacheFendSolLongueur * 0.5f) + Vector3.up * 0.05f,
                        VfxTheme.Terre, b.morgrimMartacheFendSolLongueur * 0.5f, m_Stats.preparation, 20f);
                    break;
                case Competence.Breche:
                    Telegraphier(NyxessaImpact() + Vector3.up * 0.05f, VfxTheme.Rage, 1.6f, m_Stats.preparation, 90f);
                    break;
                default:
                    Telegraphier(transform.position + Vector3.up * 0.05f, VfxTheme.Rage, b.morgrimMartacheFaucheRayon, m_Stats.preparation, b.morgrimMartacheFaucheAngle);
                    break;
            }
        }

        protected override void Frapper()
        {
            switch (m_Competence)
            {
                case Competence.FendSol: FaireFendSol(); break;
                case Competence.Breche: FaireBreche(); break;
                default: FaireFauche(); break;
            }
        }

        /// Coup en cône devant lui avec le tranchant de la hache : touche tous les joueurs dans l'arc, thème Rage.
        void FaireFauche()
        {
            var b = B;
            Vector3 impact = transform.position;
            Impact(impact + Vector3.up, VfxTheme.Rage, b.morgrimMartacheFaucheRayon, transform.forward, b.morgrimMartacheFaucheAngle);
            AudioBank.Jouer(SonsDuJeu.GolemCoup, impact, 1f);
            float r = b.morgrimMartacheFaucheRayon;
            float demiAngle = b.morgrimMartacheFaucheAngle * 0.5f;
            if (P == null) return;
            foreach (var h in P.TousLesHeros)
            {
                if (h == null || !h.Vivant) continue;
                Vector3 d = h.transform.position - impact; d.y = 0f;
                if (d.magnitude > r || Vector3.Angle(transform.forward, d) > demiAngle) continue;
                h.Sante.Encaisser(new InfoDegats
                {
                    montant = b.morgrimMartacheFaucheDegats, equipeSource = Equipe.Ennemis, source = gameObject, parable = true,
                    point = h.transform.position + Vector3.up, direction = d.normalized
                });
            }
            if (P.nyxessa == null) return;
            Vector3 dn = P.nyxessa.transform.position - impact; dn.y = 0f;
            var bo = BouclierNyxessa.Instance;
            if (dn.magnitude > (bo != null && bo.Leve ? RayonContact + 0.8f : r + 1.3f) || Vector3.Angle(transform.forward, dn) > demiAngle) return;
            m_DernierCoupNyxessa = Time.time;
            P.nyxessa.Encaisser(new InfoDegats { montant = b.morgrimMartacheFaucheDegatsNyxessa, equipeSource = Equipe.Ennemis, source = gameObject, point = impact + Vector3.up * 1.5f, direction = transform.forward });
        }

        /// Saut court suivi d'une retombée qui plante l'arme droit devant lui et fend le sol en ligne : la fissure
        /// ralentit les joueurs qui restent dedans (statut Ralenti, thème Terre : c'est le sol qui casse).
        void FaireFendSol()
        {
            var b = B;
            Vector3 origine = transform.position;
            Vector3 dir = transform.forward;
            if (EffetsJeu.Terre != null) DirtBurst.Spawn(origine + dir * b.morgrimMartacheFendSolLongueur, EffetsJeu.Terre, 0.7f);
            Impact(origine + dir * (b.morgrimMartacheFendSolLongueur * 0.5f) + Vector3.up * 0.05f, VfxTheme.Terre, b.morgrimMartacheFendSolLongueur * 0.5f, dir, 20f);
            AudioBank.Jouer(SonsDuJeu.GolemCoup, origine, 1f);
            if (P == null) return;
            foreach (var h in P.TousLesHeros)
            {
                if (h == null || !h.Vivant) continue;
                Vector3 d = h.transform.position - origine; d.y = 0f;
                float avance = Vector3.Dot(d, dir);
                if (avance < 0f || avance > b.morgrimMartacheFendSolLongueur) continue;
                Vector3 lateral = d - dir * avance;
                if (lateral.magnitude > b.morgrimMartacheFendSolLargeur * 0.5f) continue;
                float reel = h.Sante.Encaisser(new InfoDegats
                {
                    montant = b.morgrimMartacheFendSolDegats, equipeSource = Equipe.Ennemis, source = gameObject, parable = true,
                    point = h.transform.position + Vector3.up, direction = dir
                });
                if (reel > 0f) h.Statuts?.Ajouter(TypeStatut.Ralenti, b.morgrimMartacheFendSolRalentiDuree, b.morgrimMartacheFendSolRalentiForce, OrigineStatut.Ennemi);
            }
        }

        /// Frappe du côté marteau de l'arme, tournée vers le bouclier de Nyxessa plutôt que vers les joueurs :
        /// dégâts renforcés contre la paroi quand il est levé (thème Rage).
        void FaireBreche()
        {
            var b = B;
            bool leve = BouclierLeve;
            float montant = b.morgrimMartacheBrecheDegats * (leve ? b.morgrimMartacheBrecheMultiplicateurBouclier : 1f);
            Vector3 impact = NyxessaImpact();
            Impact(impact, VfxTheme.Rage, 1.6f, transform.forward, 90f);
            AudioBank.Jouer(SonsDuJeu.GolemCoup, impact, 1f);
            if (P == null || P.nyxessa == null) return;
            m_DernierCoupNyxessa = Time.time;
            P.nyxessa.Encaisser(new InfoDegats { montant = montant, equipeSource = Equipe.Ennemis, source = gameObject, point = impact, direction = transform.forward });
        }

        Vector3 NyxessaImpact()
        {
            if (P == null || P.nyxessa == null) return transform.position + transform.forward;
            return P.nyxessa.transform.position + Vector3.up * 1.5f + (transform.position - P.nyxessa.transform.position).normalized * 1.3f;
        }
    }
}
