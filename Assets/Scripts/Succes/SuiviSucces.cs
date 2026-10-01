using Deathless.Donjon;
using Deathless.Jeu;
using Deathless.Reseau;
using UnityEngine;

namespace Deathless.Succes
{
    /// Écoute de la partie pour les succès (créé par ServiceSucces au lancement du jeu, garde d'une scène à l'autre) :
    /// abonnements aux événements de Partie (nuit tenue, mort, palier, fin de partie) et sondes de ce qui n'a pas
    /// d'événement (caisse commune, nombre de joueurs, nuit prolongée, bouclier, eau du bassin, brûlures, Peau de fer).
    /// Les faits d'équipe ne sont décidés que par l'autorité (hôte ou solo) ; un client les reçoit (ServiceSucces.Equipe).
    public sealed class SuiviSucces : MonoBehaviour
    {
        // État de la partie en cours (autorité), remis à zéro à chaque partie.
        internal static float AubeRetenue;          // secondes de nuit prolongée (aube retenue par le boss)
        internal static bool CouronneBrisee;        // éclat de la couronne de Nyxar déjà brisé
        internal static bool HoteSeul;              // hôte qui continue seul après la perte du réseau (personnel)
        static bool s_Morts, s_NyxTouchee12, s_BouclierLeve, s_BouclierBrise, s_Quatuor, s_Caisse;
        static int s_JoueursMax;

        Partie m_Partie;
        BouclierNyxessa m_Bouclier;
        float m_DansLEau, m_ProchaineSonde, m_ProchainStockage;

        void OnEnable() { Sante.AnyTouche += OnToucheGlobal; }
        void OnDisable() { Sante.AnyTouche -= OnToucheGlobal; Suivre(null); }
        void OnApplicationQuit() => ServiceSucces.Stocker();

        void Update()
        {
            if (m_Partie != Partie.Instance)
            {
                Suivre(Partie.Instance);
                ServiceSucces.Stocker();   // changement de scène : retour au menu, Rejouer
            }
            var p = m_Partie;
            if (p == null || !p.EnCours) { m_DansLEau = 0f; return; }
            SuivreBouclier();
            float dt = Time.deltaTime;
            bool autorite = !Partie.ClientReseau;
            var e = p.Etat;
            if (autorite)
            {
                if (e.aubeRetenue) AubeRetenue += dt;
                if (e.joueurs.Count > s_JoueursMax) s_JoueursMax = e.joueurs.Count;
                if (!s_Quatuor && e.joueurs.Count >= 4) { s_Quatuor = true; ServiceSucces.Equipe("ACH_QUATUOR"); }
                if (s_Quatuor && e.joueurs.Count < 4) s_Quatuor = false;   // renvoyé si un quatrième revient
                if (!s_Caisse && e.orEquipe >= 1000) { s_Caisse = true; ServiceSucces.Equipe("ACH_CAISSE_1000"); }
                if (e.phase == Phase.Nuit && m_Bouclier != null && m_Bouclier.Leve) s_BouclierLeve = true;
            }
            // Bassin du donjon : 60 s d'affilée dans l'eau (héros local, vivant).
            var h = p.HerosLocal;
            if (h != null && h.Vivant && ZoneEau.FacteurEn(h.transform.position + Vector3.up * 0.2f) < 0.999f)
            {
                m_DansLEau += dt;
                if (m_DansLEau >= 60f) { m_DansLEau = -9999f; ServiceSucces.Debloquer("ACH_BASSIN"); }
            }
            else if (m_DansLEau > 0f) m_DansLEau = 0f;
            // Sondes plus lentes : réglages de test, brûlures, stockage périodique.
            if (Time.unscaledTime >= m_ProchaineSonde)
            {
                m_ProchaineSonde = Time.unscaledTime + 0.25f;
                VerifierReglagesDeTest(p);
                CompterBrulures(p);
            }
            if (Time.unscaledTime >= m_ProchainStockage) { m_ProchainStockage = Time.unscaledTime + 30f; ServiceSucces.Stocker(); }
        }

        void Suivre(Partie p)
        {
            if (m_Partie != null)
            {
                m_Partie.PartieLancee -= OnPartieLancee;
                m_Partie.PhaseChangee -= OnPhase;
                m_Partie.NuitCommencee -= OnNuit;
                m_Partie.NyxessaTouchee -= OnNyxessaTouchee;
                m_Partie.JoueurMort -= OnJoueurMort;
                m_Partie.PalierAchete -= OnPalier;
                m_Partie.BossSurgi -= OnBoss;
            }
            m_Partie = p;
            m_Bouclier = null;
            if (p == null) return;
            p.PartieLancee += OnPartieLancee;
            p.PhaseChangee += OnPhase;
            p.NuitCommencee += OnNuit;
            p.NyxessaTouchee += OnNyxessaTouchee;
            p.JoueurMort += OnJoueurMort;
            p.PalierAchete += OnPalier;
            p.BossSurgi += OnBoss;
            if (p.EnCours) OnPartieLancee();   // partie déjà lancée (abonnement tardif)
        }

        void SuivreBouclier()
        {
            var b = BouclierNyxessa.Instance;
            if (b == m_Bouclier) return;
            if (m_Bouclier != null) m_Bouclier.Brise -= OnBouclierBrise;
            m_Bouclier = b;
            if (b != null) b.Brise += OnBouclierBrise;
        }

        void OnBouclierBrise() { if (m_Partie != null && m_Partie.Etat.phase == Phase.Nuit) s_BouclierBrise = true; }

        // ----------------------------------------------------------------- Événements de Partie

        void OnPartieLancee()
        {
            ServiceSucces.NouvellePartie();
            AubeRetenue = 0f;
            CouronneBrisee = false;
            HoteSeul = false;
            s_Morts = s_NyxTouchee12 = s_BouclierLeve = s_BouclierBrise = s_Quatuor = s_Caisse = false;
            s_JoueursMax = 0;
            m_DansLEau = 0f;
            if (m_Partie != null) VerifierReglagesDeTest(m_Partie);
        }

        void OnNuit(int nuit)
        {
            s_BouclierLeve = s_BouclierBrise = false;
            AubeRetenue = 0f;
            if (nuit == 12) s_NyxTouchee12 = false;
            if (nuit >= 6 && !Partie.ClientReseau) ServiceSucces.Equipe("ACH_NUIT_6");
        }

        void OnPhase(Phase avant, Phase apres)
        {
            var p = m_Partie;
            if (p == null) return;
            bool autorite = !Partie.ClientReseau;
            if (avant == Phase.Nuit && apres == Phase.Aube)
            {
                if (autorite)
                {
                    ServiceSucces.Equipe(ServiceSucces.FaitNuit, p.Etat.nuit);
                    if (p.Etat.nuit >= 8 && s_BouclierLeve && !s_BouclierBrise) ServiceSucces.Equipe("ACH_BOUCLIER_TIENT");
                }
                // Hôte devenu seul (ContinuerSeul) : ce poste fait désormais autorité, la nuit est tenue seul.
                if (HoteSeul && !ReseauJeu.Actif) ServiceSucces.Debloquer("ACH_HOTE_SEUL");
                ServiceSucces.Stocker();
            }
            if (apres == Phase.Terminee)
            {
                if (autorite && p.Etat.resultat == Resultat.Victoire)
                {
                    ServiceSucces.Equipe("ACH_VICTOIRE");
                    if (!s_NyxTouchee12) ServiceSucces.Equipe("ACH_NYXESSA_INTACTE");
                    bool aucuneMort = !s_Morts;
                    foreach (var j in p.Etat.joueurs) if (j.score.morts > 0) aucuneMort = false;
                    if (aucuneMort && Mathf.Max(s_JoueursMax, p.Etat.joueurs.Count) >= 2) ServiceSucces.Equipe("ACH_DEATHLESS_EQUIPE");
                }
                ServiceSucces.Stocker();
            }
        }

        void OnNyxessaTouchee(float degats, Vector3 point)
        {
            if (degats > 0f && m_Partie != null && m_Partie.Etat.nuit >= 12) s_NyxTouchee12 = true;
        }

        void OnJoueurMort(int id)
        {
            s_Morts = true;
            var j = m_Partie != null ? m_Partie.JoueurLocal : null;
            if (j != null && j.id == id) ServiceSucces.Debloquer("ACH_PREMIERE_MORT");
        }

        void OnPalier(Partie.Amelioration a, int palier, string qui)
        {
            var j = m_Partie != null ? m_Partie.JoueurLocal : null;
            if (j != null && qui == j.nom) ServiceSucces.Debloquer("ACH_PREMIER_PALIER");
        }

        void OnBoss(TypeEnnemi t)
        {
            if (t == TypeEnnemi.Necromancien) CouronneBrisee = false;
        }

        // ----------------------------------------------------------------- Sondes

        /// Coups reçus par le héros local sous Peau de fer (cumul de dégâts réels).
        void OnToucheGlobal(Sante s, InfoDegats info, float reel)
        {
            var p = m_Partie;
            if (p == null || reel <= 0f) return;
            var h = p.HerosLocal;
            if (h == null || s != h.Sante || h.Statuts == null || !h.Statuts.A(TypeStatut.PeauDeFer)) return;
            m_PeauDeFer += reel;
            int entier = (int)m_PeauDeFer;
            if (entier > 0) { m_PeauDeFer -= entier; ServiceSucces.Ajouter(CatalogueSucces.StatPeauDeFer, entier); }
        }

        float m_PeauDeFer;

        /// Mage local : ennemis à son palier 3 de brûlure (ou plus) en même temps.
        void CompterBrulures(Partie p)
        {
            var h = p.HerosLocal;
            if (h == null || h.Classe == null || h.Classe.Id != "mage" || ServiceSucces.EstDebloque("ACH_BRASIER_8")) return;
            int n = 0;
            var actifs = Statuts.Actifs;
            for (int i = 0; i < actifs.Count; i++)
            {
                var st = actifs[i];
                if (st == null || st.Sante == null || st.Sante.Mort || st.Sante.equipe != Jeu.Equipe.Ennemis) continue;
                var liste = st.Liste;
                for (int k = 0; k < liste.Count; k++)
                    if (liste[k].type == TypeStatut.Brulure && liste[k].sourceId == h.Id && liste[k].PalierCourant >= 3) { n++; break; }
            }
            if (n >= 8) ServiceSucces.Debloquer("ACH_BRASIER_8");
        }

        /// Réglages de développement de GameBalance (mode de test) : la partie ne compte pas.
        static void VerifierReglagesDeTest(Partie p)
        {
            var b = p.B;
            if (b == null) return;
            if (b.joueurInvincible) ServiceSucces.MarquerPartieTest("joueur invincible");
            else if (b.nyxessaInvincible) ServiceSucces.MarquerPartieTest("Nyxessa invincible");
            else if (b.lancerDirectement) ServiceSucces.MarquerPartieTest("lancement direct");
            else if (b.commencerALaNuit) ServiceSucces.MarquerPartieTest("départ à la nuit");
            else if (b.nuitDeDepart != 1) ServiceSucces.MarquerPartieTest("nuit de départ " + b.nuitDeDepart);
            else if (!Mathf.Approximately(b.vitesseCycle, 1f)) ServiceSucces.MarquerPartieTest("cycle ×" + b.vitesseCycle);
        }
    }
}
