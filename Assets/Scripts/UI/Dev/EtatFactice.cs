using System;
using System.Collections.Generic;
using Deathless.Controls;
using Deathless.UI.Donnees;
using UnityEngine;
using UnityEngine.InputSystem;
using Random = UnityEngine.Random;

namespace Deathless.UI.Dev
{
    /// Source de données factice pour la démo des écrans 0.1 (défense solo, Paladin, pas de donjon).
    /// Implémente toutes les interfaces de DonneesUI et simule une partie accélérée : cycle jour, crépuscule, nuit,
    /// aube ; coups sur Nyxessa et son bouclier ; ennemis tués et or ; compétences en recharge ; mort et
    /// réapparition ; fin de partie. Les entrées de jeu (carte Gameplay, via InputChordResolver) déclenchent
    /// les compétences et le vote prêt, comme le ferait le vrai jeu.
    /// Les méthodes Forcer… servent aux tests et aux captures.
    public class EtatFactice : MonoBehaviour, IEtatPartie, IEtatJoueur, IScoreFin, ICommandesPartie
    {
        public InputActionAsset actions;

        [Header("Cycle (secondes de jeu)")]
        [Tooltip("Vitesse de la simulation : 4 = un jour de 120 s passe en 30 s.")]
        public float acceleration = 4f;
        public float dureeJour = 120f;
        public float dureeTransition = 5f;
        public float dureeNuit = 120f;

        [Header("Scénario")]
        [Tooltip("Nyxessa tombe pendant cette nuit (0 = jamais : victoire à l'aube de la nuit 12).")]
        public int nuitDefaite = 3;
        [Tooltip("Le joueur meurt une fois pendant cette nuit.")]
        public int nuitMort = 2;
        [Tooltip("Délai de réapparition, en secondes réelles (à décider dans le wiki).")]
        public float delaiReapparition = 10f;
        [Tooltip("Gèle la simulation (captures).")]
        public bool figer;

        // --- Partie
        bool m_EnCours;
        PhasePartie m_Phase = PhasePartie.Jour;
        int m_Nuit = 1;
        float m_Reste;
        float m_VieNyx, m_Bouclier, m_BouclierMax;
        int m_Or;
        bool m_Pret;
        float m_Angle;
        float m_Duree;
        float m_ProchainCoup, m_ProchainKill, m_ProchainDegat;
        bool m_MortCetteNuit;

        // --- Joueur
        float m_Vie = 100f, m_Endurance = 100f;
        float m_Reapparition;
        readonly List<CompetenceFactice> m_Competences = new List<CompetenceFactice>();
        CompetenceFactice m_Attaque, m_Garde, m_Charge, m_Soin;
        float m_ProchaineAuto;

        // --- Score
        ResultatPartie m_Resultat;
        readonly LigneFactice m_Ligne = new LigneFactice();
        readonly List<ILigneScore> m_Lignes = new List<ILigneScore>();
        bool m_PretFin;
        float m_RelanceDans = -1f;

        InputChordResolver m_Accords;

        public const float VieMaxNyx = 1000f;

        void Awake()
        {
            m_Attaque = new CompetenceFactice("Gameplay/AttackPrimary", "Frappe à l’épée", "Ép", 0f);
            m_Garde = new CompetenceFactice("Gameplay/AttackSecondary", "Garde et parade", "Ga", 0f);
            m_Charge = new CompetenceFactice("Gameplay/Skill1", "Charge bélier", "Ch", 8f);
            m_Soin = new CompetenceFactice("Gameplay/Skill2", "Soin sur soi", "So", 22f);
            m_Competences.AddRange(new[] { m_Attaque, m_Garde, m_Charge, m_Soin });
            m_Lignes.Add(m_Ligne);
        }

        void OnEnable()
        {
            if (actions == null && InputGlyphs.Default != null) actions = InputGlyphs.Default.actions;
            if (actions != null)
            {
                m_Accords = InputChordResolver.ForGameplay(actions);
                m_Accords.Triggered += OnAction;
            }
            DonneesUI.Enregistrer(null, null, null, this);
        }

        void OnDisable()
        {
            m_Accords?.Dispose();
            m_Accords = null;
            DonneesUI.Retirer();
        }

        // ================================================================== Simulation

        void Update()
        {
            var dtReel = Time.unscaledDeltaTime;
            if (m_RelanceDans >= 0f)
            {
                m_RelanceDans -= dtReel;
                if (m_RelanceDans < 0f) Demarrer();
            }
            if (!m_EnCours || figer || m_Phase == PhasePartie.Terminee) return;

            var dt = dtReel * acceleration;
            m_Duree += dt;
            m_Reste -= dt;
            if (m_Phase == PhasePartie.Jour && m_Pret && m_Reste > 5f) m_Reste = 5f;   // tous prêts : compte à rebours de 5 s
            if (m_Reste <= 0f) PhaseSuivante();
            if (m_Phase == PhasePartie.Terminee) return;

            foreach (var c in m_Competences) c.Avancer(dt);
            m_Garde.etat = m_Garde.finActive > m_Duree || (m_Accords != null && actions != null && m_Accords.IsHeld(actions.FindAction("Gameplay/AttackSecondary")))
                ? EtatCompetence.Active : EtatCompetence.Prete;
            m_Attaque.etat = m_Attaque.finActive > m_Duree ? EtatCompetence.Active : EtatCompetence.Prete;
            m_Endurance = Mathf.Min(100f, m_Endurance + 6f * dt);
            m_Angle = Mathf.Repeat(m_Angle + Random.Range(-40f, 40f) * dtReel + 180f, 360f) - 180f;

            if (m_Reapparition > 0f)
            {
                m_Reapparition -= dtReel;
                if (m_Reapparition <= 0f)
                {
                    m_Reapparition = 0f;
                    m_Vie = 100f;
                }
            }

            if (m_Phase == PhasePartie.Nuit) SimulerNuit(dt);
        }

        void SimulerNuit(float dt)
        {
            var avance = 1f - m_Reste / dureeNuit;
            var fatale = m_Nuit == nuitDefaite;

            // Coups sur Nyxessa (bouclier d'abord).
            m_ProchainCoup -= dt;
            if (m_ProchainCoup <= 0f)
            {
                m_ProchainCoup = Random.Range(2f, 5f);
                var degats = Random.Range(15f, 35f) * (fatale ? 5f : 1f);
                var absorbe = Mathf.Min(m_Bouclier, degats);
                m_Bouclier -= absorbe;
                m_VieNyx = Mathf.Max(0f, m_VieNyx - (degats - absorbe));
                NyxessaFrappee?.Invoke();
                if (m_VieNyx <= 0f)
                {
                    Terminer(ResultatPartie.Defaite);
                    return;
                }
            }

            if (EstMort) return;

            // Ennemis tués, or, dégâts infligés.
            m_ProchainKill -= dt;
            if (m_ProchainKill <= 0f)
            {
                m_ProchainKill = Random.Range(4f, 9f);
                m_Ligne.tues++;
                var or = Random.Range(12, 26);
                m_Ligne.or += or;
                m_Or += or;
                m_Ligne.degats += Random.Range(80, 160);
            }

            // Dégâts subis ; mort scénarisée.
            m_ProchainDegat -= dt;
            if (m_ProchainDegat <= 0f)
            {
                m_ProchainDegat = Random.Range(5f, 10f);
                m_Vie = Mathf.Max(5f, m_Vie - Random.Range(6f, 14f));
            }
            if (m_Nuit == nuitMort && !m_MortCetteNuit && avance > 0.4f) Mourir(delaiReapparition);

            // Le Paladin se bat tout seul dans la démo.
            m_ProchaineAuto -= dt;
            if (m_ProchaineAuto <= 0f)
            {
                m_ProchaineAuto = Random.Range(3f, 7f);
                if (m_Charge.EstPrete) Utiliser(m_Charge, 25f);
                else if (m_Vie < 70f && m_Soin.EstPrete) Utiliser(m_Soin, 15f);
                else m_Garde.finActive = m_Duree + 4f;
            }
        }

        void PhaseSuivante()
        {
            switch (m_Phase)
            {
                case PhasePartie.Jour:
                    DefinirPhase(PhasePartie.Crepuscule, dureeTransition);
                    break;
                case PhasePartie.Crepuscule:
                    DefinirPhase(PhasePartie.Nuit, dureeNuit);
                    m_MortCetteNuit = false;
                    m_ProchainCoup = 1.5f;
                    NuitCommencee?.Invoke(m_Nuit);
                    break;
                case PhasePartie.Nuit:
                    DefinirPhase(PhasePartie.Aube, dureeTransition);
                    break;
                case PhasePartie.Aube:
                    if (m_Nuit >= 12)
                    {
                        Terminer(ResultatPartie.Victoire);
                        return;
                    }
                    m_Nuit++;
                    m_Pret = false;
                    // Le villageois sorcier invoque le bouclier à partir du jour 2, plein à chaque aube.
                    m_BouclierMax = m_Nuit >= 2 ? 400f : 0f;
                    m_Bouclier = m_BouclierMax;
                    DefinirPhase(PhasePartie.Jour, dureeJour);
                    break;
            }
        }

        void DefinirPhase(PhasePartie phase, float duree)
        {
            m_Phase = phase;
            m_Reste = duree;
        }

        void Utiliser(CompetenceFactice c, float endurance)
        {
            if (!c.EstPrete || EstMort || m_Endurance < endurance) return;
            m_Endurance -= endurance;
            c.Lancer();
            if (c == m_Soin) m_Vie = Mathf.Min(100f, m_Vie + 35f);
        }

        void Mourir(float secondes)
        {
            m_MortCetteNuit = true;
            m_Vie = 0f;
            m_Reapparition = secondes;
            m_Ligne.morts++;
        }

        void Terminer(ResultatPartie resultat)
        {
            m_Resultat = resultat;
            m_Phase = PhasePartie.Terminee;
            m_PretFin = false;
            PartieTerminee?.Invoke();
        }

        /// Actions de jeu résolues (accords compris) : ce que le vrai jeu ferait.
        void OnAction(InputAction action)
        {
            if (!m_EnCours || m_Phase == PhasePartie.Terminee) return;
            switch (action.name)
            {
                case "Skill1": Utiliser(m_Charge, 25f); break;
                case "Skill2": Utiliser(m_Soin, 15f); break;
                case "AttackPrimary": m_Attaque.finActive = m_Duree + 0.3f; break;
                case "Ready": BasculerPret(); break;
            }
        }

        // ================================================================== Commandes (UI → jeu)

        public void LancerSolo() => Demarrer();

        public void BasculerPret()
        {
            if (!m_EnCours) return;
            if (m_Phase == PhasePartie.Terminee)
            {
                // Rejouer : en solo, tout le monde est prêt, la partie repart.
                m_PretFin = !m_PretFin;
                m_RelanceDans = m_PretFin ? 1.5f : -1f;
                return;
            }
            if (m_Phase == PhasePartie.Jour) m_Pret = !m_Pret;
        }

        public void QuitterPartie()
        {
            m_EnCours = false;
            m_RelanceDans = -1f;
            DonneesUI.Enregistrer(null, null, null, this);
        }

        public void QuitterJeu()
        {
            Debug.Log("[EtatFactice] Quitter le jeu demandé.");
#if !UNITY_EDITOR
            Application.Quit();
#endif
        }

        /// Nouvelle partie : jour 1, Nyxessa pleine, pas de bouclier.
        public void Demarrer()
        {
            m_EnCours = true;
            m_RelanceDans = -1f;
            m_Nuit = 1;
            m_Or = 0;
            m_Pret = false;
            m_Duree = 0f;
            m_VieNyx = VieMaxNyx;
            m_BouclierMax = 0f;
            m_Bouclier = 0f;
            m_Vie = 100f;
            m_Endurance = 100f;
            m_Reapparition = 0f;
            m_Ligne.Reinitialiser();
            foreach (var c in m_Competences) c.Reinitialiser();
            DefinirPhase(PhasePartie.Jour, dureeJour);
            DonneesUI.Enregistrer(this, this, this, this);
        }

        // ================================================================== Tests et captures

        /// Place la partie dans une phase donnée (déclenche la bannière si c'est une nuit).
        public void Forcer(PhasePartie phase, int nuit, float tempsRestant)
        {
            if (!m_EnCours) Demarrer();
            m_Nuit = Mathf.Max(1, nuit);
            m_BouclierMax = m_Nuit >= 2 ? 400f : 0f;
            m_Bouclier = m_BouclierMax;
            DefinirPhase(phase, tempsRestant);
            if (phase == PhasePartie.Nuit) NuitCommencee?.Invoke(m_Nuit);
        }

        public void ForcerNyxessa(float vie01, float bouclier01)
        {
            m_VieNyx = VieMaxNyx * Mathf.Clamp01(vie01);
            if (m_BouclierMax <= 0f && bouclier01 > 0f) m_BouclierMax = 400f;
            m_Bouclier = m_BouclierMax * Mathf.Clamp01(bouclier01);
        }

        public void Frapper(float angle)
        {
            m_Angle = angle;
            NyxessaFrappee?.Invoke();
        }

        public void ForcerJoueur(float vie, float endurance, float rechargeCharge, float rechargeSoin, bool garde)
        {
            if (vie > 0f) m_Reapparition = 0f;
            m_Vie = vie;
            m_Endurance = endurance;
            m_Charge.Forcer(rechargeCharge);
            m_Soin.Forcer(rechargeSoin);
            m_Garde.finActive = garde ? m_Duree + 999f : 0f;
            m_Garde.etat = garde ? EtatCompetence.Active : EtatCompetence.Prete;
        }

        public void ForcerMort(float secondes)
        {
            if (!m_EnCours) Demarrer();
            Mourir(secondes);
        }

        public void ForcerScore(int or, int degats, int tues, int morts)
        {
            m_Ligne.or = or;
            m_Or = or;
            m_Ligne.degats = degats;
            m_Ligne.tues = tues;
            m_Ligne.morts = morts;
        }

        public void ForcerFin(ResultatPartie resultat, int nuit, float dureeSecondes)
        {
            if (!m_EnCours) Demarrer();
            m_Nuit = nuit;
            m_Duree = dureeSecondes;
            Terminer(resultat);
        }

        public void ForcerPret(bool pret) => m_Pret = pret;

        // ================================================================== IEtatPartie

        public PhasePartie Phase => m_Phase;
        public int NumeroNuit => m_Nuit;
        public float TempsRestantPhase => Mathf.Max(0f, m_Reste);
        public float VieNyxessa => m_VieNyx;
        public float VieMaxNyxessa => VieMaxNyx;
        public float Bouclier => m_Bouclier;
        public float BouclierMax => m_BouclierMax;
        public int OrEquipe => m_Or;
        public bool VoteActif => m_Phase == PhasePartie.Jour;
        public int JoueursPrets => m_Pret ? 1 : 0;
        public int JoueursTotal => 1;
        public float AngleNyxessa => m_Angle;
        public event Action<int> NuitCommencee;
        public event Action NyxessaFrappee;
        public event Action PartieTerminee;

        // ================================================================== IEtatJoueur

        public string Nom => "Quentin";
        public string Classe => "Paladin";
        public Color TeinteClasse => new Color32(0xd9, 0xb2, 0x64, 0xff);
        public float Vie => m_Vie;
        public float VieMax => 100f;
        public float Endurance => m_Endurance;
        public float EnduranceMax => 100f;
        public bool EstMort => m_Reapparition > 0f;
        public float TempsAvantReapparition => m_Reapparition;
        public IReadOnlyList<ICompetenceHud> Competences => m_Competences;
        public string InviteInteraction => m_EnCours && m_Phase == PhasePartie.Jour && m_Reste > dureeJour * 0.35f ? "Parler au sorcier" : null;
        public bool EstPret => m_Pret;

        // ================================================================== IScoreFin

        public ResultatPartie Resultat => m_Resultat;
        public int NuitAtteinte => m_Resultat == ResultatPartie.Victoire ? 12 : m_Nuit;
        public float DureeSecondes => m_Duree;
        public int OrTotal => m_Or;
        public IReadOnlyList<ILigneScore> Joueurs => m_Lignes;
        int IScoreFin.JoueursPrets => m_PretFin ? 1 : 0;
        int IScoreFin.JoueursTotal => 1;
        public bool EstPretLocal => m_PretFin;

        // ================================================================== Types internes

        sealed class CompetenceFactice : ICompetenceHud
        {
            public EtatCompetence etat = EtatCompetence.Prete;
            public float finActive;
            readonly float m_Recharge;
            float m_Restante;

            public CompetenceFactice(string action, string nom, string abreviation, float recharge)
            {
                Action = action;
                Nom = nom;
                Abreviation = abreviation;
                m_Recharge = recharge;
            }

            public string Action { get; }
            public string Nom { get; }
            public Texture2D Icone => null;
            public string Abreviation { get; }
            public EtatCompetence Etat => m_Restante > 0f ? EtatCompetence.Recharge : etat;
            public float RechargeRestante => m_Restante;
            public float RechargeTotale => m_Recharge;
            public bool EstPrete => m_Restante <= 0f;

            public void Lancer() => m_Restante = m_Recharge;
            public void Forcer(float restante) => m_Restante = Mathf.Clamp(restante, 0f, m_Recharge);
            public void Avancer(float dt) => m_Restante = Mathf.Max(0f, m_Restante - dt);

            public void Reinitialiser()
            {
                m_Restante = 0f;
                finActive = 0f;
                etat = EtatCompetence.Prete;
            }
        }

        sealed class LigneFactice : ILigneScore
        {
            public int or, degats, tues, morts;
            public string Nom => "Quentin";
            public string Classe => "Paladin";
            public Color TeinteClasse => new Color32(0xd9, 0xb2, 0x64, 0xff);
            public bool EstLocal => true;
            public int OrRapporte => or;
            public int DegatsInfliges => degats;
            public int EnnemisTues => tues;
            public int Morts => morts;
            public void Reinitialiser() => or = degats = tues = morts = 0;
        }
    }
}
