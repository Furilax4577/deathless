using UnityEngine;

namespace Deathless.Jeu
{
    /// Vue du cycle : suit l'horloge de Partie et pilote l'ambiance du village (CycleJourNuit → DayCycle, lanternes,
    /// fenêtres, brume, lucioles), le portail (présent le jour, absent la nuit : la charge de Nyxessa l'ouvre à l'aube et
    /// revient au cristal au crépuscule ; c'est CycleJourNuit qui pose PortalVisual.ouvert), la musique et les sons de
    /// phase. Aucun minuteur propre : tout vient de Partie.Etat.
    public class VueCycle : MonoBehaviour
    {
        public CycleJourNuit cycle;
        public PortalVisual portail;

        AudioSource m_Bourdon;
        bool m_PortailOuvert = true;

        Partie P => Partie.Instance;

        void Start()
        {
            if (P != null) P.PhaseChangee += OnPhase;
            // Menu principal (partie en attente) : plan de nuit, donc musique de nuit ; le jour 1 relance celle du jour.
            AudioBank.Musique(P != null && P.Etat.phase != Phase.Attente && P.Etat.phase != Phase.Nuit ? SonsDuJeu.MusiqueJour : SonsDuJeu.MusiqueNuit);
            if (portail != null) m_Bourdon = AudioBank.Boucle(SonsDuJeu.PortailBourdon, portail.transform, 0.5f);
        }

        void OnPhase(Phase avant, Phase apres)
        {
            Vector3 pp = portail != null ? portail.transform.position : Vector3.zero;
            switch (apres)
            {
                case Phase.Jour:
                    AudioBank.Musique(SonsDuJeu.MusiqueJour);
                    break;
                case Phase.Crepuscule:
                    AudioBank.Jouer2D(SonsDuJeu.TombeeNuit, 0.9f);
                    AudioBank.Jouer(SonsDuJeu.PortailFermeture, pp, 1f);
                    AudioBank.Jouer(SonsDuJeu.RetourEnergie, pp, 0.8f);
                    AudioBank.Musique(SonsDuJeu.MusiqueNuit);
                    break;
                case Phase.Aube:
                    AudioBank.Jouer2D(SonsDuJeu.Aube, 0.9f);
                    AudioBank.Jouer(SonsDuJeu.ChargePortail, pp, 0.8f);
                    AudioBank.Musique(SonsDuJeu.MusiqueJour);
                    break;
            }
        }

        void Update()
        {
            var p = P;
            if (p == null || cycle == null) return;
            var e = p.Etat;
            var b = GameBalance.Courant;
            // Durées du cycle d'ambiance = durées de la partie (accélérées en mode test).
            cycle.dureeJour = b.Duree(Phase.Jour);
            cycle.dureeCrepuscule = b.Duree(Phase.Crepuscule);
            cycle.dureeNuit = b.Duree(Phase.Nuit);
            cycle.dureeAube = b.Duree(Phase.Aube);
            switch (e.phase)
            {
                // Menu principal : plan de nuit figé (milieu de la nuit), portail ouvert pour ce seul plan (en partie, il
                // est absent la nuit). Rien d'autre ne tourne avant LancerSolo (ni vagues ni horloge).
                case Phase.Attente: cycle.Piloter(CycleJourNuit.Phase.Nuit, cycle.dureeNuit * 0.5f); break;
                case Phase.Jour:
                    // Jour écourté (tous prêts) : le soleil suit le temps restant, pas le temps écoulé.
                    cycle.Piloter(CycleJourNuit.Phase.Jour, Mathf.Clamp(cycle.dureeJour - e.TempsRestant, 0f, cycle.dureeJour));
                    break;
                case Phase.Crepuscule: cycle.Piloter(CycleJourNuit.Phase.Crepuscule, e.tempsPhase); break;
                case Phase.Nuit: cycle.Piloter(CycleJourNuit.Phase.Nuit, e.tempsPhase); break;
                case Phase.Aube: cycle.Piloter(CycleJourNuit.Phase.Aube, e.tempsPhase); break;
                case Phase.Terminee: break;   // ambiance figée
            }
            cycle.portailForceOuvert = e.phase == Phase.Attente;
            bool ouvert = portail != null && portail.ouvert;
            if (ouvert != m_PortailOuvert)
            {
                m_PortailOuvert = ouvert;
                if (m_Bourdon != null) { if (ouvert) m_Bourdon.Play(); else m_Bourdon.Stop(); }
                if (ouvert && portail != null) AudioBank.Jouer(SonsDuJeu.PortailOuverture, portail.transform.position, 1f);
            }
        }
    }
}
